using NExpect;
using NUnit.Framework;
using static NExpect.Expectations;

namespace HtmlDiff.Tests.Bugs
{
    [TestFixture]
    public class Issue3
    {
        // https://github.com/Rohland/htmldiff.net/issues/3
        [Test]
        public void Execute_NonAlphaNumericAdjoinedToWordDiff_PrefixIsNotIncludedInDiff()
        {
            // Arrange
            var oldText = "The Dealer.";
            var newText = "The Dealer info,";
            
            // Act
            var output = HtmlDiff.Execute(oldText, newText);
            
            // Assert
            Expect(output)
                .To.Equal("The Dealer<del class='diffmod'>.</del><ins class='diffmod'>&nbsp;info,</ins>");
        }
    }
}
