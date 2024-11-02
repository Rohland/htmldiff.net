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
        private const string DelTag = "del";
        private const string InsTag = "ins";

        private readonly StringBuilder _content;
        private string _newText;
        private string _oldText;
        

        private static Dictionary<string, int> _specialCaseClosingTags = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            {"</strong>", 0},
            {"</em>", 0},
            {"</b>",0},
            {"</i>",0},
            {"</big>",0},
            {"</small>",0},
            {"</u>",0},
            {"</sub>",0},
            {"</sup>",0},
            {"</strike>",0},
            {"</s>",0},
            {"</span>", 0}
        };

        private static readonly Regex SpecialCaseOpeningTagRegex = new Regex(
            "<((strong)|(b)|(i)|(em)|(big)|(small)|(u)|(sub)|(sup)|(strike)|(s)|(span))[\\>\\s]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);


        /// <summary>
        /// Tracks opening and closing formatting tags to ensure that we don't inadvertently generate invalid html during the diff process.
        /// </summary>
        private readonly Stack<string> _specialTagDiffStack; 

        private string[] _newWords;
        private string[] _oldWords;
        private int _matchGranularity;
        private readonly List<Regex> _blockExpressions; 

        /// <summary>
        /// Defines how to compare repeating words. Valid values are from 0 to 1.
        /// This value allows to exclude some words from comparison that eventually
        /// reduces the total time of the diff algorithm.
        /// 0 means that all words are excluded so the diff will not find any matching words at all.
        /// 1 (default value) means that all words participate in comparison so this is the most accurate case.
        /// 0.5 means that any word that occurs more than 50% times may be excluded from comparison. This doesn't
        /// mean that such words will definitely be excluded but only gives a permission to exclude them if necessary.
        /// </summary>
        public double RepeatingWordsAccuracy { get; set; }

        /// <summary>
        /// If true all whitespaces are considered as equal
        /// </summary>
        public bool IgnoreWhitespaceDifferences { get; set; }

        /// <summary>
        /// If some match is too small and located far from its neighbors then it is considered as orphan
        /// and removed. For example:
        /// <code>
        /// aaaaa bb ccccccccc dddddd ee
        /// 11111 bb 222222222 dddddd ee
        /// </code>
        /// will find two matches <code>bb</code> and <code>dddddd ee</code> but the first will be considered
        /// as orphan and ignored, as result it will consider texts <code>aaaaa bb ccccccccc</code> and
        /// <code>11111 bb 222222222</code> as single replacement:
        /// <code>
        /// &lt;del&gt;aaaaa bb ccccccccc&lt;/del&gt;&lt;ins&gt;11111 bb 222222222&lt;/ins&gt; dddddd ee
        /// </code>
        /// This property defines relative size of the match to be considered as orphan, from 0 to 1.
        /// 1 means that all matches will be considered as orphans.
        /// 0 (default) means that no match will be considered as orphan.
        /// 0.2 means that if match length is less than 20% of distance between its neighbors it is considered as orphan.
        /// </summary>
        public double OrphanMatchThreshold { get; set; }

        /// <summary>
        ///     Initializes a new instance of the class.
        /// </summary>
        /// <param name="oldText">The old text.</param>
        /// <param name="newText">The new text.</param>
        public HtmlDiff(string oldText, string newText)
        {
            RepeatingWordsAccuracy = 1d; //by default all repeating words should be compared

            _oldText = oldText;
            _newText = newText;

            _content = new StringBuilder();
            _specialTagDiffStack = new Stack<string>();
            _blockExpressions = new List<Regex>();
        }

        public static string Execute(string oldText, string newText)
        {
            return new HtmlDiff(oldText, newText).Build();
        }

        /// <summary>
        /// Builds the HTML diff output
        /// </summary>
        /// <returns>HTML diff markup</returns>
        public string Build()
        {
            // If there is no difference, don't bother checking for differences
            if (_oldText == _newText)
            {
                return _newText;
            }

            SplitInputsToWords();

            _matchGranularity = Math.Min(MatchGranularityMaximum, Math.Min(_oldWords.Length, _newWords.Length));

            var operations = Operations();

            foreach (var item in operations)
            {
                PerformOperation(item);
            }

            return _content.ToString();
        }

        /// <summary>
        /// Uses <paramref name="expression"/> to group text together so that any change detected within the group is treated as a single block
        /// </summary>
        /// <param name="expression"></param>
        public void AddBlockExpression(Regex expression)
        {
            _blockExpressions.Add(expression);
        }

        private void SplitInputsToWords()
        {
            _oldWords = WordSplitter.ConvertHtmlToListOfWords(_oldText, _blockExpressions);

            //free memory, allow it for GC
            _oldText = null;

            _newWords = WordSplitter.ConvertHtmlToListOfWords(_newText, _blockExpressions);

            //free memory, allow it for GC
            _newText = null;
        }

        private void PerformOperation(Operation operation)
        {
#if DEBUG
            operation.PrintDebugInfo(_oldWords, _newWords);
#endif

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
            var text = _newWords.Where((s, pos) => pos >= operation.StartInNew && pos < operation.EndInNew).ToList();
            InsertTag(InsTag, cssClass, text);
        }

        private void ProcessDeleteOperation(Operation operation, string cssClass)
        {
            var text = _oldWords.Where((s, pos) => pos >= operation.StartInOld && pos < operation.EndInOld).ToList();
            InsertTag(DelTag, cssClass, text);
        }

        private void ProcessEqualOperation(Operation operation)
        {
            var result =
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

                // nonTags contains all entries of words until first tag is found.
                var nonTags = ExtractConsecutiveWords(words, x => !Utils.IsTag(x));

                if (nonTags.Length != 0)
                {
                    var text = Utils.WrapText(string.Join("", nonTags), tag, cssClass);

                    _content.Append(text);
                }

                // if words doesn't contain any tags, the insert operation is complete.
                if (words.Count == 0)
                {
                    break;
                }

                // if there are still words left, they must start with a tag, but still can contain nonTag entries.
                // e.g. </span></big>bar
                // the remaining words need to be handled separately devided in a tagBlock, which definitely contains at least one word and a potentially existing second block which starts with a nonTag but may contain tags later on.
                int IndexOfFirstNonTag = words.FindIndex(x => !Utils.IsTag(x));

                // if there are no nonTags, the whole block is a tagBlock and the indext of the last tag is the last index of the block.
                // if there are nonTags, the index of the last tag is the index before the first nonTag.
                int IndexLastTagInFirstTagBlock = (IndexOfFirstNonTag == -1) ? words.Count - 1 : IndexOfFirstNonTag - 1;

                var specialCaseTagInjection = string.Empty;
                var specialCaseTagInjectionIsBefore = false;

                // handle opening tag
                if (SpecialCaseOpeningTagRegex.IsMatch(words[0]))
                {
                    _specialTagDiffStack.Push(words[0]);
                    specialCaseTagInjection = "<ins class='mod'>";
                    if (tag == DelTag)
                    {
                        words.RemoveAt(0);
                        // following tags may be formatting tags as well, follow through
                        while (words.Count > 0 && SpecialCaseOpeningTagRegex.IsMatch(words[0]))
                        {
                            words.RemoveAt(0);
                        }
                    }
                }

                // handle closing tag
                else if (_specialCaseClosingTags.ContainsKey(words[0]))
                {
                    var openingTag = _specialTagDiffStack.Count == 0 ? null : _specialTagDiffStack.Pop();
                    var hasOpeningTag = !(openingTag is null);
                    var openingAndClosingTagsMatch = Utils.GetTagName(openingTag) == Utils.GetTagName(words[IndexLastTagInFirstTagBlock]);

                    if (hasOpeningTag && openingAndClosingTagsMatch)
                    {
                        specialCaseTagInjection = "</ins>";
                        specialCaseTagInjectionIsBefore = true;
                    }

                    // if the tag has a corresponding opening tag, but they don't match, we need to push the opening tag back onto the stack
                    else if (hasOpeningTag)
                    {
                        _specialTagDiffStack.Push(openingTag);
                    }

                    if (tag == DelTag)
                    {
                        words.RemoveAt(0);
                        // following tags may be formatting tags as well, follow through
                        while (words.Count > 0 && _specialCaseClosingTags.ContainsKey(words[0]))
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

                // if there are still words left, they must start with a nonTag and need to be handled in the next iteration.
                if (words.Count != 0)
                {
                    InsertTag(tag, cssClass, words);
                    break;
                }
            }
        }

        private string[] ExtractConsecutiveWords(List<string> words, Func<string, bool> condition)
        {
            int? indexOfFirstTag = null;

            for (var i = 0; i < words.Count; i++)
            {
                var word = words[i];

                if (i == 0 && word == " ")
                {
                    words[i] = "&nbsp;";
                }

                if (!condition(word))
                {
                    indexOfFirstTag = i;
                    break;
                }
            }

            if (indexOfFirstTag != null)
            {
                var items = words.Where((s, pos) => pos >= 0 && pos < indexOfFirstTag).ToArray();
                if (indexOfFirstTag.Value > 0)
                {
                    words.RemoveRange(0, indexOfFirstTag.Value);
                }
                return items;
            }
            else
            {
                var items = words.Where((s, pos) => pos >= 0 && pos <= words.Count).ToArray();
                words.RemoveRange(0, words.Count);
                return items;
            }
        }

        private List<Operation> Operations()
        {
            int positionInOld = 0, positionInNew = 0;
            var operations = new List<Operation>();

            var matches = MatchingBlocks();

            matches.Add(new Match(_oldWords.Length, _newWords.Length, 0));

            //Remove orphans from matches.
            //If distance between left and right matches is 4 times longer than length of current match then it is considered as orphan
            var mathesWithoutOrphans = RemoveOrphans(matches);

            foreach (var match in mathesWithoutOrphans)
            {
                var matchStartsAtCurrentPositionInOld = (positionInOld == match.StartInOld);
                var matchStartsAtCurrentPositionInNew = (positionInNew == match.StartInNew);

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

        private IEnumerable<Match> RemoveOrphans(IEnumerable<Match> matches)
        {
            Match prev = null;
            Match curr = null;
            foreach (var next in matches)
            {
                if (curr == null)
                {
                    prev = new Match(0, 0, 0);
                    curr = next;
                    continue;
                }

                if (prev.EndInOld == curr.StartInOld && prev.EndInNew == curr.StartInNew
                    || curr.EndInOld == next.StartInOld && curr.EndInNew == next.StartInNew)
                //if match has no diff on the left or on the right
                {
                    yield return curr;
                    prev = curr;
                    curr = next;
                    continue;
                }

                var oldDistanceInChars = Enumerable.Range(prev.EndInOld, next.StartInOld - prev.EndInOld)
                    .Sum(i => _oldWords[i].Length);
                var newDistanceInChars = Enumerable.Range(prev.EndInNew, next.StartInNew - prev.EndInNew)
                    .Sum(i => _newWords[i].Length);
                var currMatchLengthInChars = Enumerable.Range(curr.StartInNew, curr.EndInNew - curr.StartInNew)
                    .Sum(i => _newWords[i].Length);
                if (currMatchLengthInChars > Math.Max(oldDistanceInChars, newDistanceInChars) * OrphanMatchThreshold)
                {
                    yield return curr;
                }
                
                prev = curr;
                curr = next;
            }

            yield return curr; //assume that the last match is always vital
        }

        private List<Match> MatchingBlocks()
        {
            var matchingBlocks = new List<Match>();
            FindMatchingBlocks(0, _oldWords.Length, 0, _newWords.Length, matchingBlocks);
            return matchingBlocks;
        }


        private void FindMatchingBlocks(
            int startInOld, 
            int endInOld, 
            int startInNew, 
            int endInNew,
            List<Match> matchingBlocks)
        {
            var match = FindMatch(startInOld, endInOld, startInNew, endInNew);

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
            // For large texts it is more likely that there is a Match of size bigger than maximum granularity.
            // If not then go down and try to find it with smaller granularity.
            for (var i = _matchGranularity; i > 0 ; i--)
            {
                var options = new MatchOptions
                {
                    BlockSize = i,
                    RepeatingWordsAccuracy = RepeatingWordsAccuracy,
                    IgnoreWhitespaceDifferences = IgnoreWhitespaceDifferences
                };
                var finder = new MatchFinder(_oldWords, _newWords, startInOld, endInOld, startInNew, endInNew, options);
                var match = finder.FindMatch();
                if (match != null)
                    return match;
            }
            return null;
        }
    }
}