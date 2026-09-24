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

        // Each hunk takes equal context unless it touches the sequence start or end, so GNU
        // patch does not misread uneven context as an anchor: here both sides drop to zero.
        Assert.Contains("@@ -2 +2,2 @@ whitespace-only: line break\n-class Foo {\n+class Foo\n+{\n@@ -3 +4 @@\n-int x;\n+int y;", output);
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
    public void FullContextKeepsEveryLineAroundInteriorSplits()
    {
        // Round 2 review: null context must retain every unchanged line even when a label split
        // leaves hunks with unequal context in the middle of the sequence.
        string[] before = [.. Enumerable.Range(1, 12).Select(i => $"l{i}")];
        string[] after = [.. before];
        after[3] = "L4";
        after[8] = "L9";
        var diff = new MappedTextDiff(
            new TextDiffSequence(before),
            new TextDiffSequence(after),
            [
                new TextDiffChange(new TextDiffRange(3, 1), new TextDiffRange(3, 1), label: new TextDiffChangeLabel("whitespace-only")),
                new TextDiffChange(new TextDiffRange(8, 1), new TextDiffRange(8, 1)),
            ]);

        Assert.DoesNotContain(
            MappedTextDiffLowering.ToDisplayLines(diff, contextLines: null),
            line => line.Kind == TextDiffDisplayLineKind.Omission);
        string output = RenderMarkdown(diff, contextLines: null);
        Assert.Contains("@@ -1,6 +1,6 @@ whitespace-only\n l1\n", output);
        Assert.Contains("@@ -7,6 +7,6 @@\n", output);
        Assert.Contains(" l12\n", output);
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

    [Fact]
    public void MidSequenceSplitGivesEachHunkEqualContext()
    {
        // Round 1 review repro A: adjacent differently labeled changes in the middle of a file.
        string[] before = [.. Enumerable.Range(1, 12).Select(i => $"l{i}")];
        string[] after = [.. before];
        after[7] = "L8";
        after[8] = "L9";
        var diff = new MappedTextDiff(
            new TextDiffSequence(before),
            new TextDiffSequence(after),
            [
                new TextDiffChange(new TextDiffRange(7, 1), new TextDiffRange(7, 1), label: new TextDiffChangeLabel("whitespace-only")),
                new TextDiffChange(new TextDiffRange(8, 1), new TextDiffRange(8, 1)),
            ]);

        string output = RenderMarkdown(diff, contextLines: 3);

        Assert.Contains("@@ -8 +8 @@ whitespace-only\n-l8\n+L8\n@@ -9 +9 @@\n-l9\n+L9\n", output);
        AssertGnuApplicable(output, diff.Before.Lines.Length);
    }

    [Fact]
    public void SplitThatWouldStrandAnUnterminatedFinalLineStaysOneUnlabeledHunk()
    {
        // Round 1 review repro C: the After side's unterminated final line would otherwise end an
        // earlier hunk than the Before side's.
        var diff = new MappedTextDiff(
            new TextDiffSequence(["u0", "b1", "b2"], finalLineTerminator: TextDiffLineTerminator.Absent),
            new TextDiffSequence(["u0", "a1"], finalLineTerminator: TextDiffLineTerminator.Absent),
            [
                new TextDiffChange(new TextDiffRange(1, 1), new TextDiffRange(1, 1), label: new TextDiffChangeLabel("whitespace-only")),
                new TextDiffChange(new TextDiffRange(2, 1), new TextDiffRange(2, 0)),
            ]);

        string output = RenderMarkdown(diff, contextLines: 3);

        Assert.Single(output.Split('\n'), line => line.StartsWith("@@", StringComparison.Ordinal));
        Assert.Contains("@@ -1,3 +1,2 @@\n", output);
        AssertGnuApplicable(output, diff.Before.Lines.Length);
    }

    [Fact]
    public void SpectreGlyphSpansDoubleCallerBackslashes()
    {
        const string line = "a\\ b·c\\\td→e";
        var diff = new MappedTextDiff(
            new TextDiffSequence([line]),
            new TextDiffSequence([""]),
            [
                new TextDiffChange(
                    new TextDiffRange(0, 1),
                    new TextDiffRange(0, 1),
                    [new TextDiffInnerMapping(new TextDiffSpan(0, 0, line.Length), new TextDiffSpan(0, 0, 0))],
                    label: new TextDiffChangeLabel("whitespace-only", showWhitespace: true))
            ]);

        var writer = MarkoutWriter.Create(NewSpectreFormatter(), new MarkoutWriterOptions { TextDiffContextLines = 0, NewLine = "\n" });
        writer.WriteTextDiff(diff);

        Assert.Contains("a\\\\·b\\·c\\\\→d\\→e", writer.ToString());
    }

    [Fact]
    public void RandomLabeledDiffsStayGnuApplicable()
    {
        var random = new Random(230);
        TextDiffChangeLabel?[] labels = [null, new TextDiffChangeLabel("one"), new TextDiffChangeLabel("two")];
        int[]?[] contexts = [[0], [1], [3], [5], null];
        for (int iteration = 0; iteration < 3000; iteration++)
        {
            var before = new List<string>();
            var after = new List<string>();
            var changes = new List<TextDiffChange>();
            int changeCount = random.Next(0, 5);
            for (int c = 0; c < changeCount; c++)
            {
                int gap = random.Next(0, 5);
                for (int g = 0; g < gap; g++)
                {
                    string shared = $"s{before.Count}";
                    before.Add(shared);
                    after.Add(shared);
                }

                int removed = random.Next(0, 3);
                int added = removed == 0 ? random.Next(1, 3) : random.Next(0, 3);
                int beforeStart = before.Count;
                int afterStart = after.Count;
                for (int r = 0; r < removed; r++) before.Add($"b{before.Count}");
                for (int a = 0; a < added; a++) after.Add($"a{after.Count}");
                changes.Add(new TextDiffChange(
                    new TextDiffRange(beforeStart, removed),
                    new TextDiffRange(afterStart, added),
                    label: labels[random.Next(labels.Length)]));
            }

            int tail = random.Next(0, 4);
            for (int t = 0; t < tail; t++)
            {
                string shared = $"t{before.Count}";
                before.Add(shared);
                after.Add(shared);
            }

            TextDiffLineTerminator beforeTerminator = Terminator(random, before.Count);
            TextDiffLineTerminator afterTerminator = tail > 0 && before.Count > 0 ? beforeTerminator : Terminator(random, after.Count);
            MappedTextDiff diff;
            try
            {
                diff = new MappedTextDiff(
                    new TextDiffSequence(before, finalLineTerminator: beforeTerminator),
                    new TextDiffSequence(after, finalLineTerminator: afterTerminator),
                    changes);
            }
            catch (ArgumentException)
            {
                continue;
            }

            int? context = contexts[random.Next(contexts.Length)]?[0];
            if (context is null)
                Assert.DoesNotContain(MappedTextDiffLowering.ToDisplayLines(diff, null), line => line.Kind == TextDiffDisplayLineKind.Omission);
            else
                AssertGnuApplicable(RenderMarkdown(diff, context), before.Count);
        }
    }

    static TextDiffLineTerminator Terminator(Random random, int count)
        => count == 0 ? TextDiffLineTerminator.Unknown : random.Next(2) == 0 ? TextDiffLineTerminator.Present : TextDiffLineTerminator.Absent;

    /// <summary>
    /// Checks the unified output against the structural rules GNU patch applies: header counts
    /// match the hunk body, hunks are ordered and share no line, unequal context appears only
    /// where the hunk touches the sequence start or end, and a no-newline marker appears only in
    /// the last hunk.
    /// </summary>
    static void AssertGnuApplicable(string markdown, int beforeLineCount)
    {
        var hunks = new List<(int OldStart, int OldCount, List<string> Body)>();
        foreach (string line in markdown.Split('\n'))
        {
            if (line.StartsWith("@@ ", StringComparison.Ordinal))
            {
                string range = line.Split(' ')[1][1..];
                string[] parts = range.Split(',');
                int count = parts.Length > 1 ? int.Parse(parts[1]) : 1;
                int start = int.Parse(parts[0]);
                hunks.Add((count == 0 ? start : start - 1, count, []));
            }
            else if (hunks.Count > 0 && line.Length > 0 && line[0] is ' ' or '-' or '+' or '\\')
            {
                hunks[^1].Body.Add(line);
            }
        }

        int previousEnd = 0;
        for (int index = 0; index < hunks.Count; index++)
        {
            var (oldStart, oldCount, body) = hunks[index];
            List<string> lines = [.. body.Where(line => line[0] != '\\')];
            Assert.Equal(oldCount, lines.Count(line => line[0] is ' ' or '-'));
            Assert.True(oldStart >= previousEnd, "hunks share a line");
            previousEnd = oldStart + oldCount;

            int leading = lines.TakeWhile(line => line[0] == ' ').Count();
            int trailing = lines.AsEnumerable().Reverse().TakeWhile(line => line[0] == ' ').Count();
            if (leading < trailing)
                Assert.True(oldStart == 0, "uneven context not at the sequence start\n" + markdown);
            if (trailing < leading)
                Assert.True(previousEnd == beforeLineCount, "uneven context not at the sequence end\n" + markdown);
            if (index < hunks.Count - 1)
                Assert.DoesNotContain(body, line => line[0] == '\\');
        }

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
