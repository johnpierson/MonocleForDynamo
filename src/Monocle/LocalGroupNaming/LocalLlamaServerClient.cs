using System;
using System.ComponentModel;
using System.Diagnostics;
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
        private readonly LocalLlamaServerOptions options;
        private readonly SemaphoreSlim startupLock = new SemaphoreSlim(1, 1);
        // Guards serverProcess/httpClient/sessionCancellation: Disable() runs on the
        // UI thread while requests run on threadpool continuations.
        private readonly object stateLock = new object();
        private HttpClient httpClient;
        private Process serverProcess;
        private CancellationTokenSource sessionCancellation;
        private string lastServerError;
        private bool stopRequested;
        private bool disposed;

        public bool IsEnabled { get; private set; }

        public LocalLlamaServerClient(LocalLlamaServerOptions options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<string> SuggestNameAsync(string prompt, CancellationToken cancellationToken)
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

            var request = new JObject
            {
                ["model"] = Path.GetFileNameWithoutExtension(options.ModelPath),
                ["messages"] = new JArray
                {
                    new JObject
                    {
                        ["role"] = "system",
                        ["content"] = "You name visual programming workflows. First infer the transformation represented by all supplied node names, then return only a natural 3 to 7 word title. Do not mention UI controls such as sliders unless they are the workflow purpose."
                    },
                    new JObject
                    {
                        ["role"] = "user",
                        ["content"] = prompt
                    }
                },
                ["temperature"] = 0.1,
                ["max_tokens"] = 48,
                ["stream"] = false
            };

            try
            {
                using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, sessionToken))
                using (var content = new StringContent(request.ToString(Formatting.None), Encoding.UTF8, "application/json"))
                using (var response = await client.PostAsync("v1/chat/completions", content, linked.Token).ConfigureAwait(false))
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

        public async Task EnableAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            stopRequested = false;
            await EnsureStartedAsync(cancellationToken).ConfigureAwait(false);
            IsEnabled = true;
        }

        public void Disable()
        {
            IsEnabled = false;
            stopRequested = true;

            CancellationTokenSource cancellation;
            lock (stateLock)
            {
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
                    sessionCancellation = cancellation;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = options.ServerPath,
                    Arguments = $"-m {Quote(options.ModelPath)} --host 127.0.0.1 --port {port} -c 2048 -t 4 -ngl 0",
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
                    Timeout = TimeSpan.FromSeconds(45)
                };
                lock (stateLock)
                {
                    serverProcess = process;
                    httpClient = client;
                }

                using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellation.Token))
                {
                    await WaitUntilHealthyAsync(process, client, linked.Token).ConfigureAwait(false);
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
            var timeoutAt = DateTime.UtcNow.AddSeconds(90);
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

            throw new TimeoutException($"The local model did not finish loading within 90 seconds. {lastServerError}".Trim());
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
