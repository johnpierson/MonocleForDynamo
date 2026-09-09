using System;
using System.Text;

namespace MonocleViewExtension.Foca
{
    public static class FocaCodeGenerator
    {
        public static string BuildRevitElementCode(string displayText, int elementId)
        {
            var normalizedDisplayText = NormalizeCommentText(displayText);
            return $"//{normalizedDisplayText}\nRevit.Elements.ElementSelector.ByElementId({elementId});";
        }

        private static string NormalizeCommentText(string displayText)
        {
            if (string.IsNullOrEmpty(displayText)) return string.Empty;

            var builder = new StringBuilder(displayText.Length);
            foreach (var character in displayText)
            {
                builder.Append(character == '\r' || character == '\n' ? ' ' : character);
            }

            return builder.ToString().Trim();
        }
    }
}
