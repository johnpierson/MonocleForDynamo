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
        /// Estimated-token budget for one group listing. The server context is 4,096 tokens
        /// (<c>LocalLlamaServerClient.ContextSize</c>); the fixed system prompt and example
        /// turns take roughly 400, chat framing about 100, the completion 48 and the retry
        /// suffix about 50, which leaves comfortable headroom under this figure even though the
        /// estimate is deliberately pessimistic.
        /// </summary>
        public const int MaximumPromptTokens = 2600;

        private const string Header = "Contents:\n";
        private const string Footer = "\nTitle:";

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

        /// <summary>
        /// Builds the group listing, bounded both by item count and by the estimated token
        /// budget, because sixty code-block descriptions tokenise to far more than sixty node
        /// names. Items that do not fit are summarised as "... and N more nodes".
        /// </summary>
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

            var budget = MaximumPromptTokens
                         - EstimateTokens(Header)
                         - EstimateTokens(Footer)
                         - EstimateTokens(FormatOmitted(9999));
            var lines = new List<string>();
            var usedTokens = 0;
            foreach (var item in items.Take(MaximumListedItems))
            {
                var line = "- " + item.Description +
                           (item.Count > 1 ? " x" + item.Count.ToString(CultureInfo.InvariantCulture) : string.Empty) + "\n";
                var cost = EstimateTokens(line);
                if (lines.Count > 0 && usedTokens + cost > budget) break;

                lines.Add(line);
                usedTokens += cost;
            }

            var prompt = new StringBuilder(Header);
            foreach (var line in lines) prompt.Append(line);

            var omitted = items.Skip(lines.Count).Sum(item => item.Count);
            if (omitted > 0) prompt.Append(FormatOmitted(omitted));

            prompt.Append(Footer);
            return prompt.ToString();
        }

        public static string BuildRetry(string originalPrompt, string rejectedTitle)
        {
            if (string.IsNullOrWhiteSpace(originalPrompt))
                throw new ArgumentException("The original prompt is required.", nameof(originalPrompt));

            return originalPrompt + "\n\n" +
                   $"Rejected proposal: {Clean(rejectedTitle)}" + "\n" +
                   "Propose a different title for the complete collection. Do not repeat any listed node name." + "\n" +
                   "Title:";
        }

        /// <summary>
        /// Pessimistic token estimate for Qwen-style tokenisers: every digit is a token (Qwen
        /// splits numbers digit by digit), every punctuation character is a token, and a run of
        /// letters costs one token per four characters. Prose is over-estimated by up to half,
        /// code and numbers are estimated closely, which is the safe direction for a budget.
        /// </summary>
        public static int EstimateTokens(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            var tokens = 0;
            var letterRun = 0;
            foreach (var character in text)
            {
                if (char.IsLetter(character))
                {
                    letterRun++;
                    continue;
                }

                tokens += LetterRunCost(letterRun);
                letterRun = 0;

                if (char.IsDigit(character) || !char.IsWhiteSpace(character)) tokens++;
            }

            return tokens + LetterRunCost(letterRun);
        }

        private static int LetterRunCost(int length)
        {
            return length == 0 ? 0 : (length + 3) / 4;
        }

        private static string FormatOmitted(int omitted)
        {
            return "- ... and " + omitted.ToString(CultureInfo.InvariantCulture) + (omitted == 1 ? " more node" : " more nodes") + "\n";
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
