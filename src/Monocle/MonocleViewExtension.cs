using System;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Input;
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

        }

        public void Loaded(ViewLoadedParams p)
        {
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
