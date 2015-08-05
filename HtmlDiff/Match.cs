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
    }
}