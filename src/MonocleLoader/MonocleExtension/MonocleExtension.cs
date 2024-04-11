using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Dynamo.Extensions;
using Dynamo.Interfaces;
using Dynamo.Logging;
using Dynamo.PackageManager;
using Dynamo.Wpf.Extensions;
using ProtoCore.AST;

namespace MonocleExtension
{
    public class MonocleExtension : IExtension
    {
        public bool ReadyCalled = false;
        public string UniqueId => "53301BE8-BDA9-47CA-9EF0-2B70808B13A5";
        public string Name => "MonocleExtension";


        public void Ready(ReadyParams rp)
        {
            this.ReadyCalled = true;
           
        }

        public void Dispose()
        {
        }

        public void Startup(StartupParams sp)
        {
            
            //only try to run this if the view extension DLL is missing (first run after install)
            if (!File.Exists(Global.MonocleViewExtensionDll))
            {
                //check which version of Dynamo core is loaded
                var dynamoCore = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName.Contains("DynamoCore"));

                Global.DynamoVersion = dynamoCore.GetName().Version;

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
                

                

                //write the view extension XML
                File.WriteAllText(Global.ViewExtensionXml, Global.ViewExtensionXmlText);


                //now try to load the view extension
                MessageBox.Show("monocle view extension downloaded, please restart Dynamo to finish loading it.",
                "monocle for dynamo");
            }
        }


        public void Shutdown()
        {
        }


    }
}
