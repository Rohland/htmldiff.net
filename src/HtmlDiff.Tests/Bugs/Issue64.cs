using NExpect;
using NUnit.Framework;
using static NExpect.Expectations;

namespace HtmlDiff.Tests.Bugs
{
    [TestFixture]
    public class Issue64
    {
        // https://github.com/Rohland/htmldiff.net/issues/64
        // Testcase from Issue 64
        [TestCase("<div><span>Lorem ipsum dolor sit amet</span><span></span><span>Nulla sed leo dapibus</span></div>",
             "<div><span>Lorem ipsum sit amet</span><span>Sed congue posuere est</span></div>",
             "<div><span>Lorem ipsum <del class='diffdel'>dolor </del>sit amet</span><span><del class='diffmod'>Nulla sed leo dapibus</del><ins class='diffmod'>Sed congue posuere est</ins></span></div>")]
        public void TestCase_IssueX_duplicate_opening_tag(string oldText, string newText, string expectedResult)
        {
            var diff = new HtmlDiff(oldText, newText);
            diff.OrphanMatchThreshold = 0.2;
            var result = diff.Build();
            Expect(result).To.Equal(expectedResult);
        }
    }
}
