using Dynamo.Controls;
using Dynamo.ViewModels;
using Dynamo.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MonocleViewExtension.Utilities
{
    internal static class NodeUtils
    {
        private static WeakReference<WorkspaceView> _cachedWorkspaceView;

        /// <summary>
        /// The workspace view currently hosted by the Dynamo window. Cached, and only re-discovered when the
        /// cached one is no longer loaded inside this window, so callers don't walk the whole window every time.
        /// </summary>
        internal static WorkspaceView GetWorkspaceView(DependencyObject dynamoView)
        {
            if (dynamoView == null) return null;

            if (_cachedWorkspaceView != null && _cachedWorkspaceView.TryGetTarget(out var cached) &&
                cached.IsLoaded && cached.IsVisible && cached.IsDescendantOf(dynamoView))
            {
                return cached;
            }

            var found = dynamoView.FindVisualChild<WorkspaceView>(w => w.IsVisible) ?? dynamoView.FindVisualChild<WorkspaceView>();
            _cachedWorkspaceView = found == null ? null : new WeakReference<WorkspaceView>(found);
            return found;
        }

        /// <summary>
        /// Node views in the current workspace. Walks only the workspace (not the library, sidebars or 3D view)
        /// and never descends into a node view's own template.
        /// </summary>
        internal static List<NodeView> GetNodeViews(DependencyObject dynamoView)
        {
            var root = (DependencyObject)GetWorkspaceView(dynamoView) ?? dynamoView;
            return root.FindVisualChildren<NodeView>(descendIntoMatches: false).ToList();
        }

        internal static Rect GetBoundingRectangle(List<NodeView> nodeViews)
        {
            List<double> yAxis = new List<double>();
            List<double> xAxis = new List<double>();

            foreach (var nv in nodeViews)
            {
                NodeViewModel nvm = nv.DataContext as NodeViewModel;

                if (Globals.DynamoVersion.CompareTo(Globals.NewUiVersion) >= 0)
                {
                    var border = nv.FindVisualChildren<Border>().FirstOrDefault(n => n.Name.ToLower().Equals("selectionborder"));

                    var geo = border.RenderSize;

                    xAxis.Add(nvm.Left);
                    xAxis.Add(nvm.Left + geo.Width);
                    yAxis.Add(nvm.Top);
                    yAxis.Add(nvm.Top + geo.Height);
                }
                else
                {
                    var border = (System.Windows.Shapes.Rectangle)nv.FindName("selectionBorder");
                    var geo = border.RenderedGeometry.Bounds;

                    xAxis.Add(nvm.Left);
                    xAxis.Add(nvm.Left + geo.Width);
                    yAxis.Add(nvm.Top);
                    yAxis.Add(nvm.Top + geo.Height);
                }
            }

            double xMin = xAxis.Min() - 14;
            double yMin = yAxis.Min() - 14;
            double xMax = xAxis.Max() + 14;
            double yMax = yAxis.Max() + 14;
            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }
    }
    internal static class ImageUtils
    {
        /// <summary>
        /// Load a WPF resource (Build Action: Resource) from this assembly, e.g. "Resources/smalls.JPG".
        /// The previous implementation looked in manifest resources, where these images don't live, so it always failed.
        /// </summary>
        public static BitmapImage LoadImage(Assembly a, string relativePath)
        {
            try
            {
                var assemblyName = a.GetName().Name;
                var img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri($"pack://application:,,,/{assemblyName};component/{relativePath.TrimStart('/')}", UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                img.Freeze();
                return img;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
    public static class StringUtils
    {
        public static string SetCustomNodeNotePrefix(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                return "Custom Node: ";
            }

            return !Char.IsWhiteSpace(prefix[prefix.Length - 1]) ? $"{prefix} " : prefix;
        }

        public static string SimplifyString(this string str)
        {
            return str.ToLower().Replace(" ", "").Replace(".", "");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static int Compute(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];
            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            // Step 7
            return d[n, m];
        }
        public static string CleanupString(this string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    sb.Append(c);
                }
            }
            return sb.ToString().ToLower();
        }
    }
    public static class Compatibility
    {
        /// <summary>
        /// Check if DevExpress is loaded (typically for KiwiCodes)
        /// </summary>
        public static void CheckForDevExpress()
        {
            // DevExpress.Xpf.Core.v22.1 etc. (not DevExpress.Xpf.Core.Extensions.*)
            Globals.DevExpress = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => System.Text.RegularExpressions.Regex.IsMatch(a.GetName().Name ?? string.Empty,
                    @"^DevExpress\.Xpf\.Core(\.v\d+\.\d+)?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase));

            Globals.IsDevExpressLoaded = Globals.DevExpress != null;
        }

        /// <summary>
        /// This allows us to fix our UI when a user has KiwiCodes Family Browser loaded
        /// </summary>
        /// <param name="window"></param>
        public static void FixThemesForDevExpress(Window window)
        {
            if (Globals.DevExpress is null) return;

            try
            {
                // ThemeManager.SetThemeName(DependencyObject, string) is a static attached-property setter.
                // Look the type up by name instead of enumerating every type in DevExpress.Xpf.Core.
                var themeManager = Globals.DevExpress.GetType("DevExpress.Xpf.Core.ThemeManager", false);
                var setThemeName = themeManager?.GetMethod("SetThemeName", BindingFlags.Public | BindingFlags.Static,
                    null, new[] { typeof(DependencyObject), typeof(string) }, null);
                setThemeName?.Invoke(null, new object[] { window, "None" });
            }
            catch (Exception)
            {
                //do nothing
            }
        }
    }
}
