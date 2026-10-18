using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace MonocleExtension
{
    /// <summary>
    /// Pure helpers for the loader: parsing deploy/SHA256SUMS, picking a build, hashing, and writing the view
    /// extension definition. No Dynamo types, so these are covered by the regression tests.
    /// </summary>
    internal static class DeployManifest
    {
        internal const string ViewExtensionFileName = "MonocleViewExtension.dll";

        // "<64 hex>  4.2/MonocleViewExtension.dll" or "<64 hex> *4.2/MonocleViewExtension.dll" (sha256sum text/binary mode)
        private static readonly Regex LinePattern = new Regex(
            @"^(?<hash>[0-9a-fA-F]{64})\s+\*?(?<build>\d+\.\d+)/MonocleViewExtension\.dll\s*$",
            RegexOptions.CultureInvariant);

        private static readonly Regex BuildPattern = new Regex(@"^\d+\.\d+$", RegexOptions.CultureInvariant);

        /// <summary>Map of build ("4.2") to lower-case SHA-256.</summary>
        internal static Dictionary<string, string> Parse(string text)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(text)) return result;

            foreach (var rawLine in text.Split('\n'))
            {
                var match = LinePattern.Match(rawLine.Trim());
                if (match.Success) result[match.Groups["build"].Value] = match.Groups["hash"].Value.ToLowerInvariant();
            }

            return result;
        }

        internal static bool IsBuildName(string name) => name != null && BuildPattern.IsMatch(name);

        /// <summary>The newest build that is safe for this Dynamo: same major version, minor &lt;= running minor.</summary>
        internal static string ChooseBuild(Version dynamoVersion, IEnumerable<string> available)
        {
            var set = new HashSet<string>(available ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
            return DynamoBuildVersionResolver.GetCandidates(dynamoVersion).FirstOrDefault(set.Contains);
        }

        internal static string Sha256(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
            {
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            }
        }

        internal static string RelativeAssemblyPath(string build) =>
            build == null ? @"..\bin\" + ViewExtensionFileName : $@"..\bin\{build}\{ViewExtensionFileName}";

        internal static string DefinitionXml(string build) =>
            "<ViewExtensionDefinition>\r\n" +
            $"  <AssemblyPath>{RelativeAssemblyPath(build)}</AssemblyPath>\r\n" +
            "  <TypeName>MonocleViewExtension.MonocleViewExtension</TypeName>\r\n" +
            "</ViewExtensionDefinition>\r\n";
    }
}
