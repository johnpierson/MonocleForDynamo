using System;
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
        private HttpClient httpClient;
        private Process serverProcess;
        private string lastServerError;
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

            await EnsureStartedAsync(cancellationToken).ConfigureAwait(false);

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

            using (var content = new StringContent(request.ToString(Formatting.None), Encoding.UTF8, "application/json"))
            using (var response = await httpClient.PostAsync("v1/chat/completions", content, cancellationToken).ConfigureAwait(false))
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

        public async Task EnableAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            await EnsureStartedAsync(cancellationToken).ConfigureAwait(false);
            IsEnabled = true;
        }

        public void Disable()
        {
            IsEnabled = false;
            StopServer();
        }

        private async Task EnsureStartedAsync(CancellationToken cancellationToken)
        {
            if (serverProcess != null && !serverProcess.HasExited && httpClient != null) return;

            await startupLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (serverProcess != null && !serverProcess.HasExited && httpClient != null) return;

                options.Validate();
                var port = FindAvailablePort();
                lastServerError = null;

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

                serverProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
                serverProcess.ErrorDataReceived += OnServerErrorDataReceived;
                if (!serverProcess.Start())
                {
                    throw new InvalidOperationException("The local model server could not be started.");
                }

                serverProcess.BeginErrorReadLine();
                httpClient = new HttpClient
                {
                    BaseAddress = new Uri($"http://127.0.0.1:{port}/"),
                    Timeout = TimeSpan.FromSeconds(45)
                };

                await WaitUntilHealthyAsync(cancellationToken).ConfigureAwait(false);
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

        private async Task WaitUntilHealthyAsync(CancellationToken cancellationToken)
        {
            var timeoutAt = DateTime.UtcNow.AddSeconds(90);
            while (DateTime.UtcNow < timeoutAt)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (serverProcess == null || serverProcess.HasExited)
                {
                    throw new InvalidOperationException(
                        $"The local model server exited during startup. {lastServerError}".Trim());
                }

                try
                {
                    using (var response = await httpClient.GetAsync("health", cancellationToken).ConfigureAwait(false))
                    {
                        if (response.StatusCode == HttpStatusCode.OK) return;
                    }
                }
                catch (HttpRequestException)
                {
                    // The server socket is not ready yet.
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

        private void StopServer()
        {
            httpClient?.Dispose();
            httpClient = null;

            if (serverProcess == null) return;

            serverProcess.ErrorDataReceived -= OnServerErrorDataReceived;
            if (!serverProcess.HasExited)
            {
                serverProcess.Kill();
                serverProcess.WaitForExit(3000);
            }

            serverProcess.Dispose();
            serverProcess = null;
        }

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(LocalLlamaServerClient));
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Disable();
            startupLock.Dispose();
        }
    }
}
