using HtmlDiff;
using NUnit.Framework;

namespace Test.HtmlDiff
{
    [TestFixture]
    public class UtilTests
    {
        [TestCase("<div>", true, "div")]
        [TestCase("<div class='test'>", true, "div")]
        [TestCase("<div/>", true, "div")]
        [TestCase("<div />", true, "div")]
        [TestCase("<div    />", true, "div")]
        [TestCase("</>", false, null)]
        public void TryGetTagNameTests(
            string input,
            bool tagFound,
            string name)
        {
            var found = Utils.TryGetTagName(
                input,
                out var nameResult);
            Assert.AreEqual(
                tagFound, 
                found);
            Assert.AreEqual(
                name,
                nameResult);
        }
    }
}