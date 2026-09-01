using System.Text.RegularExpressions;
using Markout;
using Markout.Ansi.Spectre;
using Markout.Formatting;
using Spectre.Console;

namespace Markout.Tests;

public partial class MappedTextDiffTests
{
    [Fact]
    public void ConstructorSnapshotsSequencesAndChanges()
    {
        var beforeLines = new[] { "old" };
        var afterLines = new[] { "new" };
        var changes = new[]
        {
            new TextDiffChange(new TextDiffRange(0, 1), new TextDiffRange(0, 1))
        };

        var diff = new MappedTextDiff(
            new TextDiffSequence(beforeLines),
            new TextDiffSequence(afterLines),
            changes);
        beforeLines[0] = "mutated";
        afterLines[0] = "mutated";
        changes[0] = new TextDiffChange(new TextDiffRange(0, 0), new TextDiffRange(0, 1));

        Assert.Equal("old", diff.Before.Lines[0]);
        Assert.Equal("new", diff.After.Lines[0]);
        Assert.Equal(TextDiffChangeForm.Replacement, diff.Changes[0].Form);
    }

    [Fact]
    public void ConstructorRejectsUnequalUnchangedGaps()
    {
        var ex = Assert.Throws<ArgumentException>(() => new MappedTextDiff(
            new TextDiffSequence(["old", "tail"]),
            new TextDiffSequence(["new", "tail", "extra"]),
            [new TextDiffChange(new TextDiffRange(0, 1), new TextDiffRange(0, 1))]));

        Assert.Contains("trailing unchanged gap", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConstructorAllowsAdditionThenRemovalAtOneBoundary()
    {
        var diff = new MappedTextDiff(
            new TextDiffSequence(["old", "tail"]),
            new TextDiffSequence(["new", "tail"]),
            [
                new TextDiffChange(new TextDiffRange(0, 0), new TextDiffRange(0, 1)),
                new TextDiffChange(new TextDiffRange(0, 1), new TextDiffRange(1, 0))
            ]);

        Assert.Equal(2, diff.Changes.Length);
    }

    [Fact]
    public void ConstructorRejectsAnInnerSpanThatSplitsASurrogatePair()
    {
        var ex = Assert.Throws<ArgumentException>(() => new MappedTextDiff(
            new TextDiffSequence(["a😀b"]),
            new TextDiffSequence(["a😁b"]),
            [
                new TextDiffChange(
                    new TextDiffRange(0, 1),
                    new TextDiffRange(0, 1),
                    [new TextDiffInnerMapping(
                        new TextDiffSpan(0, 1, 1),
                        new TextDiffSpan(0, 1, 2))])
            ]));

        Assert.Contains("surrogate pair", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConstructorRejectsAnUnterminatedFinalLineSharedWithANonFinalLine()
    {
        var ex = Assert.Throws<ArgumentException>(() => new MappedTextDiff(
            new TextDiffSequence(["a"], finalLineTerminator: TextDiffLineTerminator.Absent),
            new TextDiffSequence(["a", "b"], finalLineTerminator: TextDiffLineTerminator.Absent),
            [new TextDiffChange(new TextDiffRange(1, 0), new TextDiffRange(1, 1))]));

        Assert.Contains("unterminated final line", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConstructorAcceptsGnuShapeForAppendingAfterAnUnterminatedLine()
    {
        var diff = new MappedTextDiff(
            new TextDiffSequence(["a"], finalLineTerminator: TextDiffLineTerminator.Absent),
            new TextDiffSequence(["a", "b"], finalLineTerminator: TextDiffLineTerminator.Absent),
            [new TextDiffChange(new TextDiffRange(0, 1), new TextDiffRange(0, 2))]);

        Assert.Equal(TextDiffChangeForm.Replacement, diff.Changes[0].Form);
    }

    [Fact]
    public void ConstructorRejectsMalformedUtf16()
    {
        Assert.Throws<ArgumentException>(() => new TextDiffSequence(["\uD800"]));
    }

    [Theory]
    [InlineData("line\rbreak", null)]
    [InlineData("line\nbreak", null)]
    [InlineData("line", "label\rbreak")]
    [InlineData("line", "label\nbreak")]
    public void SequenceRejectsEmbeddedLineTerminators(string line, string? label)
    {
        Assert.Throws<ArgumentException>(() => new TextDiffSequence([line], label));
    }

    [Fact]
    public void ConstructorAllowsEmptySequencesWithoutTerminatorAssertions()
    {
        var diff = new MappedTextDiff(
            new TextDiffSequence([]),
            new TextDiffSequence([]),
            []);
        var writer = MarkoutWriter.Create(
            new MarkdownFormatter(),
            new MarkoutWriterOptions { NewLine = "\n" });

        Assert.True(diff.IsEmpty);
        Assert.True(writer.WriteTextDiff(diff));
        Assert.Empty(writer.ToString());
    }

    [Fact]
    public void ContextSelectionReportsExactOmittedRanges()
    {
        var diff = new MappedTextDiff(
            new TextDiffSequence(["0", "1", "2", "old", "4", "5", "6"]),
            new TextDiffSequence(["0", "1", "2", "new", "4", "5", "6"]),
            [new TextDiffChange(new TextDiffRange(3, 1), new TextDiffRange(3, 1))]);

        var records = MappedTextDiffLowering.ToDisplayLines(diff, contextLines: 1);
        var omissions = records.Where(r => r.Kind == TextDiffDisplayLineKind.Omission).ToArray();

        Assert.Equal(2, omissions.Length);
        Assert.Equal(new TextDiffRange(0, 2), omissions[0].BeforeRange);
        Assert.Equal(new TextDiffRange(0, 2), omissions[0].AfterRange);
        Assert.Equal(new TextDiffRange(5, 2), omissions[1].BeforeRange);
        Assert.Equal(new TextDiffRange(5, 2), omissions[1].AfterRange);
    }

    [Fact]
    public void MarkdownRendersGnuCompatibleReplacementAndAnnotation()
    {
        var writer = MarkoutWriter.Create(
            new MarkdownFormatter(),
            new MarkoutWriterOptions { TextDiffContextLines = 0, NewLine = "\n" });

        Assert.True(writer.WriteTextDiff(Sample()));
        var output = Normalize(writer.ToString());

        Assert.Contains("```diff\n--- Before\n+++ After\n@@ -1,2 +1,2 @@", output);
        Assert.Contains("-if (value < 0)\n-return 0;", output);
        Assert.Contains("+if (value <= 0)\n+return 1;", output);
        Assert.Contains("> **WARNING — After line 1, columns 11-12:** Includes zero", output);
    }

    [Fact]
    public void MarkdownPreservesTabsAndContainsTerminalControls()
    {
        var supplementaryFormat = char.ConvertFromUtf32(0xE0001);
        var diff = new MappedTextDiff(
            new TextDiffSequence(["\told\u001b[31m\u202e\u2028" + supplementaryFormat]),
            new TextDiffSequence(["\tnew\u200b\u2029"]),
            [new TextDiffChange(new TextDiffRange(0, 1), new TextDiffRange(0, 1))]);
        var writer = MarkoutWriter.Create(
            new MarkdownFormatter(),
            new MarkoutWriterOptions { TextDiffContextLines = 0, NewLine = "\n" });

        writer.WriteTextDiff(diff);
        var output = Normalize(writer.ToString());

        Assert.Contains("-\told\\u001B[31m\\u202E\\u2028\\U000E0001", output);
        Assert.Contains("+\tnew\\u200B\\u2029", output);
        Assert.True(output.IndexOf('\u001b') < 0, $"Raw ESC at {output.IndexOf('\u001b')}");
        Assert.True(output.IndexOf('\u202e') < 0, $"Raw bidi control at {output.IndexOf('\u202e')}");
        Assert.True(output.IndexOf('\u200b') < 0, $"Raw zero-width control at {output.IndexOf('\u200b')}");
        Assert.True(output.IndexOf('\u2028') < 0, $"Raw line separator at {output.IndexOf('\u2028')}");
        Assert.True(output.IndexOf('\u2029') < 0, $"Raw paragraph separator at {output.IndexOf('\u2029')}");
        Assert.DoesNotContain(supplementaryFormat, output, StringComparison.Ordinal);
    }

    [Fact]
    public void MarkdownContainsAnnotationLineTerminatorsAndInlineSyntax()
    {
        var diff = new MappedTextDiff(
            new TextDiffSequence(["old"]),
            new TextDiffSequence(["new"]),
            [
                new TextDiffChange(
                    new TextDiffRange(0, 1),
                    new TextDiffRange(0, 1),
                    annotations:
                    [
                        TextDiffAnnotation.ForChange(
                            "line\r\n**bold** ~~strike~~ [link] <script> \u001b")
                    ])
            ]);
        var writer = MarkoutWriter.Create(new MarkdownFormatter());

        writer.WriteTextDiff(diff);
        var output = writer.ToString();

        Assert.Contains(
            "line\\\\r\\\\n\\*\\*bold\\*\\* \\~\\~strike\\~\\~ "
            + "\\[link\\] &lt;script&gt; \\\\u001B",
            output);
        Assert.True(output.IndexOf('\u001b') < 0, $"Raw ESC at {output.IndexOf('\u001b')}");
    }

    [Fact]
    public void MarkdownChoosesAFenceLongerThanCallerContent()
    {
        var diff = new MappedTextDiff(
            new TextDiffSequence(["```old"]),
            new TextDiffSequence(["```new"]),
            [new TextDiffChange(new TextDiffRange(0, 1), new TextDiffRange(0, 1))]);
        var writer = MarkoutWriter.Create(new MarkdownFormatter());

        writer.WriteTextDiff(diff);

        Assert.StartsWith("````diff", writer.ToString(), StringComparison.Ordinal);
        Assert.EndsWith("````", writer.ToString().TrimEnd(), StringComparison.Ordinal);
    }

    [Fact]
    public void MarkdownTerminatesDiffBeforeTheFollowingBlock()
    {
        using var output = new StringWriter();
        var writer = new MarkoutWriter(
            output,
            new MarkdownFormatter(),
            new MarkoutWriterOptions { NewLine = "\n" });

        writer.WriteTextDiff(Sample());
        writer.WriteHeading(2, "Following");
        writer.Flush();

        var normalized = Normalize(output.ToString());
        Assert.Contains("Includes zero\n\n## Following", normalized);
        Assert.DoesNotContain("\n\n\n", normalized);
    }

    [Fact]
    public void MarkdownEmitsFinalTerminatorMarkersAtTheOwningLines()
    {
        var replacement = new MappedTextDiff(
            new TextDiffSequence(["old"], finalLineTerminator: TextDiffLineTerminator.Absent),
            new TextDiffSequence(["new"], finalLineTerminator: TextDiffLineTerminator.Absent),
            [new TextDiffChange(new TextDiffRange(0, 1), new TextDiffRange(0, 1))]);
        var sharedContext = new MappedTextDiff(
            new TextDiffSequence(["old", "same"], finalLineTerminator: TextDiffLineTerminator.Absent),
            new TextDiffSequence(["new", "same"], finalLineTerminator: TextDiffLineTerminator.Absent),
            [new TextDiffChange(new TextDiffRange(0, 1), new TextDiffRange(0, 1))]);

        var replacementOutput = RenderMarkdown(replacement, contextLines: 0);
        var contextOutput = RenderMarkdown(sharedContext, contextLines: null);

        Assert.Equal(2, Regex.Matches(replacementOutput, @"\\ No newline at end of file").Count);
        Assert.Single(Regex.Matches(contextOutput, @"\\ No newline at end of file").Cast<Match>());
        Assert.Contains(" same\n\\ No newline at end of file", contextOutput);
    }

    [Fact]
    public void MarkdownUsesCanonicalPureAdditionAndRemovalRanges()
    {
        var addition = new MappedTextDiff(
            new TextDiffSequence(["same"]),
            new TextDiffSequence(["same", "added"]),
            [new TextDiffChange(new TextDiffRange(1, 0), new TextDiffRange(1, 1))]);
        var removal = new MappedTextDiff(
            new TextDiffSequence(["same", "removed"]),
            new TextDiffSequence(["same"]),
            [new TextDiffChange(new TextDiffRange(1, 1), new TextDiffRange(1, 0))]);

        Assert.Contains("@@ -1,0 +2 @@", RenderMarkdown(addition, contextLines: 0));
        Assert.Contains("@@ -2 +1,0 @@", RenderMarkdown(removal, contextLines: 0));
    }

    [Fact]
    public void HumanAnnotationsIdentifyChangesAndEmptySpanInsertionPoints()
    {
        var diff = new MappedTextDiff(
            new TextDiffSequence(["old-1", "same", "old-2"]),
            new TextDiffSequence(["new-1", "same", "new-2"]),
            [
                new TextDiffChange(
                    new TextDiffRange(0, 1),
                    new TextDiffRange(0, 1),
                    annotations: [TextDiffAnnotation.ForChange("first")]),
                new TextDiffChange(
                    new TextDiffRange(2, 1),
                    new TextDiffRange(2, 1),
                    annotations:
                    [
                        TextDiffAnnotation.ForSpan(
                            TextDiffSide.After,
                            new TextDiffSpan(2, 3, 0),
                            "caret")
                    ])
            ]);
        var plain = MarkoutWriter.Create(
            new PlainTextFormatter(),
            new MarkoutWriterOptions { TextDiffContextLines = 0, NewLine = "\n" });
        var unicode = MarkoutWriter.Create(
            new UnicodeFormatter(),
            new MarkoutWriterOptions { TextDiffContextLines = 0, NewLine = "\n" });

        plain.WriteTextDiff(diff);
        unicode.WriteTextDiff(diff);

        Assert.Contains("Annotation (change 1): first", plain.ToString());
        Assert.Contains(
            "After line 3, insertion point at column 4",
            unicode.ToString());
    }

    [Fact]
    public void UnicodeEscapesLiteralIntralineMarkerTokens()
    {
        var diff = new MappedTextDiff(
            new TextDiffSequence(["literal [-not mapped-]"]),
            new TextDiffSequence(["literal {+not mapped+}"]),
            [new TextDiffChange(new TextDiffRange(0, 1), new TextDiffRange(0, 1))]);
        var writer = MarkoutWriter.Create(
            new UnicodeFormatter(),
            new MarkoutWriterOptions { TextDiffContextLines = 0 });

        writer.WriteTextDiff(diff);
        var output = writer.ToString();

        Assert.Contains(@"literal \[-not mapped-]", output);
        Assert.Contains(@"literal \{+not mapped+}", output);
        Assert.DoesNotContain(" - literal [-", output);
        Assert.DoesNotContain(" + literal {+", output);
    }

    [Fact]
    public void TableDiffIgnoresGenericProjectionAndRowLimits()
    {
        var writer = MarkoutWriter.Create(
            new TableFormatter(),
            new MarkoutWriterOptions
            {
                MaxItems = 1,
                RowWindow = MarkoutRowWindow.Head(1),
                Projection = new MarkoutProjection { IncludeColumns = ["after_text"] },
                TableMode = MarkoutTableMode.Tsv,
                NewLine = "\n"
            });

        writer.WriteTextDiff(Sample());
        var output = Normalize(writer.ToString());

        Assert.Contains("record_kind\tchange_address\tchange_form", output);
        Assert.Contains("sequence", output);
        Assert.Contains("removal", output);
        Assert.Contains("addition", output);
        Assert.Contains("inner_mapping", output);
        Assert.DoesNotContain("more", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void JsonlDiffCarriesChangeAndSequenceProvenance()
    {
        var writer = MarkoutWriter.Create(
            new TableFormatter(),
            new MarkoutWriterOptions
            {
                TableMode = MarkoutTableMode.Jsonl,
                OmitEmptyJsonFields = true,
                NewLine = "\n"
            });

        writer.WriteTextDiff(Sample());
        var output = Normalize(writer.ToString());

        Assert.Contains("\"record_kind\":\"sequence\"", output);
        Assert.Contains("\"record_kind\":\"removal\"", output);
        Assert.Contains("\"change_address\":\"0\"", output);
        Assert.Contains("\"before_text\":\"if (value < 0)\"", output);
        Assert.Contains("\"record_kind\":\"inner_mapping\"", output);
    }

    [Theory]
    [InlineData(MarkoutTableMode.Pretty)]
    [InlineData(MarkoutTableMode.Tsv)]
    [InlineData(MarkoutTableMode.Jsonl)]
    public void StructuredDiffPreservesInlineSyntaxAndContainsNonGraphicText(
        MarkoutTableMode mode)
    {
        var supplementaryFormat = char.ConvertFromUtf32(0xE0001);
        var longSuffix = new string('x', 4096);
        var before = "<code>x</code>\t| \u001b \u202e \u200b \u2028 \u2029 "
            + supplementaryFormat
            + " "
            + longSuffix;
        var diff = new MappedTextDiff(
            new TextDiffSequence([before], "Before <code>"),
            new TextDiffSequence(["after"], "After |"),
            [new TextDiffChange(new TextDiffRange(0, 1), new TextDiffRange(0, 1))]);
        var writer = MarkoutWriter.Create(
            new TableFormatter(),
            new MarkoutWriterOptions
            {
                TableMode = mode,
                OmitEmptyJsonFields = true,
                NewLine = "\n"
            });

        writer.WriteTextDiff(diff);
        var output = writer.ToString();
        var visibleOutput = mode == MarkoutTableMode.Jsonl
            ? output.Replace("\\\\", "\\")
            : output;

        Assert.Contains(
            "<code>x</code>\\t| \\u001B \\u202E \\u200B \\u2028 \\u2029 "
            + "\\U000E0001 "
            + longSuffix,
            visibleOutput);
        Assert.Contains("Before <code>", output);
        Assert.Contains("After |", output);
        Assert.True(output.IndexOf('\u001b') < 0, $"Raw ESC at {output.IndexOf('\u001b')}");
        Assert.True(output.IndexOf('\u202e') < 0, $"Raw bidi control at {output.IndexOf('\u202e')}");
        Assert.True(output.IndexOf('\u200b') < 0, $"Raw zero-width control at {output.IndexOf('\u200b')}");
        Assert.True(output.IndexOf('\u2028') < 0, $"Raw line separator at {output.IndexOf('\u2028')}");
        Assert.True(output.IndexOf('\u2029') < 0, $"Raw paragraph separator at {output.IndexOf('\u2029')}");
        Assert.DoesNotContain(supplementaryFormat, output, StringComparison.Ordinal);
    }

    [Fact]
    public void JsonlTypedValuesKeepTextualDiffFieldsAsStrings()
    {
        var diff = new MappedTextDiff(
            new TextDiffSequence(["1"], "true"),
            new TextDiffSequence(["false"]),
            [new TextDiffChange(new TextDiffRange(0, 1), new TextDiffRange(0, 1))]);
        var writer = MarkoutWriter.Create(
            new TableFormatter(),
            new MarkoutWriterOptions
            {
                TableMode = MarkoutTableMode.Jsonl,
                JsonTypedValues = true,
                OmitEmptyJsonFields = true,
                NewLine = "\n"
            });

        writer.WriteTextDiff(diff);
        var output = Normalize(writer.ToString());

        Assert.Contains("\"label\":\"true\"", output);
        Assert.Contains("\"before_text\":\"1\"", output);
        Assert.Contains("\"after_text\":\"false\"", output);
        Assert.Contains("\"before_line\":0", output);
        Assert.DoesNotContain("\"before_text\":1", output);
    }

    [Fact]
    public void StructuredOmissionCarriesExactRangesAndHiddenCount()
    {
        var diff = new MappedTextDiff(
            new TextDiffSequence(["0", "1", "old", "3", "4"]),
            new TextDiffSequence(["0", "1", "new", "3", "4"]),
            [new TextDiffChange(new TextDiffRange(2, 1), new TextDiffRange(2, 1))]);
        var writer = MarkoutWriter.Create(
            new TableFormatter(),
            new MarkoutWriterOptions
            {
                TableMode = MarkoutTableMode.Jsonl,
                OmitEmptyJsonFields = true,
                TextDiffContextLines = 0,
                NewLine = "\n"
            });

        writer.WriteTextDiff(diff);
        var output = Normalize(writer.ToString());

        Assert.Contains(
            "\"record_kind\":\"omission\",\"side\":\"both\",\"before_start\":\"0\","
            + "\"before_count\":\"2\",\"after_start\":\"0\",\"after_count\":\"2\","
            + "\"hidden_count\":\"2\"",
            output);
        Assert.Contains(
            "\"record_kind\":\"omission\",\"side\":\"both\",\"before_start\":\"3\","
            + "\"before_count\":\"2\",\"after_start\":\"3\",\"after_count\":\"2\","
            + "\"hidden_count\":\"2\"",
            output);
    }

    [Fact]
    public void UnicodeDiffShowsLineNumbersInnerSpansAndAnnotations()
    {
        var writer = MarkoutWriter.Create(
            new UnicodeFormatter(),
            new MarkoutWriterOptions { TextDiffContextLines = 0, NewLine = "\n" });

        writer.WriteTextDiff(Sample());
        var output = Normalize(writer.ToString());

        Assert.Contains("1      - if (value [-<-] 0)", output);
        Assert.Contains("1 + if (value {+<=+} 0)", output);
        Assert.Contains("↳ Warning — After line 1, columns 11-12: Includes zero", output);
    }

    [Fact]
    public void SpectreDiffUsesAnsiForSidesAndInnerSpans()
    {
        var writer = MarkoutWriter.Create(
            NewSpectreFormatter(),
            new MarkoutWriterOptions { TextDiffContextLines = 0, NewLine = "\n" });

        writer.WriteTextDiff(Sample());
        var output = writer.ToString();

        Assert.Contains("\u001b[31m", output);
        Assert.Contains("\u001b[32m", output);
        Assert.Contains("\u001b[7m<\u001b[27m", output);
        Assert.Contains("Includes zero", output);
    }

    [Fact]
    public void SpectreContainsSupplementaryFormatScalars()
    {
        var supplementaryFormat = char.ConvertFromUtf32(0xE0001);
        var diff = new MappedTextDiff(
            new TextDiffSequence(["old" + supplementaryFormat]),
            new TextDiffSequence(["new"]),
            [new TextDiffChange(new TextDiffRange(0, 1), new TextDiffRange(0, 1))]);
        var writer = MarkoutWriter.Create(NewSpectreFormatter());

        writer.WriteTextDiff(diff);
        var output = writer.ToString();

        Assert.Contains("\\U000E0001", output);
        Assert.DoesNotContain(supplementaryFormat, output, StringComparison.Ordinal);
    }

    [Fact]
    public void WriteTextDiffReturnsFalseForUnsupportedFormatter()
    {
        var writer = MarkoutWriter.Create(new TextDiffLessFormatter());

        Assert.False(writer.WriteTextDiff(Sample()));
    }

    [Fact]
    public void MarkoutShapeAllIncludesMappedTextDiffs()
    {
        Assert.True(MarkoutShape.All.HasFlag(MarkoutShape.TextDiffs));
    }

    internal static MappedTextDiff Sample() => new(
        new TextDiffSequence(
            ["if (value < 0)", "return 0;"],
            "Before",
            TextDiffLineTerminator.Present),
        new TextDiffSequence(
            ["if (value <= 0)", "return 1;"],
            "After",
            TextDiffLineTerminator.Present),
        [
            new TextDiffChange(
                new TextDiffRange(0, 2),
                new TextDiffRange(0, 2),
                [
                    new TextDiffInnerMapping(
                        new TextDiffSpan(0, 10, 1),
                        new TextDiffSpan(0, 10, 2)),
                    new TextDiffInnerMapping(
                        new TextDiffSpan(1, 7, 1),
                        new TextDiffSpan(1, 7, 1))
                ],
                [
                    TextDiffAnnotation.ForSpan(
                        TextDiffSide.After,
                        new TextDiffSpan(0, 10, 2),
                        "Includes zero",
                        CalloutSeverity.Warning)
                ])
        ]);

    private static SpectreFormatter NewSpectreFormatter()
        => new(AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(TextWriter.Null)
        }));

    private static string Normalize(string value)
        => value.Replace("\r\n", "\n").TrimEnd();

    private static string RenderMarkdown(MappedTextDiff diff, int? contextLines)
    {
        var writer = MarkoutWriter.Create(
            new MarkdownFormatter(),
            new MarkoutWriterOptions
            {
                TextDiffContextLines = contextLines,
                NewLine = "\n"
            });
        writer.WriteTextDiff(diff);
        return Normalize(writer.ToString());
    }

    private sealed class TextDiffLessFormatter : IMarkoutFormatter;
}
