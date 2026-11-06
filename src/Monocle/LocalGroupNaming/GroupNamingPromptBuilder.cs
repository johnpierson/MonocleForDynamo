using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MonocleViewExtension.LocalGroupNaming
{
    /// <summary>A few-shot example: one group listing and the title the model should answer with.</summary>
    internal sealed class GroupNamingExample
    {
        public GroupNamingExample(string prompt, string title)
        {
            Prompt = prompt;
            Title = title;
        }

        public string Prompt { get; }

        public string Title { get; }
    }

    internal static class GroupNamingPromptBuilder
    {
        /// <summary>Longest group listing sent to the model; larger groups are summarised.</summary>
        public const int MaximumListedItems = 60;

        /// <summary>
        /// Fixed system message. It is identical for every request so llama-server's prompt
        /// cache reuses its tokens and only the group contents are processed per request.
        /// </summary>
        public const string SystemPrompt =
            "You name groups of nodes in Autodesk Dynamo graphs (visual programs, often used with Revit). " +
            "Given the contents of one group, reply with only a short title that says what the group accomplishes as a whole.\n" +
            "Rules:\n" +
            "- 3 to 7 words in Title Case, plain words only: no quotes, punctuation, code, labels, or explanation.\n" +
            "- Start with a verb when the group performs an action (Create, Collect, Filter, Export, Set, Read, Calculate, Place, Compare).\n" +
            "- Describe the purpose, not the mechanics. Never repeat a node name verbatim and never use the words Group, Nodes, Workflow, Graph, Dynamo, or Script.";

        /// <summary>
        /// Few-shot examples sent as prior user/assistant turns rather than inside the system
        /// prompt, so the model sees bare titles as answers and never has a "Title:" label to
        /// echo. They are built with <see cref="Build"/> so their shape matches real requests,
        /// and they never change, so the prompt cache covers them too.
        /// </summary>
        public static readonly IReadOnlyList<GroupNamingExample> ExampleTurns = new[]
        {
            new GroupNamingExample(
                Build(new[] { "Number Slider", "Number Slider", "Point.ByCoordinates", "List.Create" }),
                "Create Points From Slider Values"),
            new GroupNamingExample(
                Build(new[] { "Categories: Doors", "All Elements of Category", "Element.GetParameterValueByName", "String: \"Mark\"" }),
                "Read Door Mark Values"),
            new GroupNamingExample(
                Build(new[] { "File Path: schedule.xlsx", "Data.ImportExcel", "List.Transpose", "List.GetItemAtIndex" }),
                "Import Schedule Data From Excel")
        };

        public static string Build(IEnumerable<string> nodeDescriptions)
        {
            if (nodeDescriptions == null) throw new ArgumentNullException(nameof(nodeDescriptions));

            var items = nodeDescriptions
                .Where(description => !string.IsNullOrWhiteSpace(description))
                .Select(Clean)
                .GroupBy(description => description, StringComparer.OrdinalIgnoreCase)
                .Select(group => new { Description = group.Key, Count = group.Count() })
                .ToList();

            if (items.Count == 0)
            {
                throw new InvalidOperationException("The selected group does not contain any nameable nodes.");
            }

            var prompt = new StringBuilder();
            prompt.AppendLine("Contents:");
            foreach (var item in items.Take(MaximumListedItems))
            {
                prompt.Append("- ").Append(item.Description);
                if (item.Count > 1) prompt.Append(" x").Append(item.Count.ToString(CultureInfo.InvariantCulture));
                prompt.AppendLine();
            }

            var omitted = items.Skip(MaximumListedItems).Sum(item => item.Count);
            if (omitted > 0)
            {
                prompt.Append("- ... and ").Append(omitted.ToString(CultureInfo.InvariantCulture))
                    .AppendLine(omitted == 1 ? " more node" : " more nodes");
            }

            prompt.AppendLine();
            prompt.Append("Title:");
            return prompt.ToString();
        }

        public static string BuildRetry(string originalPrompt, string rejectedTitle)
        {
            if (string.IsNullOrWhiteSpace(originalPrompt))
                throw new ArgumentException("The original prompt is required.", nameof(originalPrompt));

            return originalPrompt + Environment.NewLine + Environment.NewLine +
                   $"Rejected proposal: {Clean(rejectedTitle)}" + Environment.NewLine +
                   "Propose a different title for the complete collection. Do not repeat any listed node name." + Environment.NewLine +
                   "Title:";
        }

        private static string Clean(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            return string.Join(" ", value
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Replace('\t', ' ')
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
