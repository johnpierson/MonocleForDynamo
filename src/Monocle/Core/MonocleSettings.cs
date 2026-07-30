using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace MonocleViewExtension.Core
{
    public class GroupSetting
    {
        [XmlElement("GroupId")]
        public int GroupId { get; set; }

        [XmlElement("Text")]
        public string GroupText { get; set; }

        [XmlElement("Color")]
        public string GroupColor { get; set; }

        [XmlElement("FontSize")]
        public int FontSize { get; set; } = 36;

        public GroupSetting Clone() => new GroupSetting
        {
            GroupId = GroupId,
            GroupText = GroupText,
            GroupColor = GroupColor,
            FontSize = FontSize
        };
    }

    public class CustomNodeIdentifierSettings
    {
        [XmlElement("Color")]
        public string CustomNodeColor { get; set; }

        [XmlElement("BorderThickness")]
        public double BorderThickness { get; set; }

        public CustomNodeIdentifierSettings Clone() => new CustomNodeIdentifierSettings
        {
            CustomNodeColor = CustomNodeColor,
            BorderThickness = BorderThickness
        };
    }

    /// <summary>
    /// Everything a user can configure, serialized to MonocleSettings.xml.
    /// </summary>
    public class MonocleSettings
    {
        [XmlArray(ElementName = "GroupSettings")]
        public List<GroupSetting> Settings { get; set; } = new List<GroupSetting>();

        [XmlElement("CustomNodeIdentifierSettings")]
        public CustomNodeIdentifierSettings CustomNodeIdentifierSettings { get; set; }

        [XmlElement("CustomNodeNotePrefix")]
        public string CustomNodeNotePrefix { get; set; }

        [XmlElement("IsFocaEnabled")]
        public bool IsFocaEnabled { get; set; }

        [XmlElement("IsConnectoEnabled")]
        public bool IsConnectoEnabled { get; set; }

        [XmlElement("InCanvasSearchEnabled")]
        public bool InCanvasSearchEnabled { get; set; }

        [XmlElement("QuickSaveDateFormat")]
        public string QuickSaveDateFormat { get; set; }

        /// <summary>
        /// The one and only place Monocle's defaults are declared. Every call returns a fresh
        /// object graph, so handing these out can never alias the caller's live settings.
        /// </summary>
        public static MonocleSettings CreateDefault() => new MonocleSettings
        {
            Settings = new List<GroupSetting>
            {
                new GroupSetting { GroupId = 1, GroupColor = "#B9F9E1", GroupText = "Actions",    FontSize = 36 },
                new GroupSetting { GroupId = 2, GroupColor = "#FFB8D8", GroupText = "Input",      FontSize = 36 },
                new GroupSetting { GroupId = 3, GroupColor = "#FFC999", GroupText = "Outputs",    FontSize = 36 },
                new GroupSetting { GroupId = 4, GroupColor = "#A4E1FF", GroupText = "Review",     FontSize = 36 },
                new GroupSetting { GroupId = 5, GroupColor = "#FFFFA07A", GroupText = "To Revit", FontSize = 36 },
                new GroupSetting { GroupId = 6, GroupColor = "#FF87CEFA", GroupText = "Annotation", FontSize = 36 }
            },
            CustomNodeIdentifierSettings = new CustomNodeIdentifierSettings
            {
                CustomNodeColor = "#ADE4DE",
                BorderThickness = 4
            },
            CustomNodeNotePrefix = "Custom Node: ",
            IsFocaEnabled = true,
            // Connect-o-matic splices nodes into wires on Alt+drop, which surprises people who
            // did not ask for it. Opt-in.
            IsConnectoEnabled = false,
            InCanvasSearchEnabled = true,
            QuickSaveDateFormat = "-yyyy.MM.dd_HH.mm.ss"
        };

        public MonocleSettings Clone() => new MonocleSettings
        {
            Settings = Settings?.Select(s => s.Clone()).ToList() ?? new List<GroupSetting>(),
            CustomNodeIdentifierSettings = CustomNodeIdentifierSettings?.Clone(),
            CustomNodeNotePrefix = CustomNodeNotePrefix,
            IsFocaEnabled = IsFocaEnabled,
            IsConnectoEnabled = IsConnectoEnabled,
            InCanvasSearchEnabled = InCanvasSearchEnabled,
            QuickSaveDateFormat = QuickSaveDateFormat
        };

        /// <summary>
        /// Fills in anything a hand-edited or older settings file left out, so consumers never
        /// have to null-check.
        /// </summary>
        internal void FillGapsFromDefaults()
        {
            var defaults = CreateDefault();

            if (Settings == null || Settings.Count == 0)
            {
                Settings = defaults.Settings;
            }
            else
            {
                foreach (var missing in defaults.Settings.Where(d => Settings.All(s => s.GroupId != d.GroupId)))
                {
                    Settings.Add(missing);
                }
                Settings = Settings.OrderBy(s => s.GroupId).ToList();
            }

            if (CustomNodeIdentifierSettings == null)
            {
                CustomNodeIdentifierSettings = defaults.CustomNodeIdentifierSettings;
            }
            else if (string.IsNullOrWhiteSpace(CustomNodeIdentifierSettings.CustomNodeColor))
            {
                CustomNodeIdentifierSettings.CustomNodeColor = defaults.CustomNodeIdentifierSettings.CustomNodeColor;
            }

            if (CustomNodeIdentifierSettings.BorderThickness <= 0)
            {
                CustomNodeIdentifierSettings.BorderThickness = defaults.CustomNodeIdentifierSettings.BorderThickness;
            }

            CustomNodeNotePrefix = NormalizeNotePrefix(CustomNodeNotePrefix, defaults.CustomNodeNotePrefix);

            if (string.IsNullOrWhiteSpace(QuickSaveDateFormat))
            {
                QuickSaveDateFormat = defaults.QuickSaveDateFormat;
            }
        }

        /// <summary>
        /// The prefix is concatenated straight onto a node name, so it needs its own trailing space.
        /// </summary>
        internal static string NormalizeNotePrefix(string prefix, string fallback)
        {
            if (string.IsNullOrWhiteSpace(prefix)) return fallback;

            return char.IsWhiteSpace(prefix[prefix.Length - 1]) ? prefix : prefix + " ";
        }
    }
}
