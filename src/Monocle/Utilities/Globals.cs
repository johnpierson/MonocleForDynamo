using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using Dynamo.PackageManager;
using MonocleViewExtension.Core;

namespace MonocleViewExtension.Utilities
{
    /// <summary>
    /// Compatibility shim over <see cref="MonocleContext"/>. Features read their state from the
    /// context they are handed; this exists only until the last of them has been migrated, at
    /// which point the whole class goes away.
    /// </summary>
    public class Globals
    {
        private static MonocleSettingsService SettingsService => MonocleContext.Current?.Settings;

        public static PackageManagerExtension PmExtension => MonocleContext.Current?.PmExtension;

        public static Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();
        public static readonly string Version = ExecutingAssembly.GetName().Version.ToString();

        public static string TempPath = Path.GetTempPath();

        public static string SettingsFile
        {
            get => SettingsService?.SettingsFilePath;
            set { if (SettingsService != null) SettingsService.SettingsFilePath = value; }
        }

        public static bool IsFocaEnabled
        {
            get => SettingsService?.Current.IsFocaEnabled ?? true;
            set { if (SettingsService != null) SettingsService.Current.IsFocaEnabled = value; }
        }

        public static bool IsConnectoEnabled
        {
            get => SettingsService?.Current.IsConnectoEnabled ?? false;
            set { if (SettingsService != null) SettingsService.Current.IsConnectoEnabled = value; }
        }

        public static bool InCanvasSearchEnabled => SettingsService?.Current.InCanvasSearchEnabled ?? true;

        public static string QuickSaveDateFormat => SettingsService?.Current.QuickSaveDateFormat ?? "-yyyy.MM.dd_HH.mm.ss";

        public static string CustomNodeNotePrefix => SettingsService?.Current.CustomNodeNotePrefix ?? "Custom Node: ";

        public static Color CustomNodeIdentificationColor =>
            (Color)ColorConverter.ConvertFromString(
                SettingsService?.Current.CustomNodeIdentifierSettings.CustomNodeColor ?? "#ADE4DE");

        public static double CustomNodeBorderThickness =>
            SettingsService?.Current.CustomNodeIdentifierSettings.BorderThickness ?? 4;

        internal static Version DynamoVersion => MonocleContext.Current?.DynamoVersion ?? new Version(0, 0);
        internal static Version NewUiVersion => new Version(2, 13, 0, 1875);

        public static bool IsDevExpressLoaded { get; set; }
        internal static Assembly DevExpress { get; set; }

        public static IReadOnlyDictionary<string, GroupSetting> MonocleGroupSettings =>
            SettingsService?.GroupsByKey ?? new Dictionary<string, GroupSetting>();
    }
}
