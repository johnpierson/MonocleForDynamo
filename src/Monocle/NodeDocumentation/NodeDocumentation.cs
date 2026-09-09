using System;
using System.IO;

namespace MonocleViewExtension.NodeDocumentation
{
    public class NodeDocumentation
    {
        public string BasePath { get; set; }
        public string NodeName { get; set; }
        public string FullNodeName { get; set; }
        public string Description { get; set; }
        public string FullDescription { get; set; }
        public string MarkdownPath => Path.Combine(BasePath, $"{FullNodeName}.md");
        public string SampleGraphImage => $"{FullNodeName}_img.jpg";
        public string SampleGraphImagePath => Path.Combine(BasePath, SampleGraphImage);
        public string SampleGraph => Path.Combine(BasePath, $"{FullNodeName}.dyn");

        public NodeDocumentation(string basePath, string fullNodeName, string nodeName)
        {
            BasePath = basePath;
            NodeName = nodeName;
            FullNodeName = fullNodeName;
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(BasePath)) throw new ArgumentException("A documentation directory is required.", nameof(BasePath));
            if (string.IsNullOrWhiteSpace(NodeName)) throw new ArgumentException("A node name is required.", nameof(NodeName));
            if (string.IsNullOrWhiteSpace(FullNodeName)) throw new ArgumentException("A full node name is required.", nameof(FullNodeName));

            var fullBasePath = Path.GetFullPath(BasePath);
            if (!Directory.Exists(fullBasePath)) throw new DirectoryNotFoundException($"Documentation directory does not exist: {fullBasePath}");

            ValidateFileNamePart(FullNodeName, nameof(FullNodeName));

            // Force path resolution here so invalid combinations fail before any export begins.
            _ = Path.GetFullPath(MarkdownPath);
            _ = Path.GetFullPath(SampleGraphImagePath);
            _ = Path.GetFullPath(SampleGraph);
        }

        private static void ValidateFileNamePart(string value, string propertyName)
        {
            var candidate = value + ".dyn";
            foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
            {
                if (candidate.IndexOf(invalidCharacter) >= 0)
                {
                    throw new ArgumentException($"{propertyName} contains an invalid filename character.", propertyName);
                }
            }

            if (!string.Equals(Path.GetFileName(candidate), candidate, StringComparison.Ordinal))
            {
                throw new ArgumentException($"{propertyName} must be a filename, not a path.", propertyName);
            }
        }

        public void ReadMarkdown()
        {
            var fullText = File.ReadAllText(MarkdownPath);
            FullDescription = ParseFullDescription(fullText);
        }

        public static string ParseFullDescription(string fullText)
        {
            if (string.IsNullOrEmpty(fullText)) return string.Empty;

            const string inDepthHeading = "## In Depth";
            var headingIndex = fullText.IndexOf(inDepthHeading, StringComparison.Ordinal);
            if (headingIndex < 0) return string.Empty;

            var descriptionStart = headingIndex + inDepthHeading.Length;
            var separatorIndex = fullText.IndexOf("___", descriptionStart, StringComparison.Ordinal);
            var descriptionLength = separatorIndex < 0
                ? fullText.Length - descriptionStart
                : separatorIndex - descriptionStart;

            return fullText.Substring(descriptionStart, descriptionLength).Trim();
        }
    }
}
