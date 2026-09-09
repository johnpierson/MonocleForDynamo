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
            public int FontSize { get; set; } = 36;
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

            [XmlElement("IsConnectoEnabled")]
            public bool IsConnectoEnabled { get; set; } = true;

            [XmlElement("InCanvasSearchEnabled")]
            public bool InCanvasSearchEnabled { get; set; } = true;
            [XmlElement("QuickSaveDateFormat")]
            public string QuickSaveDateFormat { get; set; } = "-yyyy.MM.dd_HH.mm.ss";
        }


        public static void SerializeModels(string filename, MonocleSettings settings)
        {
            if (string.IsNullOrWhiteSpace(filename)) throw new ArgumentException("A settings filename is required.", nameof(filename));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var fullPath = Path.GetFullPath(filename);
            var directory = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrEmpty(directory)) throw new IOException("The settings directory could not be resolved.");

            Directory.CreateDirectory(directory);
            var temporaryPath = Path.Combine(directory,
                $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
            var xmls = new XmlSerializer(settings.GetType());
            try
            {
                using (var fs = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(fs))
                {
                    xmls.Serialize(writer, settings);
                    writer.Flush();
                    fs.Flush(true);
                }

                if (File.Exists(fullPath))
                {
                    File.Replace(temporaryPath, fullPath, null);
                }
                else
                {
                    File.Move(temporaryPath, fullPath);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }
        public static MonocleSettings DeserializeModels(string filename)
        {
            using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var xmls = new XmlSerializer(typeof(MonocleSettings));
                return (MonocleSettings)xmls.Deserialize(fs);
            }
        }

        public static bool LoadMonocleSettings()
        {
            if (!File.Exists(Globals.SettingsFile)) return true;

            ValidatedSettings validatedSettings;
            try
            {
                var newSettings = DeserializeModels(Globals.SettingsFile);
                validatedSettings = Validate(newSettings);
            }
            catch (Exception exception)
            {
                LogMessage.Warning($"Failed to load MonocleSettings.xml from '{Globals.SettingsFile}': {exception.Message}", WarningLevel.Mild);
                return false;
            }

            // Assign only after every section has been deserialized and validated.
            Globals.MonocleGroupSettings = validatedSettings.GroupSettings;
            Globals.CustomNodeIdentificationColor = validatedSettings.CustomNodeIdentificationColor;
            Globals.CustomNodeBorderThickness = validatedSettings.CustomNodeBorderThickness;
            Globals.CustomNodeNotePrefix = validatedSettings.CustomNodeNotePrefix;
            Globals.IsFocaEnabled = validatedSettings.IsFocaEnabled;
            Globals.IsConnectoEnabled = validatedSettings.IsConnectoEnabled;
            Globals.InCanvasSearchEnabled = validatedSettings.InCanvasSearchEnabled;
            Globals.QuickSaveDateFormat = validatedSettings.QuickSaveDateFormat;

            // Remember the selected settings file without allowing a user-settings write
            // failure to make an already-applied, validated load look like a parse failure.
            try
            {
                Properties.UserSettings.Default.MonocleSettingsFile = Globals.SettingsFile;
                Properties.UserSettings.Default.Save();
            }
            catch (Exception exception)
            {
                LogMessage.Warning($"Failed to remember MonocleSettings.xml location: {exception.Message}", WarningLevel.Mild);
            }

            return true;
        }

        public static void SaveMonocleSettings()
        {
            try
            {
                Utilities.Settings.MonocleSettings settings = new Utilities.Settings.MonocleSettings { Settings = Globals.MonocleGroupSettings.Values.ToList(), CustomNodeIdentifierSettings = new CustomNodeIdentifierSettings(){CustomNodeColor = Globals.CustomNodeIdentificationColor.ToString(),BorderThickness = Globals.CustomNodeBorderThickness }, CustomNodeNotePrefix = Globals.CustomNodeNotePrefix, IsFocaEnabled = Globals.IsFocaEnabled, InCanvasSearchEnabled = Globals.InCanvasSearchEnabled, QuickSaveDateFormat = Globals.QuickSaveDateFormat, IsConnectoEnabled = Globals.IsConnectoEnabled};
                Utilities.Settings.SerializeModels(Globals.SettingsFile, settings);
            }
            catch (Exception e)
            {
                LogMessage.Warning($"Failed to write MonocleSettings.xml - {e.Message}", WarningLevel.Mild);
            }

        }

        private sealed class ValidatedSettings
        {
            public Dictionary<string, GroupSetting> GroupSettings { get; set; }
            public Color CustomNodeIdentificationColor { get; set; }
            public double CustomNodeBorderThickness { get; set; }
            public string CustomNodeNotePrefix { get; set; }
            public bool IsFocaEnabled { get; set; }
            public bool IsConnectoEnabled { get; set; }
            public bool InCanvasSearchEnabled { get; set; }
            public string QuickSaveDateFormat { get; set; }
        }

        private static ValidatedSettings Validate(MonocleSettings settings)
        {
            if (settings == null) throw new InvalidDataException("The settings document is empty.");
            if (settings.Settings == null || settings.Settings.Count == 0)
            {
                throw new InvalidDataException("The settings document has no group settings.");
            }
            if (settings.CustomNodeIdentifierSettings == null)
            {
                throw new InvalidDataException("The custom node identifier settings section is missing.");
            }

            var groups = new Dictionary<string, GroupSetting>();
            foreach (var group in settings.Settings)
            {
                if (group == null) throw new InvalidDataException("A group setting is empty.");
                if (group.GroupId <= 0) throw new InvalidDataException("Group IDs must be positive.");
                if (string.IsNullOrWhiteSpace(group.GroupText)) throw new InvalidDataException("Group text is required.");
                ValidateColor(group.GroupColor, "group color");
                if (group.FontSize <= 0) throw new InvalidDataException("Group font sizes must be positive.");

                var key = $"Group{group.GroupId}";
                if (groups.ContainsKey(key)) throw new InvalidDataException($"Duplicate group ID: {group.GroupId}.");
                groups.Add(key, group);
            }

            var customSettings = settings.CustomNodeIdentifierSettings;
            var customColor = ValidateColor(customSettings.CustomNodeColor, "custom node color");
            if (double.IsNaN(customSettings.BorderThickness) || double.IsInfinity(customSettings.BorderThickness) || customSettings.BorderThickness <= 0)
            {
                throw new InvalidDataException("Custom node border thickness must be a finite positive number.");
            }

            if (string.IsNullOrWhiteSpace(settings.QuickSaveDateFormat))
            {
                throw new InvalidDataException("Quick Save date format is required.");
            }
            try
            {
                DateTime.Now.ToString(settings.QuickSaveDateFormat);
            }
            catch (FormatException exception)
            {
                throw new InvalidDataException("Quick Save date format is invalid.", exception);
            }

            return new ValidatedSettings
            {
                GroupSettings = groups,
                CustomNodeIdentificationColor = customColor,
                CustomNodeBorderThickness = customSettings.BorderThickness,
                CustomNodeNotePrefix = StringUtils.SetCustomNodeNotePrefix(settings.CustomNodeNotePrefix),
                IsFocaEnabled = settings.IsFocaEnabled,
                IsConnectoEnabled = settings.IsConnectoEnabled,
                InCanvasSearchEnabled = settings.InCanvasSearchEnabled,
                QuickSaveDateFormat = settings.QuickSaveDateFormat
            };
        }

        private static Color ValidateColor(string colorValue, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(colorValue)) throw new InvalidDataException($"{propertyName} is required.");

            object convertedColor;
            try
            {
                convertedColor = ColorConverter.ConvertFromString(colorValue);
            }
            catch (Exception exception) when (exception is FormatException || exception is NotSupportedException || exception is ArgumentException)
            {
                throw new InvalidDataException($"{propertyName} is invalid.", exception);
            }

            if (!(convertedColor is Color)) throw new InvalidDataException($"{propertyName} is invalid.");
            return (Color)convertedColor;
        }
    }
}
