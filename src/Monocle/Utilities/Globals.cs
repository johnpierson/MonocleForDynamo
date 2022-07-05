using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Media;
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
                {"Group1",new Settings.GroupSetting(){GroupId = 1, GroupColor = "#FFD3D3D3",GroupText = "Background"}},
                {"Group2",new Settings.GroupSetting(){GroupId = 2, GroupColor = "#FFB0C4DE",GroupText = "Input"}},
                {"Group3",new Settings.GroupSetting(){GroupId = 3, GroupColor = "#FF90EE90",GroupText = "Control"}},
                {"Group4",new Settings.GroupSetting(){GroupId = 4, GroupColor = "#FFFFA07A",GroupText = "To Revit"}},
                {"Group5",new Settings.GroupSetting(){GroupId = 5, GroupColor = "#FF87CEFA",GroupText = "Annotation"}},
                {"Group6",new Settings.GroupSetting(){GroupId = 6, GroupColor = "#FFFFE4C4",GroupText = "Info"}}

            };

        public static Dictionary<string, Settings.GroupSetting> DefaultGroupSettings =
            new Dictionary<string, Settings.GroupSetting>
            {
                {"Group1",new Settings.GroupSetting(){GroupId = 1, GroupColor = "#FFD3D3D3",GroupText = "Background"}},
                {"Group2",new Settings.GroupSetting(){GroupId = 2, GroupColor = "#FFB0C4DE",GroupText = "Input"}},
                {"Group3",new Settings.GroupSetting(){GroupId = 3, GroupColor = "#FF90EE90",GroupText = "Control"}},
                {"Group4",new Settings.GroupSetting(){GroupId = 4, GroupColor = "#FFFFA07A",GroupText = "To Revit"}},
                {"Group5",new Settings.GroupSetting(){GroupId = 5, GroupColor = "#FF87CEFA",GroupText = "Annotation"}},
                {"Group6",new Settings.GroupSetting(){GroupId = 6, GroupColor = "#FFFFE4C4",GroupText = "Info"}}

            };

    }
}
