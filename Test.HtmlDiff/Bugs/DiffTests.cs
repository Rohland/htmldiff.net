using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Test.HtmlDiff.Bugs
{
    [TestFixture]
    public class DiffTests
    {
        // https://github.com/Rohland/htmldiff.net/issues/3
        [Test]
        public void Execute_NonAlphaNumericAdjoinedToWordDiff_PrefixIsNotIncludedInDiff()
        {
            var oldText = "The Dealer.";
            var newText = "The Dealer info,";
            var output = global::HtmlDiff.HtmlDiff.Execute(oldText, newText);
            Assert.AreEqual("The Dealer<del class='diffmod'>.</del><ins class='diffmod'>&nbsp;info,</ins>", output);
        }
    }
}
