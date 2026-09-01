using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Dynamo.Controls;
using Dynamo.PackageManager;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;

namespace MonocleViewExtension.Core
{
    /// <summary>
    /// Everything a Monocle feature needs from its host, resolved once at load and handed to
    /// features explicitly instead of reached for through global state.
    /// </summary>
    public sealed class MonocleContext
    {
        private MonocleContext(ViewLoadedParams loadedParams)
        {
            LoadedParams = loadedParams;
            DynamoViewModel = loadedParams.DynamoWindow.DataContext as DynamoViewModel;
            Log = new MonocleLog(DynamoViewModel);
            DynamoVersion = ResolveDynamoVersion();
            PmExtension = loadedParams.ViewStartupParams.ExtensionManager.Extensions
                .OfType<PackageManagerExtension>().FirstOrDefault();
            Settings = new MonocleSettingsService(ResolveSettingsPath(), Log);
        }

        /// <summary>
        /// Set once by <see cref="Create"/>. Exists so the remaining static call sites can find
        /// the context while features are migrated to constructor injection; it goes away with
        /// the last of them.
        /// </summary>
        internal static MonocleContext Current { get; private set; }

        public ViewLoadedParams LoadedParams { get; }
        public DynamoViewModel DynamoViewModel { get; }
        public Window DynamoWindow => LoadedParams.DynamoWindow;
        public DynamoView DynamoView => LoadedParams.DynamoWindow as DynamoView;
        public Version DynamoVersion { get; }
        public PackageManagerExtension PmExtension { get; }
        public MonocleSettingsService Settings { get; }
        public IMonocleLogger Log { get; }

        public static MonocleContext Create(ViewLoadedParams loadedParams)
        {
            Current = new MonocleContext(loadedParams);
            Current.Settings.Load();
            return Current;
        }

        internal static void Clear() => Current = null;

        /// <summary>
        /// The Dynamo we are actually running inside, which is not necessarily the one this
        /// assembly was compiled against.
        /// </summary>
        private static Version ResolveDynamoVersion()
        {
            try
            {
                return Assembly.Load("DynamoCore").GetName().Version;
            }
            catch (Exception)
            {
                return new Version(0, 0);
            }
        }

        /// <summary>
        /// Prefers the settings file the user last loaded, falling back to the one shipped in
        /// the package's extra folder.
        /// </summary>
        private string ResolveSettingsPath()
        {
            var lastUsed = Properties.UserSettings.Default.MonocleSettingsFile;
            if (!string.IsNullOrWhiteSpace(lastUsed) && File.Exists(lastUsed))
            {
                return lastUsed;
            }

            if (!string.IsNullOrWhiteSpace(lastUsed))
            {
                Log.Info($"Settings file {lastUsed} no longer exists. Falling back to the packaged defaults.");
            }

            return Path.Combine(PackageExtraFolder, "MonocleSettings.xml");
        }

        private static string PackageExtraFolder
        {
            get
            {
                var binFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var packageFolder = binFolder == null ? null : Directory.GetParent(binFolder)?.FullName;
                return Path.Combine(packageFolder ?? Path.GetTempPath(), "extra");
            }
        }
    }
}
