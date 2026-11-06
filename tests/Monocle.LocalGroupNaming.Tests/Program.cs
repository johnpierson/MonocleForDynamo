using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MonocleViewExtension.LocalGroupNaming;
using Newtonsoft.Json.Linq;

namespace Monocle.LocalGroupNaming.Tests
{
    internal static class Program
    {
        private static int failures;

        public static int Main()
        {
            var testRoot = Path.Combine(Path.GetTempPath(), "MonocleLocalGroupNamingTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testRoot);

            try
            {
                TestPromptContainsEveryNodeDescription();
                TestPromptAggregatesDuplicateNames();
                TestPromptTruncatesLargeGroups();
                TestPromptRejectsEmptyGroups();
                TestRetryRejectsCopiedNodeName();
                TestSystemPromptStatesTheRules();
                TestGrammarShape();
                TestGrammarAndValidatorAgree();
                TestValidatorAcceptsConciseTitle();
                TestValidatorStripsCommonModelFormatting();
                TestValidatorRejectsLongAndStructuredResponses();
                TestValidatorRecognizesCopiedNodeNames();
                TestNodeDescriptionHelpers();
                TestCompletionRequestShape();
                TestServerArguments();
                TestDownloadManifestIsPinnedAndSecure();
                TestNamingStateRejectsManualEditsAndCancelledSessions();
                TestNamingQueueProcessesConsecutiveGroups();
                TestNamingQueueRecoversAfterRequestFailure();
                TestNamingQueueCancelsPendingRequests();
                TestResumableDownloadResumesAfterInterruption(testRoot).GetAwaiter().GetResult();
                TestResumableDownloadRestartsWhenRangeIsIgnored(testRoot).GetAwaiter().GetResult();
                TestProvisionerInstallsResumesAndRemovesSupersededVersions(testRoot).GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                failures++;
                Console.Error.WriteLine(exception);
            }
            finally
            {
                TryDelete(testRoot);
            }

            if (failures == 0)
            {
                Console.WriteLine("All local group naming tests passed.");
                return 0;
            }

            Console.Error.WriteLine($"{failures} local group naming test(s) failed.");
            return 1;
        }

        private static void TestPromptContainsEveryNodeDescription()
        {
            var prompt = GroupNamingPromptBuilder.Build(new[]
            {
                "Number Slider",
                "Point.ByCoordinates",
                "List Create",
                "Categories: Doors",
                "String: \"Mark\""
            });

            AssertContains(prompt, "Contents:");
            AssertContains(prompt, "- Number Slider");
            AssertContains(prompt, "- Point.ByCoordinates");
            AssertContains(prompt, "- List Create");
            AssertContains(prompt, "- Categories: Doors");
            AssertContains(prompt, "- String: \"Mark\"");
            Assert(prompt.TrimEnd().EndsWith("Title:", StringComparison.Ordinal), "Expected the prompt to end with the title cue.");
            Assert(!prompt.Contains("/no_think"), "Thinking is disabled through the chat template, not the prompt text.");
        }

        private static void TestPromptAggregatesDuplicateNames()
        {
            var prompt = GroupNamingPromptBuilder.Build(new[]
            {
                "Number Slider",
                "Number Slider",
                "number slider",
                "Point.ByCoordinates"
            });

            AssertContains(prompt, "- Number Slider x3");
            Assert(!prompt.Contains("x1"), "Single items must not carry a count.");
        }

        private static void TestPromptTruncatesLargeGroups()
        {
            var descriptions = Enumerable.Range(1, GroupNamingPromptBuilder.MaximumListedItems + 20)
                .Select(index => "Node " + index)
                .ToList();

            var prompt = GroupNamingPromptBuilder.Build(descriptions);

            AssertContains(prompt, "- Node " + GroupNamingPromptBuilder.MaximumListedItems + Environment.NewLine);
            Assert(!prompt.Contains("- Node " + (GroupNamingPromptBuilder.MaximumListedItems + 1) + Environment.NewLine),
                "Expected items past the listing cap to be summarised instead of listed.");
            AssertContains(prompt, "- ... and 20 more nodes");
        }

        private static void TestPromptRejectsEmptyGroups()
        {
            AssertThrows<InvalidOperationException>(() =>
                GroupNamingPromptBuilder.Build(new List<string>()));
            AssertThrows<InvalidOperationException>(() =>
                GroupNamingPromptBuilder.Build(new[] { " ", null, "" }));
        }

        private static void TestRetryRejectsCopiedNodeName()
        {
            var retry = GroupNamingPromptBuilder.BuildRetry(
                "Original prompt",
                "Point.ByCoordinates");

            AssertContains(retry, "Original prompt");
            AssertContains(retry, "Rejected proposal: Point.ByCoordinates");
            AssertContains(retry, "complete collection");
            Assert(retry.TrimEnd().EndsWith("Title:", StringComparison.Ordinal), "Expected the retry to end with the title cue.");
        }

        private static void TestSystemPromptStatesTheRules()
        {
            var system = GroupNamingPromptBuilder.SystemPrompt;
            AssertContains(system, "3 to 7 words");
            AssertContains(system, "Never repeat a node name");
            AssertContains(system, "Contents: Number Slider x2; Point.ByCoordinates; List.Create");
            AssertContains(system, "Title: Create Points From Slider Values");
            Assert(system.Length < 1500, "The system prompt is cached per request; keep it short.");
        }

        private static void TestGrammarShape()
        {
            var grammar = GroupNameGrammar.Gbnf;
            Assert(grammar.StartsWith("root ::= word", StringComparison.Ordinal), "The grammar must start at the root rule.");
            AssertContains(grammar, "{2,6}");
            AssertContains(grammar, "word ::= [A-Za-z0-9] [A-Za-z0-9'&/+-]{0,19}");
            Assert(!grammar.Contains("\\n") && !grammar.Contains("\"\\\""), "The grammar must not allow line breaks or quotes.");

            var regex = new Regex(GroupNameGrammar.RegexPattern);
            Assert(regex.IsMatch("Create Points From Sliders"), "A four word title must satisfy the grammar.");
            Assert(regex.IsMatch("Read Door Mark Values"), "A plain title must satisfy the grammar.");
            Assert(regex.IsMatch("Walls/Floors Area Takeoff"), "Slashes inside words are allowed.");
            Assert(!regex.IsMatch("Create Points"), "Two words are below the minimum.");
            Assert(!regex.IsMatch("One Two Three Four Five Six Seven Eight"), "Eight words exceed the maximum.");
            Assert(!regex.IsMatch("Sure, here is a name"), "Punctuation such as commas is excluded.");
            Assert(!regex.IsMatch("Title: Create Points From Sliders"), "Label prefixes are excluded.");
            Assert(!regex.IsMatch("Create Points\nFrom Sliders"), "Line breaks are excluded.");
            Assert(!regex.IsMatch("Point.ByCoordinates List Create"), "Dots cannot appear, so API identifiers cannot be copied.");
        }

        private static void TestGrammarAndValidatorAgree()
        {
            foreach (var title in new[]
            {
                "Create Points From Sliders",
                "Read Door Mark Values",
                "Export Rooms To Excel 2026",
                "3D Geometry Preview Checks",
                "Walls/Floors Area Takeoff",
                "Owner's Room Data Export"
            })
            {
                Assert(Regex.IsMatch(title, GroupNameGrammar.RegexPattern), $"'{title}' should satisfy the grammar.");
                var valid = GroupNameValidator.TryNormalize(title, out var normalized, out var error);
                Assert(valid, error ?? $"Expected the validator to accept the grammar-shaped title '{title}'.");
                AssertEqual(title, normalized);
            }

            Assert(GroupNameValidator.TryNormalize("create points from sliders", out var capitalized, out _),
                "Lower-case titles are accepted.");
            AssertEqual("Create points from sliders", capitalized);
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

            Assert(GroupNameValidator.TryNormalize("**Group name: Export   Room Data**", out var spaced, out _),
                "Expected bold markers and a label prefix to be stripped.");
            AssertEqual("Export Room Data", spaced);
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

            Assert(!GroupNameValidator.TryNormalize("Group", out _, out _), "Expected a generic title to be rejected.");
            Assert(!GroupNameValidator.TryNormalize("Untitled Group", out _, out _), "Expected a generic title to be rejected.");
            Assert(!GroupNameValidator.TryNormalize(new string('A', GroupNameValidator.MaximumCharacters + 1), out _, out _),
                "Expected an over-long title to be rejected.");
        }

        private static void TestValidatorRecognizesCopiedNodeNames()
        {
            var nodeNames = new[] { "Number Slider", "Point.ByCoordinates", "List Create", "Code Block: x = 5", "Categories: Doors" };
            Assert(GroupNameValidator.MatchesNodeName("List Create", nodeNames),
                "Expected an exact node name to be rejected.");
            Assert(GroupNameValidator.MatchesNodeName("Point By Coordinates", nodeNames),
                "Expected a node name with punctuation removed to be rejected.");
            Assert(GroupNameValidator.MatchesNodeName("point bycoordinates", nodeNames),
                "Expected the comparison to ignore case and spacing.");
            Assert(GroupNameValidator.MatchesNodeName("Code Block", nodeNames),
                "Expected the node name part of a description to be rejected.");
            Assert(!GroupNameValidator.MatchesNodeName("Read Door Mark Values", nodeNames),
                "Expected a synthesized group name not to match a node name.");
            Assert(!GroupNameValidator.MatchesNodeName("Point Coordinate List Creation", nodeNames),
                "Expected a synthesized group name not to match a node name.");
            Assert(GroupNameValidator.IsApiStyleIdentifier("Point.ByCoordinates"), "Dotted identifiers are API style.");
            Assert(GroupNameValidator.IsApiStyleIdentifier("Get_Elements"), "Underscored identifiers are API style.");
            Assert(!GroupNameValidator.IsApiStyleIdentifier("Collect Door Marks"), "Plain words are not API style.");

            Assert(GroupNameSuggester.TryGetValidName("Read Door Mark Values", nodeNames, out var accepted, out _),
                "Expected a synthesized title to pass the suggester's checks.");
            AssertEqual("Read Door Mark Values", accepted);
            Assert(!GroupNameSuggester.TryGetValidName("Point By Coordinates", nodeNames, out _, out _),
                "Expected the suggester to reject a copied node name.");
        }

        private static void TestNodeDescriptionHelpers()
        {
            AssertEqual("Code Block: x = 5; y = 6", GroupNodeDescription.Compose("Code Block", "x = 5;\r\n y = 6"));
            AssertEqual("Python Script", GroupNodeDescription.Compose("Python Script", null));
            AssertEqual("Python Script", GroupNodeDescription.Compose("  Python   Script ", "   "));
            Assert(GroupNodeDescription.Compose(null, null) == null, "Nothing to describe yields null.");
            AssertEqual("\"Mark\"", GroupNodeDescription.Quote("Mark"));
            Assert(GroupNodeDescription.Quote("  ") == null, "Empty string values are not quoted.");

            AssertEqual("collect sheet names", GroupNodeDescription.FirstMeaningfulLine(
                "import clr\r\nclr.AddReference('RevitAPI')\r\n# collect sheet names\r\ndoc = DocumentManager.Instance.CurrentDBDocument"));
            AssertEqual("doc = DocumentManager.Instance.CurrentDBDocument", GroupNodeDescription.FirstMeaningfulLine(
                "import clr\nfrom Autodesk.Revit.DB import *\ndoc = DocumentManager.Instance.CurrentDBDocument;\nOUT = doc"));
            AssertEqual("comment", GroupNodeDescription.FirstMeaningfulLine("//comment;\nRevit.Elements.Category.ById(-2000011)"));
            AssertEqual("x = 5", GroupNodeDescription.FirstMeaningfulLine("x = 5;"));
            Assert(GroupNodeDescription.FirstMeaningfulLine("  \r\n\n") == null, "Blank code yields null.");
            Assert(GroupNodeDescription.FirstMeaningfulLine("// \n;;;") == null, "Lines without letters or digits are skipped.");

            var longDetail = new string('a', 200);
            var composed = GroupNodeDescription.Compose("Code Block", longDetail);
            Assert(composed.Length <= GroupNodeDescription.MaximumLength, "Descriptions are capped.");
            Assert(composed.EndsWith("...", StringComparison.Ordinal), "Truncated descriptions end with an ellipsis.");
            Assert(composed.StartsWith("Code Block: aaa", StringComparison.Ordinal), "The node name leads the description.");
            AssertEqual("abc", GroupNodeDescription.Truncate("abc", 3));
            AssertEqual("ab...", GroupNodeDescription.Truncate("abcdefgh", 5));
        }

        private static void TestCompletionRequestShape()
        {
            var request = LocalLlamaServerClient.BuildCompletionRequest("model", "Contents:\n- Watch\n\nTitle:", false);
            var messages = (JArray)request["messages"];
            AssertEqual("system", messages[0]["role"].Value<string>());
            AssertEqual(GroupNamingPromptBuilder.SystemPrompt, messages[0]["content"].Value<string>());
            AssertEqual("user", messages[1]["role"].Value<string>());
            AssertEqual("Contents:\n- Watch\n\nTitle:", messages[1]["content"].Value<string>());
            AssertEqual(GroupNameGrammar.Gbnf, request["grammar"].Value<string>());
            Assert(request["chat_template_kwargs"]["enable_thinking"].Value<bool>() == false,
                "Thinking must be disabled through the chat template.");
            Assert(request["temperature"].Value<double>() == LocalLlamaServerClient.DefaultTemperature, "Expected the default temperature.");
            Assert(request["max_tokens"].Value<int>() == LocalLlamaServerClient.MaximumCompletionTokens, "Expected the completion token cap.");
            Assert(request["stream"].Value<bool>() == false, "Streaming is not used.");
            Assert(request["cache_prompt"].Value<bool>(), "The shared system prompt must be cached.");
            Assert(request["top_p"].Value<double>() == 0.8 && request["top_k"].Value<int>() == 20,
                "Expected Qwen's published non-thinking nucleus and top-k settings.");
            Assert(request["min_p"].Value<double>() == 0.0,
                "min_p must be sent explicitly because llama.cpp's default is non-zero.");

            var retry = LocalLlamaServerClient.BuildCompletionRequest("model", "prompt", true);
            Assert(retry["temperature"].Value<double>() == LocalLlamaServerClient.RetryTemperature,
                "A retry must sample at a higher temperature so it can propose something different.");
            Assert(LocalLlamaServerClient.RetryTemperature > LocalLlamaServerClient.DefaultTemperature, "Retry temperature must exceed the default.");
        }

        private static void TestServerArguments()
        {
            var options = LocalLlamaServerOptions.CreateExternal(@"C:\runtime\llama-server.exe", @"C:\models\my model.gguf", LocalModelManifest.Current);
            var arguments = LocalLlamaServerClient.BuildServerArguments(options, 4321);

            AssertContains(arguments, "-m \"C:\\models\\my model.gguf\"");
            AssertContains(arguments, "--host 127.0.0.1 --port 4321");
            AssertContains(arguments, "-c " + LocalLlamaServerClient.ContextSize);
            AssertContains(arguments, "-np 1");
            AssertContains(arguments, "--jinja --reasoning off");
            AssertContains(arguments, "--no-ui --no-slots");
            Assert(!arguments.Contains("-ngl"), "GPU layer flags are left to the runtime build.");
            Assert(!arguments.Contains(" -t "), "The thread count is left to llama.cpp's physical-core default.");
            Assert(!options.IsManagedInstallation, "An external runtime is not a managed installation.");

            var withExtra = options.WithAdditionalServerArguments("  -t 6 ");
            Assert(LocalLlamaServerClient.BuildServerArguments(withExtra, 1).EndsWith(" -t 6", StringComparison.Ordinal),
                "Additional arguments are appended after the defaults so they can override them.");

            var managed = LocalLlamaServerOptions.CreateManaged(@"C:\root", LocalModelManifest.Current);
            Assert(managed.IsManagedInstallation, "A managed installation must say so.");
            AssertEqual(Path.Combine(@"C:\root", LocalModelManifest.Current.InstallationVersion, "model", LocalModelManifest.Current.ModelFileName), managed.ModelPath);
            AssertEqual(Path.Combine(@"C:\root", LocalModelManifest.Current.InstallationVersion, "runtime", "llama-server.exe"), managed.ServerPath);
            AssertEqual(@"C:\root", managed.InstallationRootDirectory);
        }

        private static void TestDownloadManifestIsPinnedAndSecure()
        {
            var manifest = LocalModelManifest.Current;
            Assert(manifest.ModelDownloadUrl.StartsWith("https://", StringComparison.Ordinal),
                "Expected the model download to use HTTPS.");
            Assert(Regex.IsMatch(manifest.ModelDownloadUrl, "/resolve/[0-9a-f]{40}/"),
                "Expected the model download to pin an immutable commit instead of a mutable branch.");
            Assert(manifest.RuntimeDownloadUrl.StartsWith("https://github.com/ggml-org/llama.cpp/releases/download/" + manifest.RuntimeVersion + "/", StringComparison.Ordinal),
                "Expected the runtime download to come from the pinned llama.cpp release.");
            Assert(manifest.RuntimeDownloadUrl.EndsWith("/" + manifest.RuntimeArchiveName, StringComparison.Ordinal),
                "Expected the runtime URL to end with the archive name.");
            Assert(manifest.RuntimeLicenseUrl.Contains("/" + manifest.RuntimeVersion + "/"),
                "Expected the runtime license link to point at the pinned release.");
            Assert(IsSha256(manifest.ModelSha256), "Expected a pinned model SHA-256 checksum.");
            Assert(IsSha256(manifest.RuntimeSha256), "Expected a pinned runtime SHA-256 checksum.");
            Assert(manifest.ModelFileSize > 1L * 1024 * 1024 * 1024, "Expected a multi-gigabyte model size in the manifest.");
            Assert(manifest.RuntimeArchiveSize > 1024 * 1024, "Expected the runtime archive size in the manifest.");
            Assert(manifest.InstallationVersion.Contains(manifest.RuntimeVersion),
                "Expected the installation version to change with the runtime pin.");
            Assert(manifest.RequiredFreeBytes > manifest.ModelFileSize, "Expected disk space headroom beyond the model size.");
            Assert(manifest.ApproximateDownloadSize.EndsWith(" GB", StringComparison.Ordinal), "Expected the download size in gigabytes.");
            // Binary units, matching Windows Explorer and the setup window.
            AssertEqual("2.3 GB", LocalModelManifest.FormatSize(2497280640));
            AssertEqual("24 MB", LocalModelManifest.FormatSize(24708445));
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

        private static async Task TestResumableDownloadResumesAfterInterruption(string testRoot)
        {
            var payload = CreatePayload(3 * 1024 * 1024 + 123, seed: 1);
            var handler = new FakeFileHandler();
            handler.AddFile("model.gguf", payload);
            handler.FailAfterBytes("model.gguf", 1_300_000);
            var destination = Path.Combine(testRoot, "resume", "model.gguf.download");
            Directory.CreateDirectory(Path.GetDirectoryName(destination));

            using (var client = new HttpClient(handler, false))
            {
                var interrupted = false;
                try
                {
                    await ResumableDownloader.DownloadAsync(client, "https://example.test/model.gguf", destination, "model", payload.Length, null, CancellationToken.None);
                }
                catch (IOException)
                {
                    interrupted = true;
                }

                Assert(interrupted, "Expected the simulated network failure to surface.");
                Assert(File.Exists(destination) && new FileInfo(destination).Length == 1_300_000,
                    "Expected the partial file to be kept for resumption.");

                var progress = new List<long>();
                await ResumableDownloader.DownloadAsync(
                    client,
                    "https://example.test/model.gguf",
                    destination,
                    "model",
                    payload.Length,
                    new SynchronousProgress(progress),
                    CancellationToken.None);
                Assert(handler.Requests == 2, "Expected exactly one retry request.");
                AssertEqual("bytes=1300000-", handler.RangeHeaders[1]);
                Assert(progress.Count > 0 && progress[0] == 1_300_000 && progress.Last() == payload.Length,
                    "Expected progress to start at the resumed offset and end at the full size.");
                Assert(new FileInfo(destination).Length == payload.Length, "Expected the resumed file to be complete.");
                AssertEqual(Sha256(payload), Sha256File(destination));
            }
        }

        private static async Task TestResumableDownloadRestartsWhenRangeIsIgnored(string testRoot)
        {
            var payload = CreatePayload(2 * 1024 * 1024 + 7, seed: 2);
            var handler = new FakeFileHandler { IgnoreRange = true };
            handler.AddFile("runtime.zip", payload);
            var destination = Path.Combine(testRoot, "restart", "runtime.zip.download");
            Directory.CreateDirectory(Path.GetDirectoryName(destination));

            // A stale partial file from an earlier attempt that the server will not resume.
            File.WriteAllBytes(destination, payload.Take(500_000).Select(b => (byte)(b ^ 0xFF)).ToArray());

            using (var client = new HttpClient(handler, false))
            {
                await ResumableDownloader.DownloadAsync(client, "https://example.test/runtime.zip", destination, "runtime", payload.Length, null, CancellationToken.None);
            }

            Assert(handler.RangeHeaders[0] == "bytes=500000-", "Expected a resume attempt for the partial file.");
            Assert(new FileInfo(destination).Length == payload.Length, "Expected a full restart when the server ignores the range.");
            AssertEqual(Sha256(payload), Sha256File(destination));

            // Without a known size a partial file cannot be trusted and is discarded.
            File.WriteAllBytes(destination, new byte[10]);
            Assert(ResumableDownloader.GetResumableLength(destination, null) == 0 && !File.Exists(destination),
                "Expected a partial file of unknown total size to be discarded.");

            // A server that answers a range request from the wrong offset must not have its body stitched on.
            var misbehaving = new FakeFileHandler { MisreportRangeStart = true };
            misbehaving.AddFile("runtime.zip", payload);
            File.WriteAllBytes(destination, payload.Take(400_000).ToArray());
            using (var client = new HttpClient(misbehaving, false))
            {
                await AssertThrowsAsync<InvalidDataException>(
                    () => ResumableDownloader.DownloadAsync(client, "https://example.test/runtime.zip", destination, "runtime", payload.Length, null, CancellationToken.None),
                    "Expected a mismatched resume offset to be rejected.");
            }

            Assert(!File.Exists(destination), "Expected the untrustworthy partial file to be discarded so the next attempt restarts.");
        }

        private static async Task TestProvisionerInstallsResumesAndRemovesSupersededVersions(string testRoot)
        {
            var runtimeArchive = CreateRuntimeArchive();
            var model = CreatePayload(2 * 1024 * 1024 + 1, seed: 3);
            var manifest = new LocalModelManifest(
                installationVersion: "test-model-llama-b1-v2",
                modelDisplayName: "Test 4B",
                modelFileName: "test-4b.gguf",
                modelFileSize: model.Length,
                modelSha256: Sha256(model),
                modelDownloadUrl: "https://example.test/resolve/0123456789abcdef0123456789abcdef01234567/test-4b.gguf?download=true",
                modelSourceUrl: "https://example.test/test-4b",
                modelLicenseName: "Apache License 2.0",
                modelLicenseUrl: "https://example.test/LICENSE",
                runtimeVersion: "b1",
                runtimeArchiveName: "llama-b1-bin-win-cpu-x64.zip",
                runtimeArchiveSize: runtimeArchive.Length,
                runtimeSha256: Sha256(runtimeArchive),
                runtimeDownloadUrl: "https://example.test/releases/download/b1/llama-b1-bin-win-cpu-x64.zip",
                runtimeLicenseName: "MIT License",
                runtimeLicenseUrl: "https://example.test/b1/LICENSE");

            var root = Path.Combine(testRoot, "installs");
            var superseded = Path.Combine(root, "old-model-llama-b0-v1");
            Directory.CreateDirectory(Path.Combine(superseded, "model"));
            File.WriteAllBytes(Path.Combine(superseded, "model", "old.gguf"), new byte[1024]);
            var inUse = Path.Combine(root, "busy-model-llama-b0-v1");
            Directory.CreateDirectory(inUse);

            var handler = new FakeFileHandler();
            handler.AddFile("llama-b1-bin-win-cpu-x64.zip", runtimeArchive);
            handler.AddFile("test-4b.gguf", model);
            handler.FailAfterBytes("test-4b.gguf", 1024 * 1024);

            var options = LocalLlamaServerOptions.CreateManaged(root, manifest);
            var provisioner = new LocalModelProvisioner(options, handler);

            Assert(provisioner.RequiresAgreement, "A fresh installation must ask for license acceptance.");
            Assert(provisioner.RequiresDownload, "A fresh installation must download.");
            await AssertThrowsAsync<InvalidOperationException>(() => provisioner.EnsureInstalledAsync(null, CancellationToken.None),
                "Downloading before the licenses are accepted must be refused.");

            provisioner.RecordAgreementAcceptance();
            Assert(!provisioner.RequiresAgreement, "Acceptance must be remembered for this installation version.");

            using (new FileStream(Path.Combine(inUse, ".install.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                await AssertThrowsAsync<IOException>(() => provisioner.EnsureInstalledAsync(null, CancellationToken.None),
                    "The simulated network failure during the model download must surface.");

                Assert(File.Exists(options.ServerPath), "The runtime must be installed before the model download starts.");
                Assert(File.Exists(Path.Combine(options.InstallationDirectory, ".runtime.complete")), "The runtime marker must be written.");
                Assert(!File.Exists(Path.Combine(options.InstallationDirectory, "runtime-staging")), "Extraction scratch space must be removed.");
                var partial = options.ModelPath + ".download";
                Assert(File.Exists(partial) && new FileInfo(partial).Length == 1024 * 1024,
                    "The interrupted model download must keep its partial file.");
                Assert(!File.Exists(options.ModelPath), "An incomplete model must not be installed.");
                Assert(provisioner.RequiresDownload, "An incomplete model still requires a download.");
                Assert(Directory.Exists(superseded), "Older versions are only removed once the new one is complete.");

                var progress = new List<long>();
                await provisioner.EnsureInstalledAsync(new SynchronousProgress(progress), CancellationToken.None);

                Assert(handler.RangeHeaders.Any(header => header == "bytes=1048576-"), "The model download must resume where it stopped.");
                Assert(File.Exists(options.ModelPath) && new FileInfo(options.ModelPath).Length == model.Length, "The model must be installed.");
                AssertEqual(Sha256(model), Sha256File(options.ModelPath));
                Assert(!File.Exists(partial), "The partial file must be promoted to the final model.");
                Assert(!File.Exists(Path.Combine(options.InstallationDirectory, manifest.RuntimeArchiveName + ".download")),
                    "The verified runtime archive must be removed after extraction.");
                Assert(!provisioner.RequiresDownload, "A complete installation needs no download.");
                Assert(progress.Count > 0 && progress[0] == 1024 * 1024, "Progress must report the resumed offset first.");
                Assert(!Directory.Exists(superseded), "A superseded installation must be removed after a successful install.");
                Assert(Directory.Exists(inUse), "An installation locked by another process must be left alone.");
            }

            // With the lock released the busy directory is removed on the next run.
            provisioner.RemoveSupersededInstallations();
            Assert(!Directory.Exists(inUse), "A released installation must be removed on the next run.");
            Assert(Directory.Exists(options.InstallationDirectory), "The current installation must never be removed.");

            options.Validate();
            var third = handler.Requests;
            await provisioner.EnsureInstalledAsync(null, CancellationToken.None);
            Assert(handler.Requests == third, "A complete installation must not download anything.");
        }

        private static byte[] CreateRuntimeArchive()
        {
            using (var stream = new MemoryStream())
            {
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
                {
                    AddEntry(archive, "llama-b1-bin-win-cpu-x64/llama-server.exe", Encoding.ASCII.GetBytes("not a real executable"));
                    AddEntry(archive, "llama-b1-bin-win-cpu-x64/ggml.dll", Encoding.ASCII.GetBytes("not a real library"));
                    AddEntry(archive, "llama-b1-bin-win-cpu-x64/LICENSE", Encoding.ASCII.GetBytes("MIT"));
                }

                return stream.ToArray();
            }
        }

        private static void AddEntry(ZipArchive archive, string name, byte[] content)
        {
            var entry = archive.CreateEntry(name);
            using (var output = entry.Open())
            {
                output.Write(content, 0, content.Length);
            }
        }

        private static byte[] CreatePayload(int length, int seed)
        {
            var random = new Random(seed);
            var payload = new byte[length];
            random.NextBytes(payload);
            return payload;
        }

        private static string Sha256(byte[] bytes)
        {
            using (var sha256 = SHA256.Create())
            {
                return string.Concat(sha256.ComputeHash(bytes).Select(value => value.ToString("X2")));
            }
        }

        private static string Sha256File(string path)
        {
            return Sha256(File.ReadAllBytes(path));
        }

        private sealed class SynchronousProgress : IProgress<LocalModelDownloadProgress>
        {
            private readonly List<long> values;

            public SynchronousProgress(List<long> values)
            {
                this.values = values;
            }

            public void Report(LocalModelDownloadProgress value)
            {
                lock (values)
                {
                    values.Add(value.BytesReceived);
                }
            }
        }

        /// <summary>Serves in-memory files with Range support and one simulated mid-stream failure per file.</summary>
        private sealed class FakeFileHandler : HttpMessageHandler
        {
            private readonly Dictionary<string, byte[]> files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, long> failures = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

            public bool IgnoreRange { get; set; }

            /// <summary>Answer range requests with a body and Content-Range that start one byte early.</summary>
            public bool MisreportRangeStart { get; set; }

            public int Requests { get; private set; }

            public List<string> RangeHeaders { get; } = new List<string>();

            public void AddFile(string name, byte[] content)
            {
                files[name] = content;
            }

            public void FailAfterBytes(string name, long bytes)
            {
                failures[name] = bytes;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requests++;
                var path = request.RequestUri.AbsolutePath;
                var name = files.Keys.FirstOrDefault(key => path.EndsWith("/" + key, StringComparison.OrdinalIgnoreCase));
                if (name == null) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));

                var payload = files[name];
                var range = request.Headers.Range?.Ranges.FirstOrDefault();
                RangeHeaders.Add(request.Headers.Range?.ToString());

                long from = 0;
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                if (range != null && range.From.HasValue && !IgnoreRange)
                {
                    from = range.From.Value;
                    if (from >= payload.Length)
                    {
                        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.RequestedRangeNotSatisfiable));
                    }

                    if (MisreportRangeStart && from > 0) from--;
                    response.StatusCode = HttpStatusCode.PartialContent;
                }

                long? failAfter = null;
                if (failures.TryGetValue(name, out var failAt))
                {
                    failAfter = failAt;
                    failures.Remove(name);
                }

                response.Content = new StreamContent(new FailingStream(payload, (int)from, failAfter));
                response.Content.Headers.ContentLength = payload.Length - from;
                if (response.StatusCode == HttpStatusCode.PartialContent)
                {
                    response.Content.Headers.ContentRange = new ContentRangeHeaderValue(from, payload.Length - 1, payload.Length);
                }

                return Task.FromResult(response);
            }
        }

        /// <summary>Reads a byte array and throws like a dropped connection once a given number of bytes has been delivered.</summary>
        private sealed class FailingStream : Stream
        {
            private readonly byte[] payload;
            private readonly long? failAfter;
            private int position;
            private long delivered;

            public FailingStream(byte[] payload, int offset, long? failAfter)
            {
                this.payload = payload;
                position = offset;
                this.failAfter = failAfter;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (failAfter.HasValue && delivered >= failAfter.Value)
                {
                    throw new IOException("Simulated network failure.");
                }

                var remaining = payload.Length - position;
                if (remaining <= 0) return 0;

                var toCopy = Math.Min(count, remaining);
                if (failAfter.HasValue) toCopy = (int)Math.Min(toCopy, failAfter.Value - delivered);
                Array.Copy(payload, position, buffer, offset, toCopy);
                position += toCopy;
                delivered += toCopy;
                return toCopy;
            }

            public override void Flush()
            {
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
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

        private static void TryDelete(string directory)
        {
            try
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
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

        private static async Task AssertThrowsAsync<TException>(Func<Task> action, string message) where TException : Exception
        {
            try
            {
                await action().ConfigureAwait(false);
                Assert(false, message + $" (expected {typeof(TException).Name})");
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
