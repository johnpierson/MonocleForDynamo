using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
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

    internal sealed class LocalModelProvisioner
    {
        private const string AgreementFileName = "third-party-licenses.accepted";
        private const string RuntimeMarkerFileName = ".runtime.complete";
        private const string ModelMarkerFileName = ".model.complete";
        private readonly LocalLlamaServerOptions options;

        public LocalModelProvisioner(LocalLlamaServerOptions options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public bool RequiresAgreement => options.IsManagedInstallation && !HasAcceptedAgreements();

        public bool RequiresDownload => options.IsManagedInstallation &&
            (!RuntimeIsInstalled() || !ModelIsInstalled());

        public bool HasAcceptedAgreements()
        {
            if (!options.IsManagedInstallation) return true;

            var path = Path.Combine(options.InstallationDirectory, AgreementFileName);
            if (!File.Exists(path)) return false;

            var firstLine = File.ReadLines(path).FirstOrDefault();
            return string.Equals(
                firstLine,
                "Version=" + LocalModelManifest.InstallationVersion,
                StringComparison.Ordinal);
        }

        public void RecordAgreementAcceptance()
        {
            if (!options.IsManagedInstallation) return;

            Directory.CreateDirectory(options.InstallationDirectory);
            File.WriteAllText(
                Path.Combine(options.InstallationDirectory, AgreementFileName),
                string.Join(Environment.NewLine, new[]
                {
                    "Version=" + LocalModelManifest.InstallationVersion,
                    "AcceptedUtc=" + DateTime.UtcNow.ToString("O"),
                    "RuntimeLicense=" + LocalModelManifest.RuntimeLicenseUrl,
                    "ModelLicense=" + LocalModelManifest.ModelLicenseUrl
                }));
        }

        public async Task EnsureInstalledAsync(
            IProgress<LocalModelDownloadProgress> progress,
            CancellationToken cancellationToken)
        {
            if (!options.IsManagedInstallation)
            {
                options.Validate();
                return;
            }

            if (!HasAcceptedAgreements())
            {
                throw new InvalidOperationException(
                    "The third-party runtime and model license terms must be accepted before downloading.");
            }

            EnsureAvailableDiskSpace();
            Directory.CreateDirectory(options.InstallationDirectory);

            using (var httpClient = new HttpClient { Timeout = TimeSpan.FromHours(6) })
            {
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MonocleForDynamo/2026.6");

                if (!RuntimeIsInstalled())
                {
                    await InstallRuntimeAsync(httpClient, progress, cancellationToken).ConfigureAwait(false);
                }

                if (!ModelIsInstalled())
                {
                    await InstallModelAsync(httpClient, progress, cancellationToken).ConfigureAwait(false);
                }
            }

            options.Validate();
        }

        private bool RuntimeIsInstalled()
        {
            var markerPath = Path.Combine(options.InstallationDirectory, RuntimeMarkerFileName);
            return File.Exists(options.ServerPath) && File.Exists(markerPath) && string.Equals(
                File.ReadAllText(markerPath).Trim(),
                LocalModelManifest.RuntimeSha256,
                StringComparison.OrdinalIgnoreCase);
        }

        private bool ModelIsInstalled()
        {
            var markerPath = Path.Combine(options.InstallationDirectory, ModelMarkerFileName);
            if (!File.Exists(options.ModelPath) || !File.Exists(markerPath)) return false;

            var modelInfo = new FileInfo(options.ModelPath);
            return modelInfo.Length == LocalModelManifest.ModelFileSize && string.Equals(
                File.ReadAllText(markerPath).Trim(),
                LocalModelManifest.ModelSha256,
                StringComparison.OrdinalIgnoreCase);
        }

        private async Task InstallRuntimeAsync(
            HttpClient httpClient,
            IProgress<LocalModelDownloadProgress> progress,
            CancellationToken cancellationToken)
        {
            var archivePath = Path.Combine(options.InstallationDirectory, LocalModelManifest.RuntimeArchiveName + ".download");
            var stagingDirectory = Path.Combine(options.InstallationDirectory, "runtime-staging");
            var runtimeDirectory = Path.GetDirectoryName(options.ServerPath);

            DeleteFileIfPresent(archivePath);
            DeleteDirectoryIfPresent(stagingDirectory);

            try
            {
                await DownloadAsync(
                    httpClient,
                    LocalModelManifest.RuntimeDownloadUrl,
                    archivePath,
                    "llama.cpp runtime",
                    progress,
                    cancellationToken).ConfigureAwait(false);

                VerifySha256(archivePath, LocalModelManifest.RuntimeSha256);
                Directory.CreateDirectory(stagingDirectory);
                ExtractArchiveSafely(archivePath, stagingDirectory);

                var serverCandidates = Directory.GetFiles(
                    stagingDirectory,
                    "llama-server.exe",
                    SearchOption.AllDirectories);
                if (serverCandidates.Length != 1)
                {
                    throw new InvalidDataException("The downloaded llama.cpp archive did not contain one llama-server.exe.");
                }

                var extractedRuntimeDirectory = Path.GetDirectoryName(serverCandidates[0]);
                DeleteDirectoryIfPresent(runtimeDirectory);
                CopyDirectory(extractedRuntimeDirectory, runtimeDirectory);
                File.WriteAllText(
                    Path.Combine(options.InstallationDirectory, RuntimeMarkerFileName),
                    LocalModelManifest.RuntimeSha256);
            }
            finally
            {
                DeleteFileIfPresent(archivePath);
                DeleteDirectoryIfPresent(stagingDirectory);
            }
        }

        private async Task InstallModelAsync(
            HttpClient httpClient,
            IProgress<LocalModelDownloadProgress> progress,
            CancellationToken cancellationToken)
        {
            var modelDirectory = Path.GetDirectoryName(options.ModelPath);
            var temporaryPath = options.ModelPath + ".download";
            Directory.CreateDirectory(modelDirectory);
            DeleteFileIfPresent(temporaryPath);

            try
            {
                await DownloadAsync(
                    httpClient,
                    LocalModelManifest.ModelDownloadUrl,
                    temporaryPath,
                    "Qwen3 4B model",
                    progress,
                    cancellationToken).ConfigureAwait(false);

                var downloadedFile = new FileInfo(temporaryPath);
                if (downloadedFile.Length != LocalModelManifest.ModelFileSize)
                {
                    throw new InvalidDataException(
                        $"The downloaded model was {downloadedFile.Length} bytes; expected {LocalModelManifest.ModelFileSize} bytes.");
                }

                VerifySha256(temporaryPath, LocalModelManifest.ModelSha256);
                DeleteFileIfPresent(options.ModelPath);
                File.Move(temporaryPath, options.ModelPath);
                File.WriteAllText(
                    Path.Combine(options.InstallationDirectory, ModelMarkerFileName),
                    LocalModelManifest.ModelSha256);
            }
            finally
            {
                DeleteFileIfPresent(temporaryPath);
            }
        }

        private static async Task DownloadAsync(
            HttpClient httpClient,
            string url,
            string destinationPath,
            string component,
            IProgress<LocalModelDownloadProgress> progress,
            CancellationToken cancellationToken)
        {
            using (var response = await httpClient.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                var totalBytes = response.Content.Headers.ContentLength;
                var buffer = new byte[1024 * 1024];
                long bytesReceived = 0;

                using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var output = new FileStream(
                    destinationPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    buffer.Length,
                    true))
                {
                    int bytesRead;
                    while ((bytesRead = await input.ReadAsync(
                        buffer,
                        0,
                        buffer.Length,
                        cancellationToken).ConfigureAwait(false)) > 0)
                    {
                        await output.WriteAsync(
                            buffer,
                            0,
                            bytesRead,
                            cancellationToken).ConfigureAwait(false);
                        bytesReceived += bytesRead;
                        progress?.Report(new LocalModelDownloadProgress(component, bytesReceived, totalBytes));
                    }
                }
            }
        }

        private static void VerifySha256(string path, string expectedHash)
        {
            using (var stream = File.OpenRead(path))
            using (var sha256 = SHA256.Create())
            {
                var actualHash = string.Concat(sha256.ComputeHash(stream).Select(value => value.ToString("X2")));
                if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException(
                        $"Checksum verification failed for '{Path.GetFileName(path)}'. Expected {expectedHash}, received {actualHash}.");
                }
            }
        }

        private static void ExtractArchiveSafely(string archivePath, string destinationDirectory)
        {
            var destinationRoot = Path.GetFullPath(destinationDirectory) + Path.DirectorySeparatorChar;
            using (var archive = ZipFile.OpenRead(archivePath))
            {
                foreach (var entry in archive.Entries)
                {
                    var relativePath = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                    var destinationPath = Path.GetFullPath(Path.Combine(destinationDirectory, relativePath));
                    if (!destinationPath.StartsWith(destinationRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidDataException("The downloaded runtime archive contains an unsafe path.");
                    }

                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(destinationPath);
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                    using (var input = entry.Open())
                    using (var output = File.Create(destinationPath))
                    {
                        input.CopyTo(output);
                    }
                }
            }
        }

        private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
        {
            Directory.CreateDirectory(destinationDirectory);
            foreach (var file in Directory.GetFiles(sourceDirectory))
            {
                File.Copy(file, Path.Combine(destinationDirectory, Path.GetFileName(file)), true);
            }

            foreach (var directory in Directory.GetDirectories(sourceDirectory))
            {
                CopyDirectory(directory, Path.Combine(destinationDirectory, Path.GetFileName(directory)));
            }
        }

        private void EnsureAvailableDiskSpace()
        {
            if (ModelIsInstalled()) return;

            var root = Path.GetPathRoot(Path.GetFullPath(options.InstallationDirectory));
            var drive = new DriveInfo(root);
            const long minimumFreeBytes = 3L * 1024 * 1024 * 1024;
            if (drive.AvailableFreeSpace < minimumFreeBytes)
            {
                throw new IOException(
                    $"At least 3 GB of free disk space is required to install local group naming. '{root}' has {drive.AvailableFreeSpace / (1024 * 1024)} MB available.");
            }
        }

        private static void DeleteFileIfPresent(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }

        private static void DeleteDirectoryIfPresent(string path)
        {
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path)) Directory.Delete(path, true);
        }
    }
}
