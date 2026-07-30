using System.Windows.Controls;
using MonocleViewExtension.Core;

namespace MonocleViewExtension.MonocleSettings
{
    internal class MonocleSettingsCommand
    {
        /// <summary>
        /// Create the monocle settings menu with flyouts.
        /// </summary>
        public static void AddMenuItem(MenuItem menuItem, MonocleContext ctx)
        {
            menuItem.Items.Add(new Separator());

            var settingsFlyout = new MenuItem { Header = Properties.Resources.SettingsMenuItemHeader };

            var saveSettings = new MenuItem { Header = Properties.Resources.SettingsSaveMenuItemHeader };
            saveSettings.Click += (sender, args) => ctx.Settings.Save();
            settingsFlyout.Items.Add(saveSettings);

            var loadSettings = new MenuItem { Header = Properties.Resources.SettingsLoadMenuItemHeader };
            loadSettings.Click += (sender, args) =>
            {
                var openFileDialog = new System.Windows.Forms.OpenFileDialog { Filter = "XML Files (*.xml) | *.xml" };
                if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

                ctx.Settings.SettingsFilePath = openFileDialog.FileName;

                if (ctx.Settings.Load())
                {
                    RememberSettingsFile(ctx.Settings.SettingsFilePath);
                }
            };
            settingsFlyout.Items.Add(loadSettings);

            settingsFlyout.Items.Add(new Separator());

            var restoreSettings = new MenuItem { Header = Properties.Resources.SettingsRestoreMenuItemHeader };
            restoreSettings.Click += (sender, args) =>
            {
                ctx.Settings.RestoreDefaults();
                ctx.Settings.Save();
            };
            settingsFlyout.Items.Add(restoreSettings);

            menuItem.Items.Add(settingsFlyout);
        }

        private static void RememberSettingsFile(string path)
        {
            Properties.UserSettings.Default.MonocleSettingsFile = path;
            Properties.UserSettings.Default.Save();
        }
    }
}
