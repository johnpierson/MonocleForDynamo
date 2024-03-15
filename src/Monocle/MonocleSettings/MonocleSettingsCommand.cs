﻿using System.Windows.Controls;
using MonocleViewExtension.Utilities;

namespace MonocleViewExtension.MonocleSettings
{
    internal class MonocleSettingsCommand
    {
        /// <summary>
        /// Create the monocle settings menu with flyouts.
        /// </summary>
        /// <param name="menuItem">monocle menu item</param>
        public static void AddMenuItem(MenuItem menuItem)
        {
            menuItem.Items.Add(new Separator());

            var settingsFlyout = new MenuItem { Header = Properties.Resources.SettingsMenuItemHeader };

            var saveSettings = new MenuItem { Header = Properties.Resources.SettingsSaveMenuItemHeader };

            saveSettings.Click += (sender, args) =>
            {
                Settings.SaveMonocleSettings();
            };
            settingsFlyout.Items.Add(saveSettings);



            var loadSettings = new MenuItem{ Header = Properties.Resources.SettingsLoadMenuItemHeader };

            loadSettings.Click += (sender, args) =>
            {

                System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog { Filter = "XML Files (*.xml) | *.xml" };

                if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

                Globals.SettingsFile = openFileDialog.FileName;

                Settings.LoadMonocleSettings();
            };
            settingsFlyout.Items.Add(loadSettings);

            settingsFlyout.Items.Add(new Separator());
            var restoreSettings = new MenuItem { Header = Properties.Resources.SettingsRestoreMenuItemHeader };

            restoreSettings.Click += (sender, args) =>
            {
                Globals.MonocleGroupSettings = Globals.DefaultGroupSettings;
                Settings.SaveMonocleSettings();
                Settings.LoadMonocleSettings();
            };
            settingsFlyout.Items.Add(restoreSettings);


            //add the about menu and a separator
            menuItem.Items.Add(settingsFlyout);
            

        }
    }
}
