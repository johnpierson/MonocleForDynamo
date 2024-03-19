using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Dynamo.Logging;
using Dynamo.PackageManager;
using Dynamo.Wpf.Extensions;
using Dynamo.Wpf.Utilities;
using MonocleViewExtension.About;
using MonocleViewExtension.BetterSave;
using MonocleViewExtension.FancyPaste;
using MonocleViewExtension.Foca;
using MonocleViewExtension.GraphResizerer;
using MonocleViewExtension.MonocleSettings;
using MonocleViewExtension.NodeDocumentation;
using MonocleViewExtension.NodeSwapper;
using MonocleViewExtension.PackageUsage;
using MonocleViewExtension.SimpleSearch;
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

            //for testing localization
#if DEBUG
            //Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
#endif
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

            //check if the last used settings file exists, if so, use it instead of the defaults.
            if (File.Exists(Properties.UserSettings.Default.MonocleSettingsFile))
            {
                Globals.SettingsFile = Properties.UserSettings.Default.MonocleSettingsFile;
            }
            else
            {
                Dynamo.Logging.LogMessage.Warning($"Failed to load settings file from, {Properties.UserSettings.Default.MonocleSettingsFile}. Please check if the file exists. Using default settings instead,", WarningLevel.Mild);
            }
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
            NodeSwapperCommand.AddMenuItem(monocleMenuItem, p);
            FocaCommand.EnableFoca(p, monocleMenuItem);
            SimpleSearchCommand.AddMenuItem(p, monocleMenuItem, this);
            StandardViewsCommand.EnableStandardViews(p);
            MonocleSettingsCommand.AddMenuItem(monocleMenuItem);
            FancyPasteCommand.AddMenuItem(p);
            BetterSaveCommand.AddMenuItem(p);
            ScaffoldTheJacobSmallSpecial(p);

            NodeDocumentationCommand.AddMenuItem(monocleMenuItem,p);


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

        internal void ScaffoldTheJacobSmallSpecial(ViewLoadedParams p)
        {
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

            jacobSmallSpecial.Click += (sender, args) =>
            {
                Process.Start(@"https://forum.dynamobim.com/t/2022-1-latest-revit-update-broke-dynamo/73412/3");
            };

            myDynamoNoWorkie.Items.Add(wrapPanel);

            //in Dynamo 2.18+, the team decided I can't add things to the help menu the old way, this fixes that.
            var allMenus = p.dynamoMenu.Items.OfType<MenuItem>();
            var helpMenu = allMenus.FirstOrDefault(m => m.Name.Equals("HelpMenu"));

            helpMenu?.Items.Add(myDynamoNoWorkie);
        }
    }
}
