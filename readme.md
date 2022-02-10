## Project Description

A library for comparing two HTML files/snippets and highlighting the differences using simple HTML.

This HTML Diff implementation is a C# port of the ruby implementation found [here](https://github.com/myobie/htmldiff).

## Usage

Grab the latest stable version from Nuget: https://www.nuget.org/packages/htmldiff.net/

Example input:

```csharp
string oldText = @"<p>This is some sample text to demonstrate the capability of the <strong>HTML diff tool</strong>.</p>
                    <p>It is based on the Ruby implementation found <a href='http://github.com/myobie/htmldiff'>here</a>. Note how the link has no tooltip</p>
                    <table cellpadding='0' cellspacing='0'>
                    <tr><td>Some sample text</td><td>Some sample value</td></tr>
                    <tr><td>Data 1 (this row will be removed)</td><td>Data 2</td></tr>
                    </table>";

string newText = @"<p>This is some sample text to demonstrate the awesome capabilities of the <strong>HTML diff tool</strong>.</p><br/><br/>Extra spacing here that was not here before.
                    <p>It is based on the Ruby implementation found <a title='Cool tooltip' href='http://github.com/myobie/htmldiff'>here</a>. Note how the link has a tooltip now and the HTML diff algorithm has preserved formatting.</p>
                    <table cellpadding='0' cellspacing='0'>
                    <tr><td>Some sample <strong>bold text</strong></td><td>Some sample value</td></tr>
                    </table>";

HtmlDiff diffHelper = new HtmlDiff(oldText, newText);
string diffOutput = diffHelper.Build();
```

The example above is [included here](https://github.com/Rohland/htmldiff.net/tree/master/Demo)

This is what the old and new HTML look like:

![image](https://user-images.githubusercontent.com/231471/153353160-e140159e-06f7-4888-8af2-44a8bfa2f184.png)

![image](https://user-images.githubusercontent.com/231471/153353225-6ece8d00-3dec-474e-ad2b-ae9a8cb1af68.png)

![image](https://user-images.githubusercontent.com/231471/153353271-9ce2db37-0e49-4246-afe7-8b8c83935739.png)

#### Styling

You can see that the algorithm as originally developed takes care of the nasty HTML parsing to figure out how to highlight the differences. The changes are marked up using “ins” and “del” tags. You can easily style these tags as I have done. The CSS below is responsible for rendering the differences as per the example.

```css
ins {
	background-color: #cfc;
	text-decoration: none;
}

del {
	color: #999;
	background-color:#FEC8C8;
}
```

#### Blocks

The tokenizer works by running the diff on words, but sometimes this isn't ideal. For example, it may look clunky when a date is edited from `12 Jan 2022` to `14 Feb 2022`. It might be neater to treat the diff on the entire date rather than the independent tokens.

You can achieve this using `AddBlockExpression`. Note, the Regex example is not meant to be exhaustive to cover all dates. If text matches the expression, the entire phrase is included as a single token to be compared, and that results in a much neater output.

```csharp
diffHelper.AddBlockExpression(
  new Regex(@"[\d]{1,2}[\s]*(Jan|Feb)[\s]*[\d]{4}",
  RegexOptions.IgnoreCase));
var diff = diffHelper.Build();
```
