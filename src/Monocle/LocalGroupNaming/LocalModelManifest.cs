using System;
using System.Globalization;

namespace MonocleViewExtension.LocalGroupNaming
{
    /// <summary>
    /// The pinned third-party components Monocle downloads for local group naming.
    /// Every URL points at an immutable revision and every file is verified against
    /// the published SHA-256 before it is used, so a re-uploaded "latest" cannot
    /// change what runs on the user's machine. Changing the model or runtime means
    /// bumping <see cref="InstallationVersion"/>, which also re-prompts for the licenses.
    /// </summary>
    internal sealed class LocalModelManifest
    {
        private const long Megabyte = 1024L * 1024L;

        public static readonly LocalModelManifest Current = new LocalModelManifest(
            installationVersion: "qwen3.5-4b-q4km-llama-b12110-v2",
            modelDisplayName: "Qwen3.5 4B",
            modelFileName: "Qwen3.5-4B-Q4_K_M.gguf",
            modelFileSize: 2740937888,
            // Hugging Face LFS sha256 of the file at the pinned revision.
            modelSha256: "00FE7986FF5F6B463E62455821146049DB6F9313603938A70800D1FB69EF11A4",
            // Pinned to the repository revision the checksum was taken from, so a
            // re-uploaded 'main' cannot change what gets downloaded.
            modelDownloadUrl: "https://huggingface.co/unsloth/Qwen3.5-4B-GGUF/resolve/e87f176479d0855a907a41277aca2f8ee7a09523/Qwen3.5-4B-Q4_K_M.gguf?download=true",
            modelSourceUrl: "https://huggingface.co/unsloth/Qwen3.5-4B-GGUF",
            modelLicenseName: "Apache License 2.0",
            modelLicenseUrl: "https://huggingface.co/Qwen/Qwen3.5-4B/blob/main/LICENSE",
            runtimeVersion: "b12110",
            runtimeArchiveName: "llama-b12110-bin-win-cpu-x64.zip",
            runtimeArchiveSize: 24708445,
            // SHA-256 published by GitHub for the release asset (releases API 'digest' field).
            runtimeSha256: "98901E327C252875ACCAF2D1BF364E10E868843B0B63468BAAD1AA945CDC5DA2",
            runtimeDownloadUrl: "https://github.com/ggml-org/llama.cpp/releases/download/b12110/llama-b12110-bin-win-cpu-x64.zip",
            runtimeLicenseName: "MIT License",
            runtimeLicenseUrl: "https://github.com/ggml-org/llama.cpp/blob/b12110/LICENSE");

        public LocalModelManifest(
            string installationVersion,
            string modelDisplayName,
            string modelFileName,
            long modelFileSize,
            string modelSha256,
            string modelDownloadUrl,
            string modelSourceUrl,
            string modelLicenseName,
            string modelLicenseUrl,
            string runtimeVersion,
            string runtimeArchiveName,
            long runtimeArchiveSize,
            string runtimeSha256,
            string runtimeDownloadUrl,
            string runtimeLicenseName,
            string runtimeLicenseUrl)
        {
            if (modelFileSize <= 0) throw new ArgumentOutOfRangeException(nameof(modelFileSize));
            if (runtimeArchiveSize <= 0) throw new ArgumentOutOfRangeException(nameof(runtimeArchiveSize));

            InstallationVersion = Require(installationVersion, nameof(installationVersion));
            ModelDisplayName = Require(modelDisplayName, nameof(modelDisplayName));
            ModelFileName = Require(modelFileName, nameof(modelFileName));
            ModelFileSize = modelFileSize;
            ModelSha256 = Require(modelSha256, nameof(modelSha256));
            ModelDownloadUrl = Require(modelDownloadUrl, nameof(modelDownloadUrl));
            ModelSourceUrl = Require(modelSourceUrl, nameof(modelSourceUrl));
            ModelLicenseName = Require(modelLicenseName, nameof(modelLicenseName));
            ModelLicenseUrl = Require(modelLicenseUrl, nameof(modelLicenseUrl));
            RuntimeVersion = Require(runtimeVersion, nameof(runtimeVersion));
            RuntimeArchiveName = Require(runtimeArchiveName, nameof(runtimeArchiveName));
            RuntimeArchiveSize = runtimeArchiveSize;
            RuntimeSha256 = Require(runtimeSha256, nameof(runtimeSha256));
            RuntimeDownloadUrl = Require(runtimeDownloadUrl, nameof(runtimeDownloadUrl));
            RuntimeLicenseName = Require(runtimeLicenseName, nameof(runtimeLicenseName));
            RuntimeLicenseUrl = Require(runtimeLicenseUrl, nameof(runtimeLicenseUrl));
        }

        public string InstallationVersion { get; }

        public string ModelDisplayName { get; }

        public string ModelFileName { get; }

        public long ModelFileSize { get; }

        public string ModelSha256 { get; }

        public string ModelDownloadUrl { get; }

        public string ModelSourceUrl { get; }

        public string ModelLicenseName { get; }

        public string ModelLicenseUrl { get; }

        public string RuntimeVersion { get; }

        public string RuntimeArchiveName { get; }

        public long RuntimeArchiveSize { get; }

        public string RuntimeSha256 { get; }

        public string RuntimeDownloadUrl { get; }

        public string RuntimeLicenseName { get; }

        public string RuntimeLicenseUrl { get; }

        /// <summary>Free space needed for a fresh install: the model, the archive plus its extraction, and headroom.</summary>
        public long RequiredFreeBytes => ModelFileSize + 3 * RuntimeArchiveSize + 256 * Megabyte;

        /// <summary>Download size as shown to the user before they accept, for example "2.6 GB".</summary>
        public string ApproximateDownloadSize => FormatSize(ModelFileSize + RuntimeArchiveSize);

        public static string FormatSize(long bytes)
        {
            const double gigabyte = 1024d * 1024d * 1024d;
            return bytes >= gigabyte
                ? (bytes / gigabyte).ToString("0.0", CultureInfo.InvariantCulture) + " GB"
                : (bytes / (double)Megabyte).ToString("0", CultureInfo.InvariantCulture) + " MB";
        }

        private static string Require(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A value is required.", parameterName);
            return value;
        }
    }
}
