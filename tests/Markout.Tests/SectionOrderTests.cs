using Markout;
using Markout.Formatting;

namespace Markout.Tests;

/// <summary>
/// Covers <see cref="MarkoutWriterOptions.SectionOrder"/>.
///
/// <para>
/// The claim under test is not "sections can be reordered" — string surgery on rendered
/// Markdown can do that. It is that ordering applied at the writer seam works for
/// formats whose output has no heading to reorder, and that asking for the order the
/// document already had changes nothing at all.
/// </para>
/// </summary>
public class SectionOrderTests
{
    public static TheoryData<MarkoutTableMode> AllModes()
    {
        var data = new TheoryData<MarkoutTableMode>();
        foreach (var mode in Enum.GetValues<MarkoutTableMode>())
            data.Add(mode);
        return data;
    }

    private static void WriteDocument(MarkoutWriter writer)
    {
        writer.WriteHeading(1, "Title");
        writer.WriteParagraph("preamble");
        writer.WriteSectionStart(2, "Alpha");
        writer.WriteTable(["Name"], [["a1"]]);
        writer.WriteSectionStart(2, "Beta");
        writer.WriteTable(["Name"], [["b1"]]);
        writer.WriteSectionStart(2, "Gamma");
        writer.WriteTable(["Name"], [["g1"]]);
    }

    private static string Render(MarkoutWriterOptions options, IMarkoutFormatter? formatter = null)
    {
        var writer = new MarkoutWriter(formatter ?? new MarkdownFormatter(), options);
        WriteDocument(writer);
        return writer.ToString();
    }

    private static string Render(MarkoutTableMode mode, MarkoutWriterOptions options)
    {
        options.TableMode = mode;
        return Render(options, new TableFormatter());
    }

    private static int PositionOf(string output, string marker)
    {
        var index = output.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(index >= 0, $"'{marker}' missing from output:\n{output}");
        return index;
    }

    // ── The ordering itself ──

    [Fact]
    public void NamedSections_LeadInTheOrderGiven()
    {
        var output = Render(new MarkoutWriterOptions { SectionOrder = ["Gamma", "Alpha"] });

        Assert.True(PositionOf(output, "g1") < PositionOf(output, "a1"));
        Assert.True(PositionOf(output, "a1") < PositionOf(output, "b1"));
    }

    /// <summary>
    /// The point of the seam. TSV and JSONL carry no heading, so reordering their
    /// sections by scanning rendered text is not possible at all — yet the writer knows
    /// exactly where each section began.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllModes))]
    public void EveryTableMode_InheritsTheOrder(MarkoutTableMode mode)
    {
        var output = Render(mode, new MarkoutWriterOptions { SectionOrder = ["Gamma", "Alpha"] });

        Assert.True(PositionOf(output, "g1") < PositionOf(output, "a1"));
        Assert.True(PositionOf(output, "a1") < PositionOf(output, "b1"));
    }

    [Fact]
    public void UnnamedSections_KeepTheOrderTheyWereWrittenIn()
    {
        var output = Render(new MarkoutWriterOptions { SectionOrder = ["Gamma"] });

        Assert.True(PositionOf(output, "g1") < PositionOf(output, "a1"));
        Assert.True(PositionOf(output, "a1") < PositionOf(output, "b1"));
    }

    [Fact]
    public void SectionNames_MatchWithoutRegardToCase()
    {
        var output = Render(new MarkoutWriterOptions { SectionOrder = ["gAmMa"] });

        Assert.True(PositionOf(output, "g1") < PositionOf(output, "a1"));
    }

    // ── Asking for what you already have ──

    /// <summary>
    /// The strongest available statement that ordering is a reordering and not a
    /// re-rendering: naming every section in the order it was written must reproduce the
    /// unordered document byte for byte, blank lines included.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllModes))]
    public void TheOrderTheDocumentAlreadyHad_ChangesNothing(MarkoutTableMode mode)
    {
        Assert.Equal(
            Render(mode, new MarkoutWriterOptions()),
            Render(mode, new MarkoutWriterOptions { SectionOrder = ["Alpha", "Beta", "Gamma"] }));
    }

