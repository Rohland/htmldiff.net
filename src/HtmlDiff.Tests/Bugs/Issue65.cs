using NExpect;
using NUnit.Framework;
using static NExpect.Expectations;

namespace HtmlDiff.Tests.Bugs
{
    [TestFixture]
    public class Issue65
    {
        // https://github.com/Rohland/htmldiff.net/issues/65
        //Test case from issue 65
        [TestCase("<div><span>Word1 in sentence1</span></div><img imageId=\"id1\" style=\"width: 20px; height: 20px\" src=\"toto\"/>",
             "<div><span>Word2 in sentence2</span></div><img imageId=\"id2\" style=\"width: 20px; height: 20px\" src=\"tata\"/>",
             "<div><span><del class='diffmod'>Word1</del><ins class='diffmod'>Word2</ins> in <del class='diffmod'>sentence1</del><ins class='diffmod'>sentence2</ins></span></div><del class='diffmod'><img imageId=\"id1\" style=\"width: 20px; height: 20px\" src=\"toto\"/></del><ins class='diffmod'><img imageId=\"id2\" style=\"width: 20px; height: 20px\" src=\"tata\"/></ins>")]
        public void TestCase_Issue65_inconsistent_del_and_ins_order_with_img(string oldText, string newText, string expectedResult)
        {
            var diff = new HtmlDiff(oldText, newText);
            diff.OrphanMatchThreshold = 0.2;
            var result = diff.Build();
            Expect(result).To.Equal(expectedResult);
        }
    }
}
