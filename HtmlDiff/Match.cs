using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace HtmlDiff
{
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


#if DEBUG

        public void PrintWordsFromOld(string [] oldWords)
        {
            var text = string.Join("", oldWords.Where((s, pos) => pos >= this.StartInOld && pos < this.EndInOld).ToArray());
            Debug.WriteLine("OLD: " + text);
        }

        public void PrintWordsFromNew(string [] newWords)
        {
            var text = string.Join("", newWords.Where((s, pos) => pos >= this.StartInNew && pos < this.EndInNew).ToArray());
            Debug.WriteLine("NEW: " + text);
        }

#endif
    }
}