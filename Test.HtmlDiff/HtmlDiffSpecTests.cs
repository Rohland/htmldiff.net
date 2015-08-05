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
        [TestCase("a c", "a b c", "a <ins class='diffins'>b </ins>c")]
        [TestCase("a b c", "a c", "a <del class='diffdel'>b </del>c")]
        [TestCase("a b c", "a d c", "a <del class='diffmod'>b</del><ins class='diffmod'>d</ins> c")]
        [TestCase("<a title='xx'>test</a>", "<a title='yy'>test</a>", "<a title='yy'>test</a>")]
        [TestCase("<img src='logo.jpg'/>", "", "<del class='diffdel'><img src='logo.jpg'/></del>")]
        [TestCase("", "<img src='logo.jpg'/>", "<ins class='diffins'><img src='logo.jpg'/></ins>")]

        // TODO: Don't speak Chinese, this needs to be validated
        [TestCase("这个是中文内容, CSharp is the bast", "这是中国语内容，CSharp is the best language.", "这<del class='diffdel'>个</del>是中<del class='diffmod'>文</del><ins class='diffmod'>国语</ins>内容<del class='diffmod'>, CSharp</del><ins class='diffmod'>，CSharp</ins> is the <del class='diffmod'>bast</del><ins class='diffmod'>best language.</ins>")]
        public void Execute_WithStandardSpecs_OutputVerified(string oldtext, string newText, string delta)
        {
            Assert.AreEqual(delta, global::HtmlDiff.HtmlDiff.Execute(oldtext, newText));
        }

        [Test(Description = "This is a benchmark test, it requires much memory and cpu to run so by default is disabled")]
        public void ExecutePerformance_WithLongText()
        {
            Assert.Inconclusive("Disabled by default");

            const int iterations = 300;
            const string template = @"Lorem ipsum dolor sit amet {0}, consectetur adipiscing elit. Nunc sollicitudin mauris eget nibh {1} semper, in bibendum felis rutrum. Aliquam dictum {2} ut ante id dictum. Integer quis tincidunt metus. Maecenas ultricies tristique {3} fringilla. Cras non erat id elit rhoncus accumsan eget quis neque. Fusce accumsan justo mauris, et pulvinar leo lacinia molestie. Nam ullamcorper dapibus velit a pulvinar. Cras a hendrerit neque {4}, sit amet faucibus ante. {5} Nullam in nisl augue. Suspendisse consectetur id ipsum at dignissim. Etiam euismod sollicitudin metus non volutpat,{6}. Nullam non mollis risus, nec consequat ipsum.";
            var words = "Donec condimentum, tellus a aliquam feugiat, dui diam fringilla massa, sed facilisis risus magna quis augue. Aenean tempus metus at quam aliquet, ultrices venenatis nulla faucibus. Maecenas sit amet lobortis tortor. Vestibulum fringilla fringilla diam, non tempus quam pretium gravida. In pretium vitae erat sed bibendum. Sed ultrices risus et aliquet sollicitudin. Fusce ac diam justo. Morbi lobortis quam vestibulum volutpat cursus. Suspendisse vestibulum augue et interdum convallis.".
                        Split(' ').Cast<object>().ToArray();
            var empty = Enumerable.Repeat((object)"", words.Length).ToArray();

            var oldText = new StringBuilder();
            for (int i = 0; i < iterations; i++)
            {
                if (i % 2 == 0)
                    oldText.AppendFormat(template, empty);
                else if (i % 5 == 0)
                    oldText.AppendFormat(template, words.Skip(i % words.Length).Concat(words.Skip(words.Length - (i % words.Length))).ToArray());
                else if (i % 7 == 0)
                    oldText.AppendFormat(template, words);
            }

            words = words.Reverse().ToArray();
            var newText = new StringBuilder();
            for (int i = 0; i < iterations; i++)
            {
                if (i % 3 == 0)
                    newText.AppendFormat(template, empty);
                else if (i % 2 == 0)
                    newText.AppendFormat(template, words.Skip(i % words.Length).Concat(words.Skip(words.Length - (i % words.Length))).ToArray());
                else if (i % 11 == 0)
                    newText.AppendFormat(template, words);
            }

            var s = Stopwatch.StartNew();
            var delta = global::HtmlDiff.HtmlDiff.Execute(oldText.ToString(), newText.ToString());
            s.Stop();
            Assert.IsNotNullOrEmpty(delta);
            Debug.WriteLine("Execution time: {0}, oldText size: {1}, newText size: {2}, delta size: {3}", s.Elapsed, oldText.Length, newText.Length, delta.Length);
        }
    }
}
