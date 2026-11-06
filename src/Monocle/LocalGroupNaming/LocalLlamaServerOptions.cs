using System;
using System.IO;

namespace MonocleViewExtension.LocalGroupNaming
{
    internal sealed class LocalLlamaServerOptions
    {
        private const string ServerEnvironmentVariable = "MONOCLE_LOCAL_AI_SERVER_PATH";
        private const string ModelEnvironmentVariable = "MONOCLE_LOCAL_AI_MODEL_PATH";

        public string InstallationDirectory { get; private set; }

        public string ServerPath { get; private set; }

        public string ModelPath { get; private set; }

        public bool IsManagedInstallation { get; private set; }

        public static LocalLlamaServerOptions CreateDefault()
        {
            var configuredServer = Environment.GetEnvironmentVariable(ServerEnvironmentVariable);
            var configuredModel = Environment.GetEnvironmentVariable(ModelEnvironmentVariable);
            var hasConfiguredServer = !string.IsNullOrWhiteSpace(configuredServer);
            var hasConfiguredModel = !string.IsNullOrWhiteSpace(configuredModel);

            if (hasConfiguredServer != hasConfiguredModel)
            {
                throw new InvalidOperationException(
                    $"Set both {ServerEnvironmentVariable} and {ModelEnvironmentVariable}, or neither.");
            }

            if (hasConfiguredServer)
            {
                return new LocalLlamaServerOptions
                {
                    InstallationDirectory = Path.GetDirectoryName(configuredServer),
                    ServerPath = configuredServer,
                    ModelPath = configuredModel,
                    IsManagedInstallation = false
                };
            }

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(localAppData))
            {
                throw new InvalidOperationException("The current user's local application data directory could not be resolved.");
            }

            var installationDirectory = Path.Combine(
                localAppData,
                "Monocle",
                "LocalGroupNaming",
                LocalModelManifest.InstallationVersion);

            return new LocalLlamaServerOptions
            {
                InstallationDirectory = installationDirectory,
                ServerPath = Path.Combine(installationDirectory, "runtime", "llama-server.exe"),
                ModelPath = Path.Combine(installationDirectory, "model", LocalModelManifest.ModelFileName),
                IsManagedInstallation = true
            };
        }

        public void Validate()
        {
            if (!File.Exists(ServerPath))
            {
                throw new FileNotFoundException(
                    $"The local AI runtime was not found at '{ServerPath}'.",
                    ServerPath);
            }

            if (!File.Exists(ModelPath))
            {
                throw new FileNotFoundException(
                    $"The local naming model was not found at '{ModelPath}'.",
                    ModelPath);
            }
        }
    }
}
