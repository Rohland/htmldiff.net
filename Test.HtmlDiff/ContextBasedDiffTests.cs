using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Test.HtmlDiff.Properties;

namespace Test.HtmlDiff
{
    [TestClass]
    public class ContextBasedDiffTests
    {
        private string GenerateTestData(bool withMods, int items=30)
        {
            StringBuilder sb = new StringBuilder();
            for (int nItem = 0; nItem < items; nItem++)
            {
                if (nItem % 10 == 0 && withMods)
                    sb.AppendLine(String.Format("<div>Changed Line {0}</div>", nItem));
                else
                    sb.AppendLine(String.Format("<div>Line {0}</div>", nItem));
            }
            return sb.ToString();
        }

        [TestMethod]
        public void NoContextDiff()
        {
            string original = GenerateTestData(false);
            var diff = new global::HtmlDiff.HtmlDiff(original, GenerateTestData(true));
            string result = diff.Build();
            System.Diagnostics.Trace.WriteLine("----------------------");
            System.Diagnostics.Trace.WriteLine(result);
            Assert.AreEqual(original.Split('\n').Count(), result.Split('\n').Count(), "Should be 31 lines in the diff");
        }

        [TestMethod]
        public void TwoLineContextWithNoDiff()
        {
            string original = GenerateTestData(false);
            var diff = new global::HtmlDiff.HtmlDiff(original, original);
            diff.LinesContext = 2;
            string result = diff.Build();
            System.Diagnostics.Trace.WriteLine("----------------------");
            System.Diagnostics.Trace.WriteLine(result);
            Assert.IsNull(result, "No document should be returned");
        }

        [TestMethod]
        public void NoContextWithNoDiff()
        {
            string original = GenerateTestData(false);
            var diff = new global::HtmlDiff.HtmlDiff(original, original);
            string result = diff.Build();
            System.Diagnostics.Trace.WriteLine("----------------------");
            System.Diagnostics.Trace.WriteLine(result);
            Assert.AreEqual(original, result, "Documents should be the same");
        }

        [TestMethod]
        public void OneLineContextDiff()
        {
            var diff = new global::HtmlDiff.HtmlDiff(GenerateTestData(false), GenerateTestData(true));
            diff.LinesContext = 1;
            string result = diff.Build();
            System.Diagnostics.Trace.WriteLine("----------------------");
            System.Diagnostics.Trace.WriteLine(result);
            Assert.AreEqual(13, result.Split('\n').Count(), "Should be 13 lines in the diff"); 
                // There were some extra spaces put into the diff itself
        }

        [TestMethod]
        public void ThreeLineContextDiff()
        {
            var diff = new global::HtmlDiff.HtmlDiff(GenerateTestData(false), GenerateTestData(true));
            diff.LinesContext = 3;
            string result = diff.Build();
            System.Diagnostics.Trace.WriteLine("----------------------");
            System.Diagnostics.Trace.WriteLine(result);
            Assert.AreEqual(23, result.Split('\n').Count(), "Should be 23 lines in the diff");
        }

        [TestMethod]
        public void OneSideEmptyContextDiff()
        {
            var diff = new global::HtmlDiff.HtmlDiff(GenerateTestData(false), String.Empty);
            diff.LinesContext = 3;
            string result = diff.Build();
            System.Diagnostics.Trace.WriteLine("----------------------");
            System.Diagnostics.Trace.WriteLine(result);
            Assert.AreEqual(34, result.Split('\n').Count(), "Should be 34 lines in the diff");
        }

        [TestMethod]
        public void OneSideNullContextDiff()
        {
            var diff = new global::HtmlDiff.HtmlDiff(GenerateTestData(false), null);
            diff.LinesContext = 3;
            string result = diff.Build();
            System.Diagnostics.Trace.WriteLine("----------------------");
            System.Diagnostics.Trace.WriteLine(result);
            Assert.AreEqual(34, result.Split('\n').Count(), "Should be 34 lines in the diff");
        }

        [TestMethod]
        public void ThreeLineContextDiffEndChange()
        {
            var diff = new global::HtmlDiff.HtmlDiff(GenerateTestData(false,31), GenerateTestData(true,31));
            diff.LinesContext = 3;
            string result = diff.Build();
            System.Diagnostics.Trace.WriteLine("----------------------");
            System.Diagnostics.Trace.WriteLine(result);
            Assert.AreEqual(29, result.Split('\n').Count(), "Should be 23 lines in the diff");
        }

        [TestMethod]
        public void LargeThreeLineContextChange()
        {
            var diff = new global::HtmlDiff.HtmlDiff(Resources.String1, Resources.String2);
            diff.LinesContext = 3;
            string result = diff.Build();
            System.Diagnostics.Trace.WriteLine("----------------------");
            System.Diagnostics.Trace.WriteLine(result);
            Assert.AreEqual(74, result.Split('\n').Count(), "Should be 23 lines in the diff");
        }
    }
}

