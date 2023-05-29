using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace HtmlDiff
{
    public class WordSplitter
    {
        private string text;
        private IList<Regex> blockExpressions;
        private bool isBlockCheckRequired;
        private BlockFinderResult blockLocations;
        private Mode mode;
        private bool isGrouping;
        private int globbingUntil;
        private List<char> currentWord;
        private List<string> words;
        private const int NotGlobbing = -1;
        
        private bool currentWordHasChars => currentWord.Count > 0;

        public WordSplitter(
            string text,
            IList<Regex> blockExpressions)
        {
            this.text = text;
            this.blockExpressions = blockExpressions;
            blockLocations = FindBlocksToBeGrouped(text, blockExpressions);
            isBlockCheckRequired = blockLocations.HasBlocks;
            mode = Mode.Character;
            globbingUntil = NotGlobbing;
            currentWord = new List<char>();
            words = new List<string>();
        }

        public string[] Process()
        {
            for (var index = 0; index < text.Length; index++)
            {
                var character = text[index];
                ProcessCharacter(index, character);
            }
            AppendCurrentWordToWords();
            return words.ToArray();
        }

        private void ProcessCharacter(
            int index,
            char character)
        {
            if (IsGlobbing(index, character))
            {
                return;
            }
            switch (mode)
            {
                case Mode.Character:
                    ProcessTextCharacter(character);
                    break;
                case Mode.Tag:
                    ProcessHtmlTagContinuation(character);
                    break;
                case Mode.Whitespace:
                    ProcessWhiteSpaceContinuation(character);
                    break;
                case Mode.Entity:
                    ProcessEntityContinuation(character);
                    break;
            }
        }

        private void ProcessEntityContinuation(char character)
        {
            if (Utils.IsStartOfTag(character))
            {
                AppendCurrentWordToWords();
                currentWord.Add(character);
                mode = Mode.Tag;
            }
            else if (char.IsWhiteSpace(character))
            {
                AppendCurrentWordToWords();
                currentWord.Add(character);
                mode = Mode.Whitespace;
            }
            else if (Utils.IsEndOfEntity(character))
            {
                var switchToNextMode = true;
                if (currentWordHasChars)
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
                AppendCurrentWordToWords();
                currentWord.Add(character);
                mode = Mode.Character;
            }
        }

        private void ProcessWhiteSpaceContinuation(char character)
        {
            if (Utils.IsStartOfTag(character))
            {
                AppendCurrentWordToWords();
                currentWord.Add(character);
                mode = Mode.Tag;
            }
            else if (Utils.IsStartOfEntity(character))
            {
                AppendCurrentWordToWords();
                currentWord.Add(character);
                mode = Mode.Entity;
            }
            else if (Utils.IsWhiteSpace(character))
            {
                currentWord.Add(character);
            }
            else
            {
                AppendCurrentWordToWords();
                currentWord.Add(character);
                mode = Mode.Character;
            }
        }

        private void ProcessHtmlTagContinuation(char character)
        {
            if (Utils.IsEndOfTag(character))
            {
                currentWord.Add(character);
                AppendCurrentWordToWords();
                mode = Utils.IsWhiteSpace(character) 
                    ? Mode.Whitespace
                    : Mode.Character;
            }
            else
            {
                currentWord.Add(character);
            }
        }

        private void ProcessTextCharacter(char character)
        {
            if (Utils.IsStartOfTag(character))
            {
                AppendCurrentWordToWords();
                currentWord.Add('<');
                mode = Mode.Tag;
            }
            else if (Utils.IsStartOfEntity(character))
            {
                AppendCurrentWordToWords();
                currentWord.Add(character);
                mode = Mode.Entity;
            }
            else if (Utils.IsWhiteSpace(character))
            {
                AppendCurrentWordToWords();
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
                AppendCurrentWordToWords();
                currentWord.Add(character);
            }
        }

        private void AppendCurrentWordToWords()
        {
            if (currentWordHasChars)
            {
                words.Add(new string(currentWord.ToArray()));
                currentWord.Clear();
            }
        }

        private bool IsGlobbing(
            int index,
            char character)
        {
            if (!isBlockCheckRequired)
            {
                return false;
            }
            var isCurrentBlockTerminating = index == globbingUntil;
            if (isCurrentBlockTerminating)
            {
                globbingUntil = NotGlobbing;
                isGrouping = false;
                AppendCurrentWordToWords();
            }
            if (blockLocations.IsInBlock(index, out var until))
            {
                isGrouping = true;
                globbingUntil = until;
            }
            if (isGrouping)
            {
                currentWord.Add(character);
                mode = Mode.Character;
            }
            return isGrouping;
        }

        BlockFinderResult FindBlocksToBeGrouped(
            string text,
            IList<Regex> blockExpressions)
        {
            var finder = new BlockFinder(text, blockExpressions);
            return finder.FindBlocks();
        }

        public static string[] ConvertHtmlToListOfWords(
            string text,
            IList<Regex> blockExpressions)
        {
            var converter = new WordSplitter(
                text,
                blockExpressions);
            return converter.Process();
        }
    }

    class BlockFinderResult
    {
        private IDictionary<int, int> blocks;

        public BlockFinderResult()
        {
            blocks = new Dictionary<int, int>();
        }

        public void AddBlock(int from, int to)
        {
            blocks.Add(from, to);
        }

        public bool IsInBlock(int location, out int endLocation)
        {
            return blocks.TryGetValue(location, out endLocation);
        }

        public bool HasBlocks => blocks.Keys.Any();
    }

    class BlockFinder
    {
        private string text;
        private IList<Regex> blockExpressions;

        public BlockFinder(
            string text,
            IList<Regex> blockExpressions)
        {
            this.text = text;
            this.blockExpressions = blockExpressions;
        }

        public BlockFinderResult FindBlocks()
        {
            var result = new BlockFinderResult();
            if (blockExpressions == null)
            {
                return result;
            }

            foreach (var exp in blockExpressions)
            {
                ProcessBlockMatcher(exp, result);
            }

            return result;
        }

        private void ProcessBlockMatcher(
            Regex exp,
            BlockFinderResult result)
        {
            var matches = exp.Matches(text);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                TryAddBlock(exp, match, result);
            }
        }

        private void TryAddBlock(Regex exp,
            System.Text.RegularExpressions.Match match,
            BlockFinderResult result)
        {
            try
            {
                var from = match.Index;
                var to = match.Index + match.Length;
                result.AddBlock(from, to);
            }
            catch (ArgumentException)
            {
                var msg =
                    $"One or more block expressions result in a text sequence that overlaps. Current expression: {exp}";
                throw new ArgumentException(msg);
            }
        }
    }
}