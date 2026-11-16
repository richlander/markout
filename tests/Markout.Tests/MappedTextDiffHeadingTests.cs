using System.Text;
using Markout;
using Markout.Formatting;

namespace Markout.Tests;

public class MappedTextDiffHeadingTests
{
    // Before:                        After:
    // 0 public class Widget          0 public class Widget
    // 1 {                            1 {
    // 2     public void Run()        2     public void Run()
    // 3     {                        3     {
    // 4         Old();               4         New();
    // 5     }                        5     }
    // 6     public void Stop()       6     public void Stop()
    // 7     {                        7     {
    // 8         a();                 8         b();
    // 9     }                        9     }
    // 10 }                           10 }
    static readonly string[] BeforeLines =
    [
        "public class Widget", "{", "    public void Run()", "    {", "        Old();", "    }",
        "    public void Stop()", "    {", "        a();", "    }", "}",
    ];

    static readonly string[] AfterLines =
    [
        "public class Widget", "{", "    public void Run()", "    {", "        New();", "    }",
        "    public void Stop()", "    {", "        b();", "    }", "}",
    ];

    static readonly TextDiffChange[] TwoChanges =
    [
        new(new TextDiffRange(4, 1), new TextDiffRange(4, 1)),
        new(new TextDiffRange(8, 1), new TextDiffRange(8, 1)),
    ];

    static TextDiffHeadings Declarations(string? fallback = "namespace Example") => new(
        [
            new TextDiffHeading(0, "public class Widget"),
            new TextDiffHeading(2, "public void Run()"),
            new TextDiffHeading(6, "public void Stop()"),
        ],
        fallback);

    [Fact]
    public void EachHunkTakesTheLastCandidateBeforeItsFirstBeforeLine()
    {
        var diff = new MappedTextDiff(
            new TextDiffSequence(BeforeLines),
            new TextDiffSequence(AfterLines),
            TwoChanges,
            Declarations());

        string[] headers = Headers(RenderMarkdown(diff, contextLines: 1));

        Assert.Equal(["@@ -4,3 +4,3 @@ public void Run()", "@@ -8,3 +8,3 @@ public void Stop()"], headers);
    }

    [Fact]
    public void ACandidateOnTheHunksFirstLineDoesNotHeadIt()
    {
        // Git searches from the line before the hunk, so a hunk that starts on a declaration line
        // takes the previous declaration.
        var diff = new MappedTextDiff(
            new TextDiffSequence(BeforeLines),
            new TextDiffSequence(AfterLines),
            [new TextDiffChange(new TextDiffRange(4, 1), new TextDiffRange(4, 1))],
            Declarations());

        string[] headers = Headers(RenderMarkdown(diff, contextLines: 2));

        Assert.Equal(["@@ -3,5 +3,5 @@ public class Widget"], headers);
    }

    [Fact]
    public void AHunkNoCandidatePrecedesTakesTheFallback()
    {
        var diff = new MappedTextDiff(
            new TextDiffSequence(BeforeLines),
            new TextDiffSequence(AfterLines),
            TwoChanges,
            Declarations());

        string[] headers = Headers(RenderMarkdown(diff, contextLines: null));

        Assert.Equal(["@@ -1,11 +1,11 @@ namespace Example"], headers);
    }

    [Fact]
    public void AHunkWithoutCandidateOrFallbackHasNoHeading()
    {
        var diff = new MappedTextDiff(
            new TextDiffSequence([]),
            new TextDiffSequence(["public double Read();"]),
            [new TextDiffChange(new TextDiffRange(0, 0), new TextDiffRange(0, 1))],
            new TextDiffHeadings([]));

        Assert.Equal(["@@ -0,0 +1 @@"], Headers(RenderMarkdown(diff, contextLines: 3)));
    }

