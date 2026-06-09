using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MonocleExtension
{
    internal class Global
    {
        internal static Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();

        internal static string PackageBinFolder => Path.GetDirectoryName(ExecutingAssembly.Location);
        internal static string PackageRoot => Directory.GetParent(PackageBinFolder)?.FullName;
        internal static string PackageExtraFolder => Path.Combine(PackageRoot, "extra");
        internal static string MonocleViewExtensionDll => Path.Combine(PackageBinFolder, "MonocleViewExtension.dll");

        internal static string ViewExtensionXml => Path.Combine(PackageExtraFolder, "Monocle_ViewExtensionDefinition.xml");
        internal static string ViewExtensionXmlText => "<ViewExtensionDefinition>\r\n  <AssemblyPath>..\\bin\\MonocleViewExtension.dll</AssemblyPath>\r\n  <TypeName>MonocleViewExtension.MonocleViewExtension</TypeName>\r\n</ViewExtensionDefinition>\r\n";


        internal static Version DynamoVersion { get; set; } = new Version(3, 0, 0);
        internal static string TruncatedDynVersion => $"{DynamoVersion.Major}.{DynamoVersion.Minor}";
        internal static Version DotNet8Version => new Version(3, 0, 0);

    }
}
