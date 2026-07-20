using System;
using System.IO;
using System.Linq;
using Dynamo.Extensions;
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

                // Download the view extension built for this Dynamo major/minor version.
                DownloadFile(Global.TruncatedDynVersion, Global.MonocleViewExtensionDll);
            }
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
