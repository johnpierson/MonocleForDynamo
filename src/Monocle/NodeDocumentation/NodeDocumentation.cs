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

        public void ReadMarkdown()
        {
            var fullText = File.ReadAllText(MarkdownPath);

            var dropFirst = fullText.Split(new string[] { "## In Depth" }, StringSplitOptions.None);

            var inDepth = dropFirst[1].Split(new string[] { "___" }, StringSplitOptions.None);

            FullDescription = inDepth[0];
        }
    }
}
