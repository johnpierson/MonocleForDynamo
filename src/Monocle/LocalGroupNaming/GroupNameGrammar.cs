using System.Globalization;
using System.Text;

namespace MonocleViewExtension.LocalGroupNaming
{
    /// <summary>
    /// llama.cpp GBNF grammar that constrains sampling to a bare group title: three
    /// to seven plain words separated by single spaces and nothing else. Because the
    /// sampler can only pick tokens the grammar accepts, preambles ("Sure, here is"),
    /// quotes, markdown, JSON, reasoning tags and multi-line answers cannot occur,
    /// and the validator is left to judge content rather than formatting.
    /// </summary>
    internal static class GroupNameGrammar
    {
        public const int MinimumWords = 3;
        public const int MaximumWords = 7;
        public const int MaximumWordLength = 20;

        private const string WordStart = "[A-Za-z0-9]";
        private const string WordBody = "[A-Za-z0-9'&/+-]";

        /// <summary>The grammar sent as the <c>grammar</c> field of a llama-server completion request.</summary>
        public static readonly string Gbnf = Build();

        /// <summary>A regular expression accepting exactly the strings <see cref="Gbnf"/> accepts.</summary>
        public static readonly string RegexPattern = BuildRegexPattern();

        private static string Build()
        {
            var culture = CultureInfo.InvariantCulture;
            var grammar = new StringBuilder();
            grammar.Append("root ::= word (\" \" word){")
                .Append((MinimumWords - 1).ToString(culture)).Append(',')
                .Append((MaximumWords - 1).ToString(culture)).Append("}\n");
            grammar.Append("word ::= ").Append(WordStart).Append(' ').Append(WordBody).Append("{0,")
                .Append((MaximumWordLength - 1).ToString(culture)).Append("}\n");
            return grammar.ToString();
        }

        private static string BuildRegexPattern()
        {
            var culture = CultureInfo.InvariantCulture;
            var word = WordStart + WordBody + "{0," + (MaximumWordLength - 1).ToString(culture) + "}";
            return "^" + word + "( " + word + "){" +
                   (MinimumWords - 1).ToString(culture) + "," +
                   (MaximumWords - 1).ToString(culture) + "}$";
        }
    }
}
