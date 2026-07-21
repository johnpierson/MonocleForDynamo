using System;
using System.IO;
using System.Reflection;

namespace MonocleViewExtension.LocalGroupNaming
{
    internal sealed class LocalLlamaServerOptions
    {
        private const string ServerEnvironmentVariable = "MONOCLE_LOCAL_AI_SERVER_PATH";
        private const string ModelEnvironmentVariable = "MONOCLE_LOCAL_AI_MODEL_PATH";

        public string ServerPath { get; private set; }

        public string ModelPath { get; private set; }

        public static LocalLlamaServerOptions CreateDefault()
        {
            var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrWhiteSpace(assemblyDirectory))
            {
                throw new InvalidOperationException("Monocle's installation directory could not be resolved.");
            }

            var localAiDirectory = Path.Combine(assemblyDirectory, "local-ai");
            return new LocalLlamaServerOptions
            {
                ServerPath = GetConfiguredPath(ServerEnvironmentVariable,
                    Path.Combine(localAiDirectory, "llama-server.exe")),
                ModelPath = GetConfiguredPath(ModelEnvironmentVariable,
                    Path.Combine(localAiDirectory, "Qwen3-4B-Q4_K_M.gguf"))
            };
        }

        public void Validate()
        {
            if (!File.Exists(ServerPath))
            {
                throw new FileNotFoundException(
                    $"The local AI runtime was not found at '{ServerPath}'. Set {ServerEnvironmentVariable} to experiment with another location.",
                    ServerPath);
            }

            if (!File.Exists(ModelPath))
            {
                throw new FileNotFoundException(
                    $"The local naming model was not found at '{ModelPath}'. Set {ModelEnvironmentVariable} to experiment with another location.",
                    ModelPath);
            }
        }

        private static string GetConfiguredPath(string environmentVariable, string fallbackPath)
        {
            var configuredPath = Environment.GetEnvironmentVariable(environmentVariable);
            return string.IsNullOrWhiteSpace(configuredPath) ? fallbackPath : configuredPath;
        }
    }
}
