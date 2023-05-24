using System.Text.RegularExpressions;
using NUnit.Framework;
using NExpect;
using static NExpect.Expectations;

namespace HtmlDiff.Tests
{
    [TestFixture]
    public class WordSplitterSpecTests
    {
        [Test]
        [TestCase("<td>(.*?)</td>", "<td>n a</td><td>c</td>", new[] { "<td>n a</td>", "<td>c</td>" })]
        [TestCase("<td>(.*?)</td>", "<td>n a</td><br/><td>c</td>", new[] { "<td>n a</td>", "<br/>", "<td>c</td>" })]
        public void ConvertHtmlToListOfWords_WithGroups_WordsSplitEndOfGroup(string regex, string text, string[] expected)
        {
            // Arrange

            // Act
            var words = WordSplitter.ConvertHtmlToListOfWords(
                text,
                new[] { new Regex(regex, RegexOptions.IgnoreCase) });

            // Assert
            Expect(words)
                .To.Equal(expected);
        }
    }
}