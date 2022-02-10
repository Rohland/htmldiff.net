using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace HtmlDiff
{
    public static class Utils
    {
        private static Regex openingTagRegex = new Regex(
            "^\\s*<[^>]+>\\s*$",
            RegexOptions.Compiled);
        private static Regex closingTagTexRegex = new Regex(
            "^\\s*</[^>]+>\\s*$",
            RegexOptions.Compiled);
        private static Regex tagWordRegex = new Regex(
            @"<[^\s>]+",
            RegexOptions.Compiled);
        private static Regex whitespaceRegex = new Regex(
            "^(\\s|&nbsp;)+$",
            RegexOptions.Compiled);
        private static Regex wordRegex = new Regex(
            @"[\w\#@]+",
            RegexOptions.Compiled | RegexOptions.ECMAScript);
        private static Regex tagRegex = new Regex(
            @"<([^\s>/]+)",
            RegexOptions.Compiled);

        private static readonly string[] SpecialCaseWordTags = { "<img" };

        private static readonly HashSet<string> SelfClosingTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "br",
            "area",
            "base",
            "embed",
            "hr",
            "iframe",
            "img",
            "input",
            "link",
            "meta",
            "param",
            "source",
            "track"
        };

        public static bool IsTag(string item)
        {
            if (SpecialCaseWordTags.Any(re => item != null && item.StartsWith(re))) return false;
            return IsOpeningTag(item) || IsClosingTag(item);
        }

        private static bool IsOpeningTag(string item)
        {
            return openingTagRegex.IsMatch(item);
        }

        private static bool IsClosingTag(string item)
        {
            return closingTagTexRegex.IsMatch(item);
        }

        public static string StripTagAttributes(string word)
        {
            var tag = tagWordRegex.Match(word).Value;
            word = tag + (word.EndsWith("/>") ? "/>" : ">");
            return word;
        }

        public static bool TryGetTagName(
            string text,
            out string tag)
        {
            tag = null;
            var match = tagRegex.Match(text);
            if (!match.Success || match.Groups.Count < 2)
            {
                return false;
            }

            tag = match.Groups[1].Value;
            return true;
        }

        public static string WrapText(
            string text,
            string tagName,
            string cssClass)
        {
            return string.Format("<{0} class='{1}'>{2}</{0}>", tagName, cssClass, text);
        }

        public static bool IsStartOfTag(char val)
        {
            return val == '<';
        }

        public static bool IsEndOfTag(char val)
        {
            return val == '>';
        }

        public static bool IsStartOfEntity(char val)
        {
            return val == '&';
        }

        public static bool IsEndOfEntity(char val)
        {
            return val == ';';
        }

        public static bool IsWhiteSpace(string value)
        {
            return whitespaceRegex.IsMatch(value);
        }

        public static bool IsWhiteSpace(char value)
        {
            return char.IsWhiteSpace(value);
        }

        public static string StripAnyAttributes(string word)
        {
            if (IsTag(word))
            {
                return StripTagAttributes(word);
            }
            return word;
        }

        public static bool IsWord(char text)
        {
            return wordRegex.IsMatch(new string(new[] { text }));
        }

        public static bool IsValidSelfClosingTag(string tag)
        {
            return SelfClosingTags.Contains(tag);
        }
    }
}