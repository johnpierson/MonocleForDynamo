using System;
using System.IO;
using System.Reflection;

namespace MonocleExtension
{
    internal static class Global
    {
        internal static readonly Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();

        internal static string PackageBinFolder => Path.GetDirectoryName(ExecutingAssembly.Location);
        internal static string PackageRoot => Directory.GetParent(PackageBinFolder)?.FullName;
        internal static string PackageExtraFolder => Path.Combine(PackageRoot, "extra");

        /// <summary>Pre-2026.10 location of the view extension. Superseded by bin\&lt;major.minor&gt;\.</summary>
        internal static string LegacyViewExtensionDll => Path.Combine(PackageBinFolder, DeployManifest.ViewExtensionFileName);

        internal static string BuildFolder(string build) => Path.Combine(PackageBinFolder, build);
        internal static string BuildDll(string build) => Path.Combine(BuildFolder(build), DeployManifest.ViewExtensionFileName);

        internal static string ViewExtensionXml => Path.Combine(PackageExtraFolder, "Monocle_ViewExtensionDefinition.xml");

        internal static Version DynamoVersion { get; set; } = new Version(3, 0, 0);
        internal static string TruncatedDynVersion => $"{DynamoVersion.Major}.{DynamoVersion.Minor}";
    }
}
