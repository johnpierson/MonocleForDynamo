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

        /// <summary>Comment lines of Dynamo's stock Python node templates, matched whole.</summary>
        private static readonly string[] TemplateComments =
        {
            "load the python standard and designscript libraries",
            "enable python support and load designscript library",
            "the inputs to this node will be stored as a list in the in variables",
            "place your code below this line",
            "assign your output to the out variable"
        };

        /// <summary>Comment lines that are machinery rather than prose.</summary>
        private static readonly string[] MachineryCommentPrefixes = { "#!", "# -*-", "#-*-", "# coding", "#coding", "#region", "#endregion" };

        /// <summary>Statements that are import or template plumbing, matched by prefix.</summary>
        private static readonly string[] BoilerplateStatementPrefixes = { "import ", "from ", "clr.", "using ", "sys.path" };

        /// <summary>Template statements matched whole after whitespace normalisation.</summary>
        private static readonly string[] BoilerplateStatements = { "out = 0", "out = in", "dataenteringnode = in" };

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
        /// Picks the single most descriptive line of a code block or script: the first human
        /// comment that is not part of Dynamo's template, otherwise the first statement that is
        /// not import or template plumbing, otherwise nothing (so a stock, unedited Python node
        /// is described by its name alone).
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
                .Select(StripCommentMarker)
                .FirstOrDefault(body => body != null && !IsTemplateComment(body));
            if (comment != null) return comment;

            var statement = lines.FirstOrDefault(line => !IsCommentLine(line) && !IsBoilerplateStatement(line));
            return statement == null ? null : TrimStatement(statement);
        }

        private static bool IsCommentLine(string line)
        {
            return line.StartsWith("#", StringComparison.Ordinal) ||
                   line.StartsWith("//", StringComparison.Ordinal) ||
                   line.StartsWith("/*", StringComparison.Ordinal) ||
                   line.StartsWith("\"\"\"", StringComparison.Ordinal) ||
                   line.StartsWith("'''", StringComparison.Ordinal);
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

        /// <summary>The prose of a comment line, or null when the line is not a comment (or is comment machinery).</summary>
        private static string StripCommentMarker(string line)
        {
            if (MachineryCommentPrefixes.Any(prefix => line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))) return null;

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

        private static bool IsTemplateComment(string commentBody)
        {
            var normalized = Clean(commentBody).TrimEnd('.', ' ').ToLowerInvariant();
            return TemplateComments.Any(template => string.Equals(normalized, template, StringComparison.Ordinal));
        }

        private static bool IsBoilerplateStatement(string line)
        {
            if (BoilerplateStatementPrefixes.Any(prefix => line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))) return true;

            var normalized = Clean(line).TrimEnd(';', ' ').ToLowerInvariant();
            return BoilerplateStatements.Any(statement => string.Equals(normalized, statement, StringComparison.Ordinal));
        }

        private static string TrimStatement(string statement)
        {
            var trimmed = statement.TrimEnd(';', ' ', '\t');
            return trimmed.Length == 0 ? statement : trimmed;
        }
    }
}
