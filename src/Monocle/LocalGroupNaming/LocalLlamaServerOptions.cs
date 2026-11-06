using System;
using System.IO;

namespace MonocleViewExtension.LocalGroupNaming
{
    internal sealed class LocalLlamaServerOptions
    {
        private const string ServerEnvironmentVariable = "MONOCLE_LOCAL_AI_SERVER_PATH";
        private const string ModelEnvironmentVariable = "MONOCLE_LOCAL_AI_MODEL_PATH";
        private const string ServerArgumentsEnvironmentVariable = "MONOCLE_LOCAL_AI_SERVER_ARGS";
        private const string InstallationRootFolderName = "LocalGroupNaming";

        public LocalModelManifest Manifest { get; private set; }

        /// <summary>Parent of every versioned installation; superseded versions are removed from here.</summary>
        public string InstallationRootDirectory { get; private set; }

        public string InstallationDirectory { get; private set; }

        public string ServerPath { get; private set; }

        public string ModelPath { get; private set; }

        public bool IsManagedInstallation { get; private set; }

        /// <summary>Extra llama-server arguments for development and benchmarking (for example "-t 6").</summary>
        public string AdditionalServerArguments { get; private set; }

        public static LocalLlamaServerOptions CreateDefault()
        {
            return CreateDefault(LocalModelManifest.Current);
        }

        public static LocalLlamaServerOptions CreateDefault(LocalModelManifest manifest)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));

            var configuredServer = Environment.GetEnvironmentVariable(ServerEnvironmentVariable);
            var configuredModel = Environment.GetEnvironmentVariable(ModelEnvironmentVariable);
            var hasConfiguredServer = !string.IsNullOrWhiteSpace(configuredServer);
            var hasConfiguredModel = !string.IsNullOrWhiteSpace(configuredModel);

            if (hasConfiguredServer != hasConfiguredModel)
            {
                throw new InvalidOperationException(
                    $"Set both {ServerEnvironmentVariable} and {ModelEnvironmentVariable}, or neither.");
            }

            LocalLlamaServerOptions options;
            if (hasConfiguredServer)
            {
                options = CreateExternal(configuredServer, configuredModel, manifest);
            }
            else
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrWhiteSpace(localAppData))
                {
                    throw new InvalidOperationException("The current user's local application data directory could not be resolved.");
                }

                options = CreateManaged(Path.Combine(localAppData, "Monocle", InstallationRootFolderName), manifest);
            }

            var additionalArguments = Environment.GetEnvironmentVariable(ServerArgumentsEnvironmentVariable);
            return string.IsNullOrWhiteSpace(additionalArguments)
                ? options
                : options.WithAdditionalServerArguments(additionalArguments);
        }

        /// <summary>A Monocle-managed installation under <paramref name="installationRootDirectory"/>.</summary>
        public static LocalLlamaServerOptions CreateManaged(string installationRootDirectory, LocalModelManifest manifest)
        {
            if (string.IsNullOrWhiteSpace(installationRootDirectory))
                throw new ArgumentException("An installation root directory is required.", nameof(installationRootDirectory));
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));

            var installationDirectory = Path.Combine(installationRootDirectory, manifest.InstallationVersion);
            return new LocalLlamaServerOptions
            {
                Manifest = manifest,
                InstallationRootDirectory = installationRootDirectory,
                InstallationDirectory = installationDirectory,
                ServerPath = Path.Combine(installationDirectory, "runtime", "llama-server.exe"),
                ModelPath = Path.Combine(installationDirectory, "model", manifest.ModelFileName),
                IsManagedInstallation = true,
                AdditionalServerArguments = string.Empty
            };
        }

        /// <summary>A developer-supplied runtime and model that Monocle neither downloads nor verifies.</summary>
        public static LocalLlamaServerOptions CreateExternal(string serverPath, string modelPath, LocalModelManifest manifest)
        {
            if (string.IsNullOrWhiteSpace(serverPath)) throw new ArgumentException("A server path is required.", nameof(serverPath));
            if (string.IsNullOrWhiteSpace(modelPath)) throw new ArgumentException("A model path is required.", nameof(modelPath));

            return new LocalLlamaServerOptions
            {
                Manifest = manifest ?? LocalModelManifest.Current,
                InstallationRootDirectory = null,
                InstallationDirectory = Path.GetDirectoryName(serverPath),
                ServerPath = serverPath,
                ModelPath = modelPath,
                IsManagedInstallation = false,
                AdditionalServerArguments = string.Empty
            };
        }

        public LocalLlamaServerOptions WithAdditionalServerArguments(string arguments)
        {
            return new LocalLlamaServerOptions
            {
                Manifest = Manifest,
                InstallationRootDirectory = InstallationRootDirectory,
                InstallationDirectory = InstallationDirectory,
                ServerPath = ServerPath,
                ModelPath = ModelPath,
                IsManagedInstallation = IsManagedInstallation,
                AdditionalServerArguments = arguments?.Trim() ?? string.Empty
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
