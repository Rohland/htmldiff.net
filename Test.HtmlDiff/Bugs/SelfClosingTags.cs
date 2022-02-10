using NUnit.Framework;

namespace Test.HtmlDiff.Bugs
{
    [TestFixture]
    public class SelfClosingTagTests
    {
        [Test]
        public void WithSelfClosingTag_ReplacedWithData_CellDiffEmitted()
        {
            // arrange
            var oldText = "<table><tr></td><td>sample</td></tr></table>";
            var newText = "<table><tr><td>test</td><td>sample</td></tr></table>";

            // act
            var diff = global::HtmlDiff.HtmlDiff.Execute(
                oldText,
                newText);

            // assert
            var expected = "<table><tr><td><ins class='diffins'>test</ins></td><td>sample</td></tr></table>";
            Assert.AreEqual(
                expected,
                diff);
        }

        [Test]
        public void WithCellWithContent_ReplacedWithSelfClosingEmptyTag_NoNewCellsAreEmitted()
        {
            // arrange
            var oldText = "<table><tr><td>text</td><td>test</td></tr></table>";
            var newText = "<table><tr><td/><td>test</td></tr></table>";

            // act
            var diff = global::HtmlDiff.HtmlDiff.Execute(
                oldText,
                newText);

            // assert
            var expected = "<table><tr><td><del class='diffdel'>text</del></td><td>test</td></tr></table>";
            Assert.AreEqual(
                expected,
                diff
                );
        }
        
        [Test]
        public void WithSpanContent_ReplacedWithSelfClosingEmptyTag_DiffCorrect()
        {
            // arrange
            var oldText = "<span>test</span>";
            var newText = "<span/>";

            // act
            var diff = global::HtmlDiff.HtmlDiff.Execute(
                oldText,
                newText);

            // assert
            var expected = "<span><del class='diffdel'>test</del></span>";
            Assert.AreEqual(
                expected,
                diff
            );
        }
        
        [TestCase("<br/>")]
        [TestCase("<area/>")]
        [TestCase("<base/>")]
        [TestCase("<embed/>")]
        [TestCase("<hr/>")]
        [TestCase("<iframe/>")]
        [TestCase("<img/>")]
        [TestCase("<input/>")]
        [TestCase("<link/>")]
        [TestCase("<meta/>")]
        [TestCase("<param/>")]
        [TestCase("<source/>")]
        [TestCase("<track/>")]
        public void WithStandardClosingTags_DiffMaintainsSelfClosing(string tag)
        {
            // arrange
            var oldText = $"test{tag}";
            var newText = tag;

            // act
            var diff = global::HtmlDiff.HtmlDiff.Execute(
                oldText,
                newText);

            // assert
            var expected = $"<del class='diffdel'>test</del>{tag}";
            Assert.AreEqual(
                expected,
                diff
            );
        }
        
    }
}