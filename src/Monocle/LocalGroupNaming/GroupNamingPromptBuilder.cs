using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonocleViewExtension.LocalGroupNaming
{
    internal static class GroupNamingPromptBuilder
    {
        public static string Build(IEnumerable<GroupNodeSummary> nodes)
        {
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));

            var nodeCounts = nodes
                .Where(node => node != null && !string.IsNullOrWhiteSpace(node.Name))
                .GroupBy(node => Clean(node.Name), StringComparer.OrdinalIgnoreCase)
                .Select(group => new { Name = group.Key, Count = group.Count() })
                .ToList();

            if (nodeCounts.Count == 0)
            {
                throw new InvalidOperationException("The selected group does not contain any nameable nodes.");
            }

            var prompt = new StringBuilder();
            prompt.AppendLine("/no_think");
            prompt.AppendLine("Given the following nodes in this group in Autodesk Dynamo, what would a logical name of this group be?");
            prompt.AppendLine();
            prompt.AppendLine("Nodes:");
            foreach (var node in nodeCounts)
            {
                prompt.Append("- ").Append(node.Name);
                if (node.Count > 1) prompt.Append(" x").Append(node.Count);
                prompt.AppendLine();
            }

            prompt.AppendLine();
            prompt.AppendLine("Consider all node names together and name what the collection logically accomplishes.");
            prompt.AppendLine("Do not copy the name of any single node as the answer.");
            prompt.Append("Reply with only a concise 3 to 7 word proposed group name:");
            return prompt.ToString();
        }

        public static string BuildRetry(string originalPrompt, string rejectedTitle)
        {
            if (string.IsNullOrWhiteSpace(originalPrompt))
                throw new ArgumentException("The original prompt is required.", nameof(originalPrompt));

            return originalPrompt + Environment.NewLine + Environment.NewLine +
                   $"Rejected proposal: {Clean(rejectedTitle)}" + Environment.NewLine +
                   "Propose a different logical name based on the complete collection. Do not copy any listed node name." + Environment.NewLine +
                   "New group name:";
        }

        private static string Clean(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            return string.Join(" ", value
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
