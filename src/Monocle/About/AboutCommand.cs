using System;
using System.Net;
using System.Windows.Controls;
using System.Windows.Media;
using Dynamo.Wpf.Extensions;
using MonocleViewExtension.Utilities;
using Newtonsoft.Json;

namespace MonocleViewExtension.About
{
    public class AboutCommand
    {
        /// <summary>
        /// Create the about menu
        /// </summary>
        /// <param name="menuItem">monocle menu item</param>
        /// <param name="p">our view loaded parameters for dynamo</param>
        public static void AddMenuItem(MenuItem menuItem, ViewLoadedParams p)
        {

            var viewModel = new AboutViewModel(p);

            var aboutMenu = new MenuItem{Header = Properties.Resources.ResourceManager.GetString("AboutMenuItemHeader") };

            aboutMenu.Click += (sender, args) =>
            {
                var window = new AboutView
                {
                    // Set the data context for the main grid in the window.
                    MainGrid = { DataContext = viewModel },
                    // Set the owner of the window to the Dynamo window.
                    Owner = p.DynamoWindow
                };

                window.Show();
            };

            //add the about menu and a separator
            menuItem.Items.Add(aboutMenu);
            menuItem.Items.Add(new Separator());


            //check to see if an update is available
            if (MiscUtils.CheckForInternetConnection() && CheckForUpdate("johnpierson","monoclefordynamo"))
            {
                aboutMenu.Foreground = new SolidColorBrush(Colors.LawnGreen);
                aboutMenu.Header = $"{Properties.Resources.ResourceManager.GetString("AboutMenuItemUpdateHeader")}{Latest}";
                aboutMenu.ToolTip = Properties.Resources.ResourceManager.GetString("AboutMenuItemUpdateTooltip");
            }

        }

        internal static string Latest;
        private static bool CheckForUpdate(string username, string repoName)
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.Headers.Add("User-Agent", "Unity web player");

                    string address = $"https://api.github.com/repos/{username}/{repoName}/releases/latest";
                    Uri uri = new Uri(address);

                    string releases = webClient.DownloadString(uri);

                    var json = JsonConvert.DeserializeObject<LatestReleaseVersion>(releases);

                    Latest = json.TagName;

                    Version currentVersion = Version.Parse(Globals.Version);

                    Version latestVersion = Version.Parse($"{Latest}");

                    return currentVersion.CompareTo(latestVersion) <= 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
            
        }

        internal class LatestReleaseVersion
        {
            [JsonProperty("tag_name")]
            public string TagName { get; set; }
        }
    }
}
