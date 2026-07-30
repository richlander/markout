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
    public void FlushingTwice_DoesNotEmitTheDocumentTwice()
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
    /// Reading the result and then flushing are two emit paths, and the second must not
    /// repeat what the first already wrote. This reads the result — the earlier version
    /// of this test flushed twice and never called ToString, so it covered the same path
    /// as the test above rather than the interaction it is named for.
    /// </summary>
    [Fact]
    public void FlushingAfterReadingTheResult_DoesNotEmitItTwice()
    {
        var writer = new MarkoutWriter(
            new MarkdownFormatter(),
            new MarkoutWriterOptions { SectionOrder = ["Gamma"] });
        WriteDocument(writer);

        var read = writer.ToString();
        writer.Flush();

        Assert.Equal(1, CountOccurrences(read, "g1"));
        Assert.Equal(read, writer.ToString());
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

    /// <summary>
    /// A repeated name has to resolve to one rank, and the first one wins. The list has
    /// to interleave to show that: ["Gamma", "Alpha", "Gamma"] puts Gamma first under
    /// first-wins and last under last-wins, where ["Gamma", "Gamma", "Alpha"] reads the
    /// same either way and would pass without deciding anything.
    /// </summary>
    [Fact]
    public void ARepeatedNameInTheOrder_TakesItsFirstPosition()
    {
        var output = Render(new MarkoutWriterOptions { SectionOrder = ["Gamma", "Alpha", "Gamma"] });

        Assert.True(PositionOf(output, "g1") < PositionOf(output, "a1"));
        Assert.True(PositionOf(output, "a1") < PositionOf(output, "b1"));
    }

    [Fact]
    public void ANullNameInTheOrder_IsNotAnError()
    {
        var output = Render(new MarkoutWriterOptions { SectionOrder = ["Gamma", null!] });

        Assert.True(PositionOf(output, "g1") < PositionOf(output, "a1"));
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

    // ── Every block kind, at a seam ──

    /// <summary>
    /// Sweeps the block a section opens with, and the block the section before it ends
    /// with, across every kind the writer can emit.
    ///
    /// <para>
    /// This is the third fixture gap on this change, and the same gap each time: the
    /// assertions were right and the documents they compared were too alike to tell.
    /// Comparing marker positions missed layout; writing a preamble before every
    /// section made a misattributed separator into a uniform prefix; giving every
    /// section the same shape made a positional separator vector indistinguishable from
    /// a computed one. Each fix widened the oracle and the next defect walked in through
    /// whatever the oracle still held constant.
    /// </para>
    ///
    /// <para>
    /// What it held constant here was the kind of block. A heading separates itself from
    /// whatever precedes it, but so do a quotation, a rule, a callout, an array and a
    /// description list — and treating headings as the only self-separating opening lost
    /// their separator at a seam. An explicit <c>WriteBlankLine</c> is the opposite case:
    /// it is the seam's separator arriving as content, and computing another one doubles
    /// it. Both were found by a reviewer, in a fixture that wrote paragraphs, tables and
    /// fields.
    /// </para>
    /// </summary>
    public static TheoryData<string, string, bool, bool> BlockKindCases()
    {
        string[] kinds =
        [
            "paragraph", "field", "table", "streaming-table", "list", "quotation",
            "array", "rule", "callout", "descriptions", "breakdown", "code",
            "blank-line", "sub-heading"
        ];

        var data = new TheoryData<string, string, bool, bool>();
        foreach (var opening in kinds)
            foreach (var closing in kinds)
                foreach (var markdown in (bool[])[true, false])
                    foreach (var preamble in (bool[])[true, false])
                        data.Add(opening, closing, markdown, preamble);
        return data;
    }

    [Theory]
    [MemberData(nameof(BlockKindCases))]
    public void ASectionOpeningWithAnyBlockKind_MatchesOneWrittenInThatOrder(
        string opening, string closing, bool markdown, bool preamble)
    {
        // Alpha ends with the closing kind and Beta opens with the opening kind, so
        // reordering to Beta, Alpha puts a pair that were never adjacent at a seam —
        // which is the only place the separator rule can be wrong.
        string[] order = ["Beta", "Alpha"];

        static string Write(bool markdown, bool preamble, string[]? order, string[] names, string opening, string closing)
        {
            var options = new MarkoutWriterOptions();
            if (order != null)
                options.SectionOrder = order;

            IMarkoutFormatter formatter = markdown ? new MarkdownFormatter() : new TableFormatter();
            var writer = new MarkoutWriter(formatter, options);

            if (preamble)
                writer.WriteParagraph("preamble");

            foreach (var name in names)
            {
                writer.WriteSectionStart(2, name, headless: true);
                WriteBlock(writer, name == "Beta" ? opening : closing, name.ToLowerInvariant());
            }

            return writer.ToString();
        }

        var ordered = Write(markdown, preamble, order, ["Alpha", "Beta"], opening, closing);
        var native = Write(markdown, preamble, null, ["Beta", "Alpha"], opening, closing);

        Assert.Equal(native, ordered);
    }

    private static void WriteBlock(MarkoutWriter writer, string kind, string marker)
    {
        switch (kind)
        {
            case "paragraph":
                writer.WriteParagraph(marker);
                break;
            case "field":
                writer.WriteField("owner", marker);
                break;
            case "table":
                writer.WriteTable(["Name"], [[marker]]);
                break;
            case "streaming-table":
                writer.WriteTableStart("Name");
                writer.WriteTableRow(marker);
                writer.WriteTableEnd();
                break;
            case "list":
                writer.WriteList(marker, marker + "2");
                break;
            case "quotation":
                writer.WriteQuotation(marker);
                break;
            case "array":
                writer.WriteArray("items", marker, marker + "2");
                break;
            case "rule":
                writer.WriteRule();
                break;
            case "callout":
                writer.WriteCallout(CalloutSeverity.Note, marker);
                break;
            case "descriptions":
                writer.WriteDescriptions([new Description(marker, "text")]);
                break;
            case "breakdown":
                writer.WriteBreakdown([new Breakdown(marker, [new Slice("a", 1)])]);
                break;
            case "code":
                writer.WriteCodeStart("text");
                writer.WriteParagraph(marker);
                writer.WriteCodeEnd();
                break;
            case "blank-line":
                writer.WriteBlankLine();
                writer.WriteField("owner", marker);
                break;
            case "sub-heading":
                writer.WriteHeading(3, marker);
                writer.WriteField("owner", marker);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown block kind.");
        }
    }

    // ── Transparency ──

    /// <summary>
    /// A target whose newline changes partway through gets the line ending it had at
    /// each point, buffered or not. Separators are the part that can drift: they are
    /// dropped where they were written and put back at flush, which may be long after
    /// the target's newline moved on, so re-reading it there would rewrite line endings
    /// chosen earlier. The newline follows the section name rather than its position,
    /// so the document written natively in the requested order is a real oracle.
    /// </summary>
    [Theory]
    [MemberData(nameof(ReorderCases))]
    public void ANewlineChangedMidDocument_SurvivesReordering(
        MarkoutTableMode mode, string[] order, bool preamble, bool uniformShapes)
    {
        _ = mode;
        _ = uniformShapes;

        static string Write(string[]? order, IReadOnlyList<string> names, bool preamble)
        {
            var target = new StringWriter { NewLine = "<0>" };
            var options = new MarkoutWriterOptions();
            if (order != null)
                options.SectionOrder = order;

            var writer = MarkoutWriter.Create(target, new MarkdownFormatter(), options);
            if (preamble)
            {
                writer.WriteHeading(1, "Title");
                writer.WriteParagraph("preamble");
            }

            foreach (var name in names)
            {
                // Twice per section, straddling the boundary. A section with a heading
                // writes it immediately, so the separator ahead of it uses the newline
                // in force at the section start; a headless one writes nothing until its
                // first block, by which point the newline has moved. Both spellings have
                // to reach back to the right one, so the fixture contains both.
                target.NewLine = $"<{name}-open>";
                writer.WriteSectionStart(2, name, headless: name != "Beta");
                target.NewLine = $"<{name}-body>";
                writer.WriteParagraph(name.ToLowerInvariant());
            }

            writer.Flush();
            return target.ToString();
        }

        Assert.Equal(Write(null, Reorder(order), preamble), Write(order, Sections, preamble));
    }

    // ── A flush that emits nothing must change nothing ──

    /// <summary>
    /// A section can be open and still have rendered nothing: a projection defers its
    /// heading until the first block, a headless section never writes one, and a
    /// streaming table holds its rows until it is ended. Flushing there emits an empty
    /// document, and if that flush tore down the section state anyway, everything
    /// written afterwards would land in the preamble — unorderable, and silently so,
    /// because a preamble is exactly what a document that has not begun a section looks
    /// like. So a flush that writes nothing leaves the writer exactly as it found it.
    /// </summary>
    [Theory]
    [InlineData("projection")]
    [InlineData("headless")]
    [InlineData("streaming")]
    public void AFlushThatEmitsNothing_LeavesOrderingIntact(string kind)
    {
        Assert.Equal(WriteInterrupted(kind, null), WriteInterrupted(kind, ["Beta", "Alpha"]));
    }

    /// <summary>
    /// Writes Alpha then Beta with a <c>Flush()</c> after Alpha has begun but before it
    /// has rendered anything. With <paramref name="order"/> set the result must match
    /// the same document written natively in that order, which is what the unordered
    /// arm produces by writing Beta first.
    /// </summary>
    private static string WriteInterrupted(string kind, string[]? order)
    {
        var options = new MarkoutWriterOptions();
        if (order != null)
            options.SectionOrder = order;
        if (kind == "projection")
            options.Projection = MarkoutProjection.WithoutColumns("nothing");

        // A streaming Markdown table writes its header row straight through, so it
        // could not stand for a section that has rendered nothing; TableFormatter holds
        // rows back to size its columns, which is the case in question.
        IMarkoutFormatter formatter = kind == "streaming" ? new TableFormatter() : new MarkdownFormatter();
        var writer = new MarkoutWriter(formatter, options);
        string[] names = order == null ? ["Beta", "Alpha"] : ["Alpha", "Beta"];

        // The unordered arm writes Beta whole, so only the ordered arm can be
        // interrupted mid-section; interrupting both would compare two documents that
        // took the same path and prove nothing.
        var interrupt = order != null;

        foreach (var name in names)
        {
            var marker = name.ToLowerInvariant();

            if (kind == "streaming")
            {
                writer.WriteSectionStart(2, name, headless: true);
                writer.WriteTableStart("Name");
                writer.WriteTableRow($"{marker}1");
                if (interrupt && name == "Alpha")
                    writer.Flush();
                writer.WriteTableEnd();
            }
            else
            {
                writer.WriteSectionStart(2, name, headless: kind == "headless");
                if (interrupt && name == "Alpha")
                    writer.Flush();
                writer.WriteField("owner", marker);
            }
        }

        return writer.ToString();
    }

    /// <summary>
    /// The companion to the case above, and the reason it is not simply "flushing never
    /// finalizes". Once a section has rendered anything, the flush writes it out and the
    /// document really is committed — a section beginning afterwards could no longer be
    /// moved ahead of it. That case has to keep throwing, or the escape hatch for an
    /// unrendered section would quietly become a licence to reorder what has already
    /// left the building.
    /// </summary>
    [Fact]
    public void AFlushThatEmitsContent_StillRefusesLaterSections()
    {
        var writer = new MarkoutWriter(
            new MarkdownFormatter(),
            new MarkoutWriterOptions { SectionOrder = ["Beta", "Alpha"] });

        writer.WriteSectionStart(2, "Alpha");
        writer.WriteTableStart("Name");
        writer.WriteTableRow("alpha1");
        writer.Flush();

        Assert.Throws<InvalidOperationException>(() => writer.WriteSectionStart(2, "Beta"));
    }

    // ── Reordering is a reordering ──

    /// <summary>
    /// The <c>preamble</c> dimension is not decoration. With content before the first
    /// section, every section is preceded by a separator, so a separator wrongly
    /// captured with the section that follows it becomes a uniform prefix and the
    /// document still reads correctly. Without a preamble the first section has no
    /// separator and the rest do, so a captured separator surfaces as a document that
    /// opens on a blank line. Only the second case can see that defect.
    /// </summary>
    public static TheoryData<MarkoutTableMode, string[], bool, bool> ReorderCases()
    {
        string[][] orders =
        [
            [], ["Alpha", "Beta", "Gamma"], ["Gamma"], ["Gamma", "Alpha"],
            ["Beta", "Alpha"], ["Gamma", "Beta", "Alpha"], ["Nope", "Beta"],
            ["beta", "ALPHA"]
        ];

        var data = new TheoryData<MarkoutTableMode, string[], bool, bool>();
        foreach (var mode in Enum.GetValues<MarkoutTableMode>())
            foreach (var order in orders)
                foreach (var preamble in (bool[])[true, false])
                    foreach (var uniformShapes in (bool[])[true, false])
                        data.Add(mode, order, preamble, uniformShapes);
        return data;
    }

    private static readonly string[] Sections = ["Alpha", "Beta", "Gamma"];

    /// <summary>
    /// Writes the three sections, either all the same shape or deliberately not.
    ///
    /// <para>
    /// The <paramref name="uniformShapes"/> dimension is what makes the seam rule
    /// testable. Whether a blank line belongs between two blocks depends on what they
    /// are — with <c>TableFormatter</c> a table is set off by one and consecutive
    /// fields are not — so when every section has the same shape every seam gets the
    /// same answer and any positional replay of those answers is indistinguishable from
    /// computing them. Giving the middle section a table and the outer two a bare field
    /// makes the answers differ, so moving the table has to move the blank lines with
    /// it.
    /// </para>
    /// </summary>
    private static void WriteSections(
        MarkoutWriter writer,
        IReadOnlyList<string> names,
        bool preamble = true,
        bool uniformShapes = true)
    {
        if (preamble)
        {
            writer.WriteHeading(1, "Title");
            writer.WriteParagraph("preamble");
        }

        foreach (var name in names)
        {
            var marker = name.ToLowerInvariant();
            writer.WriteSectionStart(2, name);

            if (uniformShapes)
            {
                writer.WriteParagraph($"{marker} intro");
                writer.WriteTable(["Name"], [[$"{marker}1"], [$"{marker}2"]]);
                writer.WriteField("owner", marker);
                continue;
            }

            // Shape follows the name, not the position, so the oracle writing the same
            // names in a different order writes the same shapes.
            if (name == "Beta")
                writer.WriteTable(["Name"], [[$"{marker}1"], [$"{marker}2"]]);
            else
                writer.WriteField("owner", marker);
        }
    }

    /// <summary>
    /// The independent oracle for the tests below: the sequence the requested order
    /// asks for, computed by hand. It is deliberately not the production ordering code
    /// — the claim under test is about rendering, not about sequencing, and on three
    /// sections the expected sequence is short enough to be obviously right.
    /// </summary>
    private static string[] Reorder(string[] order)
    {
        var rank = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < order.Length; i++)
            rank.TryAdd(order[i], i);

        return [.. Sections
            .Select((name, ordinal) => (name, ordinal))
            .OrderBy(s => rank.TryGetValue(s.name, out var r) ? r : int.MaxValue)
            .ThenBy(s => s.ordinal)
            .Select(s => s.name)];
    }

    /// <summary>
    /// The load-bearing claim, and the one that byte-identity alone does not reach:
    /// reordering a document must produce the document that would have been written in
    /// that order. Asserting only that markers appear in the right relative positions
    /// lets layout rot go unnoticed — a blank line captured with the section that
    /// follows it travels when that section moves, which leaves the document opening on
    /// a blank line and the new seam with no separation at all. Comparing whole
    /// documents catches that; comparing marker positions does not.
    /// </summary>
    [Theory]
    [MemberData(nameof(ReorderCases))]
    public void AReorderedDocument_MatchesOneWrittenInThatOrder(
        MarkoutTableMode mode, string[] order, bool preamble, bool uniformShapes)
    {
        var ordered = new MarkoutWriter(
            new TableFormatter(),
            new MarkoutWriterOptions { TableMode = mode, SectionOrder = order });
        WriteSections(ordered, Sections, preamble, uniformShapes);

        var native = new MarkoutWriter(new TableFormatter(), new MarkoutWriterOptions { TableMode = mode });
        WriteSections(native, Reorder(order), preamble, uniformShapes);

        Assert.Equal(native.ToString(), ordered.ToString());
    }

    [Theory]
    [MemberData(nameof(ReorderCases))]
    public void AReorderedMarkdownDocument_MatchesOneWrittenInThatOrder(
        MarkoutTableMode mode, string[] order, bool preamble, bool uniformShapes)
    {
        _ = mode;

        var ordered = new MarkoutWriter(new MarkdownFormatter(), new MarkoutWriterOptions { SectionOrder = order });
        WriteSections(ordered, Sections, preamble, uniformShapes);

        var native = new MarkoutWriter(new MarkdownFormatter());
        WriteSections(native, Reorder(order), preamble, uniformShapes);

        Assert.Equal(native.ToString(), ordered.ToString());
    }

    /// <summary>
    /// JSONL has no headings, so a moved section carries no marker with it — but it can
    /// still carry a blank line, and a blank first line is an empty record rather than
    /// cosmetic damage. Reordering must not add one.
    /// </summary>
    [Fact]
    public void AReorderedJsonlDocument_DoesNotOpenWithAnEmptyRecord()
    {
        var writer = new MarkoutWriter(
            new TableFormatter(),
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, SectionOrder = ["Gamma", "Alpha"] });
        WriteSections(writer, Sections, preamble: false);

        var output = writer.ToString();

        Assert.False(output.StartsWith('\n'), $"Output opens with a blank record:\n{output}");
        Assert.StartsWith("{", output, StringComparison.Ordinal);
    }

    // ── The cost of ordering, and who pays it ──

    /// <summary>
    /// Ordering wraps the caller's writer, and a wrapper that is not transparent is a
    /// regression for every caller who never asked for one. Flushing has to reach the
    /// target exactly once whether or not an order was requested.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Flushing_ReachesTheTargetOnce(bool ordered)
    {
        var target = new CountingWriter();
        var options = ordered ? new MarkoutWriterOptions { SectionOrder = ["Gamma"] } : new MarkoutWriterOptions();
        var writer = MarkoutWriter.Create(target, new MarkdownFormatter(), options);
        WriteSections(writer, Sections);

        writer.Flush();

        Assert.Equal(1, target.Flushes);
    }

    /// <summary>
    /// Ordering buffers the whole document, so emitting it is the end of it: a section
    /// written afterwards could no longer move ahead of one already written out. Saying
    /// so is the point — the alternative is a document that silently comes out in an
    /// order nobody asked for.
    /// </summary>
    [Fact]
    public void WritingAfterTheDocumentWasEmitted_Throws()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(
            sw,
            new MarkdownFormatter(),
            new MarkoutWriterOptions { SectionOrder = ["Beta", "Alpha"] });
        writer.WriteSectionStart(2, "Alpha");
        writer.WriteParagraph("a1");
        writer.Flush();

        Assert.Throws<InvalidOperationException>(() => writer.WriteParagraph("a2"));
    }

    [Fact]
    public void WritingAfterAnEmptyDocumentWasFlushed_IsAllowed()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(
            sw,
            new MarkdownFormatter(),
            new MarkoutWriterOptions { SectionOrder = ["Beta", "Alpha"] });

        writer.Flush();
        WriteSections(writer, Sections);
        writer.Flush();

        Assert.True(PositionOf(sw.ToString(), "beta1") < PositionOf(sw.ToString(), "alpha1"));
    }

    /// <summary>
    /// ToString is what a debugger calls, and against a target it cannot read from it
    /// has nothing to return anyway. Emitting there would commit the document as a side
    /// effect of being looked at.
    /// </summary>
    [Fact]
    public void ReadingTheResultOfAWriterItCannotRead_DoesNotEmitAnything()
    {
        var target = new CountingWriter();
        var writer = MarkoutWriter.Create(
            target,
            new MarkdownFormatter(),
            new MarkoutWriterOptions { SectionOrder = ["Gamma", "Alpha"] });
        WriteSections(writer, Sections);

        _ = writer.ToString();
        Assert.Equal(0, target.Written.Length);

        writer.Flush();
        Assert.True(PositionOf(target.Written.ToString(), "gamma1") < PositionOf(target.Written.ToString(), "alpha1"));
    }

    // ── Transparency of the wrapper ──

    [Fact]
    public void ANewlineChosenAfterTheWriterWasBuilt_StillReachesTheOutput()
    {
        static string Render(bool ordered)
        {
            var sw = new StringWriter();
            var options = ordered ? new MarkoutWriterOptions { SectionOrder = ["Alpha"] } : new MarkoutWriterOptions();
            var writer = MarkoutWriter.Create(sw, new MarkdownFormatter(), options);
            sw.NewLine = "\r\n";
            WriteSections(writer, Sections);
            writer.Flush();
            return sw.ToString();
        }

        Assert.Equal(Render(false), Render(true));
        Assert.Contains("\r\n", Render(true), StringComparison.Ordinal);
    }

    [Fact]
    public void TheTargetsFormatProvider_IsNotReplacedByTheWrapper()
    {
        var target = new StringWriter(System.Globalization.CultureInfo.GetCultureInfo("fr-FR"));
        var writer = MarkoutWriter.Create(
            target,
            new MarkdownFormatter(),
            new MarkoutWriterOptions { SectionOrder = ["Alpha"] });
        WriteSections(writer, Sections);
        writer.Flush();

        Assert.Equal("fr-FR", Assert.IsType<System.Globalization.CultureInfo>(target.FormatProvider).Name);
    }

    /// <summary>
    /// Freezing options has to freeze the order too. Holding the reference would let a
    /// caller change what a frozen options object renders, which is the one thing
    /// freezing exists to prevent.
    /// </summary>
    [Fact]
    public void MutatingTheListAfterAssigningIt_DoesNotChangeTheOrder()
    {
        var live = new List<string> { "Gamma" };
        var options = new MarkoutWriterOptions { SectionOrder = live };
        options.MakeReadOnly();

        live.Clear();
        live.Add("Beta");

        var writer = new MarkoutWriter(new MarkdownFormatter(), options);
        WriteSections(writer, Sections);
        var output = writer.ToString();

        Assert.True(PositionOf(output, "gamma1") < PositionOf(output, "alpha1"));
    }

    private sealed class CountingWriter : TextWriter
    {
        public int Flushes;
        public readonly System.Text.StringBuilder Written = new();

        public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;
        public override void Write(char value) => Written.Append(value);
        public override void Write(string? value) => Written.Append(value);
        public override void Flush() => Flushes++;
        public override string ToString() => Written.ToString();
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
