using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MonocleViewExtension.LocalGroupNaming
{
    /// <summary>One answer from the local model.</summary>
    internal sealed class LocalModelCompletion
    {
        public LocalModelCompletion(string content, string finishReason)
        {
            Content = content ?? string.Empty;
            FinishReason = finishReason ?? string.Empty;
        }

        public string Content { get; }

        public string FinishReason { get; }

        /// <summary>True when generation stopped at the token limit, so the text may end mid-word.</summary>
        public bool IsTruncated => string.Equals(FinishReason, "length", StringComparison.OrdinalIgnoreCase);
    }

    internal sealed class LocalLlamaServerClient : IDisposable
    {
        /// <summary>
        /// Tokens the server may generate for one title. The grammar's longest output (seven
        /// 20-character words) is about 40 tokens; anything cut off here is rejected and retried.
        /// </summary>
        public const int MaximumCompletionTokens = 48;

        /// <summary>
        /// Prompt context in tokens. A fixed size matters: "-c 0" sizes the cache for the model's
        /// full training context (40,960 tokens for Qwen3-4B, far more for newer models), which
        /// would reserve gigabytes of RAM for a prompt that never exceeds a few hundred tokens.
        /// </summary>
        public const int ContextSize = 4096;

        // Qwen's published non-thinking settings are temperature 0.7, top_p 0.8, top_k 20 and
        // min_p 0 (llama.cpp's own default min_p is non-zero, so it is sent explicitly). A
        // lower temperature keeps titles steadier; the retry raises it to get a different proposal.
        public const double DefaultTemperature = 0.3;
        public const double RetryTemperature = 0.8;
        public const double TopP = 0.8;
        public const int TopK = 20;
        public const double MinP = 0.0;
        private const string ChatCompletionsPath = "v1/chat/completions";
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(120);
        private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(180);
        private static readonly TimeSpan WarmUpTimeout = TimeSpan.FromSeconds(60);

        private readonly LocalLlamaServerOptions options;
        private readonly SemaphoreSlim startupLock = new SemaphoreSlim(1, 1);
        // Guards serverProcess/httpClient/sessionCancellation: Disable() runs on the
        // UI thread while requests run on threadpool continuations.
        private readonly object stateLock = new object();
        private HttpClient httpClient;
        private Process serverProcess;
        private CancellationTokenSource sessionCancellation;
        private long sessionGeneration;
        private string lastServerError;
        private bool stopRequested;
        private bool disposed;

        public bool IsEnabled { get; private set; }

        /// <summary>
        /// Raised on a worker thread when a request finds the server process dead while the
        /// feature was enabled. The client has already disabled itself; the message explains
        /// what happened and how to recover.
        /// </summary>
        public event EventHandler<string> ServerFaulted;

        public LocalLlamaServerClient(LocalLlamaServerOptions options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        internal bool TryCaptureSessionGeneration(out long generation)
        {
            lock (stateLock)
            {
                if (!IsEnabled || sessionCancellation == null || sessionCancellation.IsCancellationRequested)
                {
                    generation = 0;
                    return false;
                }

                generation = sessionGeneration;
                return true;
            }
        }

        internal bool IsSessionCurrent(long generation)
        {
            lock (stateLock)
            {
                return generation != 0 &&
                       IsEnabled &&
                       sessionGeneration == generation &&
                       sessionCancellation != null &&
                       !sessionCancellation.IsCancellationRequested;
            }
        }

        /// <summary>
        /// Asks the model for one title. <paramref name="isRetry"/> raises the sampling
        /// temperature so a second attempt explores a different proposal instead of
        /// repeating the rejected one.
        /// </summary>
        public async Task<LocalModelCompletion> SuggestNameAsync(string prompt, bool isRetry, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(prompt)) throw new ArgumentException("A prompt is required.", nameof(prompt));
            ThrowIfDisposed();
            ThrowIfDisabled();

            if (!IsServerRunning())
            {
                // The process died (Task Manager, antivirus, a GGML abort). Restarting it here
                // would keep the user waiting for minutes behind a "Naming group..." indicator
                // and then discard the answer; stop cleanly and tell them instead.
                FaultServer("The local model server stopped unexpectedly. Turn local group naming off and on again to restart it.");
                throw new OperationCanceledException("Local group naming was turned off.");
            }

            HttpClient client;
            CancellationTokenSource session;
            lock (stateLock)
            {
                client = httpClient;
                session = sessionCancellation;
            }

            if (client == null || session == null)
            {
                throw new OperationCanceledException("Local group naming was turned off.");
            }

            var sessionToken = session.Token;
            var request = BuildCompletionRequest(Path.GetFileNameWithoutExtension(options.ModelPath), prompt, isRetry);

            try
            {
                using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, sessionToken))
                using (var content = new StringContent(request.ToString(Formatting.None), Encoding.UTF8, "application/json"))
                using (var response = await client.PostAsync(ChatCompletionsPath, content, linked.Token).ConfigureAwait(false))
                {
                    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException(
                            $"The local model server returned {(int)response.StatusCode} ({response.ReasonPhrase}): {body}");
                    }

                    var result = JObject.Parse(body);
                    var choice = result.SelectToken("choices[0]");
                    var suggestion = choice?.SelectToken("message.content")?.Value<string>();
                    var finishReason = choice?.SelectToken("finish_reason")?.Value<string>();
                    if (string.IsNullOrWhiteSpace(suggestion) && !string.Equals(finishReason, "length", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("The local model server response did not contain a suggestion.");
                    }

                    return new LocalModelCompletion(suggestion, finishReason);
                }
            }
            catch (ObjectDisposedException)
            {
                // Disable() disposed the HttpClient while the request was in flight.
                throw new OperationCanceledException("Local group naming was turned off.");
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested && !sessionToken.IsCancellationRequested)
            {
                throw new TimeoutException("The local model server did not respond in time.");
            }
        }

        /// <summary>
        /// The llama-server chat completion request: the fixed system prompt, the few-shot
        /// examples as prior user/assistant turns (so the model sees bare titles as answers and
        /// has no label to echo), then the group. Besides the OpenAI-compatible fields it uses
        /// two llama.cpp extensions: <c>grammar</c> constrains decoding to a bare 3-7 word title,
        /// and <c>chat_template_kwargs.enable_thinking=false</c> makes hybrid reasoning models
        /// answer directly instead of emitting a reasoning block.
        /// </summary>
        internal static JObject BuildCompletionRequest(string modelName, string prompt, bool isRetry)
        {
            var messages = new JArray
            {
                new JObject
                {
                    ["role"] = "system",
                    ["content"] = GroupNamingPromptBuilder.SystemPrompt
                }
            };

            foreach (var example in GroupNamingPromptBuilder.ExampleTurns)
            {
                messages.Add(new JObject { ["role"] = "user", ["content"] = example.Prompt });
                messages.Add(new JObject { ["role"] = "assistant", ["content"] = example.Title });
            }

            messages.Add(new JObject { ["role"] = "user", ["content"] = prompt });

            return new JObject
            {
                ["model"] = modelName,
                ["messages"] = messages,
                ["temperature"] = isRetry ? RetryTemperature : DefaultTemperature,
                ["top_p"] = TopP,
                ["top_k"] = TopK,
                ["min_p"] = MinP,
                ["max_tokens"] = MaximumCompletionTokens,
                ["stream"] = false,
                ["cache_prompt"] = true,
                ["grammar"] = GroupNameGrammar.Gbnf,
                ["chat_template_kwargs"] = new JObject
                {
                    ["enable_thinking"] = false
                }
            };
        }

        /// <summary>
        /// Command line for llama-server. One slot with a fixed context, the jinja chat
        /// template with reasoning switched off, no web UI or slot metrics endpoints, and a
        /// per-session API key so no other local process or web page can drive the server.
        /// The thread count is left to llama.cpp's physical-core default so small laptops
        /// and large workstations both get a sensible value.
        /// </summary>
        internal static string BuildServerArguments(LocalLlamaServerOptions options, int port, string apiKey)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            var arguments = new StringBuilder();
            arguments.Append("-m ").Append(Quote(options.ModelPath));
            arguments.Append(" --host 127.0.0.1 --port ").Append(port.ToString(CultureInfo.InvariantCulture));
            arguments.Append(" -c ").Append(ContextSize.ToString(CultureInfo.InvariantCulture)).Append(" -np 1");
            arguments.Append(" --jinja --reasoning off");
            arguments.Append(" --no-ui --no-slots");
            if (!string.IsNullOrEmpty(apiKey))
            {
                arguments.Append(" --api-key ").Append(apiKey);
            }

            if (!string.IsNullOrWhiteSpace(options.AdditionalServerArguments))
            {
                arguments.Append(' ').Append(options.AdditionalServerArguments.Trim());
            }

            return arguments.ToString();
        }

        public async Task EnableAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            lock (stateLock)
            {
                stopRequested = false;
            }

            await EnsureStartedAsync(cancellationToken).ConfigureAwait(false);
            lock (stateLock)
            {
                ThrowIfStopRequested();
                IsEnabled = true;
            }
        }

        public void Disable()
        {
            CancellationTokenSource cancellation;
            lock (stateLock)
            {
                IsEnabled = false;
                stopRequested = true;
                cancellation = sessionCancellation;
            }

            // Cancelled but left in place (and not disposed): a racing request that
            // already snapshotted it must observe the cancellation, not a stale token.
            // The next startup replaces it; an undisposed CTS holds no unmanaged resources.
            cancellation?.Cancel();

            StopServer();
        }

        private void FaultServer(string message)
        {
            CancellationTokenSource cancellation;
            lock (stateLock)
            {
                if (!IsEnabled) return;

                IsEnabled = false;
                stopRequested = true;
                cancellation = sessionCancellation;
            }

            cancellation?.Cancel();
            StopServer();
            ServerFaulted?.Invoke(this, message);
        }

        private async Task EnsureStartedAsync(CancellationToken cancellationToken)
        {
            if (IsServerRunning()) return;

            await startupLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ThrowIfDisposed();
                ThrowIfStopRequested();
                if (IsServerRunning()) return;

                options.Validate();
                var port = FindAvailablePort();
                var apiKey = Guid.NewGuid().ToString("N");
                lastServerError = null;

                // Published before the process starts so a concurrent Disable() can
                // always cancel the startup instead of missing the not-yet-set fields;
                // the re-check under the lock makes the handoff atomic with Disable().
                var cancellation = new CancellationTokenSource();
                lock (stateLock)
                {
                    ThrowIfStopRequested();
                    sessionGeneration++;
                    sessionCancellation = cancellation;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = options.ServerPath,
                    Arguments = BuildServerArguments(options, port, apiKey),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = Path.GetDirectoryName(options.ServerPath)
                };

                var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
                process.ErrorDataReceived += OnServerErrorDataReceived;
                if (!process.Start())
                {
                    process.Dispose();
                    throw new InvalidOperationException("The local model server could not be started.");
                }

                KillOnCloseJobObject.TryAssign(process);
                process.BeginErrorReadLine();
                var client = new HttpClient
                {
                    BaseAddress = new Uri($"http://127.0.0.1:{port}/"),
                    Timeout = RequestTimeout
                };
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                lock (stateLock)
                {
                    serverProcess = process;
                    httpClient = client;
                }

                using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellation.Token))
                {
                    await WaitUntilHealthyAsync(process, client, linked.Token).ConfigureAwait(false);
                    await WarmUpAsync(client, linked.Token).ConfigureAwait(false);
                }

                // Disable() may have raced the startup; tear the server back down.
                ThrowIfStopRequested();
            }
            catch
            {
                StopServer();
                throw;
            }
            finally
            {
                startupLock.Release();
            }
        }

        private bool IsServerRunning()
        {
            lock (stateLock)
            {
                return serverProcess != null && !HasExitedSafe(serverProcess) && httpClient != null;
            }
        }

        private async Task WaitUntilHealthyAsync(Process process, HttpClient client, CancellationToken cancellationToken)
        {
            var timeoutAt = DateTime.UtcNow + StartupTimeout;
            while (DateTime.UtcNow < timeoutAt)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (HasExitedSafe(process))
                {
                    throw new InvalidOperationException(
                        $"The local model server exited during startup. {lastServerError}".Trim());
                }

                try
                {
                    using (var response = await client.GetAsync("health", cancellationToken).ConfigureAwait(false))
                    {
                        if (response.StatusCode == HttpStatusCode.OK) return;
                    }
                }
                catch (HttpRequestException)
                {
                    // The server socket is not ready yet.
                }
                catch (ObjectDisposedException)
                {
                    throw new OperationCanceledException("Local group naming was turned off.");
                }
                catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    // An individual health request timed out while the model was loading.
                }

                await Task.Delay(250, cancellationToken).ConfigureAwait(false);
            }

            throw new TimeoutException(
                $"The local model did not finish loading within {StartupTimeout.TotalSeconds:0} seconds. {lastServerError}".Trim());
        }

        /// <summary>
        /// Runs one throw-away request so the system prompt and examples are tokenised and
        /// cached by the server before the user creates a group; the first real request then
        /// only has to process the group listing. Failures are ignored (the real request simply
        /// pays the prompt processing itself), except that a cancellation or disable while
        /// warming up is reported as such.
        /// </summary>
        private static async Task WarmUpAsync(HttpClient client, CancellationToken cancellationToken)
        {
            try
            {
                var request = BuildCompletionRequest("warm-up", "Contents:\n- Warm up\n\nTitle:", false);
                request["max_tokens"] = 1;
                request["temperature"] = 0;
                request.Remove("grammar");

                using (var timeout = new CancellationTokenSource(WarmUpTimeout))
                using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token))
                using (var content = new StringContent(request.ToString(Formatting.None), Encoding.UTF8, "application/json"))
                using (await client.PostAsync(ChatCompletionsPath, content, linked.Token).ConfigureAwait(false))
                {
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                // Disable() disposed the client under us; report it as the cancellation it is.
                throw new OperationCanceledException("Local group naming was turned off.");
            }
            catch (Exception)
            {
                // Best effort only.
            }
        }

        private void OnServerErrorDataReceived(object sender, DataReceivedEventArgs args)
        {
            if (!string.IsNullOrWhiteSpace(args.Data)) lastServerError = args.Data;
        }

        private static int FindAvailablePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private static bool HasExitedSafe(Process process)
        {
            try
            {
                return process.HasExited;
            }
            catch (InvalidOperationException)
            {
                // The process was disposed by a concurrent StopServer.
                return true;
            }
        }

        private void StopServer()
        {
            HttpClient clientToDispose;
            Process processToStop;
            lock (stateLock)
            {
                clientToDispose = httpClient;
                httpClient = null;
                processToStop = serverProcess;
                serverProcess = null;
            }

            clientToDispose?.Dispose();
            if (processToStop == null) return;

            processToStop.ErrorDataReceived -= OnServerErrorDataReceived;
            try
            {
                if (!processToStop.HasExited) processToStop.Kill();
            }
            catch (InvalidOperationException)
            {
                // The process exited or was disposed between the check and the kill.
            }
            catch (Win32Exception)
            {
                // The process is already terminating.
            }

            processToStop.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(LocalLlamaServerClient));
        }

        private void ThrowIfDisabled()
        {
            if (!IsEnabled) throw new OperationCanceledException("Local group naming is turned off.");
        }

        private void ThrowIfStopRequested()
        {
            if (stopRequested) throw new OperationCanceledException("Local group naming is turned off.");
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Disable();
            // The SemaphoreSlim is deliberately not disposed: an in-flight startup may
            // still release it, and an undisposed SemaphoreSlim holds no unmanaged resources.
        }
    }
}
