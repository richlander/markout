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
    public void AdjacentChangesWithDifferentLabelsShareOneUnlabeledHunk()
    {
        var diff = new MappedTextDiff(
            new TextDiffSequence(["x", "class Foo {", "int x;", "y"]),
            new TextDiffSequence(["x", "class Foo", "{", "int y;", "y"]),
            [
                new TextDiffChange(new TextDiffRange(1, 1), new TextDiffRange(1, 2), label: new TextDiffChangeLabel("whitespace-only: line break")),
                new TextDiffChange(new TextDiffRange(2, 1), new TextDiffRange(3, 1)),
            ]);

        string output = RenderMarkdown(diff, contextLines: 3);

        // No unchanged line separates the changes, so splitting them would leave a hunk inside
        // the sequence without context on one side, which git apply or patch rejects.
        Assert.Contains("@@ -1,4 +1,5 @@\n x\n-class Foo {\n+class Foo\n+{\n-int x;\n+int y;\n y", output);
        AssertGnuApplicable(output, diff.Before.Lines.Length);
        AssertGitApplicable(output, diff.Before.Lines.Length);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(null)]
    public void LabeledChangesOneUnchangedLineApartShareOneHunk(int? contextLines)
    {
        var diff = TwoLabeledChanges(gap: 1);

        string output = RenderMarkdown(diff, contextLines);

        Assert.Single(output.Split('\n'), line => line.StartsWith("@@ ", StringComparison.Ordinal));
        Assert.DoesNotContain("@@ whitespace-only", output);
        AssertGitApplicable(output, diff.Before.Lines.Length);
    }

    [Theory]
    [InlineData(3, "@@ -6,3 +6,3 @@ whitespace-only\n l6\n-l7\n+L7\n l8\n@@ -9,3 +9,3 @@\n l9\n-l10\n+L10\n l11\n")]
    [InlineData(null, "@@ -1,8 +1,8 @@ whitespace-only\n l1\n l2\n l3\n l4\n l5\n l6\n-l7\n+L7\n l8\n@@ -9,8 +9,8 @@\n l9\n-l10\n+L10\n l11\n l12\n l13\n l14\n l15\n l16")]
    public void LabeledChangesTwoUnchangedLinesApartSplitWithContextOnBothSides(int? contextLines, string expected)
    {
        var diff = TwoLabeledChanges(gap: 2);

        string output = RenderMarkdown(diff, contextLines);

        Assert.Contains(expected, output);
        AssertGitApplicable(output, diff.Before.Lines.Length);
        if (contextLines is not null)
            AssertGnuApplicable(output, diff.Before.Lines.Length);
    }

    [Fact]
    public void UnlimitedContextLabelSplitStaysGitApplicable()
    {
        // markout#237: dotnet-inspect's detailed Source Diff of JsonSerializerOptions.GetTypeInfo.
        string[] before =
        [
            "public JsonTypeInfo GetTypeInfo(Type type)", "{", "    ArgumentNullException.ThrowIfNull(type);", "",
            "    if (JsonTypeInfo.IsInvalidForSerialization(type))", "    {",
            "        ThrowHelper.ThrowArgumentException_CannotSerializeInvalidType(nameof(type), type, null, null);",
            "    }", "", "    return GetTypeInfoInternal(type, resolveIfMutable: true);", "}",
        ];
        string[] after =
        [
            "public JsonTypeInfo GetTypeInfo(Type type)", "{", "    ArgumentNullException.ThrowIfNull(type, \"type\");",
            "    if (JsonTypeInfo.IsInvalidForSerialization(type))", "    {",
            "        ThrowHelper.ThrowArgumentException_CannotSerializeInvalidType(\"type\", type, null, null);",
            "    }", "    return GetTypeInfoInternal(type, true, new Nullable<bool>(true), true);", "}",
        ];
        var lineBreaks = new TextDiffChangeLabel("whitespace-only: line breaks", TextDiffLabelEmphasis.Subdued, showWhitespace: true);
        var diff = new MappedTextDiff(
            new TextDiffSequence(before, finalLineTerminator: TextDiffLineTerminator.Absent),
            new TextDiffSequence(after, finalLineTerminator: TextDiffLineTerminator.Absent),
            [
                new TextDiffChange(new TextDiffRange(2, 1), new TextDiffRange(2, 1)),
                new TextDiffChange(new TextDiffRange(3, 1), new TextDiffRange(3, 0), label: lineBreaks),
                new TextDiffChange(new TextDiffRange(6, 1), new TextDiffRange(5, 1)),
                new TextDiffChange(new TextDiffRange(8, 1), new TextDiffRange(7, 0), label: lineBreaks),
                new TextDiffChange(new TextDiffRange(9, 1), new TextDiffRange(7, 1)),
            ]);

        string output = RenderMarkdown(diff, contextLines: null);

        AssertGitApplicable(output, before.Length);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void EveryLabeledSplitKeepsContextInsideTheSequence(int? contextLines)
    {
        var random = new Random(237);
        for (var trial = 0; trial < 500; trial++)
        {
            int length = random.Next(4, 30);
            var before = new List<string>();
            var after = new List<string>();
            var changes = new List<TextDiffChange>();
            for (var line = 0; line < length; line++)
            {
                // Replacements, removals, and additions, each with a random label.
                int kind = random.Next(6);
                if (kind > 2)
                {
                    before.Add($"l{line}");
                    after.Add($"l{line}");
                    continue;
                }

                TextDiffChangeLabel? label = random.Next(3) switch
                {
                    0 => null,
                    1 => new TextDiffChangeLabel("whitespace-only"),
                    _ => new TextDiffChangeLabel("moved"),
                };
                int removed = kind == 2 ? 0 : 1;
                int added = kind == 1 ? 0 : 1;
                changes.Add(new TextDiffChange(
                    new TextDiffRange(before.Count, removed),
                    new TextDiffRange(after.Count, added),
                    label: label));
                if (removed == 1)
                    before.Add($"b{line}");
                if (added == 1)
                    after.Add($"a{line}");
            }
            if (changes.Count == 0)
                continue;

            // Adjacent changes must leave an unchanged gap of equal size on both sides.
            if (!changes.Zip(changes.Skip(1)).All(pair =>
                    pair.Second.Before.Start - pair.First.Before.End == pair.Second.After.Start - pair.First.After.End))
                continue;

            var diff = new MappedTextDiff(new TextDiffSequence(before), new TextDiffSequence(after), changes);
            string output = RenderMarkdown(diff, contextLines);

            AssertGitApplicable(output, before.Count);
            if (contextLines is not null)
                AssertGnuApplicable(output, before.Count);
        }
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
    public void AdjacentMidSequenceChangesShareOneHunkWithEqualContext()
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

        Assert.Contains("@@ -5,8 +5,8 @@\n l5\n l6\n l7\n-l8\n+L8\n-l9\n+L9\n l10\n l11\n l12\n", output);
        AssertGnuApplicable(output, diff.Before.Lines.Length);
        AssertGitApplicable(output, diff.Before.Lines.Length);
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

    /// <summary>
    /// Asserts the rule git apply and patch share: a hunk inside the sequence keeps at least one
    /// context line on each side. git apply reads a hunk without trailing context as anchored to
    /// the end, and patch one without leading context as anchored to the start.
    /// </summary>
    static void AssertGitApplicable(string markdown, int beforeLineCount)
    {
        var hunks = new List<(int OldStart, int OldCount, int NewCount, List<string> Body)>();
        foreach (string line in markdown.Split('\n'))
        {
            if (line.StartsWith("@@ ", StringComparison.Ordinal))
            {
                string[] fields = line.Split(' ');
                string[] parts = fields[1][1..].Split(',');
                int count = parts.Length > 1 ? int.Parse(parts[1]) : 1;
                int start = int.Parse(parts[0]);
                string[] newParts = fields[2][1..].Split(',');
                int newCount = newParts.Length > 1 ? int.Parse(newParts[1]) : 1;
                hunks.Add((count == 0 ? start : start - 1, count, newCount, []));
            }
            else if (hunks.Count > 0 && line.Length > 0 && line[0] is ' ' or '-' or '+')
            {
                hunks[^1].Body.Add(line);
            }
        }

        foreach (var (oldStart, oldCount, newCount, body) in hunks)
        {
            Assert.Equal(oldCount, body.Count(line => line[0] is ' ' or '-'));
            Assert.Equal(newCount, body.Count(line => line[0] is ' ' or '+'));
            int leading = body.TakeWhile(line => line[0] == ' ').Count();
            int trailing = body.AsEnumerable().Reverse().TakeWhile(line => line[0] == ' ').Count();
            if (oldStart > 0)
                Assert.True(leading > 0, "a hunk inside the sequence has no leading context\n" + markdown);
            if (oldStart + oldCount < beforeLineCount)
                Assert.True(trailing > 0, "a hunk inside the sequence has no trailing context\n" + markdown);
        }
    }

    /// <summary>Sixteen lines; a labeled change at line 7 and an unlabeled one <paramref name="gap"/> lines later.</summary>
    static MappedTextDiff TwoLabeledChanges(int gap)
    {
        string[] before = [.. Enumerable.Range(1, 16).Select(i => $"l{i}")];
        string[] after = [.. before];
        int second = 7 + gap;
        after[6] = "L7";
        after[second] = $"L{second + 1}";
        return new MappedTextDiff(
            new TextDiffSequence(before),
            new TextDiffSequence(after),
            [
                new TextDiffChange(new TextDiffRange(6, 1), new TextDiffRange(6, 1), label: new TextDiffChangeLabel("whitespace-only")),
                new TextDiffChange(new TextDiffRange(second, 1), new TextDiffRange(second, 1)),
            ]);
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
