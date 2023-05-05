using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MonocleViewExtension.Utilities
{
    internal static class ImageUtils
    {
        public static BitmapImage LoadImage(Assembly a, string name)
        {
            var img = new BitmapImage();
            try
            {
                var resourceName = a.GetManifestResourceNames().FirstOrDefault(x => x.Contains(name));
                var stream = a.GetManifestResourceStream(resourceName);

                img.BeginInit();
                img.StreamSource = stream;
                img.EndInit();
            }
            catch (Exception)
            {
                // ignored
            }

            return img;
        }
    }
    public static class MiscUtils
    {
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        public static bool CheckForInternetConnection()
        {
            try
            {
                Ping myPing = new Ping();
                String host = "google.com";
                byte[] buffer = new byte[32];
                int timeout = 1000;
                PingOptions pingOptions = new PingOptions();
                PingReply reply = myPing.Send(host, timeout, buffer, pingOptions);
                return (reply.Status == IPStatus.Success);
            }
            catch (Exception)
            {
                return false;
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
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            try
            {
                Globals.DevExpress = assemblies.First(a => a.FullName.Contains("DevExpress.Xpf.Core"));
            }
            catch (Exception)
            {
                Globals.DevExpress = null;
            }

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
                var objType = Enumerable.First<Type>(GetTypesSafely(Globals.DevExpress),
                    t => t.Name.Equals("ThemeManager"));

                object baseObject = System.Runtime.Serialization.FormatterServices
                    .GetUninitializedObject(objType);

                objType.InvokeMember("SetThemeName",
                    BindingFlags.Default | BindingFlags.InvokeMethod, null, baseObject, new object[] { window, "None" });
            }
            catch (Exception)
            {
                //do nothing
            }
        }

        private static IEnumerable<Type> GetTypesSafely(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(x => x != null);
            }
        }
    }
}
