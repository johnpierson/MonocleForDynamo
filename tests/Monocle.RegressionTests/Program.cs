using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Media;
using MonocleViewExtension.BetterSave;
using MonocleViewExtension.Foca;
using MonocleViewExtension.NodeDocumentation;
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
                TestSettingsRoundTripAndAtomicLoad(testRoot);
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
