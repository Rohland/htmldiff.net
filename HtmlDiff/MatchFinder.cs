using System.Collections.Generic;
using System.Text;

namespace HtmlDiff
{
    /// <summary>
    /// Finds the longest match in given texts. It uses indexing with fixed granularity that is used to compare blocks of text.
    /// </summary>
    public class MatchFinder
    {
        private readonly int _blockSize;
        private readonly string[] _oldWords;
        private readonly string[] _newWords;
        private readonly int _startInOld;
        private readonly int _endInOld;
        private readonly int _startInNew;
        private readonly int _endInNew;
        private Dictionary<string, List<int>> _wordIndices;

        /// <summary>
        /// </summary>
        /// <param name="blockSize">Match granularity, defines how many words are joined into single block</param>
        /// <param name="oldWords"></param>
        /// <param name="newWords"></param>
        /// <param name="startInOld"></param>
        /// <param name="endInOld"></param>
        /// <param name="startInNew"></param>
        /// <param name="endInNew"></param>
        public MatchFinder(int blockSize, string[] oldWords, string[] newWords, int startInOld, int endInOld, int startInNew, int endInNew)
        {
            _blockSize = blockSize;
            _oldWords = oldWords;
            _newWords = newWords;
            _startInOld = startInOld;
            _endInOld = endInOld;
            _startInNew = startInNew;
            _endInNew = endInNew;
        }

        private void IndexNewWords()
        {
            _wordIndices = new Dictionary<string, List<int>>();
            var block = new Queue<string>(_blockSize);
            for (int i = _startInNew; i < _endInNew; i++)
            {
                // if word is a tag, we should ignore attributes as attribute changes are not supported (yet)
                var word = Utils.StripAnyAttributes(_newWords[i]);
                var key = PutNewWord(block, word, _blockSize);

                if (key == null)
                    continue;

                List<int> indicies = null;
                if (_wordIndices.TryGetValue(key, out indicies))
                {
                    indicies.Add(i);
                }
                else
                {
                    _wordIndices.Add(key, new List<int> { i });
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

            var result = new StringBuilder(blockSize);
            foreach (var s in block)
            {
                result.Append(s);
            }
            return result.ToString();
        }

        public Match FindMatch()
        {
            IndexNewWords();

            if (_wordIndices.Count == 0)
                return null;

            int bestMatchInOld = _startInOld;
            int bestMatchInNew = _startInNew;
            int bestMatchSize = 0;

            var matchLengthAt = new Dictionary<int, int>();
            var block = new Queue<string>(_blockSize);

            for (int indexInOld = _startInOld; indexInOld < _endInOld; indexInOld++)
            {
                var word = Utils.StripAnyAttributes(_oldWords[indexInOld]);
                var index = PutNewWord(block, word, _blockSize);

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
                        bestMatchInOld = indexInOld - newMatchLength + 1 - _blockSize + 1;
                        bestMatchInNew = indexInNew - newMatchLength + 1 - _blockSize + 1;
                        bestMatchSize = newMatchLength;
                    }
                }

                matchLengthAt = newMatchLengthAt;
            }

            return bestMatchSize != 0 ? new Match(bestMatchInOld, bestMatchInNew, bestMatchSize + _blockSize - 1) : null;
        }
    }
}