using System;
using System.Globalization;
using System.IO;

namespace MonocleViewExtension.BetterSave
{
    /// <summary>
    /// Creates a timestamped sibling for a Dynamo workspace without treating the
    /// directory or an extension's casing as part of the filename.
    /// </summary>
    public static class QuickSavePathBuilder
    {
        public static string Build(string originalPath, string dateFormat, DateTime timestamp)
        {
            if (string.IsNullOrWhiteSpace(originalPath))
            {
                throw new ArgumentException("The workspace path is required.", nameof(originalPath));
            }

            var fullOriginalPath = Path.GetFullPath(originalPath);
            var extension = Path.GetExtension(fullOriginalPath);
            if (!string.Equals(extension, ".dyn", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("The workspace path must have a .dyn extension.", nameof(originalPath));
            }

            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullOriginalPath);
            if (string.IsNullOrWhiteSpace(fileNameWithoutExtension))
            {
                throw new ArgumentException("The workspace filename is required.", nameof(originalPath));
            }

            string formattedTimestamp;
            try
            {
                formattedTimestamp = timestamp.ToString(dateFormat, CultureInfo.InvariantCulture);
            }
            catch (FormatException exception)
            {
                throw new ArgumentException("The Quick Save date format is invalid.", nameof(dateFormat), exception);
            }

            if (string.IsNullOrEmpty(formattedTimestamp))
            {
                throw new ArgumentException("The Quick Save date format must produce a filename suffix.", nameof(dateFormat));
            }

            var candidateFileName = fileNameWithoutExtension + formattedTimestamp + extension;
            foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
            {
                if (candidateFileName.IndexOf(invalidCharacter) >= 0)
                {
                    throw new ArgumentException("The Quick Save date format produces an invalid filename.", nameof(dateFormat));
                }
            }

            var directory = Path.GetDirectoryName(fullOriginalPath);
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentException("The workspace directory could not be resolved.", nameof(originalPath));
            }

            var result = Path.Combine(directory, candidateFileName);
            if (string.Equals(result, fullOriginalPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("The Quick Save filename must differ from the source workspace.", nameof(dateFormat));
            }

            return result;
        }
    }
}
