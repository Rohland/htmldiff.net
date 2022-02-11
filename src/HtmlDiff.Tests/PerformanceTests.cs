using System.Diagnostics;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NExpect;
using static NExpect.Expectations;

namespace HtmlDiff.Tests
{
    [TestFixture]
    public class PerformanceTests
    {
        [Test]
        [Explicit("benchmark test, disabled by default due to memory and cpu usage")]
        public void ExecutePerformance_WithLongText()
        {
            const int iterations = 300;
            const string template = @"Lorem ipsum dolor sit amet {0}, consectetur adipiscing elit. Nunc sollicitudin mauris eget nibh {1} semper, in bibendum felis rutrum. Aliquam dictum {2} ut ante id dictum. Integer quis tincidunt metus. Maecenas ultricies tristique {3} fringilla. Cras non erat id elit rhoncus accumsan eget quis neque. Fusce accumsan justo mauris, et pulvinar leo lacinia molestie. Nam ullamcorper dapibus velit a pulvinar. Cras a hendrerit neque {4}, sit amet faucibus ante. {5} Nullam in nisl augue. Suspendisse consectetur id ipsum at dignissim. Etiam euismod sollicitudin metus non volutpat,{6}. Nullam non mollis risus, nec consequat ipsum.";
            var words = "Donec condimentum, tellus a aliquam feugiat, dui diam fringilla massa, sed facilisis risus magna quis augue. Aenean tempus metus at quam aliquet, ultrices venenatis nulla faucibus. Maecenas sit amet lobortis tortor. Vestibulum fringilla fringilla diam, non tempus quam pretium gravida. In pretium vitae erat sed bibendum. Sed ultrices risus et aliquet sollicitudin. Fusce ac diam justo. Morbi lobortis quam vestibulum volutpat cursus. Suspendisse vestibulum augue et interdum convallis.".
                        Split(' ').Cast<object>().ToArray();
            var empty = Enumerable.Repeat((object)"", words.Length).ToArray();

            var oldText = new StringBuilder();
            for (var i = 0; i < iterations; i++)
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
            for (var i = 0; i < iterations; i++)
            {
                if (i % 3 == 0)
                    newText.AppendFormat(template, empty);
                else if (i % 2 == 0)
                    newText.AppendFormat(template, words.Skip(i % words.Length).Concat(words.Skip(words.Length - (i % words.Length))).ToArray());
                else if (i % 11 == 0)
                    newText.AppendFormat(template, words);
            }

            var s = Stopwatch.StartNew();
            var delta = HtmlDiff.Execute(oldText.ToString(), newText.ToString());
            s.Stop();
            Expect(delta)
                .Not.To.Be.Null.Or.Empty();
            Debug.WriteLine("Execution time: {0}, oldText size: {1}, newText size: {2}, delta size: {3}", s.Elapsed, oldText.Length, newText.Length, delta.Length);
        }
    }
}
