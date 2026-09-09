using System;
using System.IO;
using System.Linq;
using Dynamo.Extensions;
using Dynamo.Logging;
using System.Net;

namespace MonocleExtension
{
    public class MonocleExtension : IExtension
    {
        public bool ReadyCalled = false;
        public string UniqueId => "53301BE8-BDA9-47CA-9EF0-2B70808B13A5";
        public string Name => "MonocleExtension";

        internal string GitHubUrl => "https://raw.githubusercontent.com/johnpierson/MonocleForDynamo/master/deploy";
        public void Ready(ReadyParams rp)   
        {
            this.ReadyCalled = true;
            WriteFiles();
        }

        public void Dispose()
        {
        }

        public void Startup(StartupParams sp)
        {
            if (!ReadyCalled)
            {
                WriteFiles();
            }
        }

        internal void WriteFiles()
        {
            //only try to run this if the view extension DLL is missing (first run after install)
            if (!File.Exists(Global.MonocleViewExtensionDll))
            {
                //check which version of Dynamo core is loaded
                var dynamoCore = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => string.Equals(a.GetName().Name, "DynamoCore", StringComparison.Ordinal));
                if (dynamoCore == null)
                {
                    throw new InvalidOperationException("Monocle could not determine the loaded DynamoCore version.");
                }

                Global.DynamoVersion = dynamoCore.GetName().Version;

                // Download the exact view extension build, then fall back to older minor versions
                // of the same Dynamo major version when the exact build is not published.
                try
                {
                    var downloadedVersion = DownloadClosestBuild(Global.DynamoVersion, Global.MonocleViewExtensionDll);
                    if (!string.Equals(downloadedVersion, Global.TruncatedDynVersion, StringComparison.Ordinal))
                    {
                        LogMessage.Warning(
                            $"Monocle is using the Dynamo {downloadedVersion} view extension build for Dynamo {Global.TruncatedDynVersion}.",
                            WarningLevel.Mild);
                    }
                }
                catch (WebException exception)
                {
                    LogMessage.Warning(
                        $"Monocle does not have a view extension build for Dynamo {Global.TruncatedDynVersion} " +
                        $"or an older compatible build. The extension will remain unavailable: {exception.Message}",
                        WarningLevel.Mild);
                }
            }
        }

        internal string DownloadClosestBuild(Version dynamoVersion, string fileLocation)
        {
            var candidates = new System.Collections.Generic.List<string>(DynamoBuildVersionResolver.GetCandidates(dynamoVersion));
            WebException lastException = null;

            foreach (var candidate in candidates)
            {
                try
                {
                    DownloadFile(candidate, fileLocation);
                    return candidate;
                }
                catch (WebException exception)
                {
                    lastException = exception;
                }
            }

            throw new WebException(
                $"Tried Dynamo build versions: {string.Join(", ", candidates)}. " +
                $"The most recent download failed: {lastException?.Message}",
                lastException);
        }

        internal void DownloadFile(string version, string fileLocation)
        {
            FileInfo fileInfo = new FileInfo(fileLocation);

            string fileName = fileInfo.Name;

            var url = string.IsNullOrWhiteSpace(version)
                ? $"{GitHubUrl}/{fileName}"
                : $"{GitHubUrl}/{version}/{fileName}";
            var temporaryFile = $"{fileLocation}.download";

            Directory.CreateDirectory(fileInfo.DirectoryName);

            try
            {
                if (File.Exists(temporaryFile))
                {
                    File.Delete(temporaryFile);
                }

                using (WebClient wc = new WebClient())
                {
                    wc.Headers.Add(HttpRequestHeader.UserAgent, "MonocleForDynamo");
                    wc.DownloadFile(url, temporaryFile);
                }

                if (new FileInfo(temporaryFile).Length == 0)
                {
                    throw new InvalidDataException($"Monocle downloaded an empty view extension from {url}.");
                }

                File.Move(temporaryFile, fileLocation);
            }
            finally
            {
                if (File.Exists(temporaryFile))
                {
                    File.Delete(temporaryFile);
                }
            }
        }
        public void Shutdown()
        {
        }


    }
}
