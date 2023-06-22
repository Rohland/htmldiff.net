using NExpect;
using NUnit.Framework;
using static NExpect.Expectations;

namespace HtmlDiff.Tests
{
    [TestFixture]
    public class UtilsTests
    {
        [TestFixture]
        public class GetTagName
        {
            [TestCase("", "")]
            [TestCase(null, "")]
            [TestCase("test", "")]
            [TestCase("<test", "")]
            [TestCase("< div >", "")]
            [TestCase("</ div>", "")]
            [TestCase("<div>", "div")]
            [TestCase(" \t<div> \t", "div")]
            [TestCase("<DIV>", "div")]
            [TestCase("</div>", "div")]
            [TestCase("<div attr='test'>", "div")]
            [TestCase("<div attr=test>", "div")]
            public void WithTestCase_ShouldMapToTagName(string input, string expected)
            {
                // arrange and act
                var result = Utils.GetTagName(input);
                
                // assert
                Expect(result).To.Equal(expected);
            }
        }
    }
}