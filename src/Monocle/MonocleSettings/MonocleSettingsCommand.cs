using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Dynamo.Wpf.Extensions;
using Microsoft.Win32;
using MonocleViewExtension.Utilities;

namespace MonocleViewExtension.MonocleSettings
{
    internal class MonocleSettingsCommand
    {
        public static void AddMenuItem(MenuItem menuItem)
        {
            menuItem.Items.Add(new Separator());

            var settingsFlyout = new MenuItem { Header = "monocle settings" };

            var saveSettings = new MenuItem { Header = "save current settings to default path" };

            saveSettings.Click += (sender, args) =>
            {
                Settings.SaveMonocleSettings();
            };
            settingsFlyout.Items.Add(saveSettings);



            var loadSettings = new MenuItem{ Header = "load settings from path" };

            loadSettings.Click += (sender, args) =>
            {

                System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog { Filter = "XML Files (*.xml) | *.xml" };

                if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

                Globals.SettingsFile = openFileDialog.FileName;

                Settings.LoadMonocleSettings();
            };
            settingsFlyout.Items.Add(loadSettings);

            settingsFlyout.Items.Add(new Separator());
            var restoreSettings = new MenuItem { Header = "restore default settings" };

            restoreSettings.Click += (sender, args) =>
            {
                Globals.MonocleGroupSettings = Globals.DefaultGroupSettings;
                Settings.SaveMonocleSettings();
            };
            settingsFlyout.Items.Add(restoreSettings);


            //add the about menu and a separator
            menuItem.Items.Add(settingsFlyout);
            

        }
    }
}