    [Fact]
    public void TheOrderTheDocumentAlreadyHad_ChangesNothingInMarkdown()
    {
        Assert.Equal(
            Render(new MarkoutWriterOptions()),
            Render(new MarkoutWriterOptions { SectionOrder = ["Alpha", "Beta", "Gamma"] }));
    }

    [Fact]
    public void NamingOnlySectionsTheDocumentNeverWrote_ChangesNothing()
    {
        Assert.Equal(
            Render(new MarkoutWriterOptions()),
            Render(new MarkoutWriterOptions { SectionOrder = ["Nope", "AlsoNope"] }));
    }

    [Fact]
    public void AnEmptyOrder_ChangesNothing()
    {
        Assert.Equal(
            Render(new MarkoutWriterOptions()),
            Render(new MarkoutWriterOptions { SectionOrder = [] }));
    }

    // ── The preamble is not a section ──

    [Fact]
    public void ContentBeforeTheFirstSection_StaysFirst()
    {
        var output = Render(new MarkoutWriterOptions { SectionOrder = ["Gamma"] });

        Assert.True(PositionOf(output, "Title") < PositionOf(output, "preamble"));
        Assert.True(PositionOf(output, "preamble") < PositionOf(output, "g1"));
    }

    // ── Composition with section filtering ──

    [Fact]
    public void OrderingComposesWithSectionFiltering()
    {
        var output = Render(new MarkoutWriterOptions
        {
            IncludeSections = ["Alpha", "Gamma"],
            SectionOrder = ["Gamma", "Alpha"]
        });

        Assert.DoesNotContain("b1", output);
        Assert.True(PositionOf(output, "g1") < PositionOf(output, "a1"));
    }

    /// <summary>
    /// An excluded section still opens a buffer, so a stray write cannot be attributed to
    /// whichever section happened to precede it. Nothing writes while excluded today;
    /// this pins that the filtered document is unchanged by ordering being enabled.
    /// </summary>
    [Fact]
    public void FilteringAloneAndFilteringWithAnIdentityOrder_Agree()
    {
        Assert.Equal(
            Render(new MarkoutWriterOptions { IncludeSections = ["Alpha", "Gamma"] }),
            Render(new MarkoutWriterOptions
            {
                IncludeSections = ["Alpha", "Gamma"],
                SectionOrder = ["Alpha", "Gamma"]
            }));
    }

    // ── Emitting exactly once ──

    /// <summary>
    /// Ordering has to hold the whole document, so the flush is a real event rather than
    /// a no-op. Asking for the result twice must not emit it twice.
    /// </summary>
    [Fact]
    public void AskingForTheResultTwice_DoesNotEmitItTwice()
    {
        var writer = new MarkoutWriter(
            new MarkdownFormatter(),
            new MarkoutWriterOptions { SectionOrder = ["Gamma"] });
        WriteDocument(writer);

        var first = writer.ToString();
        var second = writer.ToString();

        Assert.Equal(first, second);
        Assert.Equal(1, CountOccurrences(second, "g1"));
    }

    [Fact]
    public void FlushingAfterReadingTheResult_DoesNotEmitItTwice()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(
            sw,
            new MarkdownFormatter(),
            new MarkoutWriterOptions { SectionOrder = ["Gamma"] });
        WriteDocument(writer);

        writer.Flush();
        writer.Flush();

