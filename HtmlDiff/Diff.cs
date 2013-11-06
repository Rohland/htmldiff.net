using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HtmlDiff
{
    public class HtmlDiff
    {
        private readonly StringBuilder _content;
        private readonly string _newText;
        private readonly string _oldText;

        private readonly string[] _specialCaseClosingTags = { "</strong>", "</b>", "</i>", "</big>", "</small>", "</u>", "</sub>", "</sup>", "</strike>", "</s>" };
        private readonly string[] _specialCaseOpeningTags = { "<strong[\\>\\s]+", "<b[\\>\\s]+", "<i[\\>\\s]+", "<big[\\>\\s]+", "<small[\\>\\s]+", "<u[\\>\\s]+", "<sub[\\>\\s]+", "<sup[\\>\\s]+", "<strike[\\>\\s]+", "<s[\\>\\s]+" };
        private static readonly string[] SpecialCaseWordTags = { "<img" };

        private string[] _newWords;
        private string[] _oldWords;
        private Dictionary<string, List<int>> _wordIndices;


        /// <summary>
        ///     Initializes a new instance of the class.
        /// </summary>
        /// <param name="oldText">The old text.</param>
        /// <param name="newText">The new text.</param>
        public HtmlDiff(string oldText, string newText)
        {
            _oldText = oldText;
            _newText = newText;

            _content = new StringBuilder();
        }

        public static string Execute(string oldText, string newText)
        {
            return new HtmlDiff(oldText, newText).Build();
        }

        public string Build(string oldText, string newText)
        {
            return new HtmlDiff(oldText, newText).Build();
        }

        /// <summary>
        ///     Builds the HTML diff output
        /// </summary>
        /// <returns>HTML diff markup</returns>
        public string Build()
        {
            SplitInputsToWords();

            IndexNewWords();

            IEnumerable<Operation> operations = Operations();

            foreach (Operation item in operations)
            {
                PerformOperation(item);
            }

            return _content.ToString();
        }

        private void IndexNewWords()
        {
            _wordIndices = new Dictionary<string, List<int>>();
            for (int i = 0; i < _newWords.Length; i++)
            {
                string word = _newWords[i];

                // if word is a tag, we should ignore attributes as attribute changes are not supported (yet)
                if (IsTag(word))
                {
                    word = StripTagAttributes(word);
                }

                if (_wordIndices.ContainsKey(word))
                {
                    _wordIndices[word].Add(i);
                }
                else
                {
                    _wordIndices[word] = new List<int> {i};
                }
            }
        }

        private static string StripTagAttributes(string word)
        {
            string tag = Regex.Match(word, @"<[^\s>]+", RegexOptions.None).Value;
            word = tag + (word.EndsWith("/>") ? "/>" : ">");
            return word;
        }

        private void SplitInputsToWords()
        {
            _oldWords = ConvertHtmlToListOfWords(Explode(_oldText));
            _newWords = ConvertHtmlToListOfWords(Explode(_newText));
        }

        private string[] ConvertHtmlToListOfWords(IEnumerable<string> characterString)
        {
            var mode = Mode.Character;
            string currentWord = String.Empty;
            var words = new List<string>();

            foreach (string character in characterString)
            {
                switch (mode)
                {
                    case Mode.Character:

                        if (IsStartOfTag(character))
                        {
                            if (currentWord != String.Empty)
                            {
                                words.Add(currentWord);
                            }

                            currentWord = "<";
                            mode = Mode.Tag;
                        }
                        else if (Regex.IsMatch(character, @"\s", RegexOptions.ECMAScript))
                        {
                            if (currentWord != String.Empty)
                            {
                                words.Add(currentWord);
                            }
                            currentWord = character;
                            mode = Mode.Whitespace;
                        }
                        else if (Regex.IsMatch(character, @"[\w\#@]+", RegexOptions.IgnoreCase | RegexOptions.ECMAScript))
                        {
                            currentWord += character;
                        }
                        else
                        {
                            if (currentWord != String.Empty)
                            {
                                words.Add(currentWord);
                            }
                            currentWord = character;
                        }

                        break;
                    case Mode.Tag:

                        if (IsEndOfTag(character))
                        {
                            currentWord += ">";
                            words.Add(currentWord);
                            currentWord = "";

                            mode = IsWhiteSpace(character) ? Mode.Whitespace : Mode.Character;
                        }
                        else
                        {
                            currentWord += character;
                        }

                        break;
                    case Mode.Whitespace:

                        if (IsStartOfTag(character))
                        {
                            if (currentWord != String.Empty)
                            {
                                words.Add(currentWord);
                            }
                            currentWord = "<";
                            mode = Mode.Tag;
                        }
                        else if (Regex.IsMatch(character, "\\s"))
                        {
                            currentWord += character;
                        }
                        else
                        {
                            if (currentWord != String.Empty)
                            {
                                words.Add(currentWord);
                            }

                            currentWord = character;
                            mode = Mode.Character;
                        }

                        break;
                }
            }
            if (currentWord != string.Empty)
            {
                words.Add(currentWord);
            }

            return words.ToArray();
        }

        private void PerformOperation(Operation operation)
        {
            switch (operation.Action)
            {
                case Action.Equal:
                    ProcessEqualOperation(operation);
                    break;
                case Action.Delete:
                    ProcessDeleteOperation(operation, "diffdel");
                    break;
                case Action.Insert:
                    ProcessInsertOperation(operation, "diffins");
                    break;
                case Action.None:
                    break;
                case Action.Replace:
                    ProcessReplaceOperation(operation);
                    break;
            }
        }

        private void ProcessReplaceOperation(Operation operation)
        {
            ProcessDeleteOperation(operation, "diffmod");
            ProcessInsertOperation(operation, "diffmod");
        }

        private void ProcessInsertOperation(Operation operation, string cssClass)
        {
            InsertTag("ins", cssClass,
                _newWords.Where((s, pos) => pos >= operation.StartInNew && pos < operation.EndInNew).ToList());
        }

        private void ProcessDeleteOperation(Operation operation, string cssClass)
        {
            List<string> text =
                _oldWords.Where((s, pos) => pos >= operation.StartInOld && pos < operation.EndInOld).ToList();
            InsertTag("del", cssClass, text);
        }

        private void ProcessEqualOperation(Operation operation)
        {
            string[] result =
                _newWords.Where((s, pos) => pos >= operation.StartInNew && pos < operation.EndInNew).ToArray();
            _content.Append(String.Join("", result));
        }


        /// <summary>
        ///     This method encloses words within a specified tag (ins or del), and adds this into "content",
        ///     with a twist: if there are words contain tags, it actually creates multiple ins or del,
        ///     so that they don't include any ins or del. This handles cases like
        ///     old: '<p>a</p>'
        ///     new: '<p>ab</p>
        ///     <p>
        ///         c</b>'
        ///         diff result: '<p>a<ins>b</ins></p>
        ///         <p>
        ///             <ins>c</ins>
        ///         </p>
        ///         '
        ///         this still doesn't guarantee valid HTML (hint: think about diffing a text containing ins or
        ///         del tags), but handles correctly more cases than the earlier version.
        ///         P.S.: Spare a thought for people who write HTML browsers. They live in this ... every day.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="cssClass"></param>
        /// <param name="words"></param>
        private void InsertTag(string tag, string cssClass, List<string> words)
        {
            while (true)
            {
                if (words.Count == 0)
                {
                    break;
                }

                string[] nonTags = ExtractConsecutiveWords(words, x => !IsTag(x));

                string specialCaseTagInjection = string.Empty;
                bool specialCaseTagInjectionIsBefore = false;

                if (nonTags.Length != 0)
                {
                    string text = WrapText(string.Join("", nonTags), tag, cssClass);

                    _content.Append(text);
                }
                else
                {
                    // Check if strong tag

                    if (_specialCaseOpeningTags.FirstOrDefault(x => Regex.IsMatch(words[0], x)) != null)
                    {
                        specialCaseTagInjection = "<ins class='mod'>";
                        if (tag == "del")
                        {
                            words.RemoveAt(0);
                        }
                    }
                    else if (_specialCaseClosingTags.Contains(words[0]))
                    {
                        specialCaseTagInjection = "</ins>";
                        specialCaseTagInjectionIsBefore = true;
                        if (tag == "del")
                        {
                            words.RemoveAt(0);
                        }
                    }
                }

                if (words.Count == 0 && specialCaseTagInjection.Length == 0)
                {
                    break;
                }

                if (specialCaseTagInjectionIsBefore)
                {
                    _content.Append(specialCaseTagInjection + String.Join("", ExtractConsecutiveWords(words, IsTag)));
                }
                else
                {
                    _content.Append(String.Join("", ExtractConsecutiveWords(words, IsTag)) + specialCaseTagInjection);
                }
            }
        }

        private string[] ExtractConsecutiveWords(List<string> words, Func<string, bool> condition)
        {
            int? indexOfFirstTag = null;

            for (int i = 0; i < words.Count; i++)
            {
                string word = words[i];

                if (!condition(word))
                {
                    indexOfFirstTag = i;
                    break;
                }
            }

            if (indexOfFirstTag != null)
            {
                string[] items = words.Where((s, pos) => pos >= 0 && pos < indexOfFirstTag).ToArray();
                if (indexOfFirstTag.Value > 0)
                {
                    words.RemoveRange(0, indexOfFirstTag.Value);
                }
                return items;
            }
            else
            {
                string[] items = words.Where((s, pos) => pos >= 0 && pos <= words.Count).ToArray();
                words.RemoveRange(0, words.Count);
                return items;
            }
        }

        private IEnumerable<Operation> Operations()
        {
            int positionInOld = 0, positionInNew = 0;
            var operations = new List<Operation>();

            var matches = MatchingBlocks();

            matches.Add(new Match(_oldWords.Length, _newWords.Length, 0));

            foreach (Match match in matches)
            {
                bool matchStartsAtCurrentPositionInOld = (positionInOld == match.StartInOld);
                bool matchStartsAtCurrentPositionInNew = (positionInNew == match.StartInNew);

                Action action;

                if (matchStartsAtCurrentPositionInOld == false
                    && matchStartsAtCurrentPositionInNew == false)
                {
                    action = Action.Replace;
                }
                else if (matchStartsAtCurrentPositionInOld
                         && matchStartsAtCurrentPositionInNew == false)
                {
                    action = Action.Insert;
                }
                else if (matchStartsAtCurrentPositionInOld == false)
                {
                    action = Action.Delete;
                }
                else // This occurs if the first few words are the same in both versions
                {
                    action = Action.None;
                }

                if (action != Action.None)
                {
                    operations.Add(
                        new Operation(action,
                            positionInOld,
                            match.StartInOld,
                            positionInNew,
                            match.StartInNew));
                }

                if (match.Size != 0)
                {
                    operations.Add(new Operation(
                        Action.Equal,
                        match.StartInOld,
                        match.EndInOld,
                        match.StartInNew,
                        match.EndInNew));
                }

                positionInOld = match.EndInOld;
                positionInNew = match.EndInNew;
            }

            return operations;
        }

        private List<Match> MatchingBlocks()
        {
            var matchingBlocks = new List<Match>();
            FindMatchingBlocks(0, _oldWords.Length, 0, _newWords.Length, matchingBlocks);
            return matchingBlocks;
        }


        private void FindMatchingBlocks(int startInOld, int endInOld, int startInNew, int endInNew,
            List<Match> matchingBlocks)
        {
            Match match = FindMatch(startInOld, endInOld, startInNew, endInNew);

            if (match != null)
            {
                if (startInOld < match.StartInOld && startInNew < match.StartInNew)
                {
                    FindMatchingBlocks(startInOld, match.StartInOld, startInNew, match.StartInNew, matchingBlocks);
                }

                matchingBlocks.Add(match);

                if (match.EndInOld < endInOld && match.EndInNew < endInNew)
                {
                    FindMatchingBlocks(match.EndInOld, endInOld, match.EndInNew, endInNew, matchingBlocks);
                }
            }
        }


        private Match FindMatch(int startInOld, int endInOld, int startInNew, int endInNew)
        {
            int bestMatchInOld = startInOld;
            int bestMatchInNew = startInNew;
            int bestMatchSize = 0;

            var matchLengthAt = new Dictionary<int, int>();

            for (int indexInOld = startInOld; indexInOld < endInOld; indexInOld++)
            {
                var newMatchLengthAt = new Dictionary<int, int>();

                string index = _oldWords[indexInOld];

                if (IsTag(index)) // strip attributes as this is not supported (yet)
                {
                    index = StripTagAttributes(index);
                }

                if (!_wordIndices.ContainsKey(index))
                {
                    matchLengthAt = newMatchLengthAt;
                    continue;
                }

                foreach (int indexInNew in _wordIndices[index])
                {
                    if (indexInNew < startInNew)
                    {
                        continue;
                    }

                    if (indexInNew >= endInNew)
                    {
                        break;
                    }


                    int newMatchLength = (matchLengthAt.ContainsKey(indexInNew - 1) ? matchLengthAt[indexInNew - 1] : 0) +
                                         1;
                    newMatchLengthAt[indexInNew] = newMatchLength;

                    if (newMatchLength > bestMatchSize)
                    {
                        bestMatchInOld = indexInOld - newMatchLength + 1;
                        bestMatchInNew = indexInNew - newMatchLength + 1;
                        bestMatchSize = newMatchLength;
                    }
                }

                matchLengthAt = newMatchLengthAt;
            }

            return bestMatchSize != 0 ? new Match(bestMatchInOld, bestMatchInNew, bestMatchSize) : null;
        }

        private static string WrapText(string text, string tagName, string cssClass)
        {
            return string.Format("<{0} class='{1}'>{2}</{0}>", tagName, cssClass, text);
        }

        private static bool IsTag(string item)
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

        private static bool IsStartOfTag(string val)
        {
            return val == "<";
        }

        private static bool IsEndOfTag(string val)
        {
            return val == ">";
        }

        private static bool IsWhiteSpace(string value)
        {
            return Regex.IsMatch(value, "\\s");
        }

        private static IEnumerable<string> Explode(string value)
        {
            return Regex.Split(value, @"");
        }
    }

    public class Match
    {
        public Match(int startInOld, int startInNew, int size)
        {
            StartInOld = startInOld;
            StartInNew = startInNew;
            Size = size;
        }

        public int StartInOld { get; set; }
        public int StartInNew { get; set; }
        public int Size { get; set; }

        public int EndInOld
        {
            get { return StartInOld + Size; }
        }

        public int EndInNew
        {
            get { return StartInNew + Size; }
        }
    }

    public class Operation
    {
        public Operation(Action action, int startInOld, int endInOld, int startInNew, int endInNew)
        {
            Action = action;
            StartInOld = startInOld;
            EndInOld = endInOld;
            StartInNew = startInNew;
            EndInNew = endInNew;
        }

        public Action Action { get; set; }
        public int StartInOld { get; set; }
        public int EndInOld { get; set; }
        public int StartInNew { get; set; }
        public int EndInNew { get; set; }
    }

    public enum Mode
    {
        Character,
        Tag,
        Whitespace,
    }

    public enum Action
    {
        Equal,
        Delete,
        Insert,
        None,
        Replace
    }
}