using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
            TestNamingStateRejectsManualEditsAndCancelledSessions();
            TestNamingQueueProcessesConsecutiveGroups();
            TestNamingQueueRecoversAfterRequestFailure();
            TestNamingQueueCancelsPendingRequests();

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
                "Number Slider",
                "Point.ByCoordinates",
                "List Create"
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
                "Number Slider",
                "Number Slider",
                "Number Slider",
                "Point.ByCoordinates"
            });

            AssertContains(prompt, "- Number Slider x3");
        }

        private static void TestPromptRejectsEmptyGroups()
        {
            AssertThrows<InvalidOperationException>(() =>
                GroupNamingPromptBuilder.Build(new List<string>()));
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
            Assert(!LocalModelManifest.ModelDownloadUrl.Contains("/resolve/main/"),
                "Expected the model download to pin an immutable revision instead of the mutable main ref.");
            Assert(LocalModelManifest.RuntimeDownloadUrl.StartsWith("https://", StringComparison.Ordinal),
                "Expected the runtime download to use HTTPS.");
            Assert(IsSha256(LocalModelManifest.ModelSha256),
                "Expected a pinned model SHA-256 checksum.");
            Assert(IsSha256(LocalModelManifest.RuntimeSha256),
                "Expected a pinned runtime SHA-256 checksum.");
            Assert(LocalModelManifest.ModelFileSize > 2L * 1024 * 1024 * 1024,
                "Expected the manifest to retain the tested Qwen3 4B model size.");
        }

        private static void TestNamingStateRejectsManualEditsAndCancelledSessions()
        {
            var groupId = Guid.NewGuid();
            var nodeId = Guid.NewGuid();
            var originalNodes = new List<LocalGroupNodeSnapshot>
            {
                new LocalGroupNodeSnapshot(nodeId, "Number Slider")
            };
            var request = new LocalGroupNamingRequest(groupId, "Original Group", originalNodes, 7);

            // The request owns the state captured before it enters the queue.
            originalNodes[0] = new LocalGroupNodeSnapshot(nodeId, "Manually Renamed Node");
            var unchangedNodes = new[] { new LocalGroupNodeSnapshot(nodeId, "Number Slider") };

            Assert(LocalGroupNamingState.ShouldApplySuggestion(
                    request, true, groupId, "Original Group", unchangedNodes),
                "Expected an unchanged group in the current session to accept a suggestion.");
            Assert(!LocalGroupNamingState.ShouldApplySuggestion(
                    request, true, groupId, "Manual Group Title", unchangedNodes),
                "Expected a manual group title edit to protect the title from an AI overwrite.");
            Assert(!LocalGroupNamingState.ShouldApplySuggestion(
                    request, false, groupId, "Original Group", unchangedNodes),
                "Expected a cancelled or disabled session to reject a suggestion.");
            Assert(!LocalGroupNamingState.ShouldApplySuggestion(
                    request, true, Guid.NewGuid(), "Original Group", unchangedNodes),
                "Expected a deleted or replaced group to reject a suggestion.");
            Assert(!LocalGroupNamingState.ShouldApplySuggestion(
                    request,
                    true,
                    groupId,
                    "Original Group",
                    new[] { new LocalGroupNodeSnapshot(nodeId, "Changed Node") }),
                "Expected a group whose nodes changed during inference to reject a suggestion.");
        }

        private static void TestNamingQueueProcessesConsecutiveGroups()
        {
            var firstGroupId = Guid.NewGuid();
            var secondGroupId = Guid.NewGuid();
            var startedGroups = new List<Guid>();
            var firstStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseFirst = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (var queue = new LocalGroupNamingQueue(
                async (request, cancellationToken) =>
                {
                    lock (startedGroups)
                    {
                        startedGroups.Add(request.GroupId);
                    }

                    if (request.GroupId == firstGroupId)
                    {
                        firstStarted.TrySetResult(true);
                        await releaseFirst.Task.ConfigureAwait(false);
                    }
                },
                (request, exception) => Assert(false, $"Unexpected queue failure: {exception.Message}")))
            {
                Assert(queue.Enqueue(CreateRequest(firstGroupId, "First Group")),
                    "Expected the first group naming request to be queued.");
                WaitForSignal(firstStarted.Task, "The first group naming request did not start.");

                Assert(queue.Enqueue(CreateRequest(secondGroupId, "Second Group")),
                    "Expected the second group naming request to be queued while the first was running.");
                Assert(GetCount(startedGroups) == 1,
                    "Expected consecutive group creation to serialize the second inference behind the first.");

                releaseFirst.SetResult(true);
                WaitForQueue(queue);
                Assert(GetCount(startedGroups) == 2,
                    "Expected every consecutive group to receive a naming request.");
                Assert(GetGroupAt(startedGroups, 0) == firstGroupId && GetGroupAt(startedGroups, 1) == secondGroupId,
                    "Expected naming requests to be processed in creation order.");
            }
        }

        private static void TestNamingQueueRecoversAfterRequestFailure()
        {
            var failedGroupId = Guid.NewGuid();
            var recoveredGroupId = Guid.NewGuid();
            var processedGroups = new List<Guid>();
            var errors = 0;

            using (var queue = new LocalGroupNamingQueue(
                (request, cancellationToken) =>
                {
                    lock (processedGroups)
                    {
                        processedGroups.Add(request.GroupId);
                    }

                    if (request.GroupId == failedGroupId)
                    {
                        throw new InvalidOperationException("Expected test failure.");
                    }

                    return Task.CompletedTask;
                },
                (request, exception) => Interlocked.Increment(ref errors)))
            {
                queue.Enqueue(CreateRequest(failedGroupId, "Failed Group"));
                queue.Enqueue(CreateRequest(recoveredGroupId, "Recovered Group"));
                WaitForQueue(queue);

                Assert(GetCount(processedGroups) == 2,
                    "Expected the queue to continue after one request failed.");
                Assert(errors == 1, "Expected the failed naming request to be reported exactly once.");
            }
        }

        private static void TestNamingQueueCancelsPendingRequests()
        {
            var activeGroupId = Guid.NewGuid();
            var pendingGroupId = Guid.NewGuid();
            var processedCount = 0;
            var activeStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (var queue = new LocalGroupNamingQueue(
                async (request, cancellationToken) =>
                {
                    Interlocked.Increment(ref processedCount);
                    activeStarted.TrySetResult(true);
                    await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
                },
                (request, exception) => Assert(false, $"Unexpected cancellation failure: {exception.Message}")))
            {
                queue.Enqueue(CreateRequest(activeGroupId, "Active Group"));
                WaitForSignal(activeStarted.Task, "The active request did not start.");
                queue.Enqueue(CreateRequest(pendingGroupId, "Pending Group"));
                queue.Cancel();
                WaitForQueue(queue);

                Assert(processedCount == 1,
                    "Expected queue cancellation to stop the active request and discard pending work.");
            }
        }

        private static LocalGroupNamingRequest CreateRequest(Guid groupId, string title)
        {
            return new LocalGroupNamingRequest(
                groupId,
                title,
                new[] { new LocalGroupNodeSnapshot(Guid.NewGuid(), "Number Slider") },
                7);
        }

        private static void WaitForSignal(Task signal, string message)
        {
            if (signal.Wait(TimeSpan.FromSeconds(5))) return;
            Assert(false, message);
        }

        private static void WaitForQueue(LocalGroupNamingQueue queue)
        {
            var idle = queue.WhenIdle;
            if (idle.Wait(TimeSpan.FromSeconds(5))) return;

            queue.Cancel();
            Assert(false, "The local group naming queue did not become idle.");
            queue.WhenIdle.Wait(TimeSpan.FromSeconds(5));
        }

        private static int GetCount(List<Guid> values)
        {
            lock (values)
            {
                return values.Count;
            }
        }

        private static Guid GetGroupAt(List<Guid> values, int index)
        {
            lock (values)
            {
                return values[index];
            }
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
