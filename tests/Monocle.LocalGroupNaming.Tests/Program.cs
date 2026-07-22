using System;
using System.Collections.Generic;
using MonocleViewExtension.LocalGroupNaming;

namespace Monocle.LocalGroupNaming.Tests
{
    internal static class Program
    {
        private static int failures;

        public static int Main()
        {
            TestPromptContainsEveryNodeName();
            TestPromptAggregatesDuplicateNames();
            TestPromptRejectsEmptyGroups();
            TestRetryRejectsCopiedNodeName();
            TestValidatorAcceptsConciseTitle();
            TestValidatorStripsCommonModelFormatting();
            TestValidatorRejectsLongAndStructuredResponses();
            TestValidatorRecognizesCopiedNodeNames();
            TestDownloadManifestIsPinnedAndSecure();

            if (failures == 0)
            {
                Console.WriteLine("All local group naming tests passed.");
                return 0;
            }

            Console.Error.WriteLine($"{failures} local group naming test(s) failed.");
            return 1;
        }

        private static void TestPromptContainsEveryNodeName()
        {
            var prompt = GroupNamingPromptBuilder.Build(new[]
            {
                new GroupNodeSummary("Number Slider"),
                new GroupNodeSummary("Point.ByCoordinates"),
                new GroupNodeSummary("List Create")
            });

            AssertContains(prompt, "Given the following nodes in this group in Autodesk Dynamo");
            AssertContains(prompt, "- Number Slider");
            AssertContains(prompt, "- Point.ByCoordinates");
            AssertContains(prompt, "- List Create");
            AssertContains(prompt, "Consider all node names together");
            Assert(!prompt.Contains("UPSTREAM") && !prompt.Contains("OUTPUT"),
                "Expected no topology-derived naming instructions.");
        }

        private static void TestPromptAggregatesDuplicateNames()
        {
            var prompt = GroupNamingPromptBuilder.Build(new[]
            {
                new GroupNodeSummary("Number Slider"),
                new GroupNodeSummary("Number Slider"),
                new GroupNodeSummary("Number Slider"),
                new GroupNodeSummary("Point.ByCoordinates")
            });

            AssertContains(prompt, "- Number Slider x3");
        }

        private static void TestPromptRejectsEmptyGroups()
        {
            AssertThrows<InvalidOperationException>(() =>
                GroupNamingPromptBuilder.Build(new List<GroupNodeSummary>()));
        }

        private static void TestRetryRejectsCopiedNodeName()
        {
            var retry = GroupNamingPromptBuilder.BuildRetry(
                "Original prompt",
                "Point.ByCoordinates");

            AssertContains(retry, "Rejected proposal: Point.ByCoordinates");
            AssertContains(retry, "complete collection");
        }

        private static void TestValidatorAcceptsConciseTitle()
        {
            var valid = GroupNameValidator.TryNormalize(
                "Point Coordinate List Creation",
                out var groupName,
                out var error);

            Assert(valid, error ?? "Expected a valid group name.");
            AssertEqual("Point Coordinate List Creation", groupName);
        }

        private static void TestValidatorStripsCommonModelFormatting()
        {
            var valid = GroupNameValidator.TryNormalize(
                "`Title: Create Revit Sheets.`\nThis title describes the nodes.",
                out var groupName,
                out var error);

            Assert(valid, error ?? "Expected model formatting to be normalized.");
            AssertEqual("Create Revit Sheets", groupName);
        }

        private static void TestValidatorRejectsLongAndStructuredResponses()
        {
            Assert(!GroupNameValidator.TryNormalize(
                "This Suggested Group Name Contains Far Too Many Extra Words",
                out _,
                out _), "Expected a title over seven words to be rejected.");

            Assert(!GroupNameValidator.TryNormalize(
                "{\"name\":\"Create Revit Sheets\"}",
                out _,
                out _), "Expected structured output to be rejected.");
        }

        private static void TestValidatorRecognizesCopiedNodeNames()
        {
            var nodeNames = new[] { "Number Slider", "Point.ByCoordinates", "List Create" };
            Assert(GroupNameValidator.MatchesNodeName("List Create", nodeNames),
                "Expected an exact node name to be rejected.");
            Assert(!GroupNameValidator.MatchesNodeName("Point Coordinate List Creation", nodeNames),
                "Expected a synthesized group name not to match a node name.");
        }

        private static void TestDownloadManifestIsPinnedAndSecure()
        {
            Assert(LocalModelManifest.ModelDownloadUrl.StartsWith("https://", StringComparison.Ordinal),
                "Expected the model download to use HTTPS.");
            Assert(LocalModelManifest.RuntimeDownloadUrl.StartsWith("https://", StringComparison.Ordinal),
                "Expected the runtime download to use HTTPS.");
            Assert(IsSha256(LocalModelManifest.ModelSha256),
                "Expected a pinned model SHA-256 checksum.");
            Assert(IsSha256(LocalModelManifest.RuntimeSha256),
                "Expected a pinned runtime SHA-256 checksum.");
            Assert(LocalModelManifest.ModelFileSize > 2L * 1024 * 1024 * 1024,
                "Expected the manifest to retain the tested Qwen3 4B model size.");
        }

        private static bool IsSha256(string value)
        {
            if (value == null || value.Length != 64) return false;
            foreach (var character in value)
            {
                var isHex = character >= '0' && character <= '9' ||
                            character >= 'A' && character <= 'F';
                if (!isHex) return false;
            }

            return true;
        }

        private static void AssertContains(string value, string expected)
        {
            Assert(value.Contains(expected), $"Expected '{value}' to contain '{expected}'.");
        }

        private static void AssertEqual(string expected, string actual)
        {
            Assert(string.Equals(expected, actual, StringComparison.Ordinal),
                $"Expected '{expected}', received '{actual}'.");
        }

        private static void AssertThrows<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
                Assert(false, $"Expected {typeof(TException).Name}.");
            }
            catch (TException)
            {
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (condition) return;
            failures++;
            Console.Error.WriteLine(message);
        }
    }
}
