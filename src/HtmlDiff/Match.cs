using System.Diagnostics;
using System.Linq;

namespace HtmlDiff
{
    public class Match
    {
        public Match(
            int startInOld,
            int startInNew,
            int size)
        {
            StartInOld = startInOld;
            StartInNew = startInNew;
            Size = size;
        }

        public int StartInOld { get; }
        public int StartInNew { get; }
        public int Size { get; }
        public int EndInOld => StartInOld + Size;
        public int EndInNew => StartInNew + Size;

#if DEBUG
        public void PrintWordsFromOld(string [] oldWords)
        {
            var text = string.Join("", oldWords.Where((s, pos) => pos >= StartInOld && pos < EndInOld).ToArray());
            Debug.WriteLine("OLD: " + text);
        }

        public void PrintWordsFromNew(string [] newWords)
        {
            var text = string.Join("", newWords.Where((s, pos) => pos >= StartInNew && pos < EndInNew).ToArray());
            Debug.WriteLine("NEW: " + text);
        }
#endif
    }
}