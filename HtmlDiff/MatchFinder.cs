using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HtmlDiff
{
    /// <summary>
    /// Finds biggest Match in given texts. It uses indexing with fixed granularity that is used to compare blocks of text.
    /// </summary>
    internal class MatchFinder
    {
        private readonly string[] _oldWords;
        private readonly string[] _newWords;
        private readonly int _startInOld;
        private readonly int _endInOld;
        private readonly int _startInNew;
        private readonly int _endInNew;
        private Dictionary<string, List<int>> _wordIndices;
        private readonly MatchOptions _options;

        /// <summary>
        /// </summary>
        /// <param name="oldWords"></param>
        /// <param name="newWords"></param>
        /// <param name="startInOld"></param>
        /// <param name="endInOld"></param>
        /// <param name="startInNew"></param>
        /// <param name="endInNew"></param>
        /// <param name="options"></param>
        public MatchFinder(string[] oldWords, string[] newWords, int startInOld, int endInOld, int startInNew, int endInNew, MatchOptions options)
        {
            _oldWords = oldWords;
            _newWords = newWords;
            _startInOld = startInOld;
            _endInOld = endInOld;
            _startInNew = startInNew;
            _endInNew = endInNew;
            _options = options;
        }

        private void IndexNewWords()
        {
            _wordIndices = new Dictionary<string, List<int>>();
            var block = new Queue<string>(_options.BlockSize);
            for (int i = _startInNew; i < _endInNew; i++)
            {
                var word = ToCleanWord(_newWords[i]);
                var key = PutNewWord(block, word, _options.BlockSize);

                if (key == null)
                    continue;

                if (_wordIndices.ContainsKey(key))
                {
                    _wordIndices[key].Add(i);
                }
                else
                {
                    _wordIndices[key] = new List<int> { i };
                }
            }
        }

        private static string PutNewWord(Queue<string> block, string word, int blockSize)
        {
            block.Enqueue(word);
            if (block.Count > blockSize)
                block.Dequeue();

            if (block.Count != blockSize)
                return null;
            
            var result = new StringBuilder();
            foreach (var s in block)
            {
                result.Append(s);
            }
            return result.ToString();
        }

        private string ToCleanWord(string word)
        {
            // if word is a tag, we should ignore attributes as attribute changes are not supported (yet)
            if (Utils.IsTag(word))
            {
                word = Utils.StripTagAttributes(word);
            }
            if (_options.IgnoreWhitespaceDifferences && Utils.IsWhitespace(word))
                return " ";

            return word;
        }

        public Match FindMatch()
        {
            IndexNewWords();
            RemoveRepeatingWords();

            if (_wordIndices.Count == 0)
                return null;

            int bestMatchInOld = _startInOld;
            int bestMatchInNew = _startInNew;
            int bestMatchSize = 0;

            var matchLengthAt = new Dictionary<int, int>();
            var block = new Queue<string>(_options.BlockSize);

            for (int indexInOld = _startInOld; indexInOld < _endInOld; indexInOld++)
            {
                var word = ToCleanWord(_oldWords[indexInOld]);
                var index = PutNewWord(block, word, _options.BlockSize);
                if (index == null)
                    continue;

                var newMatchLengthAt = new Dictionary<int, int>();

                if (!_wordIndices.ContainsKey(index))
                {
                    matchLengthAt = newMatchLengthAt;
                    continue;
                }

                foreach (int indexInNew in _wordIndices[index])
                {
                    int newMatchLength = (matchLengthAt.ContainsKey(indexInNew - 1) ? matchLengthAt[indexInNew - 1] : 0) +
                                         1;
                    newMatchLengthAt[indexInNew] = newMatchLength;

                    if (newMatchLength > bestMatchSize)
                    {
                        bestMatchInOld = indexInOld - newMatchLength + 1 - _options.BlockSize + 1;
                        bestMatchInNew = indexInNew - newMatchLength + 1 - _options.BlockSize + 1;
                        bestMatchSize = newMatchLength;
                    }
                }

                matchLengthAt = newMatchLengthAt;
            }

            return bestMatchSize != 0 ? new Match(bestMatchInOld, bestMatchInNew, bestMatchSize + _options.BlockSize - 1) : null;
        }

        /// <summary>
        /// This method removes words that occur too many times. This way it reduces total count of comparison operations
        /// and as result the diff algoritm takes less time. But the side effect is that it may detect false differences of
        /// the repeating words.
        /// </summary>
        private void RemoveRepeatingWords()
        {
            var threshold = _newWords.Length * _options.RepeatingWordsAccuracy;
            var repeatingWords = _wordIndices.Where(i => i.Value.Count > threshold).Select(i => i.Key).ToArray();
            foreach (var w in repeatingWords)
            {
                _wordIndices.Remove(w);
            }
        }
    }
}