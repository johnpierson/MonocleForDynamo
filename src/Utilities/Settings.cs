using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Xml.Serialization;
using Dynamo.Logging;

namespace MonocleViewExtension.Utilities
{
    public static class Settings
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
            public int FontSize { get; set; } = 24;
        }

        public class ComplexitySettings
        {
            [XmlElement("SimpleScore")]
            public int SimpleScore { get; set; } = 25;
            [XmlElement("MediumScore")]
            public int MediumScore { get; set; } = 50;
            [XmlElement("ComplexScore")]
            public int ComplexScore { get; set; } = 100;
        }
        public class MonocleSettings
        {
            [XmlArray(ElementName = "GroupSettings")]
            public List<GroupSetting> Settings { get; set; }
            [XmlElement("CustomNodeIdentificationColor")]
            public string CustomNodeIdentificationColor { get; set; }
            [XmlElement("CustomNodeNotePrefix")]
            public string CustomNodeNotePrefix { get; set; }
        }

        public static void SerializeModels(string filename, MonocleSettings settings)
        {
            var xmls = new XmlSerializer(settings.GetType());
            var writer = new StreamWriter(filename);
            xmls.Serialize(writer, settings);
            writer.Close();
        }
        public static MonocleSettings DeserializeModels(string filename)
        {
            var fs = new FileStream(filename, FileMode.Open);
            var xmls = new XmlSerializer(typeof(MonocleSettings));
            return (MonocleSettings)xmls.Deserialize(fs);
        }

        public static void LoadMonocleSettings()
        {
            if (!File.Exists(Globals.SettingsFile)) return;
            try
            {
                var newSettings = DeserializeModels(Globals.SettingsFile);
                var newGroupSettings = newSettings.Settings;
                Globals.MonocleGroupSettings.Clear();
                Globals.MonocleGroupSettings.AddRange(newGroupSettings.GroupBy(g => g.GroupId).Select(group => group.First()));
                Globals.CustomNodeIdentificationColor =
                    (Color) ColorConverter.ConvertFromString(newSettings.CustomNodeIdentificationColor);
                Globals.CustomNodeNotePrefix = Utilities.StringComparisonUtilities.SetCustomNodeNotePrefix(newSettings.CustomNodeNotePrefix);
            }
            catch (Exception)
            {
                //leave the defaults
            }
        }

        public static void SaveMonocleSettings()
        {
            try
            {
                Utilities.Settings.MonocleSettings settings = new Utilities.Settings.MonocleSettings { Settings = Globals.MonocleGroupSettings, CustomNodeIdentificationColor = Globals.CustomNodeIdentificationColor.ToString(), CustomNodeNotePrefix = Globals.CustomNodeNotePrefix};
                Utilities.Settings.SerializeModels(Globals.SettingsFile, settings);
            }
            catch (Exception e)
            {
                Dynamo.Logging.LogMessage.Warning($"Failed to write MonocleSettings.xml - {e.Message}", WarningLevel.Mild);
            }

        }
    }
}
