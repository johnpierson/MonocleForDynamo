using System.Linq;
using MonocleViewExtension.Core;
using Xunit;

namespace Monocle.Tests
{
    public class MonocleSettingsTests
    {
        [Fact]
        public void CreateDefault_ReturnsAnIndependentGraphEveryTime()
        {
            var first = MonocleSettings.CreateDefault();
            var second = MonocleSettings.CreateDefault();

            first.Settings[0].GroupText = "mutated";

            Assert.Equal("Actions", second.Settings[0].GroupText);
        }

        [Fact]
        public void CreateDefault_ShipsSixGroups()
        {
            var defaults = MonocleSettings.CreateDefault();

            Assert.Equal(6, defaults.Settings.Count);
            Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, defaults.Settings.Select(s => s.GroupId));
        }

        [Fact]
        public void Connectomatic_IsOffByDefault()
        {
            // The menu checkbox reads this. When the two disagreed, the tool ran while the menu
            // said it was off.
            Assert.False(MonocleSettings.CreateDefault().IsConnectoEnabled);
        }

        [Fact]
        public void Clone_DoesNotShareGroupSettings()
        {
            var original = MonocleSettings.CreateDefault();
            var copy = original.Clone();

            copy.Settings[2].GroupColor = "#000000";
            copy.CustomNodeIdentifierSettings.BorderThickness = 99;

            Assert.Equal("#FFC999", original.Settings[2].GroupColor);
            Assert.Equal(4, original.CustomNodeIdentifierSettings.BorderThickness);
        }

        [Fact]
        public void FillGapsFromDefaults_RestoresMissingGroupsInOrder()
        {
            var settings = new MonocleSettings
            {
                Settings = { new GroupSetting { GroupId = 4, GroupColor = "#111111", GroupText = "Mine" } }
            };

            settings.FillGapsFromDefaults();

            Assert.Equal(6, settings.Settings.Count);
            Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, settings.Settings.Select(s => s.GroupId));
            Assert.Equal("Mine", settings.Settings.Single(s => s.GroupId == 4).GroupText);
        }

        [Fact]
        public void FillGapsFromDefaults_SuppliesEveryNullField()
        {
            var settings = new MonocleSettings();

            settings.FillGapsFromDefaults();

            Assert.NotNull(settings.CustomNodeIdentifierSettings);
            Assert.NotEmpty(settings.CustomNodeIdentifierSettings.CustomNodeColor);
            Assert.True(settings.CustomNodeIdentifierSettings.BorderThickness > 0);
            Assert.NotEmpty(settings.CustomNodeNotePrefix);
            Assert.NotEmpty(settings.QuickSaveDateFormat);
        }

        [Theory]
        [InlineData("Package:", "Package: ")]
        [InlineData("Package: ", "Package: ")]
        [InlineData("", "Custom Node: ")]
        [InlineData(null, "Custom Node: ")]
        public void NotePrefix_AlwaysEndsInWhitespace(string configured, string expected)
        {
            // The prefix is concatenated straight onto a node name, so a missing trailing space
            // produces "Custom Node:Point.ByCoordinates".
            var settings = new MonocleSettings { CustomNodeNotePrefix = configured };

            settings.FillGapsFromDefaults();

            Assert.Equal(expected, settings.CustomNodeNotePrefix);
        }
    }
}
