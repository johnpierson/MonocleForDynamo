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


        public static string ExecutingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string ExtraFolder = ExecutingPath.Replace("bin", "extra");
        public static string SettingsFile = Path.Combine(ExtraFolder, "MonocleSettings.xml");

        public static bool IsFocaEnabled { get; set; } = true;
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
                {"Group1",new Settings.GroupSetting(){GroupId = 1, GroupColor = "#FFD3D3D3",GroupText = "Background", FontSize = 24}},
                {"Group2",new Settings.GroupSetting(){GroupId = 2, GroupColor = "#FFB0C4DE",GroupText = "Input", FontSize = 24}},
                {"Group3",new Settings.GroupSetting(){GroupId = 3, GroupColor = "#FF90EE90",GroupText = "Control", FontSize = 24}},
                {"Group4",new Settings.GroupSetting(){GroupId = 4, GroupColor = "#FFFFA07A",GroupText = "To Revit", FontSize = 24}},
                {"Group5",new Settings.GroupSetting(){GroupId = 5, GroupColor = "#FF87CEFA",GroupText = "Annotation", FontSize = 24}},
                {"Group6",new Settings.GroupSetting(){GroupId = 6, GroupColor = "#FFFFE4C4",GroupText = "Info", FontSize = 24}}

            };

        public static Dictionary<string, Settings.GroupSetting> DefaultGroupSettings =
            new Dictionary<string, Settings.GroupSetting>
            {
                {"Group1",new Settings.GroupSetting(){GroupId = 1, GroupColor = "#FFD3D3D3",GroupText = "Background", FontSize = 24}},
                {"Group2",new Settings.GroupSetting(){GroupId = 2, GroupColor = "#FFB0C4DE",GroupText = "Input", FontSize = 24}},
                {"Group3",new Settings.GroupSetting(){GroupId = 3, GroupColor = "#FF90EE90",GroupText = "Control", FontSize = 24}},
                {"Group4",new Settings.GroupSetting(){GroupId = 4, GroupColor = "#FFFFA07A",GroupText = "To Revit", FontSize = 24}},
                {"Group5",new Settings.GroupSetting(){GroupId = 5, GroupColor = "#FF87CEFA",GroupText = "Annotation", FontSize = 24}},
                {"Group6",new Settings.GroupSetting(){GroupId = 6, GroupColor = "#FFFFE4C4",GroupText = "Info", FontSize = 24}}

            };

    }
}
