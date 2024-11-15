using System;
using System.Text.RegularExpressions;

namespace Demo
{
    public partial class _Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var oldText = @"<p><i>This is</i> some sample text to <strong>demonstrate</strong> the capability of the <strong>HTML diff tool</strong>.</p>
                                <p>It is based on the <b>Ruby</b> implementation found <a href='http://github.com/myobie/htmldiff'>here</a>. Note how the link has no tooltip</p>
                                <p>What about a number change: 123456?</p>
                                <table cellpadding='0' cellspacing='0'>
                                <tr><td>Some sample text</td><td>Some sample value</td></tr>
                                <tr><td>Data 1 (this row will be removed)</td><td>Data 2</td></tr>
                                </table>
                                Here is a number 2 32
                                <br><br>
                                This date: 1 Jan 2016 is about to change (note how it is treated as a block change!)
                                <div>
                                    Here is some text with a few words ok? And this text is in a <strong>strong block</strong> right?
                                </div>
                    ";

            var newText = @"<p>This is some sample <strong>text to</strong> demonstrate the awesome capabilities of the <strong>HTML <u>diff</u> tool</strong>.</p><br/><br/>Extra spacing here that was not here before.
                                <p>It is <i>based</i> on the Ruby implementation found <a title='Cool tooltip' href='http://github.com/myobie/htmldiff'>here</a>. Note how the link has a tooltip now and the HTML diff algorithm has preserved formatting.</p>
                                <p>What about a number change: 123356?</p>
                                <table cellpadding='0' cellspacing='0'>
                                <tr><td>Some sample <strong>bold text</strong></td><td>Some sample value</td></tr>
                                </table>
                                Here is a number 2 <sup>32</sup>
                                <br><br>
                                This date: 22 Feb 2017 is about to change (note how it is treated as a block change!)
                                <div>
                                    Here is some text with a few <strong>strong <big>big words</big></strong> ok? And this text is not in a strong block right?
                                </div>
                                ";

            var diffHelper = new HtmlDiff.HtmlDiff(oldText, newText);
            litOldText.Text = oldText;
            litNewText.Text = newText;

            // Lets add a block expression to group blocks we care about (such as dates)
            diffHelper.AddBlockExpression(new Regex(@"[\d]{1,2}[\s]*(Jan|Feb)[\s]*[\d]{4}", RegexOptions.IgnoreCase));

            litDiffText.Text = diffHelper.Build();
        }
    }
}
