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

        public class CustomNodeIdentifierSettings
        {
            [XmlElement("Color")] 
            public string CustomNodeColor { get; set; } = "#ADE4DE";

            [XmlElement("BorderThickness")]
            public double BorderThickness { get; set; } = 4;
        }

       
        public class MonocleSettings
        {
            [XmlArray(ElementName = "GroupSettings")]
            public List<GroupSetting> Settings { get; set; }

            [XmlElement("CustomNodeIdentifierSettings")]
            public CustomNodeIdentifierSettings CustomNodeIdentifierSettings { get; set; }

            [XmlElement("CustomNodeNotePrefix")]
            public string CustomNodeNotePrefix { get; set; }

            [XmlElement("IsFocaEnabled")] 
            public bool IsFocaEnabled { get; set; } = true;

            [XmlElement("InCanvasSearchEnabled")]
            public bool InCanvasSearchEnabled { get; set; } = true;
            [XmlElement("QuickSaveDateFormat")]
            public string QuickSaveDateFormat { get; set; } = "-yyyy.MM.dd_HH.mm.ss";
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

                Globals.MonocleGroupSettings = new Dictionary<string, GroupSetting>();

                foreach (var g in newGroupSettings)
                {
                    Globals.MonocleGroupSettings.Add($"Group{g.GroupId}",g);
                }
                
                Globals.CustomNodeIdentificationColor =
                    (Color)ColorConverter.ConvertFromString(newSettings.CustomNodeIdentifierSettings.CustomNodeColor);

                Globals.CustomNodeBorderThickness = newSettings.CustomNodeIdentifierSettings.BorderThickness;

                Globals.CustomNodeNotePrefix = Utilities.StringUtils.SetCustomNodeNotePrefix(newSettings.CustomNodeNotePrefix);

                Globals.IsFocaEnabled = newSettings.IsFocaEnabled;

                Globals.InCanvasSearchEnabled = newSettings.InCanvasSearchEnabled;

                Globals.QuickSaveDateFormat = newSettings.QuickSaveDateFormat;

                //now store the settings file in the user settings
                Properties.UserSettings.Default.MonocleSettingsFile = Globals.SettingsFile;
                Properties.UserSettings.Default.Save();
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
                Utilities.Settings.MonocleSettings settings = new Utilities.Settings.MonocleSettings { Settings = Globals.MonocleGroupSettings.Values.ToList(), CustomNodeIdentifierSettings = new CustomNodeIdentifierSettings(){CustomNodeColor = Globals.CustomNodeIdentificationColor.ToString(),BorderThickness = Globals.CustomNodeBorderThickness }, CustomNodeNotePrefix = Globals.CustomNodeNotePrefix, IsFocaEnabled = Globals.IsFocaEnabled, InCanvasSearchEnabled = Globals.InCanvasSearchEnabled, QuickSaveDateFormat = Globals.QuickSaveDateFormat};
                Utilities.Settings.SerializeModels(Globals.SettingsFile, settings);
            }
            catch (Exception e)
            {
                Dynamo.Logging.LogMessage.Warning($"Failed to write MonocleSettings.xml - {e.Message}", WarningLevel.Mild);
            }

        }
    }
}
