using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Dynamo.Extensions;
using Dynamo.Logging;

namespace MonocleExtension
{
    /// <summary>
    /// Package-side loader. Dynamo calls Startup while loading packages, before any view extension is loaded,
    /// so this is where we make sure the right MonocleViewExtension build is on disk and point
    /// extra\Monocle_ViewExtensionDefinition.xml at it.
    ///
    /// Layout: bin\&lt;major.minor&gt;\MonocleViewExtension.dll, one folder per Dynamo build. Several Dynamo versions
    /// sharing one package folder (custom/network package paths) each get their own build instead of loading
    /// whichever build happened to be downloaded first. Downloads are verified against deploy/SHA256SUMS.
    /// </summary>
    public class MonocleExtension : IExtension, ILogSource
    {
        private bool _prepared;
        private string _buildToActivateOnShutdown;

        public string UniqueId => "53301BE8-BDA9-47CA-9EF0-2B70808B13A5";
        public string Name => "MonocleExtension";

        // MONOCLE_DEPLOY_URL lets forks and local testing point the loader at another copy of the deploy folder.
        internal static string DeployUrl =>
            (Environment.GetEnvironmentVariable("MONOCLE_DEPLOY_URL") ?? "https://raw.githubusercontent.com/johnpierson/MonocleForDynamo/master/deploy").TrimEnd('/');
        internal static string ManifestUrl => $"{DeployUrl}/SHA256SUMS";

        public event Action<ILogMessage> MessageLogged;

        public void Startup(StartupParams sp)
        {
            Prepare();
        }

        public void Ready(ReadyParams rp)
        {
            Prepare();
            // Look for a newer build in the background; it is applied on the next launch, before it is loaded.
            Task.Run(() => CheckForUpdatedBuild());
        }

        public void Shutdown()
        {
            ActivateBuildAfterLegacySession();
        }

        public void Dispose()
        {
            ActivateBuildAfterLegacySession();
        }

        internal void Prepare()
        {
            if (_prepared) return;
            _prepared = true;

            try
            {
                Global.DynamoVersion = ResolveDynamoVersion();

                var build = ChooseLocalBuild();
                if (build == null)
                {
                    build = DownloadInitialBuild();
                }

                if (build != null)
                {
                    ApplyPendingUpdate(build);
                    CopySatelliteAssemblies(build);

                    if (File.Exists(Global.LegacyViewExtensionDll))
                    {
                        // Dynamo's package scan already loaded bin\MonocleViewExtension.dll (it loads every top-level
                        // DLL in bin). A second copy with the same assembly name can't load in this process, so this
                        // session keeps using the legacy file; the new layout takes over from the next launch.
                        WriteDefinition(null);
                        _buildToActivateOnShutdown = build;
                    }
                    else
                    {
                        WriteDefinition(build);
                        TryDelete(Global.LegacyViewExtensionDll + ".old");
                    }
                }
                else if (File.Exists(Global.LegacyViewExtensionDll))
                {
                    // Offline and nothing in the new layout yet: keep using the previously downloaded file.
                    WriteDefinition(null);
                }
            }
            catch (Exception exception)
            {
                Log($"Monocle could not prepare its view extension: {exception.Message}", WarningLevel.Moderate);
            }
        }

        private static Version ResolveDynamoVersion()
        {
            var dynamoCore = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => string.Equals(a.GetName().Name, "DynamoCore", StringComparison.Ordinal));
            if (dynamoCore == null)
            {
                throw new InvalidOperationException("Monocle could not determine the loaded DynamoCore version.");
            }

            return dynamoCore.GetName().Version;
        }

        /// <summary>Best build already on disk for the running Dynamo (counts a pending update as present).</summary>
        private static string ChooseLocalBuild()
        {
            if (!Directory.Exists(Global.PackageBinFolder)) return null;

            var local = Directory.GetDirectories(Global.PackageBinFolder)
                .Select(Path.GetFileName)
                .Where(DeployManifest.IsBuildName)
                .Where(b => File.Exists(Global.BuildDll(b)) || File.Exists(Global.BuildDll(b) + ".pending"));

            return DeployManifest.ChooseBuild(Global.DynamoVersion, local);
        }

        private string DownloadInitialBuild()
        {
            var manifest = TryDownloadManifest(TimeSpan.FromSeconds(15));
            if (manifest != null)
            {
                var build = DeployManifest.ChooseBuild(Global.DynamoVersion, manifest.Keys);
                if (build == null)
                {
                    Log($"Monocle has no view extension build for Dynamo {Global.TruncatedDynVersion}.", WarningLevel.Mild);
                    return null;
                }

                DownloadVerified(build, manifest[build], Global.BuildDll(build), TimeSpan.FromSeconds(60));
                if (build != Global.TruncatedDynVersion)
                {
                    Log($"Monocle is using the Dynamo {build} view extension build for Dynamo {Global.TruncatedDynVersion}.", WarningLevel.Mild);
                }

                return build;
            }

            return null;
        }

        private void CheckForUpdatedBuild()
        {
            try
            {
                var manifest = TryDownloadManifest(TimeSpan.FromSeconds(15));
                if (manifest == null) return;

                var best = DeployManifest.ChooseBuild(Global.DynamoVersion, manifest.Keys);
                if (best == null) return;

                var target = Global.BuildDll(best);
                var expected = manifest[best];
                if (File.Exists(target) && DeployManifest.Sha256(target) == expected) return;
                if (File.Exists(target + ".pending") && DeployManifest.Sha256(target + ".pending") == expected) return;

                // An existing build may be loaded right now, so stage it; Prepare() swaps it in next launch.
                var destination = File.Exists(target) ? target + ".pending" : target;
                DownloadVerified(best, expected, destination, TimeSpan.FromSeconds(120));
                Log("Monocle downloaded an updated view extension. It will be used the next time Dynamo starts.", WarningLevel.Mild);
            }
            catch (Exception exception)
            {
                Log($"Monocle update check failed: {exception.Message}", WarningLevel.Mild);
            }
        }

