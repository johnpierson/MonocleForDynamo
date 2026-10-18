using System;

namespace MonocleViewExtension.SimpleSearch
{
    /// <summary>Normalization used by Simple Search (pure, testable).</summary>
    internal static class SearchKey
    {
        /// <summary>Upper-case, with dots and spaces removed ("List.Create" and "list create" both become "LISTCREATE").</summary>
        internal static string Normalize(string value) =>
            (value ?? string.Empty).ToUpperInvariant().Replace(".", "").Replace(" ", "");

        /// <summary>Precomputed-key match. An empty filter matches nothing (the list stays empty until the user types).</summary>
        internal static bool Matches(string normalizedKey, string normalizedFilter) =>
            !string.IsNullOrEmpty(normalizedFilter) &&
            normalizedKey != null &&
            normalizedKey.IndexOf(normalizedFilter, StringComparison.Ordinal) >= 0;
    }
}
