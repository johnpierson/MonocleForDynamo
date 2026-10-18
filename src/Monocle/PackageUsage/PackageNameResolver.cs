using System;
using System.Collections.Generic;

namespace MonocleViewExtension.PackageUsage
{
    /// <summary>
    /// Resolves node categories to package names. Built once per operation from the library's
    /// package roots so that per-node work no longer rescans the whole search library.
    /// Pure logic (no Dynamo types) so it can be regression tested.
    /// </summary>
    internal sealed class PackageNameResolver
    {
        private readonly List<string> _roots;
        private readonly Dictionary<string, bool> _isCustomByCategory = new Dictionary<string, bool>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _nameByTopCategory = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _versionBySimpleName;

        /// <param name="libraryRoots">Top-level categories of packaged/custom library entries, in library order (duplicates allowed).</param>
        /// <param name="localPackages">Installed packages as (name, versionName) pairs. May be null.</param>
        internal PackageNameResolver(IEnumerable<string> libraryRoots, IEnumerable<KeyValuePair<string, string>> localPackages)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            _roots = new List<string>();
            _versionBySimpleName = new Dictionary<string, string>(StringComparer.Ordinal);

            if (libraryRoots != null)
            {
                foreach (var root in libraryRoots)
                {
                    if (!string.IsNullOrEmpty(root) && seen.Add(root)) _roots.Add(root);
                }
            }

            if (localPackages != null)
            {
                foreach (var package in localPackages)
                {
                    if (string.IsNullOrEmpty(package.Key)) continue;
                    if (seen.Add(package.Key)) _roots.Add(package.Key);

                    var simple = Simplify(package.Key);
                    if (!_versionBySimpleName.ContainsKey(simple)) _versionBySimpleName[simple] = package.Value;
                }
            }
        }

        internal IReadOnlyList<string> Roots => _roots;

        /// <summary>True when any package root is a case-insensitive prefix of the category.</summary>
        internal bool IsCustomCategory(string category)
        {
            category = category ?? string.Empty;
            if (_isCustomByCategory.TryGetValue(category, out var cached)) return cached;

            var result = false;
            foreach (var root in _roots)
            {
                if (category.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                {
                    result = true;
                    break;
                }
            }

            _isCustomByCategory[category] = result;
            return result;
        }

        /// <summary>Closest package root (Levenshtein) to the category's first segment; first root wins ties.</summary>
        internal string GetPackageName(string category)
        {
            var top = (category ?? string.Empty).Split('.')[0];
            if (_nameByTopCategory.TryGetValue(top, out var cached)) return cached;

            string best = null;
            var bestDistance = int.MaxValue;
            foreach (var root in _roots)
            {
                var distance = Levenshtein(top, root);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = root;
                }
            }

            var result = FixKnownNames(best ?? string.Empty);
            _nameByTopCategory[top] = result;
            return result;
        }

        /// <summary>"v.{version}" for an installed package matching the resolved name, otherwise empty.</summary>
        internal string GetPackageVersion(string packageName)
        {
            if (string.IsNullOrEmpty(packageName)) return string.Empty;
            return _versionBySimpleName.TryGetValue(Simplify(packageName), out var version) && version != null
                ? $"v.{version}"
                : string.Empty;
        }

        internal static string FixKnownNames(string name)
        {
            switch (name.ToLowerInvariant())
            {
                case "springs": return "spring nodes";
                case "archilab_bumblebee": return "BumbleBee";
                default: return name;
            }
        }

        internal static string Simplify(string value) => (value ?? string.Empty).ToLower().Replace(" ", "").Replace(".", "");

        /// <summary>Two-row Levenshtein distance.</summary>
        internal static int Levenshtein(string s, string t)
        {
            s = s ?? string.Empty;
            t = t ?? string.Empty;
            if (s.Length == 0) return t.Length;
            if (t.Length == 0) return s.Length;

            var previous = new int[t.Length + 1];
            var current = new int[t.Length + 1];
            for (var j = 0; j <= t.Length; j++) previous[j] = j;

            for (var i = 1; i <= s.Length; i++)
            {
                current[0] = i;
                for (var j = 1; j <= t.Length; j++)
                {
                    var cost = t[j - 1] == s[i - 1] ? 0 : 1;
                    current[j] = Math.Min(Math.Min(previous[j] + 1, current[j - 1] + 1), previous[j - 1] + cost);
                }

                var swap = previous;
                previous = current;
                current = swap;
            }

            return previous[t.Length];
        }
    }
}
