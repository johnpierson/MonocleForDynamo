using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace MonocleViewExtension.LocalGroupNaming
{
    internal sealed class LocalModelDownloadProgress
    {
        public LocalModelDownloadProgress(string component, long bytesReceived, long? totalBytes)
        {
            Component = component;
            BytesReceived = bytesReceived;
            TotalBytes = totalBytes;
        }

        public string Component { get; }

        public long BytesReceived { get; }

        public long? TotalBytes { get; }
    }

    /// <summary>
    /// Streams a URL to disk and resumes an interrupted download with an HTTP Range
    /// request, so a multi-gigabyte model that fails at 90% on a flaky connection does
    /// not start over. Servers that ignore the range (plain 200) restart cleanly, and a
    /// partial file the server no longer recognises (416) is discarded and refetched.
    /// </summary>
    internal static class ResumableDownloader
    {
        public const int BufferSize = 1024 * 1024;

        public static async Task DownloadAsync(
            HttpClient httpClient,
            string url,
            string destinationPath,
            string component,
            long? expectedBytes,
            IProgress<LocalModelDownloadProgress> progress,
            CancellationToken cancellationToken)
        {
            if (httpClient == null) throw new ArgumentNullException(nameof(httpClient));
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("A download URL is required.", nameof(url));
            if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException("A destination path is required.", nameof(destinationPath));

            var existingBytes = GetResumableLength(destinationPath, expectedBytes);
            for (var attempt = 0; attempt < 2; attempt++)
            {
                if (existingBytes > 0)
                {
                    progress?.Report(new LocalModelDownloadProgress(component, existingBytes, expectedBytes));
                }

                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    if (existingBytes > 0) request.Headers.Range = new RangeHeaderValue(existingBytes, null);

                    using (var response = await httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken).ConfigureAwait(false))
                    {
                        if (existingBytes > 0 && response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
                        {
                            // The partial file does not match what the server has now; refetch everything.
                            DeleteIfPresent(destinationPath);
                            existingBytes = 0;
                            continue;
                        }

                        response.EnsureSuccessStatusCode();

                        var append = false;
                        if (existingBytes > 0)
                        {
                            var contentRange = response.Content.Headers.ContentRange;
                            if (response.StatusCode == HttpStatusCode.PartialContent)
                            {
                                if (contentRange == null || !contentRange.HasRange || contentRange.From != existingBytes)
                                {
                                    // A partial body from some other offset cannot be stitched onto our file.
                                    DeleteIfPresent(destinationPath);
                                    throw new InvalidDataException(
                                        $"The {component} download resumed at an unexpected position; it will restart from the beginning.");
                                }

                                append = true;
                            }
                            else
                            {
                                // The server ignored the range and is sending the whole file.
                                existingBytes = 0;
                            }
                        }

                        var totalBytes = GetTotalBytes(response, existingBytes);
                        if (expectedBytes.HasValue && totalBytes.HasValue && totalBytes.Value != expectedBytes.Value)
                        {
                            throw new InvalidDataException(
                                $"The {component} download reports {totalBytes.Value} bytes; expected {expectedBytes.Value} bytes.");
                        }

                        await CopyToFileAsync(
                            response,
                            destinationPath,
                            append,
                            existingBytes,
                            totalBytes,
                            component,
                            expectedBytes,
                            progress,
                            cancellationToken).ConfigureAwait(false);
                        return;
                    }
                }
            }

            throw new IOException($"The {component} download could not be resumed.");
        }

        /// <summary>
        /// Length of a partial file worth resuming. Without a known total size a partial
        /// file cannot be trusted, and one at or past the expected size is wrong; both are deleted.
        /// </summary>
        public static long GetResumableLength(string destinationPath, long? expectedBytes)
        {
            if (!File.Exists(destinationPath)) return 0;

            var length = new FileInfo(destinationPath).Length;
            if (length <= 0 || !expectedBytes.HasValue || length >= expectedBytes.Value)
            {
                DeleteIfPresent(destinationPath);
                return 0;
            }

            return length;
        }

        private static long? GetTotalBytes(HttpResponseMessage response, long existingBytes)
        {
            var contentRange = response.Content.Headers.ContentRange;
            if (contentRange != null && contentRange.HasLength) return contentRange.Length;

            var contentLength = response.Content.Headers.ContentLength;
            return contentLength.HasValue ? contentLength.Value + existingBytes : (long?)null;
        }

        private static async Task CopyToFileAsync(
            HttpResponseMessage response,
            string destinationPath,
            bool append,
            long existingBytes,
            long? totalBytes,
            string component,
            long? expectedBytes,
            IProgress<LocalModelDownloadProgress> progress,
            CancellationToken cancellationToken)
        {
            var buffer = new byte[BufferSize];
            var bytesReceived = existingBytes;

            using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            using (var output = new FileStream(
                destinationPath,
                append ? FileMode.Append : FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                buffer.Length,
                true))
            {
                int bytesRead;
                while ((bytesRead = await input.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await output.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                    bytesReceived += bytesRead;
                    if (expectedBytes.HasValue && bytesReceived > expectedBytes.Value)
                    {
                        throw new InvalidDataException(
                            $"The {component} download exceeded the expected {expectedBytes.Value} bytes.");
                    }

                    progress?.Report(new LocalModelDownloadProgress(component, bytesReceived, totalBytes));
                }
            }

            if (expectedBytes.HasValue && bytesReceived != expectedBytes.Value)
            {
                throw new IOException(
                    $"The {component} download ended after {bytesReceived} of {expectedBytes.Value} bytes. Enable local group naming again to resume it.");
            }
        }

        private static void DeleteIfPresent(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
