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
        public const int MinimumWords = GroupNameGrammar.MinimumWords;

        /// <summary>
        /// Words that carry no information about a group. A title made only of these
        /// ("Dynamo Node Group", "Untitled Group") is rejected; a title that merely contains
        /// one ("Group Walls By Type") is fine.
        /// </summary>
        private static readonly HashSet<string> GenericWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "group", "groups", "node", "nodes", "workflow", "graph", "dynamo", "script", "untitled",
            "new", "name", "title", "of", "the", "for", "a", "an", "and", "this", "these", "some"
        };

        private static readonly string[] LabelPrefixes = { "Title:", "Group name:", "Name:", "Answer:" };
        private static readonly char[] TrimCharacters = { '`', '"', '\'', '.', ':', ';', '-', '*', '#', ' ', '\t' };
        private static readonly char[] StructuralCharacters = { '{', '}', '[', ']', '<', '>' };

        /// <summary>
        /// True for API-style text: dotted or underscored identifiers, and camel-case words
        /// such as "GetParameterValueByName" that a model produces when it copies a node name
        /// but the grammar has stripped the punctuation.
        /// </summary>
        public static bool IsApiStyleIdentifier(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName)) return false;

            if (groupName.IndexOf('.') >= 0 ||
                groupName.IndexOf("::", StringComparison.Ordinal) >= 0 ||
                groupName.IndexOf('_') >= 0)
            {
                return true;
            }

            foreach (var word in groupName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                for (var index = 1; index < word.Length; index++)
                {
                    if (char.IsLower(word[index - 1]) && char.IsUpper(word[index])) return true;
                }
            }

            return false;
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

            if (normalized.IndexOfAny(StructuralCharacters) >= 0)
            {
                error = "The local model returned structured or unsafe text instead of a title.";
                return false;
            }

            var words = normalized.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (words.Count == 0)
            {
                error = "The local model returned an empty name.";
                return false;
            }

            if (words.Count > MaximumWords)
            {
                error = $"The suggested name must contain no more than {MaximumWords} words.";
                return false;
            }

            // The grammar allows longer titles than a group header can show; shorten by whole
            // words rather than rejecting an otherwise good proposal.
            while (string.Join(" ", words).Length > MaximumCharacters && words.Count > MinimumWords)
            {
                words.RemoveAt(words.Count - 1);
            }

            normalized = string.Join(" ", words);
            if (normalized.Length > MaximumCharacters)
            {
                error = $"The suggested name must contain between 1 and {MaximumCharacters} characters.";
                return false;
            }

            if (words.All(word => GenericWords.Contains(word)))
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
