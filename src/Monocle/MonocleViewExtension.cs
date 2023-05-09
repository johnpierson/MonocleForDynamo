using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Dynamo.Controls;
using Dynamo.Extensions;
using Dynamo.PackageManager;
using Dynamo.Wpf.Extensions;
using MonocleViewExtension.About;
using MonocleViewExtension.BetterSave;
using MonocleViewExtension.FancyPaste;
using MonocleViewExtension.Foca;
using MonocleViewExtension.GraphResizerer;
using MonocleViewExtension.MonocleSettings;
using MonocleViewExtension.NodeDocumentation;
using MonocleViewExtension.PackageUsage;
using MonocleViewExtension.Photocopier;
using MonocleViewExtension.SimpleSearch;
using MonocleViewExtension.Snippets;
using MonocleViewExtension.StandardViews;
using MonocleViewExtension.Utilities;

namespace MonocleViewExtension
{
    public class MonocleViewExtension : IViewExtension
    {
        public string UniqueId => "5A256B35-BD09-423C-82A1-372957143927";
        public string Name => "Monocle View Extension";

      
        public void Dispose()
        {
        }

        public void Startup(ViewStartupParams viewStartupParams)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
        }

        private Assembly CurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            // Get assembly name
            var assemblyName = new AssemblyName(args.Name).Name + ".dll";

            // Get resource name
            var resourceName = Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(x => x.EndsWith(".dll")).ToArray().FirstOrDefault(x => x.EndsWith(assemblyName));
            if (resourceName == null)
            {
                return null;
            }

            // Load assembly from resource
            using (var stream = Globals.ExecutingAssembly.GetManifestResourceStream(resourceName))
            {
                var bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
                return Assembly.Load(bytes);
            }
        }

        public void Loaded(ViewLoadedParams p)
        {
            //store the package manager extension for getting package versions
            Globals.PmExtension = p.ViewStartupParams.ExtensionManager.Extensions.OfType<PackageManagerExtension>().FirstOrDefault();

            /*if the user is holding down the left shift key, don't load monocle. I added this because I needed it for when I record videos that shouldn't have packages loaded.
            And yes. this is a deep reference to my roots in AutoCAD, https://knowledge.autodesk.com/support/autocad/learn-explore/caas/sfdcarticles/sfdcarticles/How-to-reset-AutoCAD-to-defaults.html
            */
            if (Keyboard.IsKeyDown(Key.LeftShift)) return;

            //load monocle settings from xml
            Settings.LoadMonocleSettings();

            //resolve the dynamo version by checking which core is loaded
            var dynamoCore = Assembly.Load("DynamoCore");
            Globals.DynamoVersion = dynamoCore.GetName().Version;

            //add the top-level menu
            var monocleMenuItem = new MenuItem { Header = "🧐 monocle" };
            //add the top level menu to the dynamo ribbon
            p.dynamoMenu.Items.Insert(6, monocleMenuItem);

            //add all of our various tools
            AboutCommand.AddMenuItem(monocleMenuItem,p);
            PackageUsageCommand.AddMenuItem(monocleMenuItem,p);
            GraphResizererCommand.AddMenuItem(monocleMenuItem, p);
            FocaCommand.EnableFoca(p, monocleMenuItem);
            SimpleSearchCommand.AddMenuItem(p, monocleMenuItem, this);
            StandardViewsCommand.EnableStandardViews(p);
            MonocleSettingsCommand.AddMenuItem(monocleMenuItem);
            FancyPasteCommand.AddMenuItem(p);
            BetterSaveCommand.AddMenuItem(p);

#if DEBUG
            SnippetsCommand.AddMenuItem(monocleMenuItem,p,this);
            PhotocopierCommand.AddMenuItem(monocleMenuItem,p);
            NodeDocumentationCommand.AddMenuItem(monocleMenuItem,p);


            MenuItem myDynamoNoWorkie = new MenuItem
            {
                Header = "My Dynamo is not loading correctly."
            };

            MenuItem jacobSmallSpecial = new MenuItem
            {
                Header = "Invoke the Jacob Small Special™️ ??"
            };
            var img = new System.Windows.Controls.Image
            {
                Source = ImageUtils.LoadImage(Assembly.GetExecutingAssembly(), "smalls.JPG"),
                Height = 32,
                Width = 32,
                Stretch = Stretch.Uniform
            };
            WrapPanel wrapPanel = new WrapPanel();
            wrapPanel.Children.Add(img);
            wrapPanel.Children.Add(jacobSmallSpecial);

            jacobSmallSpecial.Click+= (sender, args) =>
            {
                Process.Start(@"https://forum.dynamobim.com/t/2022-1-latest-revit-update-broke-dynamo/73412/3");
            };

            myDynamoNoWorkie.Items.Add(wrapPanel);

            p.AddMenuItem(MenuBarType.Help, myDynamoNoWorkie);
#endif



            /*if the user has plugins loaded in Revit (or otherwise) that use a toolkit called "DevExpress",
            we fix the overrides that toolkit forces on the app.
            A popular example of this is KiwiCodes Family Browser R3.
            This code will fix it for all of the Dynamo UI.
            */
            Compatibility.CheckForDevExpress();
            Compatibility.FixThemesForDevExpress(p.DynamoWindow);
        }

        

        public void Shutdown()
        {
            //save monocle settings
            Settings.SaveMonocleSettings();
        }
    }
}
