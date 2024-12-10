using System;
using System.IO;
using System.Linq;
using System.Threading;
using Dynamo.Extensions;
using System.Net;

namespace MonocleExtension
{
    public class MonocleExtension : IExtension
    {
        public bool ReadyCalled = false;
        public string UniqueId => "53301BE8-BDA9-47CA-9EF0-2B70808B13A5";
        public string Name => "MonocleExtension";

        internal string GitHubUrl => "https://raw.githubusercontent.com/johnpierson/MonocleForDynamo/master/deploy/"; 
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
                var dynamoCore = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName.Contains("DynamoCore"));

                Global.DynamoVersion = dynamoCore.GetName().Version;


                //download the correct Monocle dll for the Dynamo version TODO: test this
                DownloadFile(Global.TruncatedDynVersion, Global.MonocleViewExtensionDll);

                return;

                //if we are in a 3.0 version or higher, load the .net8 version of monocle
                if (Global.DynamoVersion.CompareTo(Global.DotNet8Version) >= 0)
                {
                    using (var stream = Global.ExecutingAssembly.GetManifestResourceStream(Global.MonocleForNet8))
                    {
                        var bytes = new byte[stream.Length];
                        stream.Read(bytes, 0, bytes.Length);

                        File.WriteAllBytes(Global.MonocleViewExtensionDll, bytes);
                    }
                }
                //otherwise load the .net4.8 version
                else
                {
                    using (var stream = Global.ExecutingAssembly.GetManifestResourceStream(Global.MonocleForNet48))
                    {
                        var bytes = new byte[stream.Length];
                        stream.Read(bytes, 0, bytes.Length);

                        File.WriteAllBytes(Global.MonocleViewExtensionDll, bytes);
                    }
                }


                Thread.Sleep(10 * 1000);

                //write the view extension XML
                //File.WriteAllText(Global.ViewExtensionXml, Global.ViewExtensionXmlText);
            }
        }
        internal void DownloadFile(string version, string fileLocation)
        {
            FileInfo fileInfo = new FileInfo(fileLocation);

            string fileName = fileInfo.Name;

            var url = string.IsNullOrWhiteSpace(version) ? $"{GitHubUrl}/{fileName}" : $"{GitHubUrl}/{version}/{fileName}";

            using (WebClient wc = new WebClient())
            {
                wc.Headers.Add("a", "a");
                try
                {
                    wc.DownloadFile(url, fileLocation);
                }
                catch (Exception ex)
                {
                    //
                    string messsss = ex.Message;
                }
            }
        }
        public void Shutdown()
        {
        }


    }
}
