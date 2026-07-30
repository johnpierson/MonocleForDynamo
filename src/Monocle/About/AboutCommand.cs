using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using Dynamo.Wpf.Extensions;
using MonocleViewExtension.Core;
using MonocleViewExtension.Utilities;
using Newtonsoft.Json;

namespace MonocleViewExtension.About
{
    public class AboutCommand
    {
        private static readonly HttpClient Http = CreateClient();

        /// <summary>
        /// Create the about menu
        /// </summary>
        /// <param name="menuItem">monocle menu item</param>
        /// <param name="p">our view loaded parameters for dynamo</param>
        public static void AddMenuItem(MenuItem menuItem, ViewLoadedParams p)
        {
            var viewModel = new AboutViewModel(p);

            var aboutMenu = new MenuItem { Header = Properties.Resources.AboutMenuItemHeader };

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

            /* Look for a newer release in the background. This used to run inline here: a one
               second ICMP ping followed by a synchronous HTTPS call, both on the UI thread, so a
               firewalled or slow network added seconds to Dynamo's startup before the window
               appeared. Nothing depends on the answer, so it can arrive whenever it arrives. */
            _ = ShowUpdateBadgeWhenAvailableAsync(aboutMenu);
        }

        internal static string Latest;

        private static async Task ShowUpdateBadgeWhenAvailableAsync(MenuItem aboutMenu)
        {
            try
            {
                if (!await IsUpdateAvailableAsync("johnpierson", "monoclefordynamo").ConfigureAwait(true))
                {
                    return;
                }

                // ConfigureAwait(true) put us back on the UI thread, but be explicit: this touches
                // a live MenuItem.
                aboutMenu.Dispatcher.Invoke(() =>
                {
                    aboutMenu.Foreground = new SolidColorBrush(Colors.LawnGreen);
                    aboutMenu.Header = $"{Properties.Resources.AboutMenuItemUpdateHeader}{Latest}";
                    aboutMenu.ToolTip = Properties.Resources.AboutMenuItemUpdateTooltip;
                });
            }
            catch (Exception e)
            {
                MonocleContext.Current?.Log.Info($"Update check did not complete: {e.Message}");
            }
        }

        private static async Task<bool> IsUpdateAvailableAsync(string username, string repoName)
        {
            var address = $"https://api.github.com/repos/{username}/{repoName}/releases/latest";

            var body = await Http.GetStringAsync(address).ConfigureAwait(false);

            var json = JsonConvert.DeserializeObject<LatestReleaseVersion>(body);
            if (json?.TagName == null) return false;

            Latest = json.TagName;

            // Releases are CalVer tags. Anything that is not a plain version (a prerelease suffix,
            // a "v" prefix) is not something to prompt the user about.
            if (!Version.TryParse(Latest, out var latestVersion)) return false;
            if (!Version.TryParse(Globals.Version, out var currentVersion)) return false;

            return currentVersion < latestVersion;
        }

        private static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            client.DefaultRequestHeaders.Add("User-Agent", "monocle-for-dynamo");
            return client;
        }

        internal class LatestReleaseVersion
        {
            [JsonProperty("tag_name")]
            public string TagName { get; set; }
        }
    }
}
