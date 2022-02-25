using System.Diagnostics;
using System.Linq;

namespace HtmlDiff
{
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


#if DEBUG

        public void PrintDebugInfo(string[] oldWords, string []newWords)
        {
            var oldText = string.Join("", oldWords.Where((s, pos) => pos >= this.StartInOld && pos < this.EndInOld).ToArray());
            var newText = string.Join("", newWords.Where((s, pos) => pos >= this.StartInNew && pos < this.EndInNew).ToArray());
            Debug.WriteLine(string.Format(@"Operation: {0}, Old Text: '{1}', New Text: '{2}'", Action.ToString(), oldText, newText));
        }

#endif
    }
}