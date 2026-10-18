using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace MonocleViewExtension.About
{
    /// <summary>
    /// Pure helpers for the once-a-day "is there a newer Monocle?" check (no Dynamo/WPF types so they can be tested).
    /// </summary>
    internal static class UpdateCheck
    {
        internal static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

        private static readonly Regex TagNamePattern = new Regex("\"tag_name\"\\s*:\\s*\"(?<tag>[^\"]+)\"", RegexOptions.CultureInvariant);

        /// <summary>Extract "tag_name" from a GitHub releases/latest payload without a JSON library dependency.</summary>
        internal static string ParseTagName(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            var match = TagNamePattern.Match(json);
            return match.Success ? match.Groups["tag"].Value : null;
        }

        /// <summary>
        /// True only when <paramref name="latestTag"/> is strictly newer than <paramref name="currentVersion"/>.
        /// Both are compared as Major.Minor.Build, so "2026.6.1" equals assembly version "2026.6.1.0".
        /// </summary>
        internal static bool IsNewer(string currentVersion, string latestTag)
        {
            var current = Normalize(currentVersion);
            var latest = Normalize(latestTag);
            return current != null && latest != null && latest > current;
        }

        internal static bool IsDue(DateTime? lastCheckUtc, DateTime nowUtc) =>
            lastCheckUtc == null || nowUtc - lastCheckUtc.Value >= CheckInterval || lastCheckUtc.Value > nowUtc;

        internal static Version Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            value = value.Trim().TrimStart('v', 'V');
            if (!Version.TryParse(value, out var parsed)) return null;
            return new Version(parsed.Major, parsed.Minor, Math.Max(parsed.Build, 0));
        }

        /// <summary>Cache format: "&lt;ISO-8601 UTC check time&gt;|&lt;latest tag&gt;".</summary>
        internal static bool TryReadCache(string path, out DateTime lastCheckUtc, out string latestTag)
        {
            lastCheckUtc = default(DateTime);
            latestTag = null;
            try
            {
                if (!File.Exists(path)) return false;
                var parts = File.ReadAllText(path).Trim().Split(new[] { '|' }, 2);
                if (parts.Length != 2) return false;
                if (!DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out lastCheckUtc)) return false;
                latestTag = parts[1];
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal static void WriteCache(string path, DateTime checkedUtc, string latestTag)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, $"{checkedUtc.ToString("o", CultureInfo.InvariantCulture)}|{latestTag}");
            }
            catch (Exception)
            {
                // best effort only
            }
        }
    }
}
