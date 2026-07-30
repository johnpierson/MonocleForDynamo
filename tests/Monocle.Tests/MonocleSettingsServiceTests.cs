using System;
using System.Collections.Generic;
using System.IO;
using MonocleViewExtension.Core;
using Xunit;

namespace Monocle.Tests
{
    public class MonocleSettingsServiceTests : IDisposable
    {
        private readonly string _folder;
        private readonly string _settingsFile;
        private readonly RecordingLogger _log = new RecordingLogger();

        public MonocleSettingsServiceTests()
        {
            _folder = Path.Combine(Path.GetTempPath(), "monocle-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            _settingsFile = Path.Combine(_folder, "MonocleSettings.xml");
        }

        public void Dispose()
        {
            try { Directory.Delete(_folder, recursive: true); } catch (IOException) { }
        }

        private MonocleSettingsService NewService() => new MonocleSettingsService(_settingsFile, _log);

        [Fact]
        public void SaveThenLoad_RoundTripsEveryField()
        {
            var service = NewService();
            service.Current.QuickSaveDateFormat = "-yyyyMMdd";
            service.Current.CustomNodeNotePrefix = "Pkg: ";
            service.Current.IsFocaEnabled = false;
            service.Current.IsConnectoEnabled = true;
            service.Current.InCanvasSearchEnabled = false;
            service.Current.Settings[0].GroupText = "Renamed";
            service.Current.Settings[0].GroupColor = "#123456";
            service.Current.CustomNodeIdentifierSettings.BorderThickness = 7;

            Assert.True(service.Save());

            var reloaded = NewService();
            Assert.True(reloaded.Load());

            Assert.Equal("-yyyyMMdd", reloaded.Current.QuickSaveDateFormat);
            Assert.Equal("Pkg: ", reloaded.Current.CustomNodeNotePrefix);
            Assert.False(reloaded.Current.IsFocaEnabled);
            Assert.True(reloaded.Current.IsConnectoEnabled);
            Assert.False(reloaded.Current.InCanvasSearchEnabled);
            Assert.Equal("Renamed", reloaded.Current.Settings[0].GroupText);
            Assert.Equal("#123456", reloaded.Current.Settings[0].GroupColor);
            Assert.Equal(7, reloaded.Current.CustomNodeIdentifierSettings.BorderThickness);
        }

        [Fact]
        public void RestoreDefaults_ThenEdit_LeavesLaterRestoresIntact()
        {
            // Regression: restore used to assign the defaults dictionary by reference, so the
            // first restore aliased live settings onto the defaults and every edit after that
            // rewrote them. A second restore then restored the edits.
            var service = NewService();

            service.RestoreDefaults();
            service.Current.Settings[0].GroupText = "Edited after restoring";
            service.Current.Settings[0].GroupColor = "#000000";
            service.RestoreDefaults();

            Assert.Equal("Actions", service.Current.Settings[0].GroupText);
            Assert.Equal("#B9F9E1", service.Current.Settings[0].GroupColor);
        }

        [Fact]
        public void Load_OnCorruptFile_KeepsCurrentSettingsAndLogs()
        {
            File.WriteAllText(_settingsFile, "this is not xml at all <<<");

            var service = NewService();
            service.Current.QuickSaveDateFormat = "-sentinel";

            Assert.False(service.Load());
            Assert.Equal("-sentinel", service.Current.QuickSaveDateFormat);
            Assert.Contains(_log.Warnings, w => w.Contains("Could not read settings"));
        }

        [Fact]
        public void Load_OnMissingFile_IsNotAnError()
        {
            var service = NewService();

            Assert.False(service.Load());
            Assert.Empty(_log.Warnings);
        }

        [Fact]
        public void Save_LeavesNoTemporaryFileBehind()
        {
            var service = NewService();

            Assert.True(service.Save());

            Assert.True(File.Exists(_settingsFile));
            Assert.False(File.Exists(_settingsFile + ".tmp"));
        }

        [Fact]
        public void Save_OverExistingFile_ReplacesItCompletely()
        {
            var service = NewService();
            service.Current.QuickSaveDateFormat = "-first";
            service.Save();
            var firstLength = new FileInfo(_settingsFile).Length;

            service.Current.QuickSaveDateFormat = "-second-much-longer-format-string";
            Assert.True(service.Save());

            var reloaded = NewService();
            reloaded.Load();
            Assert.Equal("-second-much-longer-format-string", reloaded.Current.QuickSaveDateFormat);
            Assert.NotEqual(firstLength, new FileInfo(_settingsFile).Length);
        }

        [Fact]
        public void Apply_TakesACopyRatherThanTheCallersObject()
        {
            var service = NewService();
            var edited = MonocleSettings.CreateDefault();
            edited.Settings[0].GroupText = "From dialog";

            service.Apply(edited);

            // The dialog keeps editing its own copy after Apply; that must not reach live settings.
            edited.Settings[0].GroupText = "Typed after applying";

            Assert.Equal("From dialog", service.Current.Settings[0].GroupText);
        }

        [Fact]
        public void Apply_RaisesSettingsChanged()
        {
            var service = NewService();
            var raised = 0;
            service.SettingsChanged += (s, e) => raised++;

            service.Apply(MonocleSettings.CreateDefault());
            service.RestoreDefaults();

            Assert.Equal(2, raised);
        }

        [Fact]
        public void GroupsByKey_UsesTheKeysTheUiAsksFor()
        {
            var service = NewService();

            var groups = service.GroupsByKey;

            Assert.Equal(6, groups.Count);
            Assert.True(groups.ContainsKey("Group1"));
            Assert.True(groups.ContainsKey("Group6"));
            Assert.Equal("Actions", groups["Group1"].GroupText);
        }

        private sealed class RecordingLogger : IMonocleLogger
        {
            public List<string> Warnings { get; } = new List<string>();
            public void Info(string message) { }
            public void Warn(string message, Exception exception = null) => Warnings.Add(message);
            public void Error(string message, Exception exception = null) => Warnings.Add(message);
        }
    }
}
