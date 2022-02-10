﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace HtmlDiff
{
    public class WordSplitter
    {
        /// <summary>
        /// Converts Html text into a list of words
        /// </summary>
        public static string[] ConvertHtmlToListOfWords(
            string text,
            IList<Regex> blockExpressions)
        {
            var mode = Mode.Character;
            var currentWord = new List<char>();
            var words = new List<string>();
            var blockLocations = FindBlocks(
                text,
                blockExpressions);
            var isBlockCheckRequired = blockLocations.Any();
            var isGrouping = false;
            var groupingUntil = -1;

            for (var index = 0; index < text.Length; index++)
            {
                var character = text[index];

                // Don't bother executing block checks if we don't have any blocks to check for!
                if (isBlockCheckRequired)
                {
                    // Check if we have completed grouping a text sequence/block
                    if (groupingUntil == index)
                    {
                        groupingUntil = -1;
                        isGrouping = false;
                    }

                    // Check if we need to group the next text sequence/block
                    var until = 0;
                    if (blockLocations.TryGetValue(index, out until))
                    {
                        isGrouping = true;
                        groupingUntil = until;
                    }

                    // if we are grouping, then we don't care about what type of character we have,
                    // it's going to be treated as a word
                    if (isGrouping)
                    {
                        currentWord.Add(character);
                        mode = Mode.Character;
                        continue;
                    }
                }

                switch (mode)
                {
                    case Mode.Character:

                        if (Utils.IsStartOfTag(character))
                        {
                            if (currentWord.Count != 0)
                            {
                                words.Add(new string(currentWord.ToArray()));
                            }

                            currentWord.Clear();
                            currentWord.Add('<');
                            mode = Mode.Tag;
                        }
                        else if (Utils.IsStartOfEntity(character))
                        {
                            if (currentWord.Count != 0)
                            {
                                words.Add(new string(currentWord.ToArray()));
                            }

                            currentWord.Clear();
                            currentWord.Add(character);
                            mode = Mode.Entity;
                        }
                        else if (Utils.IsWhiteSpace(character))
                        {
                            if (currentWord.Count != 0)
                            {
                                words.Add(new string(currentWord.ToArray()));
                            }
                            currentWord.Clear();
                            currentWord.Add(character);
                            mode = Mode.Whitespace;
                        }
                        else if (Utils.IsWord(character)
                            && (currentWord.Count == 0 || Utils.IsWord(currentWord.Last())))
                        {
                            currentWord.Add(character);
                        }
                        else
                        {
                            if (currentWord.Count != 0)
                            {
                                words.Add(new string(currentWord.ToArray()));
                            }
                            currentWord.Clear();
                            currentWord.Add(character);
                        }

                        break;
                    case Mode.Tag:

                        if (Utils.IsEndOfTag(character))
                        {
                            currentWord.Add(character);
                            var tags = ExpandTagIfNeeded(currentWord.ToArray());
                            words.AddRange(tags);
                            currentWord.Clear();

                            mode = Utils.IsWhiteSpace(character) 
                                ? Mode.Whitespace
                                : Mode.Character;
                        }
                        else
                        {
                            currentWord.Add(character);
                        }

                        break;
                    case Mode.Whitespace:

                        if (Utils.IsStartOfTag(character))
                        {
                            if (currentWord.Count != 0)
                            {
                                words.Add(new string(currentWord.ToArray()));
                            }
                            currentWord.Clear();
                            currentWord.Add(character);
                            mode = Mode.Tag;
                        }
                        else if (Utils.IsStartOfEntity(character))
                        {
                            if (currentWord.Count != 0)
                            {
                                words.Add(new string(currentWord.ToArray()));
                            }

                            currentWord.Clear();
                            currentWord.Add(character);
                            mode = Mode.Entity;
                        }
                        else if (Utils.IsWhiteSpace(character))
                        {
                            currentWord.Add(character);
                        }
                        else
                        {
                            if (currentWord.Count != 0)
                            {
                                words.Add(new string(currentWord.ToArray()));
                            }

                            currentWord.Clear();
                            currentWord.Add(character);
                            mode = Mode.Character;
                        }

                        break;
                    case Mode.Entity:
                        if (Utils.IsStartOfTag(character))
                        {
                            if (currentWord.Count != 0)
                            {
                                words.Add(new string(currentWord.ToArray()));
                            }

                            currentWord.Clear();
                            currentWord.Add(character);
                            mode = Mode.Tag;
                        }
                        else if (char.IsWhiteSpace(character))
                        {
                            if (currentWord.Count != 0)
                            {
                                words.Add(new string(currentWord.ToArray()));
                            }
                            currentWord.Clear();
                            currentWord.Add(character);
                            mode = Mode.Whitespace;
                        }
                        else if (Utils.IsEndOfEntity(character))
                        {
                            var switchToNextMode = true;
                            if (currentWord.Count != 0)
                            {
                                currentWord.Add(character);
                                words.Add(new string(currentWord.ToArray()));

                                //join &nbsp; entity with last whitespace
                                if (words.Count > 2
                                    && Utils.IsWhiteSpace(words[words.Count - 2])
                                    && Utils.IsWhiteSpace(words[words.Count - 1]))
                                {
                                    var w1 = words[words.Count - 2];
                                    var w2 = words[words.Count - 1];
                                    words.RemoveRange(words.Count - 2, 2);
                                    currentWord.Clear();
                                    currentWord.AddRange(w1);
                                    currentWord.AddRange(w2);
                                    mode = Mode.Whitespace;
                                    switchToNextMode = false;
                                }
                            }
                            if (switchToNextMode)
                            {
                                currentWord.Clear();
                                mode = Mode.Character;
                            }
                        }
                        else if (Utils.IsWord(character))
                        {
                            currentWord.Add(character);
                        }
                        else
                        {
                            if (currentWord.Count != 0)
                            {
                                words.Add(new string(currentWord.ToArray()));
                            }
                            currentWord.Clear();
                            currentWord.Add(character);
                            mode = Mode.Character;
                        }
                        break;
                }
            }
            if (currentWord.Count != 0)
            {
                words.Add(new string(currentWord.ToArray()));
            }

            return words.ToArray();
        }

        private static string[] ExpandTagIfNeeded(char[] chars)
        {
            var tag = new string(chars);
            var isSelfClosing = chars[chars.Length - 2] == '/';
            if (!isSelfClosing)
            {
                return new [] {tag};
            }

            if (!Utils.TryGetTagName(
                tag,
                out var tagName))
            {
                return new [] {tag};
            }

            if (Utils.IsValidSelfClosingTag(tagName))
            {
                return new[] {tag};
            }

            return new[]
            {
                $"{new string(chars.Take(chars.Length - 2).ToArray())}>",
                $"</{tagName}>"
            };
        }

        /// <summary>
        /// Finds any blocks that need to be grouped
        /// </summary>
        private static Dictionary<int, int> FindBlocks(
            string text, 
            IList<Regex> blockExpressions)
        {
            var blockLocations = new Dictionary<int, int>();
            if (blockExpressions == null)
            {
                return blockLocations;
            }
            foreach (var exp in blockExpressions)
            {
                var matches = exp.Matches(text);
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    try
                    {
                        blockLocations.Add(match.Index, match.Index + match.Length);
                    }
                    catch (ArgumentException)
                    {
                        var msg =
                            $"One or more block expressions result in a text sequence that overlaps. Current expression: {exp}";
                        throw new ArgumentException(msg);
                    }
                }
            }
            return blockLocations;
        }
    }
}
