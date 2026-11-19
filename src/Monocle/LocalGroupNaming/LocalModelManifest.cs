using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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

        // Model choice (2026-11-06, see docs/LOCAL_GROUP_NAMING_MODEL_RESEARCH.md): the
        // Instruct-2507 refresh of Qwen3-4B is non-thinking by construction, Apache-2.0,
        // and the same dense architecture and byte size as the original Qwen3-4B pin,
        // so it costs nothing in download or CPU time while removing the whole
        // /no_think and reasoning-stripping surface.
        public static readonly LocalModelManifest Current = new LocalModelManifest(
            installationVersion: "qwen3-4b-instruct-2507-q4km-llama-b12110-v1",
            modelDisplayName: "Qwen3 4B Instruct 2507",
            modelFileName: "Qwen3-4B-Instruct-2507-Q4_K_M.gguf",
            modelFileSize: 2497281120,
            // Hugging Face LFS sha256 of the file at the pinned revision.
            modelSha256: "3605803B982CB64AEAD44F6C1B2AE36E3ACDB41D8E46C8A94C6533BC4C67E597",
            // Pinned to the repository revision the checksum was taken from, so a
            // re-uploaded 'main' cannot change what gets downloaded.
            modelDownloadUrl: "https://huggingface.co/unsloth/Qwen3-4B-Instruct-2507-GGUF/resolve/a06e946bb6b655725eafa393f4a9745d460374c9/Qwen3-4B-Instruct-2507-Q4_K_M.gguf?download=true",
            modelSourceUrl: "https://huggingface.co/unsloth/Qwen3-4B-Instruct-2507-GGUF",
            modelLicenseName: "Apache License 2.0",
            // The official Qwen repository carries the LICENSE file (GGUF mirrors do not);
            // pinned to a commit so the text the user accepts cannot change afterwards.
            modelLicenseUrl: "https://huggingface.co/Qwen/Qwen3-4B-Instruct-2507/blob/cdbee75f17c01a7cc42f958dc650907174af0554/LICENSE",
            runtimeVersion: "b12110",
            runtimeArchiveName: "llama-b12110-bin-win-cpu-x64.zip",
            runtimeArchiveSize: 24708445,
            // SHA-256 published by GitHub for the release asset (releases API 'digest' field).
            runtimeSha256: "98901E327C252875ACCAF2D1BF364E10E868843B0B63468BAAD1AA945CDC5DA2",
            runtimeDownloadUrl: "https://github.com/ggml-org/llama.cpp/releases/download/b12110/llama-b12110-bin-win-cpu-x64.zip",
            runtimeLicenseName: "MIT License",
            runtimeLicenseUrl: "https://github.com/ggml-org/llama.cpp/blob/b12110/LICENSE",
            // Earlier pins whose installation folders this version may remove once it is
            // installed. Folders with other names belong to other Monocle builds (a Revit
            // 2024 and a Revit 2026 install can pin different versions) and are left alone.
            supersededInstallationVersions: new[]
            {
                "qwen3-4b-q4km-llama-b10075-v1",
                "qwen3.5-4b-q4km-llama-b12110-v2"
            });

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
            string runtimeLicenseUrl,
            IEnumerable<string> supersededInstallationVersions = null)
        {
            if (modelFileSize <= 0) throw new ArgumentOutOfRangeException(nameof(modelFileSize));
            if (runtimeArchiveSize <= 0) throw new ArgumentOutOfRangeException(nameof(runtimeArchiveSize));

            var superseded = (supersededInstallationVersions ?? new string[0])
                .Where(version => !string.IsNullOrWhiteSpace(version))
                .Select(version => version.Trim())
                .Where(version => !string.Equals(version, installationVersion, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            SupersededInstallationVersions = superseded.AsReadOnly();

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

        /// <summary>Installation folder names of earlier pins that may be removed once this one is installed.</summary>
        public IReadOnlyList<string> SupersededInstallationVersions { get; }

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
