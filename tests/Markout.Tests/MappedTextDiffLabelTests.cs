using Markout;
using Markout.Ansi.Spectre;
using Markout.Formatting;
using Spectre.Console;

namespace Markout.Tests;

public class MappedTextDiffLabelTests
{
    [Theory]
    [InlineData("a\nb")]
    [InlineData("a\rb")]
    public void LabelRejectsLineTerminators(string text)
        => Assert.Throws<ArgumentException>(() => new TextDiffChangeLabel(text));

    [Fact]
    public void LabelRejectsNegativeRelatedChange()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new TextDiffChangeLabel("moved", relatedChange: -1));

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void DiffRejectsSelfOrOutOfRangeRelatedChange(int related)
    {
        var ex = Assert.Throws<ArgumentException>(() => new MappedTextDiff(
            new TextDiffSequence(["a", "b"]),
            new TextDiffSequence(["a", "c"]),
            [
                new TextDiffChange(
                    new TextDiffRange(1, 1),
                    new TextDiffRange(1, 1),
                    label: new TextDiffChangeLabel("x", relatedChange: related))
            ]));

        Assert.Contains("invalid change address", ex.Message);
    }

    [Fact]
    public void UnifiedHeaderCarriesTheLabel()
    {
        // Scrutor 4.2.2 -> 5.0.0 Decorate: a blank line that held spaces became empty.
        var diff = new MappedTextDiff(
            new TextDiffSequence(["NotNull(decorator);", "        ", "return services;"]),
            new TextDiffSequence(["NotNull(decorator);", "", "return services;"]),
            [
                new TextDiffChange(
                    new TextDiffRange(1, 1),
                    new TextDiffRange(1, 1),
                    label: new TextDiffChangeLabel("whitespace-only: blank-line content", TextDiffLabelEmphasis.Subdued, showWhitespace: true))
            ]);

        string output = RenderMarkdown(diff, contextLines: 3);

        Assert.Contains("@@ -1,3 +1,3 @@ whitespace-only: blank-line content\n", output);
        Assert.Contains("\n-        \n+\n", output);
    }

    [Fact]
    public void LabelTextIsInertInTheHeader()
    {
        var diff = new MappedTextDiff(
            new TextDiffSequence(["a"]),
            new TextDiffSequence(["b"]),
            [new TextDiffChange(new TextDiffRange(0, 1), new TextDiffRange(0, 1), label: new TextDiffChangeLabel("tab\there ```\u001b[31m"))]);

        string output = RenderMarkdown(diff, contextLines: 0);

        Assert.Contains("@@ -1 +1 @@ tab\\there ```\\u001B[31m", output);
        Assert.StartsWith("````", output);
    }

    [Fact]
    public void ChangesWithDifferentLabelsNeverShareAHunkOrALine()
    {
        string[] lines = ["a", "b", "c", "d", "e", "f", "g"];
        string[] after = ["a", "B", "c", "d", "e", "F", "g"];
        var diff = new MappedTextDiff(
            new TextDiffSequence(lines),
            new TextDiffSequence(after),
            [
                new TextDiffChange(new TextDiffRange(1, 1), new TextDiffRange(1, 1), label: new TextDiffChangeLabel("whitespace-only")),
                new TextDiffChange(new TextDiffRange(5, 1), new TextDiffRange(5, 1)),
            ]);

        string output = RenderMarkdown(diff, contextLines: 3);
        string[] body = output.Split('\n');

        Assert.Equal(2, body.Count(line => line.StartsWith("@@", StringComparison.Ordinal)));
        Assert.Contains("@@ -1,4 +1,4 @@ whitespace-only", body);
        Assert.Contains("@@ -5,3 +5,3 @@", body);
        foreach (string line in new[] { " c", " d", " e" })
            Assert.Single(body, text => text == line);
    }

    [Fact]
    public void AdjacentChangesWithDifferentLabelsAreSeparateHunks()
    {
        var diff = new MappedTextDiff(
            new TextDiffSequence(["x", "class Foo {", "int x;", "y"]),
            new TextDiffSequence(["x", "class Foo", "{", "int y;", "y"]),
            [
                new TextDiffChange(new TextDiffRange(1, 1), new TextDiffRange(1, 2), label: new TextDiffChangeLabel("whitespace-only: line break")),
                new TextDiffChange(new TextDiffRange(2, 1), new TextDiffRange(3, 1)),
            ]);

        string output = RenderMarkdown(diff, contextLines: 3);

        Assert.Contains("@@ -1,2 +1,3 @@ whitespace-only: line break\n x\n-class Foo {\n+class Foo\n+{\n@@ -3,2 +4,2 @@\n-int x;\n+int y;\n y", output);
    }

    [Fact]
    public void ChangesWithEqualLabelsStillGroup()
    {
        var label = new TextDiffChangeLabel("whitespace-only");
        var diff = new MappedTextDiff(
            new TextDiffSequence(["a", "b", "c", "d"]),
            new TextDiffSequence(["a", "B", "c", "D"]),
            [
                new TextDiffChange(new TextDiffRange(1, 1), new TextDiffRange(1, 1), label: label),
                new TextDiffChange(new TextDiffRange(3, 1), new TextDiffRange(3, 1), label: new TextDiffChangeLabel("whitespace-only")),
            ]);

        string output = RenderMarkdown(diff, contextLines: 3);

        Assert.Single(output.Split('\n'), line => line.StartsWith("@@", StringComparison.Ordinal));
    }

    [Fact]
    public void FullContextSplitsTheGapBetweenLabels()
    {
        var diff = new MappedTextDiff(
            new TextDiffSequence(["a", "b", "c", "d", "e"]),
            new TextDiffSequence(["A", "b", "c", "d", "E"]),
            [
                new TextDiffChange(new TextDiffRange(0, 1), new TextDiffRange(0, 1), label: new TextDiffChangeLabel("one")),
                new TextDiffChange(new TextDiffRange(4, 1), new TextDiffRange(4, 1)),
            ]);

        string[] body = RenderMarkdown(diff, contextLines: null).Split('\n');

        Assert.Equal(2, body.Count(line => line.StartsWith("@@", StringComparison.Ordinal)));
        foreach (string line in new[] { " b", " c", " d" })
            Assert.Single(body, text => text == line);
    }

    [Fact]
    public void MovedEndsRenderTheirRelation()
    {
        var diff = MovedBlock();

        var writer = MarkoutWriter.Create(new UnicodeFormatter(), new MarkoutWriterOptions { TextDiffContextLines = 0, NewLine = "\n" });
        writer.WriteTextDiff(diff);
        string unicode = Normalize(writer.ToString());
        string markdown = RenderMarkdown(diff, contextLines: 0);

        Assert.Contains("▸ change 1: moved (1) to +2 (related change 2)", unicode);
        Assert.Contains("▸ change 2: moved (1) from -1 (related change 1)", unicode);
        Assert.Contains("@@ -1,2 +0,0 @@ moved (1) to +2", markdown);
        Assert.Contains("@@ -3,0 +2,2 @@ moved (1) from -1", markdown);
    }

    [Fact]
    public void UnicodeShowsWhitespaceGlyphsAndEscapesLiteralGlyphs()
    {
        var diff = new MappedTextDiff(
            new TextDiffSequence(["x", "  ·\t", "y"]),
            new TextDiffSequence(["x", "", "y"]),
            [
                new TextDiffChange(
                    new TextDiffRange(1, 1),
                    new TextDiffRange(1, 1),
                    [new TextDiffInnerMapping(new TextDiffSpan(1, 0, 4), new TextDiffSpan(1, 0, 0))],
                    label: new TextDiffChangeLabel("whitespace-only", TextDiffLabelEmphasis.Subdued, showWhitespace: true))
            ]);

        var writer = MarkoutWriter.Create(new UnicodeFormatter(), new MarkoutWriterOptions { TextDiffContextLines = 0, NewLine = "\n" });
        writer.WriteTextDiff(diff);
        string output = Normalize(writer.ToString());

        Assert.Contains("◦ change 1: whitespace-only", output);
        Assert.Contains("[-··\\·→-]", output);
    }

    [Fact]
    public void SpectreSubduesAndShowsWhitespace()
    {
        var diff = new MappedTextDiff(
            new TextDiffSequence(["x", "  a", "y"]),
            new TextDiffSequence(["x", "a", "y"]),
            [
                new TextDiffChange(
                    new TextDiffRange(1, 1),
                    new TextDiffRange(1, 1),
                    [new TextDiffInnerMapping(new TextDiffSpan(1, 0, 2), new TextDiffSpan(1, 0, 0))],
                    label: new TextDiffChangeLabel("whitespace-only: indentation", TextDiffLabelEmphasis.Subdued, showWhitespace: true))
            ]);

        var writer = MarkoutWriter.Create(NewSpectreFormatter(), new MarkoutWriterOptions { TextDiffContextLines = 0, NewLine = "\n" });
        writer.WriteTextDiff(diff);
        string output = writer.ToString();

        Assert.Contains("\u001b[2m", output);
        Assert.Contains("# change 1: whitespace-only: indentation", output);
        Assert.Contains("\u001b[7m··\u001b[27m", output);
    }

    [Fact]
    public void JsonlCarriesLabelFields()
    {
        var writer = MarkoutWriter.Create(
            new TableFormatter(),
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, OmitEmptyJsonFields = true, NewLine = "\n" });

        writer.WriteTextDiff(MovedBlock());
        string output = Normalize(writer.ToString());

        Assert.Contains("\"change_label\":\"moved (1) to +2\"", output);
        Assert.Contains("\"label_emphasis\":\"normal\"", output);
        Assert.Contains("\"related_change\":\"1\"", output);
    }

    static MappedTextDiff MovedBlock() => new(
        new TextDiffSequence(["A", "B", "x"]),
        new TextDiffSequence(["x", "A", "B"]),
        [
            new TextDiffChange(new TextDiffRange(0, 2), new TextDiffRange(0, 0), label: new TextDiffChangeLabel("moved (1) to +2", relatedChange: 1)),
            new TextDiffChange(new TextDiffRange(3, 0), new TextDiffRange(1, 2), label: new TextDiffChangeLabel("moved (1) from -1", relatedChange: 0)),
        ]);

    static SpectreFormatter NewSpectreFormatter()
        => new(AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(TextWriter.Null),
        }));

    static string Normalize(string value)
        => value.Replace("\r\n", "\n").TrimEnd();

    static string RenderMarkdown(MappedTextDiff diff, int? contextLines)
    {
        var writer = MarkoutWriter.Create(
            new MarkdownFormatter(),
            new MarkoutWriterOptions { TextDiffContextLines = contextLines, NewLine = "\n" });
        writer.WriteTextDiff(diff);
        return Normalize(writer.ToString());
    }
}
