using System;
using System.Collections.Generic;
using System.Windows.Media;
using MonocleViewExtension.Utilities;

namespace Dynamo.Logging
{
    public enum WarningLevel
    {
        Mild
    }

    public static class LogMessage
    {
        public static void Warning(string message, WarningLevel level)
        {
        }
    }
}

namespace MonocleViewExtension.Utilities
{
    public class Globals
    {
        public static string SettingsFile { get; set; }
        public static Dictionary<string, Settings.GroupSetting> MonocleGroupSettings { get; set; } = new Dictionary<string, Settings.GroupSetting>();
        public static Color CustomNodeIdentificationColor { get; set; }
        public static double CustomNodeBorderThickness { get; set; }
        public static string CustomNodeNotePrefix { get; set; }
        public static bool IsFocaEnabled { get; set; }
        public static bool IsConnectoEnabled { get; set; }
        public static bool InCanvasSearchEnabled { get; set; }
        public static string QuickSaveDateFormat { get; set; }
    }

    public static class StringUtils
    {
        public static string SetCustomNodeNotePrefix(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix)) return "Custom Node: ";
            return char.IsWhiteSpace(prefix[prefix.Length - 1]) ? prefix : prefix + " ";
        }
    }
}

namespace MonocleViewExtension.Properties
{
    public sealed class UserSettings
    {
        public static UserSettings Default { get; } = new UserSettings();
        public string MonocleSettingsFile { get; set; }
        public void Save()
        {
        }
    }
}
