using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HtmlDiff
{
    public class WordSplitter
    {
        /// <summary>
        /// Converts Html text into a list of words
        /// </summary>
        /// <param name="text"></param>
        /// <param name="blockExpressions"></param>
        /// <returns></returns>
        public static string[] ConvertHtmlToListOfWords(string text, List<Regex> blockExpressions)
        {
            var mode = Mode.Character;
            var currentWord = new List<char>();
            var words = new List<string>();

            Dictionary<int, int> blockLocations = FindBlocks(text, blockExpressions);

            bool isBlockCheckRequired = blockLocations.Any();
            bool isGrouping = false;
            int groupingUntil = -1;

            for (int index = 0; index < text.Length; index++)
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
                    int until = 0;
                    if (blockLocations.TryGetValue(index, out until))
                    {
                        isGrouping = true;
                        groupingUntil = until;
                    }

                    // if we are grouping, then we don't care about what type of character we have, it's going to be treated as a word
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
                            words.Add(new string(currentWord.ToArray()));
                            currentWord.Clear();

                            mode = Utils.IsWhiteSpace(character) ? Mode.Whitespace : Mode.Character;
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

        /// <summary>
        /// Finds any blocks that need to be grouped
        /// </summary>
        /// <param name="text"></param>
        /// <param name="blockExpressions"></param>
        private static Dictionary<int, int> FindBlocks(string text, List<Regex> blockExpressions)
        {
            Dictionary<int, int> blockLocations = new Dictionary<int, int>();
            
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
                    catch (ArgumentException e)
                    {
                        throw new ArgumentException("One or more block expressions result in a text sequence that overlaps. Current expression: " + exp.ToString());
                    }
                }
            }
            return blockLocations;
        }
    }
}