        private static void ApplyPendingUpdate(string build)
        {
            var target = Global.BuildDll(build);
            var pending = target + ".pending";
            if (!File.Exists(pending)) return;

            try
            {
                if (File.Exists(target)) File.Delete(target);
                File.Move(pending, target);
            }
            catch (IOException)
            {
                // Another Dynamo process is using this build; try again next launch.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        /// <summary>Localized resources live in bin\&lt;culture&gt;\; the build folder needs its own copy.</summary>
        private static void CopySatelliteAssemblies(string build)
        {
            foreach (var cultureFolder in Directory.GetDirectories(Global.PackageBinFolder))
            {
                var culture = Path.GetFileName(cultureFolder);
                if (DeployManifest.IsBuildName(culture)) continue;

                foreach (var satellite in Directory.GetFiles(cultureFolder, "MonocleViewExtension.resources.dll"))
                {
                    var destinationFolder = Path.Combine(Global.BuildFolder(build), culture);
                    var destination = Path.Combine(destinationFolder, Path.GetFileName(satellite));
                    if (File.Exists(destination)) continue;

                    try
                    {
                        Directory.CreateDirectory(destinationFolder);
                        File.Copy(satellite, destination);
                    }
                    catch (IOException)
                    {
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                }
            }
        }

        private void WriteDefinition(string build)
        {
            var xml = DeployManifest.DefinitionXml(build);
            try
            {
                if (File.Exists(Global.ViewExtensionXml) && File.ReadAllText(Global.ViewExtensionXml) == xml) return;
                Directory.CreateDirectory(Global.PackageExtraFolder);
                File.WriteAllText(Global.ViewExtensionXml, xml);
            }
            catch (Exception exception)
            {
                Log($"Monocle could not update {Global.ViewExtensionXml}: {exception.Message}", WarningLevel.Mild);
            }
        }

        /// <summary>
        /// Migration from the pre-2026.10 layout. Dynamo loads every top-level DLL in a package's bin folder while
        /// scanning for node libraries, so the old bin\MonocleViewExtension.dll is loaded (and locked) for the whole
        /// session. Renaming a loaded DLL is allowed, so at shutdown it is moved aside and the definition is pointed
        /// at the per-version build; the renamed copy is deleted on a later launch.
        /// </summary>
        private void ActivateBuildAfterLegacySession()
        {
            var build = _buildToActivateOnShutdown;
            if (build == null) return;
            _buildToActivateOnShutdown = null;

            var legacy = Global.LegacyViewExtensionDll;
            var retired = legacy + ".old";
            try
            {
                TryDelete(retired);
                if (File.Exists(legacy) && !TryDelete(legacy))
                {
                    File.Move(legacy, retired);
                }

                if (!File.Exists(legacy)) WriteDefinition(build);
            }
            catch (Exception)
            {
                // try again next session
            }
        }

        private static bool TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private Dictionary<string, string> TryDownloadManifest(TimeSpan timeout)
        {
            try
            {
                using (var client = new TimeoutWebClient(timeout))
                {
                    var manifest = DeployManifest.Parse(client.DownloadString($"{ManifestUrl}?t={DateTime.UtcNow.Ticks}"));
                    return manifest.Count > 0 ? manifest : null;
                }
            }
            catch (Exception exception)
            {
                Log($"Monocle could not download its build manifest: {exception.Message}", WarningLevel.Mild);
                return null;
            }
        }

        /// <summary>Download to a temp file, check SHA-256 against the manifest, then move into place.</summary>
        private static void DownloadVerified(string build, string expectedSha256, string destination, TimeSpan timeout)
        {
            var url = $"{DeployUrl}/{build}/{DeployManifest.ViewExtensionFileName}";
            var temporaryFile = destination + ".download";
            Directory.CreateDirectory(Path.GetDirectoryName(destination));

            try
            {
                TryDelete(temporaryFile);
                using (var client = new TimeoutWebClient(timeout))
                {
                    client.DownloadFile(url, temporaryFile);
                }

                var actual = DeployManifest.Sha256(temporaryFile);
                if (!string.Equals(actual, expectedSha256, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException($"Downloaded {url} does not match the published SHA-256 ({actual} != {expectedSha256}).");
                }

                TryDelete(destination);
                File.Move(temporaryFile, destination);
            }
            finally
            {
                TryDelete(temporaryFile);
            }
        }

        private void Log(string message, WarningLevel level)
        {
            try
            {
                MessageLogged?.Invoke(LogMessage.Warning(message, level));
            }
            catch (Exception)
            {
                // logging must never break Dynamo startup
            }
        }

        private sealed class TimeoutWebClient : WebClient
        {
            private readonly TimeSpan _timeout;

            public TimeoutWebClient(TimeSpan timeout)
            {
                _timeout = timeout;
                Headers.Add(HttpRequestHeader.UserAgent, "MonocleForDynamo");
            }

            protected override WebRequest GetWebRequest(Uri address)
            {
                var request = base.GetWebRequest(address);
                if (request != null) request.Timeout = (int)_timeout.TotalMilliseconds;
                if (request is HttpWebRequest http) http.ReadWriteTimeout = (int)_timeout.TotalMilliseconds;
                return request;
            }
        }
    }
}
