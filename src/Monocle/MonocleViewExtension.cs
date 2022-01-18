using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Dynamo.Wpf.Extensions;
using MonocleViewExtension.About;
using MonocleViewExtension.Foca;
using MonocleViewExtension.MonocleSettings;
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
            //resolve the dynamo version by checking which core is loaded
            var dsCore = AppDomain.CurrentDomain.GetAssemblies().First(a => a.FullName.Contains("DSCoreNodes"));
            Globals.DynamoVersion = dsCore.GetName().Version;

            //load monocle settings from xml
            Settings.LoadMonocleSettings();
        }

        public void Loaded(ViewLoadedParams p)
        {
            //add the top-level menu
            var monocleMenuItem = new MenuItem { Header = "🧐 monocle" };
            //add the top level menu to the dynamo ribbon
            p.dynamoMenu.Items.Insert(6, monocleMenuItem);

            AboutCommand.AddMenuItem(monocleMenuItem,p);

            PackageUsageCommand.AddMenuItem(monocleMenuItem,p);

            

            FocaCommand.EnableFoca(p, monocleMenuItem);

            SimpleSearchCommand.AddMenuItem(p, monocleMenuItem, this);

            StandardViewsCommand.EnableStandardViews(p);

            MonocleSettingsCommand.AddMenuItem(monocleMenuItem);

        }

        public void Shutdown()
        {
            //save monocle settings
            Settings.SaveMonocleSettings();
        }

        
    }
}
