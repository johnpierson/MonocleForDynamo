using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace MonocleViewExtension.LocalGroupNaming
{
    internal sealed class LocalModelProvisioner
    {
        private const string AgreementFileName = "third-party-licenses.accepted";
        private const string RuntimeMarkerFileName = ".runtime.complete";
        private const string ModelMarkerFileName = ".model.complete";
        private const string InstallLockFileName = ".install.lock";
        private const string StagingDirectoryName = "runtime-staging";
        private const string DownloadExtension = ".download";
        private const string DeletePendingMarker = ".delete-";
        private readonly LocalLlamaServerOptions options;
        private readonly HttpMessageHandler messageHandler;

        public LocalModelProvisioner(LocalLlamaServerOptions options) : this(options, null)
        {
        }

        /// <summary>Test seam: downloads go through <paramref name="messageHandler"/> instead of the network.</summary>
        internal LocalModelProvisioner(LocalLlamaServerOptions options, HttpMessageHandler messageHandler)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.messageHandler = messageHandler;
        }

        public LocalModelManifest Manifest => options.Manifest;

        public bool RequiresAgreement => options.IsManagedInstallation && !HasAcceptedAgreements();

        public bool RequiresDownload => options.IsManagedInstallation &&
            (!RuntimeIsInstalled() || !ModelIsInstalled());

        private string ModelComponentName => Manifest.ModelDisplayName + " model";

        private string RuntimeComponentName => "llama.cpp runtime " + Manifest.RuntimeVersion;

        public bool HasAcceptedAgreements()
        {
            if (!options.IsManagedInstallation) return true;

            var path = Path.Combine(options.InstallationDirectory, AgreementFileName);
            if (!File.Exists(path)) return false;

            var firstLine = File.ReadLines(path).FirstOrDefault();
            return string.Equals(
                firstLine,
                "Version=" + Manifest.InstallationVersion,
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
                    "Version=" + Manifest.InstallationVersion,
                    "AcceptedUtc=" + DateTime.UtcNow.ToString("O"),
                    "RuntimeLicense=" + Manifest.RuntimeLicenseUrl,
                    "ModelLicense=" + Manifest.ModelLicenseUrl
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

            Directory.CreateDirectory(options.InstallationDirectory);

            //The installation directory is shared by every Dynamo host (Sandbox,
            //Revit, Civil 3D); only one process may install into it at a time.
            using (AcquireInstallLock())
            {
                CleanUpStaleStaging();
                EnsureAvailableDiskSpace();

                using (var httpClient = CreateHttpClient())
                {
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

            //Only once this version is complete do the files of an older pin become
            //dead weight (a previous model alone is a multi-gigabyte download).
            RemoveSupersededInstallations();
        }

        private HttpClient CreateHttpClient()
        {
            var httpClient = messageHandler == null
                ? new HttpClient()
                : new HttpClient(messageHandler, false);
            httpClient.Timeout = TimeSpan.FromHours(6);
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MonocleForDynamo/" + ProductVersion);
            return httpClient;
        }

        private static string ProductVersion
        {
            get
            {
                var version = typeof(LocalModelProvisioner).Assembly.GetName().Version;
                return version == null ? "0.0" : version.ToString(2);
            }
        }

        private bool RuntimeIsInstalled()
        {
            var markerPath = Path.Combine(options.InstallationDirectory, RuntimeMarkerFileName);
            return File.Exists(options.ServerPath) && File.Exists(markerPath) && string.Equals(
                File.ReadAllText(markerPath).Trim(),
                Manifest.RuntimeSha256,
                StringComparison.OrdinalIgnoreCase);
        }

        private bool ModelIsInstalled()
        {
            var markerPath = Path.Combine(options.InstallationDirectory, ModelMarkerFileName);
            if (!File.Exists(options.ModelPath) || !File.Exists(markerPath)) return false;

            var modelInfo = new FileInfo(options.ModelPath);
            return modelInfo.Length == Manifest.ModelFileSize && string.Equals(
                File.ReadAllText(markerPath).Trim(),
                Manifest.ModelSha256,
                StringComparison.OrdinalIgnoreCase);
        }

        private async Task InstallRuntimeAsync(
            HttpClient httpClient,
            IProgress<LocalModelDownloadProgress> progress,
            CancellationToken cancellationToken)
        {
            var archivePath = Path.Combine(options.InstallationDirectory, Manifest.RuntimeArchiveName + DownloadExtension);
            var stagingDirectory = Path.Combine(options.InstallationDirectory, StagingDirectoryName);
            var runtimeDirectory = Path.GetDirectoryName(options.ServerPath);
            var markerPath = Path.Combine(options.InstallationDirectory, RuntimeMarkerFileName);

            //Invalidate the marker before touching the runtime directory so a failed
            //reinstall is retried instead of being reported as complete.
            DeleteFileIfPresent(markerPath);
            DeleteDirectoryIfPresent(stagingDirectory);

            try
            {
                await ResumableDownloader.DownloadAsync(
                    httpClient,
                    Manifest.RuntimeDownloadUrl,
                    archivePath,
                    RuntimeComponentName,
                    Manifest.RuntimeArchiveSize,
                    progress,
                    cancellationToken).ConfigureAwait(false);

                VerifySha256(archivePath, Manifest.RuntimeSha256, cancellationToken);
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
                File.WriteAllText(markerPath, Manifest.RuntimeSha256);
                DeleteFileIfPresent(archivePath);
            }
            catch (InvalidDataException)
            {
                //A corrupt or wrong-sized download must not be resumed next time.
                DeleteFileIfPresent(archivePath);
                throw;
            }
            finally
            {
                //A cancelled or interrupted download keeps its partial file so the next
                //attempt can resume it; only the extraction scratch space is removed.
                DeleteDirectoryIfPresent(stagingDirectory);
            }
        }

        private async Task InstallModelAsync(
            HttpClient httpClient,
            IProgress<LocalModelDownloadProgress> progress,
            CancellationToken cancellationToken)
        {
            var modelDirectory = Path.GetDirectoryName(options.ModelPath);
            var temporaryPath = options.ModelPath + DownloadExtension;
            var markerPath = Path.Combine(options.InstallationDirectory, ModelMarkerFileName);
            Directory.CreateDirectory(modelDirectory);
            DeleteFileIfPresent(markerPath);

            try
            {
                await ResumableDownloader.DownloadAsync(
                    httpClient,
                    Manifest.ModelDownloadUrl,
                    temporaryPath,
                    ModelComponentName,
                    Manifest.ModelFileSize,
                    progress,
                    cancellationToken).ConfigureAwait(false);

                var downloadedFile = new FileInfo(temporaryPath);
                if (downloadedFile.Length != Manifest.ModelFileSize)
                {
                    throw new InvalidDataException(
                        $"The downloaded model was {downloadedFile.Length} bytes; expected {Manifest.ModelFileSize} bytes.");
                }

                VerifySha256(temporaryPath, Manifest.ModelSha256, cancellationToken);
                DeleteFileIfPresent(options.ModelPath);
                File.Move(temporaryPath, options.ModelPath);
                File.WriteAllText(markerPath, Manifest.ModelSha256);
            }
            catch (InvalidDataException)
            {
                //A corrupt download would fail the same way again; start over next time.
                DeleteFileIfPresent(temporaryPath);
                throw;
            }
        }

        private static void VerifySha256(string path, string expectedHash, CancellationToken cancellationToken)
        {
            using (var stream = File.OpenRead(path))
            using (var sha256 = SHA256.Create())
            {
                var buffer = new byte[ResumableDownloader.BufferSize];
                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
                }

                sha256.TransformFinalBlock(buffer, 0, 0);
                var actualHash = string.Concat(sha256.Hash.Select(value => value.ToString("X2")));
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

        private FileStream AcquireInstallLock()
        {
            try
            {
                return new FileStream(
                    Path.Combine(options.InstallationDirectory, InstallLockFileName),
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None);
            }
            catch (IOException)
            {
                throw new InvalidOperationException(
                    "Another Dynamo instance is currently installing local group naming. Wait for that installation to finish and try again.");
            }
        }

        private void CleanUpStaleStaging()
        {
            //Partial '.download' files are deliberately kept: the downloader resumes them.
            DeleteDirectoryIfPresent(Path.Combine(options.InstallationDirectory, StagingDirectoryName));
        }

        private void EnsureAvailableDiskSpace()
        {
            var modelInstalled = ModelIsInstalled();
            if (modelInstalled && RuntimeIsInstalled()) return;

            var required = Manifest.RequiredFreeBytes;
            if (modelInstalled)
            {
                required -= Manifest.ModelFileSize;
            }
            else
            {
                var partialModel = options.ModelPath + DownloadExtension;
                if (File.Exists(partialModel))
                {
                    required -= Math.Min(new FileInfo(partialModel).Length, Manifest.ModelFileSize);
                }
            }

            var root = Path.GetPathRoot(Path.GetFullPath(options.InstallationDirectory));
            var drive = new DriveInfo(root);
            if (drive.AvailableFreeSpace < required)
            {
                throw new IOException(
                    $"At least {LocalModelManifest.FormatSize(required)} of free disk space is required to install local group naming. '{root}' has {LocalModelManifest.FormatSize(drive.AvailableFreeSpace)} available.");
            }
        }

        /// <summary>
        /// Deletes the installation folders of earlier pins named in the manifest. Only those
        /// names are touched: a folder with any other name may belong to another Monocle
        /// build (Revit 2024 and Revit 2026 can run different Monocle versions against the
        /// same %LOCALAPPDATA%). Each folder is renamed before it is deleted, which fails as a
        /// whole while another installer holds its lock or a running server has its model
        /// mapped, so an installation is either removed completely or left intact.
        /// </summary>
        internal void RemoveSupersededInstallations()
        {
            if (string.IsNullOrWhiteSpace(options.InstallationRootDirectory) ||
                !Directory.Exists(options.InstallationRootDirectory))
            {
                return;
            }

            var current = NormalizeDirectoryPath(options.InstallationDirectory);
            foreach (var directory in Directory.GetDirectories(options.InstallationRootDirectory))
            {
                if (string.Equals(NormalizeDirectoryPath(directory), current, StringComparison.OrdinalIgnoreCase)) continue;

                var name = Path.GetFileName(directory);
                var isPendingDelete = name.IndexOf(DeletePendingMarker, StringComparison.Ordinal) >= 0;
                if (!isPendingDelete && !Manifest.SupersededInstallationVersions.Contains(name, StringComparer.OrdinalIgnoreCase)) continue;

                try
                {
                    //Never follow a junction or symbolic link out of the installation root.
                    if ((new DirectoryInfo(directory).Attributes & FileAttributes.ReparsePoint) != 0) continue;

                    var target = directory;
                    if (!isPendingDelete)
                    {
                        target = Path.Combine(options.InstallationRootDirectory, name + DeletePendingMarker + Guid.NewGuid().ToString("N"));
                        Directory.Move(directory, target);
                    }

                    Directory.Delete(target, true);
                }
                catch (IOException)
                {
                    //In use by another Dynamo instance; leave it for a later run.
                }
                catch (UnauthorizedAccessException)
                {
                    //Not ours to remove.
                }
            }
        }

        private static string NormalizeDirectoryPath(string path)
        {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
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