    [Fact]
    public void ALabelFollowsTheHeadingAfterAPipe()
    {
        var diff = new MappedTextDiff(
            new TextDiffSequence(BeforeLines),
            new TextDiffSequence(AfterLines),
            [
                new TextDiffChange(
                    new TextDiffRange(4, 1),
                    new TextDiffRange(4, 1),
                    label: new TextDiffChangeLabel("whitespace-only: indentation")),
            ],
            Declarations());

        Assert.Equal(
            ["@@ -4,3 +4,3 @@ public void Run() | whitespace-only: indentation"],
            Headers(RenderMarkdown(diff, contextLines: 1)));
    }

    [Fact]
    public void ALabelWithoutAHeadingFollowsAPipe()
    {
        var diff = new MappedTextDiff(
            new TextDiffSequence(["a", "b"]),
            new TextDiffSequence(["a", "c"]),
            [
                new TextDiffChange(
                    new TextDiffRange(1, 1),
                    new TextDiffRange(1, 1),
                    label: new TextDiffChangeLabel("moved (1) from -12; whitespace-only: indentation")),
            ],
            new TextDiffHeadings([]));

        Assert.Equal(
            ["@@ -1,2 +1,2 @@ | moved (1) from -12; whitespace-only: indentation"],
            Headers(RenderMarkdown(diff, contextLines: 3)));
    }

    [Theory]
    [InlineData("public void Open(FileAccess access = FileAccess.Read | FileAccess.Write)", "whitespace-only: indentation")]
    [InlineData("public string Format(string pattern = \"a \\\\| b\\\\\")", "moved | twice")]
    [InlineData("public int this[int index]", null)]
    [InlineData("public string Format(string pattern = \"a // b\")", "label")]
    [InlineData("public void Tab(string s = \"a\tb\\\\t\")", "x")]
    public void HeadingAndLabelAreRecoverableExactly(string heading, string? label)
    {
        var diff = new MappedTextDiff(
            new TextDiffSequence(["first", "second"]),
            new TextDiffSequence(["first", "changed"]),
            [
                new TextDiffChange(
                    new TextDiffRange(1, 1),
                    new TextDiffRange(1, 1),
                    label: label is null ? null : new TextDiffChangeLabel(label)),
            ],
            new TextDiffHeadings([new TextDiffHeading(0, heading)]));

        // Zero context starts the hunk on the changed line, so the candidate on line 0 precedes it.
        string header = Assert.Single(Headers(RenderMarkdown(diff, contextLines: 0)));
        (string? parsedHeading, string? parsedLabel) = ParseHeader(header);

        Assert.Equal(heading, parsedHeading);
        Assert.Equal(label, parsedLabel);
    }

    [Fact]
    public void WithoutHeadingsOutputIsUnchanged()
    {
        TextDiffChange[] changes =
        [
            new(new TextDiffRange(4, 1), new TextDiffRange(4, 1), label: new TextDiffChangeLabel("moved | (1) to +40")),
            new(new TextDiffRange(8, 1), new TextDiffRange(8, 1)),
        ];
        var legacy = new MappedTextDiff(new TextDiffSequence(BeforeLines), new TextDiffSequence(AfterLines), changes);
        var explicitNull = new MappedTextDiff(
            new TextDiffSequence(BeforeLines),
            new TextDiffSequence(AfterLines),
            changes,
            headings: null);

        string output = RenderMarkdown(legacy, contextLines: 1);

        Assert.Equal(output, RenderMarkdown(explicitNull, contextLines: 1));
        Assert.Equal(["@@ -4,3 +4,3 @@ moved | (1) to +40", "@@ -8,3 +8,3 @@"], Headers(output));
    }

    [Fact]
    public void HeadingsChangeOnlyHeaderText()
    {
        var plain = new MappedTextDiff(new TextDiffSequence(BeforeLines), new TextDiffSequence(AfterLines), TwoChanges);
        var headed = new MappedTextDiff(
            new TextDiffSequence(BeforeLines),
            new TextDiffSequence(AfterLines),
            TwoChanges,
            Declarations());

        string[] plainLines = RenderPlainText(plain, contextLines: 1).Split('\n');
        string[] headedLines = RenderPlainText(headed, contextLines: 1).Split('\n');

        Assert.Equal(plainLines.Length, headedLines.Length);
        for (var i = 0; i < plainLines.Length; i++)
        {
            if (plainLines[i].StartsWith("@@ ", StringComparison.Ordinal))
                Assert.StartsWith(plainLines[i] + " ", headedLines[i]);
            else
                Assert.Equal(plainLines[i], headedLines[i]);
        }
    }

