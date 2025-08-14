using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using Dynamo.Events;
using Dynamo.PackageManager;

namespace MonocleViewExtension.Utilities
{
    public class Globals
    {
        public static PackageManagerExtension PmExtension { get; set; }
        public static Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();
        public static readonly string Version = ExecutingAssembly.GetName().Version.ToString();

        public static string TempPath = Path.GetTempPath();
        public static string ExecutingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string ExtraFolder = ExecutingPath.Replace("bin", "extra");
        public static string SettingsFile = Path.Combine(ExtraFolder, "MonocleSettings.xml");

        public static bool IsFocaEnabled { get; set; } = true;
        public static bool IsFocaAiEnabled { get; set; } = false;
        internal static string OpenAIApiKey { get; set; } 
        public static bool IsConnectoEnabled { get; set; } = false;
        public static bool InCanvasSearchEnabled { get; set; } = true;
        public static string QuickSaveDateFormat { get; set; } = "-yyyy.MM.dd_HH.mm.ss";
        public static string CustomNodeNotePrefix { get; set; } = "Custom Node: ";
        public static Color CustomNodeIdentificationColor = (Color)ColorConverter.ConvertFromString("#ADE4DE");
        public static double CustomNodeBorderThickness = 4;

        internal static Version DynamoVersion { get; set; }
        internal static Version SidebarMinVersion => new Version(2, 5, 0, 7460);
        internal static Version NewUiVersion => new Version(2, 13, 0, 1875);
        public static bool IsDevExpressLoaded { get; set; }
        internal static Assembly DevExpress { get; set; }

        public static Dictionary<string, Settings.GroupSetting> MonocleGroupSettings =
            new Dictionary<string, Settings.GroupSetting>
            {
                {"Group1",new Settings.GroupSetting(){GroupId = 1, GroupColor = "#B9F9E1",GroupText = "Actions", FontSize = 36}},
                {"Group2",new Settings.GroupSetting(){GroupId = 2, GroupColor = "#FFB8D8",GroupText = "Input", FontSize = 36}},
                {"Group3",new Settings.GroupSetting(){GroupId = 3, GroupColor = "#FFC999",GroupText = "Outputs", FontSize = 36}},
                {"Group4",new Settings.GroupSetting(){GroupId = 4, GroupColor = "#A4E1FF",GroupText = "Review", FontSize = 36}},
                {"Group5",new Settings.GroupSetting(){GroupId = 5, GroupColor = "#FFFFA07A",GroupText = "To Revit", FontSize = 36}},
                {"Group6",new Settings.GroupSetting(){GroupId = 6, GroupColor = "#FF87CEFA",GroupText = "Annotation", FontSize = 36}}

            };

        public static Dictionary<string, Settings.GroupSetting> DefaultGroupSettings =
            new Dictionary<string, Settings.GroupSetting>
            {
                {"Group1",new Settings.GroupSetting(){GroupId = 1, GroupColor = "#B9F9E1",GroupText = "Actions", FontSize = 36}},
                {"Group2",new Settings.GroupSetting(){GroupId = 2, GroupColor = "#FFB8D8",GroupText = "Input", FontSize = 36}},
                {"Group3",new Settings.GroupSetting(){GroupId = 3, GroupColor = "#FFC999",GroupText = "Outputs", FontSize = 36}},
                {"Group4",new Settings.GroupSetting(){GroupId = 4, GroupColor = "#A4E1FF",GroupText = "Review", FontSize = 36}},
                {"Group5",new Settings.GroupSetting(){GroupId = 5, GroupColor = "#FFFFA07A",GroupText = "To Revit", FontSize = 36}},
                {"Group6",new Settings.GroupSetting(){GroupId = 6, GroupColor = "#FF87CEFA",GroupText = "Annotation", FontSize = 36}}
            };

    }
}
