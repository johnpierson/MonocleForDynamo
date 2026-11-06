using System;
using System.Collections.Generic;
using System.Linq;

namespace MonocleViewExtension.LocalGroupNaming
{
    internal static class GroupNameValidator
    {
        private const int MaximumCharacters = 60;
        private const int MaximumWords = 7;

        public static bool IsApiStyleIdentifier(string groupName)
        {
            return !string.IsNullOrWhiteSpace(groupName) &&
                   (groupName.Contains(".") || groupName.Contains("::") || groupName.Contains("_"));
        }

        public static bool MatchesNodeName(string groupName, IEnumerable<string> nodeNames)
        {
            if (string.IsNullOrWhiteSpace(groupName) || nodeNames == null) return false;
            return nodeNames.Any(nodeName => string.Equals(
                groupName.Trim(),
                nodeName?.Trim(),
                StringComparison.OrdinalIgnoreCase));
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

            var normalized = firstLine
                .Trim()
                .Trim('`', '"', '\'', '.', ':', ';', '-', '*', '#')
                .Trim();

            if (normalized.StartsWith("Title:", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring("Title:".Length).Trim();
            }

            if (normalized.Length == 0 || normalized.Length > MaximumCharacters)
            {
                error = $"The suggested name must contain between 1 and {MaximumCharacters} characters.";
                return false;
            }

            if (normalized.IndexOfAny(new[] { '{', '}', '[', ']', '<', '>' }) >= 0)
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

            if (string.Equals(normalized, "Group", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "Dynamo Group", StringComparison.OrdinalIgnoreCase))
            {
                error = "The local model returned a generic group name.";
                return false;
            }

            groupName = normalized;
            return true;
        }
    }
}
