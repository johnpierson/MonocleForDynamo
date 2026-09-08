using System;
using System.IO;

namespace MonocleViewExtension.NodeDocumentation
{
    public static class DocumentationImagePaths
    {
        public static string BuildTemporaryPath(string outputPath, string suffix)
        {
            if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("An image output path is required.", nameof(outputPath));
            if (string.IsNullOrWhiteSpace(suffix)) throw new ArgumentException("A temporary image suffix is required.", nameof(suffix));

            var fullOutputPath = Path.GetFullPath(outputPath);
            var directory = Path.GetDirectoryName(fullOutputPath);
            if (string.IsNullOrEmpty(directory)) throw new IOException("The image output directory could not be resolved.");

            var fileName = Path.GetFileNameWithoutExtension(fullOutputPath);
            var extension = Path.GetExtension(fullOutputPath);
            var temporaryFileName = $"{fileName}{suffix}_{Guid.NewGuid():N}{extension}";

            if (!string.Equals(Path.GetFileName(temporaryFileName), temporaryFileName, StringComparison.Ordinal))
            {
                throw new ArgumentException("The temporary image suffix must be a filename suffix.", nameof(suffix));
            }

            return Path.Combine(directory, temporaryFileName);
        }
    }
}
