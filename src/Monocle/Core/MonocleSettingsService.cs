using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace MonocleViewExtension.Core
{
    /// <summary>
    /// Owns the live settings object and its XML file. Deliberately free of Dynamo and WPF types
    /// so it can be exercised without a running Dynamo.
    /// </summary>
    public class MonocleSettingsService
    {
        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(MonocleSettings));

        private readonly IMonocleLogger _log;

        public MonocleSettingsService(string settingsFilePath, IMonocleLogger log = null)
        {
            _log = log ?? NullMonocleLogger.Instance;
            SettingsFilePath = settingsFilePath;
            Current = MonocleSettings.CreateDefault();
        }

        /// <summary>Raised after Current is replaced or reloaded, so features can re-read it.</summary>
        public event EventHandler SettingsChanged;

        public MonocleSettings Current { get; private set; }

        public string SettingsFilePath { get; set; }

        /// <summary>
        /// Group settings keyed the way the UI asks for them ("Group1".."Group6").
        /// </summary>
        public IReadOnlyDictionary<string, GroupSetting> GroupsByKey =>
            Current.Settings.ToDictionary(g => $"Group{g.GroupId}", g => g);

        /// <summary>
        /// Reads SettingsFilePath. A missing file is normal (first run); a broken one is logged
        /// rather than silently swallowed, and leaves the previous settings in place.
        /// </summary>
        public bool Load()
        {
            if (string.IsNullOrWhiteSpace(SettingsFilePath) || !File.Exists(SettingsFilePath))
            {
                return false;
            }

            try
            {
                MonocleSettings loaded;
                using (var stream = new FileStream(SettingsFilePath, FileMode.Open, FileAccess.Read))
                {
                    loaded = (MonocleSettings)Serializer.Deserialize(stream);
                }

                loaded.FillGapsFromDefaults();
                Current = loaded;
                OnSettingsChanged();
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException ||
                                      e is InvalidOperationException || e is XmlException)
            {
                _log.Warn($"Could not read settings from {SettingsFilePath}. Keeping the settings already in effect.", e);
                return false;
            }
        }

        /// <summary>
        /// Writes to a temporary file and moves it into place, so a failure mid-write cannot
        /// truncate the user's existing settings.
        /// </summary>
        public bool Save()
        {
            if (string.IsNullOrWhiteSpace(SettingsFilePath))
            {
                return false;
            }

            var tempPath = SettingsFilePath + ".tmp";
            try
            {
                var directory = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var writer = new StreamWriter(tempPath, append: false))
                {
                    Serializer.Serialize(writer, Current);
                }

                if (File.Exists(SettingsFilePath))
                {
                    File.Delete(SettingsFilePath);
                }
                File.Move(tempPath, SettingsFilePath);
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException ||
                                      e is InvalidOperationException)
            {
                _log.Warn($"Could not write settings to {SettingsFilePath}.", e);
                return false;
            }
            finally
            {
                TryDeleteTemp(tempPath);
            }
        }

        /// <summary>
        /// Replaces the live settings with a fresh default graph. Nothing is shared with the
        /// previous object, so later edits cannot leak back into the defaults.
        /// </summary>
        public void RestoreDefaults()
        {
            Current = MonocleSettings.CreateDefault();
            OnSettingsChanged();
        }

        /// <summary>Applies an edited copy (from the settings dialog) and persists it.</summary>
        public void Apply(MonocleSettings edited)
        {
            if (edited == null) throw new ArgumentNullException(nameof(edited));

            Current = edited.Clone();
            Current.FillGapsFromDefaults();
            Save();
            OnSettingsChanged();
        }

        private void OnSettingsChanged() => SettingsChanged?.Invoke(this, EventArgs.Empty);

        private static void TryDeleteTemp(string tempPath)
        {
            try
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
            catch (IOException)
            {
                // A leftover .tmp is harmless; the next save overwrites it.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
