using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Dynamo.Wpf.Extensions;
using MonocleViewExtension.Utilities;

namespace MonocleViewExtension.About
{
    public class AboutCommand
    {
        private const string LatestReleaseUrl = "https://api.github.com/repos/johnpierson/monoclefordynamo/releases/latest";

        private static readonly string UpdateCachePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Monocle", "update-check.txt");

        /// <summary>
        /// Create the about menu
        /// </summary>
        /// <param name="menuItem">monocle menu item</param>
        /// <param name="p">our view loaded parameters for dynamo</param>
        public static void AddMenuItem(MenuItem menuItem, ViewLoadedParams p)
        {

            var viewModel = new AboutViewModel(p);

            var aboutMenu = new MenuItem{Header = Properties.Resources.AboutMenuItemHeader };

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

            // Check for an update off the UI thread (it used to block Dynamo startup on a ping + GitHub call).
            var dispatcher = aboutMenu.Dispatcher;
            Task.Run(() => GetLatestTagAsync()).ContinueWith(task =>
            {
                var latest = task.Status == TaskStatus.RanToCompletion ? task.Result : null;
                if (latest == null || !UpdateCheck.IsNewer(Globals.Version, latest)) return;

                dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    Latest = latest;
                    aboutMenu.Foreground = new SolidColorBrush(Colors.LawnGreen);
                    aboutMenu.Header = $"{Properties.Resources.AboutMenuItemUpdateHeader}{Latest}";
                    aboutMenu.ToolTip = Properties.Resources.AboutMenuItemUpdateTooltip;
                }));
            }, TaskScheduler.Default);
        }

        internal static string Latest;

        /// <summary>
        /// Latest release tag, from a once-a-day cache when possible (unauthenticated GitHub API calls are rate-limited
        /// per IP, which a whole office shares). Never throws.
        /// </summary>
        private static async Task<string> GetLatestTagAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                if (UpdateCheck.TryReadCache(UpdateCachePath, out var lastCheck, out var cachedTag) && !UpdateCheck.IsDue(lastCheck, now))
                {
                    return cachedTag;
                }

                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) })
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("MonocleForDynamo");
                    var json = await client.GetStringAsync(LatestReleaseUrl).ConfigureAwait(false);
                    var tag = UpdateCheck.ParseTagName(json);
                    if (tag != null) UpdateCheck.WriteCache(UpdateCachePath, now, tag);
                    return tag;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
