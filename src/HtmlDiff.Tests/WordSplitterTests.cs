using System.Text.RegularExpressions;
using NUnit.Framework;
using NExpect;
using static NExpect.Expectations;

namespace HtmlDiff.Tests
{
    [TestFixture]
    public class WordSplitterTests
    {
        [Test]
        [TestCase("<td>(.*?)</td>", "<td>a b c</td><td>d e f</td>", new[] { "<td>a b c</td>", "<td>d e f</td>" })]
        [TestCase("<td>(.*?)</td>", "<td>a b</td><br/><td>c d</td>", new[] { "<td>a b</td>", "<br/>", "<td>c d</td>" })]
        public void ConvertHtmlToListOfWords_WithNonGreedyGroups_WordsSplitEndOfGroup(
            string regex,
            string text,
            string[] expected)
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