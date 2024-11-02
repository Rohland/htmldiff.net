using NExpect;
using NUnit.Framework;
using static NExpect.Expectations;

namespace HtmlDiff.Tests.Bugs
{
    [TestFixture]
    public class Issue20
    {
        // https://github.com/Rohland/htmldiff.net/issues/20

        // Original Testcase from Issue 20, but with the correct expected result (the original expected result was missing the <ins class='mod'>[...]</ins>)
        [TestCase("<div class=\"dumb\">Thiis a text without any sup-tags and other special things</div>",
             "<div class=\"dumb\">Thiis a text <sup>1</sup>without any sup-tags and other special things</div>",
             "<div class=\"dumb\">Thiis a text <sup><ins class='mod'><ins class='diffins'>1</ins></ins></sup>without any sup-tags and other special things</div>")]

        // Inserting a new word at the end of the reformatted text
        [TestCase("<span>text remains</span>",
              "<span><strong>text remains Test</strong></span>",
              "<span><strong><ins class='mod'>text remains<ins class='diffins'>&nbsp;Test</ins></ins></strong></span>")]

        // Inserting a new word at the end of a reformatted text and another word at the end outside the reformatted text
        [TestCase("<span>text remains</span>",
             "<span><strong>text remains Test</strong> Test</span>",
             "<span><strong><ins class='mod'>text remains<ins class='diffins'>&nbsp;Test</ins></ins></strong><ins class='diffins'>&nbsp;Test</ins></span>")]

        // Twice reformatted text with an offset at the end
        [TestCase("<span>text remains</span>",
             "<span><strong><big>text </big>remains</strong></span>",
             "<span><strong><big><ins class='mod'>text </big>remains</ins></strong></span>")]

        // Inserting a new word at the beginning of a reformatted text.
        [TestCase("<span>text remains</span>",
             "<span><strong>Test text remains</strong></span>",
             "<span><strong><ins class='mod'><ins class='diffins'>Test </ins>text remains</ins></strong></span>")]
        public void TestCase_Issue20_missing_closing_tag(string oldText, string newText, string expectedResult)
        {
            // Arrange
            var diff = new HtmlDiff(oldText, newText);

            string result = diff.Build();

            // Assert
            Expect(result)
                 .To.Equal(expectedResult);
        }
    }
}
