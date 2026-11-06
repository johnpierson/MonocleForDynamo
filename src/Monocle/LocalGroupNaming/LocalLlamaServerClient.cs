using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MonocleViewExtension.LocalGroupNaming
{
    internal sealed class LocalLlamaServerClient : IDisposable
    {
        /// <summary>Tokens the server may generate for one title; seven 20-character words fit comfortably.</summary>
        public const int MaximumCompletionTokens = 32;

        /// <summary>
        /// Prompt context in tokens. A fixed size matters: "-c 0" sizes the cache for the model's
        /// full training context (hundreds of thousands of tokens for Qwen3.5), which would
        /// reserve gigabytes of RAM for a prompt that never exceeds a few hundred tokens.
        /// </summary>
        public const int ContextSize = 4096;

        public const double DefaultTemperature = 0.2;
        public const double RetryTemperature = 0.7;
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
        public async Task<string> SuggestNameAsync(string prompt, bool isRetry, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(prompt)) throw new ArgumentException("A prompt is required.", nameof(prompt));
            ThrowIfDisposed();
            ThrowIfDisabled();

            await EnsureStartedAsync(cancellationToken).ConfigureAwait(false);

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
                    var suggestion = result.SelectToken("choices[0].message.content")?.Value<string>();
                    if (string.IsNullOrWhiteSpace(suggestion))
                    {
                        throw new InvalidOperationException("The local model server response did not contain a suggestion.");
                    }

                    return suggestion;
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
        /// The llama-server chat completion request. Besides the OpenAI-compatible fields it
        /// uses two llama.cpp extensions: <c>grammar</c> constrains decoding to a bare 3-7 word
        /// title, and <c>chat_template_kwargs.enable_thinking=false</c> makes hybrid reasoning
        /// models (Qwen3, Qwen3.5) answer directly instead of emitting a reasoning block.
        /// </summary>
        internal static JObject BuildCompletionRequest(string modelName, string prompt, bool isRetry)
        {
            return new JObject
            {
                ["model"] = modelName,
                ["messages"] = new JArray
                {
                    new JObject
                    {
                        ["role"] = "system",
                        ["content"] = GroupNamingPromptBuilder.SystemPrompt
                    },
                    new JObject
                    {
                        ["role"] = "user",
                        ["content"] = prompt
                    }
                },
                ["temperature"] = isRetry ? RetryTemperature : DefaultTemperature,
                ["top_p"] = 0.9,
                ["top_k"] = 40,
                ["min_p"] = 0.05,
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
        /// template with reasoning switched off, and no web UI or slot metrics endpoints.
        /// The thread count is left to llama.cpp's physical-core default so small laptops
        /// and large workstations both get a sensible value.
        /// </summary>
        internal static string BuildServerArguments(LocalLlamaServerOptions options, int port)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            var arguments = new StringBuilder();
            arguments.Append("-m ").Append(Quote(options.ModelPath));
            arguments.Append(" --host 127.0.0.1 --port ").Append(port.ToString(CultureInfo.InvariantCulture));
            arguments.Append(" -c ").Append(ContextSize.ToString(CultureInfo.InvariantCulture)).Append(" -np 1");
            arguments.Append(" --jinja --reasoning off");
            arguments.Append(" --no-ui --no-slots");
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
                    Arguments = BuildServerArguments(options, port),
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
        /// Runs one throw-away request so the system prompt is tokenised and cached by the
        /// server before the user creates a group; the first real request then only has to
        /// process the group listing. Any failure here is ignored, the real request simply
        /// pays the prompt processing itself.
        /// </summary>
        private static async Task WarmUpAsync(HttpClient client, CancellationToken cancellationToken)
        {
            try
            {
                var request = new JObject
                {
                    ["messages"] = new JArray
                    {
                        new JObject { ["role"] = "system", ["content"] = GroupNamingPromptBuilder.SystemPrompt },
                        new JObject { ["role"] = "user", ["content"] = "Contents:\n- Warm up\n\nTitle:" }
                    },
                    ["max_tokens"] = 1,
                    ["temperature"] = 0,
                    ["stream"] = false,
                    ["cache_prompt"] = true,
                    ["chat_template_kwargs"] = new JObject { ["enable_thinking"] = false }
                };

                using (var timeout = new CancellationTokenSource(WarmUpTimeout))
                using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token))
                using (var content = new StringContent(request.ToString(Formatting.None), Encoding.UTF8, "application/json"))
                using (await client.PostAsync(ChatCompletionsPath, content, linked.Token).ConfigureAwait(false))
                {
                }
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
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
