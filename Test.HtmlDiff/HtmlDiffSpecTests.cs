using System.Diagnostics;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Test.HtmlDiff
{
    [TestFixture]
    public class HtmlDiffSpecTests
    {
        // Shamelessly copied specs from here with a few modifications: https://github.com/myobie/htmldiff/blob/master/spec/htmldiff_spec.rb
        [TestCase("a word is here", "a nother word is there", "a<ins class='diffins'> nother</ins> word is <del class='diffmod'>here</del><ins class='diffmod'>there</ins>")]
        [TestCase("one a word is somewhere", "two a nother word is somewhere", "<del class='diffmod'>one a</del><ins class='diffmod'>two a nother</ins> word is somewhere")]
        [TestCase("a c", "a b c", "a <ins class='diffins'>b </ins>c")]
        [TestCase("a b c", "a c", "a <del class='diffdel'>b </del>c")]
        [TestCase("a b c", "a <strong>b</strong> c", "a <strong><ins class='mod'>b</ins></strong> c")]
        [TestCase("a b c", "a d c", "a <del class='diffmod'>b</del><ins class='diffmod'>d</ins> c")]
        [TestCase("<a title='xx'>test</a>", "<a title='yy'>test</a>", "<a title='yy'>test</a>")]
        [TestCase("<img src='logo.jpg'/>", "", "<del class='diffdel'><img src='logo.jpg'/></del>")]
        [TestCase("", "<img src='logo.jpg'/>", "<ins class='diffins'><img src='logo.jpg'/></ins>")]
        [TestCase("symbols 'should not' belong <b>to</b> words", "symbols should not belong <b>\"to\"</b> words", "symbols <del class='diffdel'>'</del>should not<del class='diffdel'>'</del> belong <b><ins class='diffins'>\"</ins>to<ins class='diffins'>\"</ins></b> words")]
        [TestCase("entities are separate amp;words", "entities are&nbsp;separate &amp;words", "entities are<del class='diffmod'> </del><ins class='diffmod'>&nbsp;</ins>separate <del class='diffmod'>amp;</del><ins class='diffmod'>&amp;</ins>words")]

        [TestCase(
            "This is a longer piece of text to ensure the new blocksize algorithm works", 
            "This is a longer piece of text to <strong>ensure</strong> the new blocksize algorithm works decently",
            "This is a longer piece of text to <strong><ins class='mod'>ensure</ins></strong> the new blocksize algorithm works<ins class='diffins'>&nbsp;decently</ins>")]

        [TestCase(
            "By virtue of an agreement between xxx and the <b>yyy schools</b>, ...",
            "By virtue of an agreement between xxx and the <b>yyy</b> schools, ...",
            "By virtue of an agreement between xxx and the <b>yyy</b> schools, ...")]

        // TODO: Don't speak Chinese, this needs to be validated
        [TestCase("这个是中文内容, CSharp is the bast", "这是中国语内容，CSharp is the best language.", "这<del class='diffdel'>个</del>是中<del class='diffmod'>文</del><ins class='diffmod'>国语</ins>内容<del class='diffmod'>, </del><ins class='diffmod'>，</ins>CSharp is the <del class='diffmod'>bast</del><ins class='diffmod'>best language.</ins>")]
        public void Execute_WithStandardSpecs_OutputVerified(string oldtext, string newText, string delta)
        {
            Debug.WriteLine("Old text: " + oldtext);
            Debug.WriteLine("New text: " + newText);
            Debug.WriteLine("");
            Debug.WriteLine("Expected Diff: " + delta);
            var result = global::HtmlDiff.HtmlDiff.Execute(oldtext, newText);
            Debug.WriteLine("");
            Debug.WriteLine("Actual Diff: " + result);
            Assert.AreEqual(delta, result);
        }
    }
}
