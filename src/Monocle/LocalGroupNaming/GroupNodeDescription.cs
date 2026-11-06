using System;
using System.Linq;

namespace MonocleViewExtension.LocalGroupNaming
{
    /// <summary>
    /// Pure string helpers that turn a node's name plus an optional detail (code, a
    /// string value, a note) into one compact line for the naming prompt. Kept free of
    /// Dynamo types so it can be unit tested; <c>GroupNodeDescriber</c> supplies the detail.
    /// </summary>
    internal static class GroupNodeDescription
    {
        public const string DetailSeparator = ": ";
        public const int MaximumLength = 100;
        public const int MaximumDetailLength = 60;
        private const string Ellipsis = "...";

        private static readonly string[] CodeBoilerplatePrefixes =
        {
            "import ", "from ", "clr.", "using ", "#!", "sys.path", "# -*-", "# coding", "#region", "#endregion",
            "# Load", "#Load", "# Import", "#Import", "# The inputs", "#The inputs", "# Assign", "#Assign",
            "# Place your code", "#Place your code", "OUT = ", "OUT=", "dataEnteringNode"
        };

        public static string Compose(string name, string detail)
        {
            var cleanName = Clean(name);
            var cleanDetail = Clean(detail);
            if (cleanName.Length == 0) return cleanDetail.Length == 0 ? null : Truncate(cleanDetail, MaximumLength);
            if (cleanDetail.Length == 0) return Truncate(cleanName, MaximumLength);

            return Truncate(cleanName + DetailSeparator + Truncate(cleanDetail, MaximumDetailLength), MaximumLength);
        }

        public static string Quote(string value)
        {
            var clean = Clean(value);
            return clean.Length == 0 ? null : "\"" + Truncate(clean, MaximumDetailLength - 2) + "\"";
        }

        /// <summary>
        /// Picks the single most descriptive line of a code block or script: the first
        /// human comment when there is one, otherwise the first statement that is not
        /// import boilerplate, otherwise the first non-empty line.
        /// </summary>
        public static string FirstMeaningfulLine(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            var lines = text
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0 && line.Any(char.IsLetterOrDigit))
                .Take(40)
                .ToList();
            if (lines.Count == 0) return null;

            var comment = lines
                .Where(line => !IsBoilerplate(line))
                .Select(StripCommentMarker)
                .FirstOrDefault(body => body != null);
            if (comment != null) return comment;

            var statement = lines.FirstOrDefault(line => !IsBoilerplate(line));
            return TrimStatement(statement ?? lines[0]);
        }

        public static string Clean(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            return string.Join(" ", value
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Replace('\t', ' ')
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        public static string Truncate(string value, int maximumLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maximumLength) return value;
            if (maximumLength <= Ellipsis.Length) return value.Substring(0, maximumLength);

            return value.Substring(0, maximumLength - Ellipsis.Length).TrimEnd() + Ellipsis;
        }

        private static string StripCommentMarker(string line)
        {
            string body = null;
            if (line.StartsWith("//", StringComparison.Ordinal)) body = line.Substring(2);
            else if (line.StartsWith("#", StringComparison.Ordinal)) body = line.Substring(1);
            else if (line.StartsWith("/*", StringComparison.Ordinal)) body = line.Substring(2).Replace("*/", string.Empty);
            else if (line.StartsWith("\"\"\"", StringComparison.Ordinal)) body = line.Substring(3).Replace("\"\"\"", string.Empty);
            else if (line.StartsWith("'''", StringComparison.Ordinal)) body = line.Substring(3).Replace("'''", string.Empty);

            if (body == null) return null;
            body = body.Trim().TrimStart('/', '#', '*', '-', '=', ' ').TrimEnd(';', ' ').Trim();
            return body.Any(char.IsLetterOrDigit) ? body : null;
        }

        private static bool IsBoilerplate(string line)
        {
            return CodeBoilerplatePrefixes.Any(prefix => line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        private static string TrimStatement(string statement)
        {
            var trimmed = statement.TrimEnd(';', ' ', '\t');
            return trimmed.Length == 0 ? statement : trimmed;
        }
    }
}
