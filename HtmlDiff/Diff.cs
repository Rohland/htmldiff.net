using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HtmlDiff
{
    public class HtmlDiff
    {
        /// <summary>
        /// This value defines balance between speed and memory utilization. The higher it is the faster it works and more memory consumes.
        /// </summary>
        private const int MatchGranularityMaximum = 4;

        private readonly StringBuilder _content;
        private string _newText;
        private string _oldText;

        private readonly string[] _specialCaseClosingTags = { "</strong>", "</b>", "</i>", "</big>", "</small>", "</u>", "</sub>", "</sup>", "</strike>", "</s>" };
        private readonly string[] _specialCaseOpeningTags = { "<strong[\\>\\s]+", "<b[\\>\\s]+", "<i[\\>\\s]+", "<big[\\>\\s]+", "<small[\\>\\s]+", "<u[\\>\\s]+", "<sub[\\>\\s]+", "<sup[\\>\\s]+", "<strike[\\>\\s]+", "<s[\\>\\s]+" };

        private string[] _newWords;
        private string[] _oldWords;
        private int _matchGranularity;

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

            _matchGranularity = Math.Min(MatchGranularityMaximum, Math.Min(_oldWords.Length, _newWords.Length));

            IEnumerable<Operation> operations = Operations();

            foreach (Operation item in operations)
            {
                PerformOperation(item);
            }

            return _content.ToString();
        }

        private void SplitInputsToWords()
        {
            _oldWords = ConvertHtmlToListOfWords(Explode(_oldText));
            //free memory
            _oldText = null;
            _newWords = ConvertHtmlToListOfWords(Explode(_newText));
            //free memory
            _newText = null;
        }

        private static string[] ConvertHtmlToListOfWords(IEnumerable<string> characterString)
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

                string[] nonTags = ExtractConsecutiveWords(words, x => !Utils.IsTag(x));

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
                    _content.Append(specialCaseTagInjection + String.Join("", ExtractConsecutiveWords(words, Utils.IsTag)));
                }
                else
                {
                    _content.Append(String.Join("", ExtractConsecutiveWords(words, Utils.IsTag)) + specialCaseTagInjection);
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
            for (int i = _matchGranularity; i > 0 ; i--)
            {
                var finder = new MatchFinder(i, _oldWords, _newWords, startInOld, endInOld, startInNew, endInNew);
                var match = finder.FindMatch();
                if (match != null)
                    return match;
            }
            return null;
        }

        private static string WrapText(string text, string tagName, string cssClass)
        {
            return string.Format("<{0} class='{1}'>{2}</{0}>", tagName, cssClass, text);
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
}