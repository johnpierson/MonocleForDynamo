using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MonocleViewExtension.LocalGroupNaming
{
    internal sealed class GroupNameSuggestion
    {
        public GroupNameSuggestion(string title, int attempts, IReadOnlyList<string> rejectedResponses)
        {
            Title = title;
            Attempts = attempts;
            RejectedResponses = rejectedResponses ?? new string[0];
        }

        public string Title { get; }

        public int Attempts { get; }

        public IReadOnlyList<string> RejectedResponses { get; }
    }

    /// <summary>Thrown when every attempt produced an unusable title; carries what the model said.</summary>
    internal sealed class GroupNameSuggestionException : InvalidOperationException
    {
        public GroupNameSuggestionException(string message, int attempts, IReadOnlyList<string> rejectedResponses)
            : base(message)
        {
            Attempts = attempts;
            RejectedResponses = rejectedResponses ?? new string[0];
        }

        public int Attempts { get; }

        public IReadOnlyList<string> RejectedResponses { get; }
    }

    /// <summary>
    /// Turns a group's node descriptions into a validated title: one grammar-constrained
    /// request, and one retry at a higher temperature when the first proposal copies a
    /// node name, is cut off, or fails validation. Shared by the Dynamo command and the
    /// bench tool so both exercise the same prompt, sampling and validation path.
    /// </summary>
    internal static class GroupNameSuggester
    {
        public static async Task<GroupNameSuggestion> SuggestAsync(
            LocalLlamaServerClient client,
            IReadOnlyList<string> nodeDescriptions,
            CancellationToken cancellationToken)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (nodeDescriptions == null) throw new ArgumentNullException(nameof(nodeDescriptions));

            var prompt = GroupNamingPromptBuilder.Build(nodeDescriptions);
            var rejected = new List<string>();

            var completion = await client.SuggestNameAsync(prompt, false, cancellationToken).ConfigureAwait(false);
            if (TryGetValidName(completion, nodeDescriptions, out var suggestion, out var validationError))
            {
                return new GroupNameSuggestion(suggestion, 1, rejected);
            }

            rejected.Add(completion.Content);
            var retryPrompt = GroupNamingPromptBuilder.BuildRetry(prompt, completion.Content);
            completion = await client.SuggestNameAsync(retryPrompt, true, cancellationToken).ConfigureAwait(false);
            if (TryGetValidName(completion, nodeDescriptions, out suggestion, out validationError))
            {
                return new GroupNameSuggestion(suggestion, 2, rejected);
            }

            rejected.Add(completion.Content);
            throw new GroupNameSuggestionException(
                validationError ?? "The local model copied a node name instead of naming the complete group.",
                2,
                rejected);
        }

        public static bool TryGetValidName(
            LocalModelCompletion completion,
            IReadOnlyList<string> nodeDescriptions,
            out string suggestion,
            out string error)
        {
            suggestion = null;
            error = null;
            if (completion == null) return false;

            if (completion.IsTruncated)
            {
                // The token limit cut the answer off; it may end mid-word.
                error = "The local model's answer was cut off before it finished.";
                return false;
            }

            return TryGetValidName(completion.Content, nodeDescriptions, out suggestion, out error);
        }

        public static bool TryGetValidName(
            string response,
            IReadOnlyList<string> nodeDescriptions,
            out string suggestion,
            out string error)
        {
            if (!GroupNameValidator.TryNormalize(response, out suggestion, out error)) return false;

            if (GroupNameValidator.IsApiStyleIdentifier(suggestion) ||
                GroupNameValidator.MatchesNodeName(suggestion, nodeDescriptions))
            {
                suggestion = null;
                error = null;
                return false;
            }

            return true;
        }
    }
}