        Assert.Equal(1, CountOccurrences(sw.ToString(), "g1"));
    }

    /// <summary>
    /// The buffer is invisible to a caller writing to its own TextWriter, but only if the
    /// document actually reaches that writer.
    /// </summary>
    [Fact]
    public void AnExternalWriter_ReceivesTheOrderedDocument()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(
            sw,
            new MarkdownFormatter(),
            new MarkoutWriterOptions { SectionOrder = ["Gamma", "Alpha"] });
        WriteDocument(writer);
        writer.Flush();

        var output = sw.ToString();
        Assert.True(PositionOf(output, "g1") < PositionOf(output, "a1"));
        Assert.True(PositionOf(output, "a1") < PositionOf(output, "b1"));
    }

    // ── Options plumbing ──

    [Fact]
    public void SectionOrder_CannotBeSetOnFrozenOptions()
    {
        var options = new MarkoutWriterOptions();
        options.MakeReadOnly();

        Assert.Throws<InvalidOperationException>(() => options.SectionOrder = ["Gamma"]);
    }

    /// <summary>
    /// The JSONL composite-cell path copies options mid-write to resolve identity columns.
    /// Ordering is read from the writer's own options rather than that copy, so this pins
    /// the path's output rather than the copy — the copy constructor's <c>SectionOrder</c>
    /// line upholds its stated "copies every setting" contract but has no observable
    /// effect today, and cannot be gated from tests because the copy is reachable only
    /// through an internal member and this assembly has no InternalsVisibleTo.
    /// </summary>
    [Fact]
    public void OrderingHoldsAcrossTheJsonlCompositeCellPath()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(
            sw,
            new TableFormatter(),
            new MarkoutWriterOptions
            {
                TableMode = MarkoutTableMode.Jsonl,
                SectionOrder = ["Gamma", "Alpha"]
            });

        writer.WriteSectionStart(2, "Alpha");
        writer.WriteCompositeTable(MarkoutCompositeRow.Scalar("a1", "1"));
        writer.WriteSectionStart(2, "Gamma");
        writer.WriteCompositeTable(MarkoutCompositeRow.Scalar("g1", "2"));
        writer.Flush();

        var output = sw.ToString();
        Assert.True(PositionOf(output, "g1") < PositionOf(output, "a1"));
    }

    // ── Repeated names ──

    [Fact]
    public void ARepeatedSectionName_KeepsBothOccurrencesInTheirOriginalOrder()
    {
        var writer = new MarkoutWriter(
            new MarkdownFormatter(),
            new MarkoutWriterOptions { SectionOrder = ["Repeat"] });

        writer.WriteSectionStart(2, "First");
        writer.WriteTable(["Name"], [["f1"]]);
        writer.WriteSectionStart(2, "Repeat");
        writer.WriteTable(["Name"], [["r1"]]);
        writer.WriteSectionStart(2, "Repeat");
        writer.WriteTable(["Name"], [["r2"]]);

        var output = writer.ToString();
        Assert.True(PositionOf(output, "r1") < PositionOf(output, "r2"));
        Assert.True(PositionOf(output, "r2") < PositionOf(output, "f1"));
    }

    [Fact]
    public void ARepeatedNameInTheOrder_IsNotAnError()
    {
        var output = Render(new MarkoutWriterOptions { SectionOrder = ["Gamma", "Gamma", "Alpha"] });

        Assert.True(PositionOf(output, "g1") < PositionOf(output, "a1"));
        Assert.True(PositionOf(output, "a1") < PositionOf(output, "b1"));
    }

    // ── Documents with nothing to order ──

    [Fact]
    public void ADocumentWithNoSections_IsUnchanged()
    {
        var ordered = new MarkoutWriter(
            new MarkdownFormatter(),
            new MarkoutWriterOptions { SectionOrder = ["Gamma"] });
        ordered.WriteHeading(1, "Title");
        ordered.WriteParagraph("just a preamble");

        var plain = new MarkoutWriter(new MarkdownFormatter());
        plain.WriteHeading(1, "Title");
        plain.WriteParagraph("just a preamble");

        Assert.Equal(plain.ToString(), ordered.ToString());
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }
        return count;
    }
}
