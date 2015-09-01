using System.Linq;
using System.Text.RegularExpressions;

namespace HtmlDiff
{
    public static class Utils
    {
        private static readonly string[] SpecialCaseWordTags = { "<img" };

        public static bool IsTag(string item)
        {
            if (SpecialCaseWordTags.Any(re => item != null && item.StartsWith(re))) return false;
            return IsOpeningTag(item) || IsClosingTag(item);
        }

        private static bool IsOpeningTag(string item)
        {
            return Regex.IsMatch(item, "^\\s*<[^>]+>\\s*$");
        }

        private static bool IsClosingTag(string item)
        {
            return Regex.IsMatch(item, "^\\s*</[^>]+>\\s*$");
        }

        public static string StripTagAttributes(string word)
        {
            string tag = Regex.Match(word, @"<[^\s>]+", RegexOptions.None).Value;
            word = tag + (word.EndsWith("/>") ? "/>" : ">");
            return word;
        }

        public static bool IsWhitespace(string item)
        {
            return Regex.IsMatch(item, "^(\\s|&nbsp;)+$");
        }
    }
}