    [Fact]
    public void HeadingsDoNotSplitHunks()
    {
        var diff = new MappedTextDiff(
            new TextDiffSequence(BeforeLines),
            new TextDiffSequence(AfterLines),
            TwoChanges,
            Declarations());

        Assert.Equal(["@@ -3,9 +3,9 @@ public class Widget"], Headers(RenderMarkdown(diff, contextLines: 2)));
    }

    [Fact]
    public void CandidatesMustStrictlyIncrease()
        => Assert.Throws<ArgumentException>(() => new TextDiffHeadings(
            [new TextDiffHeading(2, "b"), new TextDiffHeading(2, "c")]));

    [Fact]
    public void CandidatesMustNameBeforeLines()
        => Assert.Throws<ArgumentException>(() => new MappedTextDiff(
            new TextDiffSequence(["a"]),
            new TextDiffSequence(["b"]),
            [new TextDiffChange(new TextDiffRange(0, 1), new TextDiffRange(0, 1))],
            new TextDiffHeadings([new TextDiffHeading(1, "beyond")])));

    [Theory]
    [InlineData("")]
    [InlineData("a\nb")]
    [InlineData("a\rb")]
    public void HeadingTextIsOneNonEmptyLogicalLine(string text)
    {
        Assert.Throws<ArgumentException>(() => new TextDiffHeading(0, text));
        Assert.Throws<ArgumentException>(() => new TextDiffHeadings([], text));
    }

    [Fact]
    public void HeadingLineMustNotBeNegative()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new TextDiffHeading(-1, "a"));

    /// <summary>
    /// Reads a header written with headings: the first unescaped pipe begins the label, and
    /// <c>\\</c>, <c>\|</c>, and <c>\t</c> unescape in the heading.
    /// </summary>
    static (string? Heading, string? Label) ParseHeader(string header)
    {
        int close = header.IndexOf(" @@", 3, StringComparison.Ordinal);
        string rest = header[(close + 3)..];
        if (rest.Length == 0)
            return (null, null);
        if (rest.StartsWith(" | ", StringComparison.Ordinal))
            return (null, rest[3..]);

        var heading = new StringBuilder();
        for (var i = 1; i < rest.Length; i++)
        {
            char c = rest[i];
            if (c == '\\' && i + 1 < rest.Length)
            {
                char next = rest[++i];
                heading.Append(next switch { 't' => '\t', _ => next });
                continue;
            }

            if (c == '|')
                return (heading.ToString(0, heading.Length - 1), rest[(i + 2)..]);
            heading.Append(c);
        }

        return (heading.ToString(), null);
    }

    static string[] Headers(string output)
        => output.Split('\n').Where(line => line.StartsWith("@@ ", StringComparison.Ordinal)).ToArray();

    static string RenderMarkdown(MappedTextDiff diff, int? contextLines)
    {
        var writer = MarkoutWriter.Create(
            new MarkdownFormatter(),
            new MarkoutWriterOptions { TextDiffContextLines = contextLines, NewLine = "\n" });
        writer.WriteTextDiff(diff);
        return writer.ToString().Replace("\r\n", "\n").TrimEnd();
    }

    static string RenderPlainText(MappedTextDiff diff, int? contextLines)
    {
        var writer = MarkoutWriter.Create(
            new PlainTextFormatter(),
            new MarkoutWriterOptions { TextDiffContextLines = contextLines, NewLine = "\n" });
        writer.WriteTextDiff(diff);
        return writer.ToString().Replace("\r\n", "\n").TrimEnd();
    }
}
