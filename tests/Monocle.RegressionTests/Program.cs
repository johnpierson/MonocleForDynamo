using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Media;
using MonocleExtension;
using MonocleViewExtension.About;
using MonocleViewExtension.BetterSave;
using MonocleViewExtension.Foca;
using MonocleViewExtension.NodeDocumentation;
using MonocleViewExtension.PackageUsage;
using MonocleViewExtension.SimpleSearch;
using MonocleViewExtension.Utilities;

namespace Monocle.RegressionTests
{
    internal static class Program
    {
        private static int Main()
        {
            var testRoot = Path.Combine(Path.GetTempPath(), "MonocleRegressionTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testRoot);

            try
            {
                TestQuickSavePathConstruction(testRoot);
                TestDocumentationHelpers(testRoot);
                TestFocaCodeGeneration();
                TestDynamoBuildVersionFallbacks();
                TestSettingsRoundTripAndAtomicLoad(testRoot);
                TestPackageNameResolver();
                TestUpdateCheck(testRoot);
                TestSearchKey();
                TestDeployManifest(testRoot);
                Console.WriteLine("Monocle regression tests passed.");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
            finally
            {
                if (Directory.Exists(testRoot)) Directory.Delete(testRoot, true);
            }
        }

        private static void TestQuickSavePathConstruction(string testRoot)
        {
            var sourceDirectory = Path.Combine(testRoot, "folder.dyn");
            var sourcePath = Path.Combine(sourceDirectory, "my.graph.v2.DYN");
            var timestamp = new DateTime(2026, 1, 2, 3, 4, 5);

            var result = QuickSavePathBuilder.Build(sourcePath, "-yyyy.MM.dd_HH.mm.ss", timestamp);

            Assert(Path.GetDirectoryName(result) == Path.GetFullPath(sourceDirectory), "Quick Save changed the source directory.");
            Assert(Path.GetExtension(result) == ".DYN", "Quick Save did not preserve extension casing.");
            Assert(Path.GetFileName(result) == "my.graph.v2-2026.01.02_03.04.05.DYN", "Quick Save produced an unexpected filename.");
            Assert(!string.Equals(result, sourcePath, StringComparison.OrdinalIgnoreCase), "Quick Save reused the source path.");
            Assert(sourcePath == Path.Combine(sourceDirectory, "my.graph.v2.DYN"), "Quick Save changed the source path.");

            var normalSourcePath = Path.Combine(testRoot, "normal.dyn");
            var normalResult = QuickSavePathBuilder.Build(normalSourcePath, "-yyyy", timestamp);
            Assert(Path.GetFileName(normalResult) == "normal-2026.dyn", "Quick Save did not handle a normal .dyn path.");

            AssertThrows<ArgumentException>(() => QuickSavePathBuilder.Build(sourcePath, "yyyy/MM/dd", timestamp), "Invalid date format output was accepted.");
        }

        private static void TestDocumentationHelpers(string testRoot)
        {
            var imageDirectory = Path.Combine(testRoot, "img", "docs");
            Directory.CreateDirectory(imageDirectory);
            var outputPath = Path.Combine(imageDirectory, "myimg_img.jpg");
            var temporaryPath = DocumentationImagePaths.BuildTemporaryPath(outputPath, "_background");
            Assert(Path.GetDirectoryName(temporaryPath) == Path.GetFullPath(imageDirectory), "Temporary image path changed its directory.");
            Assert(Path.GetFileName(temporaryPath).StartsWith("myimg_img_background_", StringComparison.Ordinal), "Temporary image path changed the filename stem.");

            Assert(NodeDocumentation.ParseFullDescription("## Example File\n\n![Node](./Node_img.jpg)") == string.Empty, "Sample-only markdown did not parse as empty description.");
            Assert(NodeDocumentation.ParseFullDescription("## In Depth\nDetails\n___\n## Example File") == "Details", "Full markdown description did not round-trip.");

            var documentation = new NodeDocumentation(imageDirectory, "Node.Name", "Node");
            documentation.Validate();
            Assert(Path.GetFileName(documentation.SampleGraph) == "Node.Name.dyn", "Documentation path was not built from the filename.");
        }

        private static void TestFocaCodeGeneration()
        {
            var code = FocaCodeGenerator.BuildRevitElementCode("Element\r\nselected", 42);
            Assert(code == "//Element  selected\nRevit.Elements.ElementSelector.ByElementId(42);", "FOCA generated executable code inside the comment.");
        }

        private static void TestDynamoBuildVersionFallbacks()
        {
            var candidates = DynamoBuildVersionResolver.GetCandidates(new Version(3, 7, 0)).ToArray();
            var expected = new[] { "3.7", "3.6", "3.5", "3.4", "3.3", "3.2", "3.1", "3.0" };

            Assert(candidates.SequenceEqual(expected), "Dynamo build fallback candidates were not ordered from closest to oldest.");

            var fourPointZeroCandidates = DynamoBuildVersionResolver.GetCandidates(new Version(4, 0, 0)).ToArray();
            Assert(fourPointZeroCandidates.SequenceEqual(new[] { "4.0" }), "Dynamo build fallback crossed below the major version boundary.");
            var fourPointThreeCandidates = DynamoBuildVersionResolver.GetCandidates(new Version(4, 3, 0)).ToArray();
            Assert(fourPointThreeCandidates.SequenceEqual(new[] { "4.3", "4.2", "4.1", "4.0" }), "Dynamo 4.3 build fallback candidates were not ordered from closest to oldest.");
            AssertThrows<ArgumentNullException>(() => DynamoBuildVersionResolver.GetCandidates(null).ToArray(), "Null Dynamo versions were accepted.");
        }

        private static void TestPackageNameResolver()
        {
            // library order, with duplicates (one entry per node in the library)
            var roots = new[] { "Clockwork", "archi-lab", "Clockwork", "springs", "Rhythm", "archilab_Bumblebee" };
            var local = new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string>("spring nodes", "204.1.0"),
                new System.Collections.Generic.KeyValuePair<string, string>("Rhythm", "2026.1.1"),
                new System.Collections.Generic.KeyValuePair<string, string>("Crumple", "2025.6.1"),
            };
            var resolver = new PackageNameResolver(roots, local);

            Assert(resolver.Roots.SequenceEqual(new[] { "Clockwork", "archi-lab", "springs", "Rhythm", "archilab_Bumblebee", "spring nodes", "Crumple" }),
                "Package roots were not de-duplicated in first-seen order.");
            Assert(resolver.IsCustomCategory("Clockwork.Lists.Actions"), "Package category was not recognised.");
            Assert(resolver.IsCustomCategory("rhythm.Revit.Elements"), "Package prefix match should be case-insensitive.");
            Assert(!resolver.IsCustomCategory("Core.List.Create"), "Core category was treated as a package.");
            Assert(!resolver.IsCustomCategory(null), "Null category was treated as a package.");
            Assert(resolver.GetPackageName("Clockwork.Lists") == "Clockwork", "Exact top-level category did not resolve.");
            Assert(resolver.GetPackageName("springs.Collector") == "spring nodes", "springs was not renamed to spring nodes.");
            Assert(resolver.GetPackageName("archilab_Bumblebee.Excel") == "BumbleBee", "Bumblebee name fix was lost.");
            Assert(resolver.GetPackageName("Rhythm.Revit") == "Rhythm", "Package name resolution changed.");
            Assert(resolver.GetPackageVersion("spring nodes") == "v.204.1.0", "Version lookup did not use the simplified name.");
            Assert(resolver.GetPackageVersion("Clockwork") == string.Empty, "A package that is not installed got a version.");
            Assert(PackageNameResolver.Levenshtein("kitten", "sitting") == 3, "Levenshtein distance is wrong.");
            Assert(PackageNameResolver.Levenshtein("", "abc") == 3 && PackageNameResolver.Levenshtein("abc", "") == 3, "Levenshtein empty cases are wrong.");
            Assert(new PackageNameResolver(null, null).GetPackageName("Anything.Else") == string.Empty, "An empty library should resolve to an empty name, not throw.");
        }

        private static void TestUpdateCheck(string testRoot)
        {
            Assert(UpdateCheck.ParseTagName("{\"url\":\"x\",\"tag_name\": \"2026.10.1\",\"name\":\"y\"}") == "2026.10.1", "tag_name was not parsed.");
            Assert(UpdateCheck.ParseTagName("{}") == null, "Missing tag_name should parse as null.");
            Assert(!UpdateCheck.IsNewer("2026.6.1.0", "2026.6.1"), "Equal versions were reported as an update.");
            Assert(UpdateCheck.IsNewer("2026.6.1.0", "2026.10.1"), "A newer release was not detected.");
            Assert(!UpdateCheck.IsNewer("2026.10.1.0", "2026.6.1"), "An older release was reported as an update.");
            Assert(UpdateCheck.IsNewer("2026.6.1.0", "v2026.6.2"), "A v-prefixed tag was not handled.");
            Assert(!UpdateCheck.IsNewer("2026.6.1.0", "not-a-version"), "An unparsable tag was reported as an update.");

            var now = new DateTime(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);
            Assert(UpdateCheck.IsDue(null, now), "First check should be due.");
            Assert(!UpdateCheck.IsDue(now.AddHours(-2), now), "Check repeated within a day.");
            Assert(UpdateCheck.IsDue(now.AddHours(-25), now), "Check not due after a day.");
            Assert(UpdateCheck.IsDue(now.AddHours(5), now), "A future timestamp (clock change) should force a check.");

            var cache = Path.Combine(testRoot, "update", "update-check.txt");
            UpdateCheck.WriteCache(cache, now, "2026.10.1");
            Assert(UpdateCheck.TryReadCache(cache, out var read, out var tag) && read == now && tag == "2026.10.1", "Update cache did not round-trip.");
            File.WriteAllText(cache, "garbage");
            Assert(!UpdateCheck.TryReadCache(cache, out _, out _), "A corrupt update cache was accepted.");
        }

        private static void TestSearchKey()
        {
            Assert(SearchKey.Normalize("List.Create Thing") == "LISTCREATETHING", "Search normalization changed.");
            Assert(SearchKey.Matches(SearchKey.Normalize("Core.List.Create"), SearchKey.Normalize("list create")), "Spaces/dots should be ignored when matching.");
            Assert(!SearchKey.Matches(SearchKey.Normalize("Core.List.Create"), SearchKey.Normalize("")), "Empty filter should match nothing.");
            Assert(!SearchKey.Matches(SearchKey.Normalize("Core.List.Create"), SearchKey.Normalize("flatten")), "Unrelated filter matched.");
        }

        private static void TestDeployManifest(string testRoot)
        {
            var a = new string('a', 64);
            var b = new string('B', 64);
            var text = $"{a}  2.19/MonocleViewExtension.dll\r\n{b} *4.2/MonocleViewExtension.dll\nnot a line\n{a}  4.3/Other.dll\n";
            var manifest = DeployManifest.Parse(text);
            Assert(manifest.Count == 2 && manifest["2.19"] == a && manifest["4.2"] == new string('b', 64), "SHA256SUMS was not parsed (text and binary mode, CRLF).");

            Assert(DeployManifest.ChooseBuild(new Version(4, 3, 0), manifest.Keys) == "4.2", "Closest older build in the same major was not chosen.");
            Assert(DeployManifest.ChooseBuild(new Version(3, 6, 0), manifest.Keys) == null, "A build from another major version was chosen.");
            Assert(DeployManifest.ChooseBuild(new Version(2, 19, 5), manifest.Keys) == "2.19", "Exact build was not chosen.");
            Assert(DeployManifest.IsBuildName("4.2") && DeployManifest.IsBuildName("2.19") && !DeployManifest.IsBuildName("de-DE") && !DeployManifest.IsBuildName("4"), "Build folder detection is wrong.");

            Assert(DeployManifest.DefinitionXml("4.2").Contains(@"<AssemblyPath>..\bin\4.2\MonocleViewExtension.dll</AssemblyPath>"), "Definition does not point at the build folder.");
            Assert(DeployManifest.DefinitionXml(null).Contains(@"<AssemblyPath>..\bin\MonocleViewExtension.dll</AssemblyPath>"), "Legacy definition path changed.");

            var file = Path.Combine(testRoot, "hash.bin");
            File.WriteAllText(file, "abc");
            Assert(DeployManifest.Sha256(file) == "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", "SHA-256 is wrong.");
        }

        private static void TestSettingsRoundTripAndAtomicLoad(string testRoot)
        {
            var settingsPath = Path.Combine(testRoot, "MonocleSettings.xml");
            Globals.SettingsFile = settingsPath;
            Globals.MonocleGroupSettings = new System.Collections.Generic.Dictionary<string, Settings.GroupSetting>
            {
                { "Group9", new Settings.GroupSetting { GroupId = 9, GroupText = "Test", GroupColor = "#112233", FontSize = 24 } }
            };
            Globals.CustomNodeIdentificationColor = Colors.Magenta;
            Globals.CustomNodeBorderThickness = 7;
            Globals.CustomNodeNotePrefix = "Before ";
            Globals.IsFocaEnabled = false;
            Globals.IsConnectoEnabled = true;
            Globals.InCanvasSearchEnabled = false;
            Globals.QuickSaveDateFormat = "-yyyy";

            var validSettings = new Settings.MonocleSettings
            {
                Settings = Globals.MonocleGroupSettings.Values.ToList(),
                CustomNodeIdentifierSettings = new Settings.CustomNodeIdentifierSettings { CustomNodeColor = "#123456", BorderThickness = 3 },
                CustomNodeNotePrefix = "Loaded",
                IsFocaEnabled = true,
                IsConnectoEnabled = false,
                InCanvasSearchEnabled = true,
                QuickSaveDateFormat = "-yyyy"
            };
            Settings.SerializeModels(settingsPath, validSettings);
            Globals.MonocleGroupSettings = new System.Collections.Generic.Dictionary<string, Settings.GroupSetting>
            {
                { "Group1", new Settings.GroupSetting { GroupId = 1, GroupText = "Sentinel", GroupColor = "#000000", FontSize = 20 } }
            };

            Assert(Settings.LoadMonocleSettings(), "Valid settings did not load.");
            Assert(Globals.MonocleGroupSettings.ContainsKey("Group9"), "Valid settings groups did not load.");
            Assert(Globals.CustomNodeBorderThickness == 3, "Valid settings values did not load.");

            var beforeInvalidLoad = Globals.MonocleGroupSettings["Group9"];
            var duplicateSettings = new Settings.MonocleSettings
            {
                Settings = new System.Collections.Generic.List<Settings.GroupSetting>
                {
                    new Settings.GroupSetting { GroupId = 9, GroupText = "First", GroupColor = "#123456", FontSize = 24 },
                    new Settings.GroupSetting { GroupId = 9, GroupText = "Second", GroupColor = "#123456", FontSize = 24 }
                },
                CustomNodeIdentifierSettings = validSettings.CustomNodeIdentifierSettings,
                QuickSaveDateFormat = "-yyyy"
            };
            Settings.SerializeModels(settingsPath, duplicateSettings);
            Assert(!Settings.LoadMonocleSettings(), "Duplicate group IDs were accepted.");
            Assert(ReferenceEquals(beforeInvalidLoad, Globals.MonocleGroupSettings["Group9"]), "Failed settings load changed live settings.");

            var missingSectionSettings = new Settings.MonocleSettings
            {
                Settings = validSettings.Settings,
                QuickSaveDateFormat = "-yyyy"
            };
            Settings.SerializeModels(settingsPath, missingSectionSettings);
            Assert(!Settings.LoadMonocleSettings(), "Missing settings sections were accepted.");
            Assert(ReferenceEquals(beforeInvalidLoad, Globals.MonocleGroupSettings["Group9"]), "Missing sections changed live settings.");

            var invalidColorSettings = new Settings.MonocleSettings
            {
                Settings = new System.Collections.Generic.List<Settings.GroupSetting>
                {
                    new Settings.GroupSetting { GroupId = 9, GroupText = "Test", GroupColor = "not-a-color", FontSize = 24 }
                },
                CustomNodeIdentifierSettings = validSettings.CustomNodeIdentifierSettings,
                QuickSaveDateFormat = "-yyyy"
            };
            Settings.SerializeModels(settingsPath, invalidColorSettings);
            Assert(!Settings.LoadMonocleSettings(), "Invalid colors were accepted.");
            Assert(ReferenceEquals(beforeInvalidLoad, Globals.MonocleGroupSettings["Group9"]), "Invalid colors changed live settings.");

            File.WriteAllText(settingsPath, "<MonocleSettings>");
            Assert(!Settings.LoadMonocleSettings(), "Malformed XML was accepted.");
            Assert(ReferenceEquals(beforeInvalidLoad, Globals.MonocleGroupSettings["Group9"]), "Malformed XML changed live settings.");

            var existingContent = File.ReadAllText(settingsPath);
            AssertThrows<InvalidOperationException>(() => Settings.SerializeModels(settingsPath, new UnserializableSettings
            {
                Settings = validSettings.Settings,
                CustomNodeIdentifierSettings = validSettings.CustomNodeIdentifierSettings,
                QuickSaveDateFormat = "-yyyy"
            }), "Unsupported settings serialization unexpectedly succeeded.");
            Assert(File.ReadAllText(settingsPath) == existingContent, "Failed settings serialization changed the previous file.");

            Settings.SerializeModels(settingsPath, validSettings);
            File.SetAttributes(settingsPath, FileAttributes.ReadOnly);
            try
            {
                Assert(Settings.LoadMonocleSettings(), "Read-only settings input did not load.");
            }
            finally
            {
                File.SetAttributes(settingsPath, FileAttributes.Normal);
            }
        }

        private sealed class UnserializableSettings : Settings.MonocleSettings
        {
            public Action Unsupported { get; set; }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void AssertThrows<TException>(Action action, string message) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException(message);
        }
    }
}
