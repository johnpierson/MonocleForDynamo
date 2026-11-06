using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonocleViewExtension.LocalGroupNaming
{
    internal static class GroupNameValidator
    {
        public const int MaximumCharacters = 60;
        public const int MaximumWords = GroupNameGrammar.MaximumWords;

        private static readonly string[] GenericNames =
        {
            "Group", "Dynamo Group", "New Group", "Node Group", "Group Name", "Group Title",
            "Untitled", "Untitled Group", "Nodes", "Workflow"
        };

        private static readonly string[] LabelPrefixes = { "Title:", "Group name:", "Name:", "Answer:" };
        private static readonly char[] TrimCharacters = { '`', '"', '\'', '.', ':', ';', '-', '*', '#', ' ', '\t' };
        private static readonly char[] StructuralCharacters = { '{', '}', '[', ']', '<', '>' };

        public static bool IsApiStyleIdentifier(string groupName)
        {
            return !string.IsNullOrWhiteSpace(groupName) &&
                   (groupName.IndexOf('.') >= 0 ||
                    groupName.IndexOf("::", StringComparison.Ordinal) >= 0 ||
                    groupName.IndexOf('_') >= 0);
        }

        /// <summary>
        /// True when the proposal is one of the group's own node names, compared without
        /// case, spaces or punctuation so "Point By Coordinates" still matches
        /// "Point.ByCoordinates". Node descriptions such as "Code Block: x = 5" are
        /// compared both whole and by their node name alone.
        /// </summary>
        public static bool MatchesNodeName(string groupName, IEnumerable<string> nodeNames)
        {
            if (string.IsNullOrWhiteSpace(groupName) || nodeNames == null) return false;

            var candidate = NormalizeForComparison(groupName);
            if (candidate.Length == 0) return false;

            foreach (var nodeName in nodeNames)
            {
                if (string.IsNullOrWhiteSpace(nodeName)) continue;
                if (candidate == NormalizeForComparison(nodeName)) return true;

                var separator = nodeName.IndexOf(GroupNodeDescription.DetailSeparator, StringComparison.Ordinal);
                if (separator > 0 && candidate == NormalizeForComparison(nodeName.Substring(0, separator))) return true;
            }

            return false;
        }

        public static string NormalizeForComparison(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            var builder = new StringBuilder(value.Length);
            foreach (var character in value)
            {
                if (char.IsLetterOrDigit(character)) builder.Append(char.ToLowerInvariant(character));
            }

            return builder.ToString();
        }

        public static bool TryNormalize(string response, out string groupName, out string error)
        {
            groupName = null;
            error = null;

            if (string.IsNullOrWhiteSpace(response))
            {
                error = "The local model returned an empty name.";
                return false;
            }

            var firstLine = response
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .FirstOrDefault(line => line.Length > 0);

            if (string.IsNullOrWhiteSpace(firstLine))
            {
                error = "The local model returned an empty name.";
                return false;
            }

            var normalized = firstLine.Trim(TrimCharacters);
            foreach (var prefix in LabelPrefixes)
            {
                if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    normalized = normalized.Substring(prefix.Length).Trim(TrimCharacters);
                    break;
                }
            }

            normalized = string.Join(" ", normalized.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));

            if (normalized.Length == 0 || normalized.Length > MaximumCharacters)
            {
                error = $"The suggested name must contain between 1 and {MaximumCharacters} characters.";
                return false;
            }

            if (normalized.IndexOfAny(StructuralCharacters) >= 0)
            {
                error = "The local model returned structured or unsafe text instead of a title.";
                return false;
            }

            var words = normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0 || words.Length > MaximumWords)
            {
                error = $"The suggested name must contain no more than {MaximumWords} words.";
                return false;
            }

            if (GenericNames.Any(generic => string.Equals(normalized, generic, StringComparison.OrdinalIgnoreCase)))
            {
                error = "The local model returned a generic group name.";
                return false;
            }

            if (char.IsLower(normalized[0]))
            {
                normalized = char.ToUpperInvariant(normalized[0]) + normalized.Substring(1);
            }

            groupName = normalized;
            return true;
        }
    }
}
