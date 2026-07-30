using Markout;
using Markout.Formatting;

namespace Markout.Tests;

/// <summary>
/// Covers <see cref="MarkoutRowWindow"/> resolution and its application at the
/// writer seam.
///
/// <para>
/// The point of windowing at the seam is that every table mode inherits it from
/// one resolution, so the coverage that matters here is not "a window works" but
/// "a window works identically in <em>all</em> of them". A pass that only held
/// for one mode is the defect this feature exists to remove, so the emission
/// tests are Theories over <see cref="MarkoutTableMode"/> rather than a Fact
/// about whichever mode was convenient.
/// </para>
/// </summary>
public class RowWindowTests
{
    private static readonly string[] Header = ["Name"];

    private static List<string[]> Rows(int count)
    {
        var rows = new List<string[]>(count);
        for (var i = 1; i <= count; i++)
            rows.Add([$"r{i}"]);
        return rows;
    }

    private static string Render(MarkoutWriterOptions options, IList<string[]> rows)
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter(), options);
        writer.WriteTable(Header, rows);
        return sw.ToString();
    }

    private static string RenderStreaming(MarkoutWriterOptions options, IList<string[]> rows)
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter(), options);
        writer.WriteTableStart(Header);
        foreach (var row in rows)
            writer.WriteTableRow(row);
        writer.WriteTableEnd();
        return sw.ToString();
    }

    public static TheoryData<MarkoutTableMode> AllModes =>
        new(Enum.GetValues<MarkoutTableMode>());

    // ── Resolution ──

    [Fact]
    public void Head_KeepsLeadingRows()
        => Assert.Equal((0, 2), MarkoutRowWindow.Head(2).Resolve(5));

    [Fact]
    public void Head_ClampsToTheRowsThatExist()
        => Assert.Equal((0, 3), MarkoutRowWindow.Head(10).Resolve(3));

    [Fact]
    public void Tail_KeepsTrailingRows()
        => Assert.Equal((3, 5), MarkoutRowWindow.Tail(2).Resolve(5));

    [Fact]
    public void Tail_ClampsToTheRowsThatExist()
        => Assert.Equal((0, 3), MarkoutRowWindow.Tail(10).Resolve(3));

    [Fact]
    public void Range_IsOneBasedAndInclusive()
        => Assert.Equal((1, 3), MarkoutRowWindow.Range(2, 3).Resolve(5));

    [Fact]
    public void Range_WithoutAnEnd_RunsToTheLastRow()
        => Assert.Equal((1, 5), MarkoutRowWindow.Range(2, null).Resolve(5));

    /// <summary>
    /// An absolute range names row numbers, so a range past the end is not an
    /// error — the rows it names are simply absent. Returning an empty window
    /// rather than throwing is what lets a caller window a short table without
    /// first checking how tall it is.
    /// </summary>
    [Fact]
    public void Range_PastTheEnd_ResolvesEmptyRatherThanThrowing()
        => Assert.Equal((3, 3), MarkoutRowWindow.Range(9, 12).Resolve(3));

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void ANegativeCount_MeansNoLimit(int count)
    {
        Assert.True(MarkoutRowWindow.Head(count).IsUnlimited);
        Assert.True(MarkoutRowWindow.Tail(count).IsUnlimited);
        Assert.Equal((0, 4), MarkoutRowWindow.Head(count).Resolve(4));
        Assert.Equal((0, 4), MarkoutRowWindow.Tail(count).Resolve(4));
    }

    /// <summary>
    /// A range always names a bounded start, so it is never "unlimited" even
    /// when open-ended. If this regressed, an open-ended range would be skipped
    /// entirely by the <c>IsUnlimited</c> fast path and silently keep every row.
    /// </summary>
    [Fact]
    public void AnOpenEndedRange_IsNotUnlimited()
        => Assert.False(MarkoutRowWindow.Range(2, null).IsUnlimited);

    [Fact]
    public void Range_RefusesAnEndBeforeItsStart()
        => Assert.Throws<ArgumentOutOfRangeException>(() => MarkoutRowWindow.Range(3, 2));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Range_RefusesAStartBeforeTheFirstRow(int start)
        => Assert.Throws<ArgumentOutOfRangeException>(() => MarkoutRowWindow.Range(start, null));

    [Fact]
    public void Resolve_RefusesANegativeRowCount()
        => Assert.Throws<ArgumentOutOfRangeException>(() => MarkoutRowWindow.Head(1).Resolve(-1));

    /// <summary>
    /// Callers use the resolved pair to slice without re-clamping, so the
    /// ordering and bounds invariant is the contract, not an implementation
    /// detail. Swept rather than spot-checked because the three kinds clamp on
    /// different sides.
    /// </summary>
    [Fact]
    public void Resolve_AlwaysReturnsAValidRange()
    {
        for (var dataCount = 0; dataCount <= 6; dataCount++)
        {
            foreach (var window in AllWindows())
            {
                var (keepStart, keepEnd) = window.Resolve(dataCount);
                Assert.True(
                    0 <= keepStart && keepStart <= keepEnd && keepEnd <= dataCount,
                    $"{window.Kind} over {dataCount} rows resolved to ({keepStart}, {keepEnd})");
            }
        }

        static IEnumerable<MarkoutRowWindow> AllWindows()
        {
            for (var n = -1; n <= 7; n++)
            {
                yield return MarkoutRowWindow.Head(n);
                yield return MarkoutRowWindow.Tail(n);
            }
            for (var start = 1; start <= 7; start++)
            {
                yield return MarkoutRowWindow.Range(start, null);
                for (var end = start; end <= 7; end++)
                    yield return MarkoutRowWindow.Range(start, end);
            }
        }
    }

    // ── Emission, in every table mode ──

    [Theory]
    [MemberData(nameof(AllModes))]
    public void EveryTableMode_HonorsAHeadWindow(MarkoutTableMode mode)
    {
        var output = Render(
            new MarkoutWriterOptions { TableMode = mode, RowWindow = MarkoutRowWindow.Head(2) },
            Rows(5));

        Assert.Contains("r1", output);
        Assert.Contains("r2", output);
        Assert.DoesNotContain("r3", output);
        Assert.DoesNotContain("r5", output);
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public void EveryTableMode_HonorsATailWindow(MarkoutTableMode mode)
    {
        var output = Render(
            new MarkoutWriterOptions { TableMode = mode, RowWindow = MarkoutRowWindow.Tail(2) },
            Rows(5));

        Assert.DoesNotContain("r1", output);
        Assert.DoesNotContain("r3", output);
        Assert.Contains("r4", output);
        Assert.Contains("r5", output);
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public void EveryTableMode_HonorsARangeWindow(MarkoutTableMode mode)
    {
        var output = Render(
            new MarkoutWriterOptions { TableMode = mode, RowWindow = MarkoutRowWindow.Range(2, 3) },
            Rows(5));

        Assert.DoesNotContain("r1", output);
        Assert.Contains("r2", output);
        Assert.Contains("r3", output);
        Assert.DoesNotContain("r4", output);
    }

    /// <summary>
    /// A window is selection, not summarization: its output has to stay
    /// machine-consumable. An ellipsis appended to JSONL is a malformed record,
    /// and a windowed table's row count must equal what a caller computing the
    /// same window would report — which an ellipsis row breaks.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllModes))]
    public void AWindow_ReportsNoEllipsisAndNoSkippedCount(MarkoutTableMode mode)
    {
        var output = Render(
            new MarkoutWriterOptions { TableMode = mode, RowWindow = MarkoutRowWindow.Head(2) },
            Rows(5));

        Assert.DoesNotContain("more", output);
        Assert.DoesNotContain("...", output);
    }

    /// <summary>
    /// The negative control for the test above: MaxItems on the same data does
    /// announce what it dropped. Without this, "no ellipsis" would also pass on a
    /// build where ellipsis reporting was broken outright.
    /// </summary>
    [Fact]
    public void MaxItems_StillAnnouncesWhatItDropped()
    {
        var output = Render(new MarkoutWriterOptions { MaxItems = 2 }, Rows(5));

        Assert.Contains("... and 3 more", output);
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public void AWindowPastTheEnd_KeepsTheHeaderAndEmitsNoRows(MarkoutTableMode mode)
    {
        var output = Render(
            new MarkoutWriterOptions { TableMode = mode, RowWindow = MarkoutRowWindow.Range(9, 12) },
            Rows(3));

        Assert.DoesNotContain("r1", output);
        Assert.DoesNotContain("r3", output);

        // The header survives an empty window in every mode that has one. TSV
        // and JSONL render headers as stable names, so this is deliberately
        // case-insensitive; JSONL emits records only and has no header row.
        if (mode != MarkoutTableMode.Jsonl)
            Assert.Contains("name", output, StringComparison.OrdinalIgnoreCase);
    }

    // ── Interaction with MaxItems ──

    /// <summary>
    /// The window selects and MaxItems then caps the selection, so the ellipsis
    /// counts only what the cap dropped. Were the order reversed, MaxItems would
    /// cap rows 1-2 and the tail window would then select from those, yielding
    /// r2 instead of r4.
    /// </summary>
    [Fact]
    public void AWindowSelectsFirst_AndMaxItemsCapsTheSelection()
    {
        var output = Render(
            new MarkoutWriterOptions { RowWindow = MarkoutRowWindow.Tail(3), MaxItems = 2 },
            Rows(6));

        Assert.Contains("r4", output);
        Assert.Contains("r5", output);
        Assert.DoesNotContain("r6", output);
        Assert.DoesNotContain("r2", output);
        Assert.Contains("... and 1 more", output);
    }

    [Fact]
    public void AWindowNarrowerThanMaxItems_LeavesNothingForTheCapToReport()
    {
        var output = Render(
            new MarkoutWriterOptions { RowWindow = MarkoutRowWindow.Head(2), MaxItems = 10 },
            Rows(6));

        Assert.Contains("r2", output);
        Assert.DoesNotContain("r3", output);
        Assert.DoesNotContain("more", output);
    }

    // ── The streaming seam ──

    /// <summary>
    /// <see cref="TableFormatter"/> is batch-only, so the tests above exercise
    /// buffered emission however rows are handed in. <see cref="MarkdownFormatter"/>
    /// is the streaming formatter, and without a window it writes each row
    /// straight through — which is the path a window has to interrupt.
    /// </summary>
    private static string RenderMarkdown(MarkoutWriterOptions options, IList<string[]> rows)
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new MarkdownFormatter(), options);
        writer.WriteTable(Header, rows);
        return sw.ToString();
    }

    private static string RenderMarkdownStreaming(MarkoutWriterOptions options, IList<string[]> rows)
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new MarkdownFormatter(), options);
        writer.WriteTableStart(Header);
        foreach (var row in rows)
            writer.WriteTableRow(row);
        writer.WriteTableEnd();
        return sw.ToString();
    }

    [Fact]
    public void Markdown_HonorsAWindow()
    {
        var output = RenderMarkdown(
            new MarkoutWriterOptions { RowWindow = MarkoutRowWindow.Range(2, 3) },
            Rows(5));

        Assert.DoesNotContain("r1", output);
        Assert.Contains("r2", output);
        Assert.Contains("r3", output);
        Assert.DoesNotContain("r4", output);
    }

    /// <summary>
    /// The gate on forced buffering. Rows arriving one at a time cannot answer
    /// "which are the last two" until the last one lands, so a window has to stop
    /// the streaming formatter from writing rows straight through. Without that,
    /// this emits all five rows and no window is applied at all.
    /// </summary>
    [Fact]
    public void MarkdownStreamedRows_HonorATailWindow()
    {
        var options = new MarkoutWriterOptions { RowWindow = MarkoutRowWindow.Tail(2) };
        var output = RenderMarkdownStreaming(options, Rows(5));

        Assert.DoesNotContain("r1", output);
        Assert.DoesNotContain("r3", output);
        Assert.Contains("r4", output);
        Assert.Contains("r5", output);
        Assert.Equal(RenderMarkdown(options, Rows(5)), output);
    }

    [Fact]
    public void MarkdownStreamedRows_AgreeWithBatchedRowsForEveryWindowKind()
    {
        foreach (var window in AllWindowKinds())
        {
            var options = new MarkoutWriterOptions { RowWindow = window };
            Assert.Equal(
                RenderMarkdown(options, Rows(5)),
                RenderMarkdownStreaming(options, Rows(5)));
        }
    }

    /// <summary>
    /// The gate on deferring MaxItems while a window is active. Capping rows as
    /// they arrive would cap the wrong set: here the cap must apply to the three
    /// rows the tail selected, not to the first two rows that happened to show up.
    /// </summary>
    [Fact]
    public void MarkdownStreamedRows_CapTheSelectionRatherThanTheArrivals()
    {
        var output = RenderMarkdownStreaming(
            new MarkoutWriterOptions { RowWindow = MarkoutRowWindow.Tail(3), MaxItems = 2 },
            Rows(6));

        Assert.Contains("r4", output);
        Assert.Contains("r5", output);
        Assert.DoesNotContain("r1", output);
        Assert.DoesNotContain("r6", output);
        Assert.Contains("... and 1 more", output);
    }

    /// <summary>
    /// The negative control for deferral: without a window MaxItems still caps as
    /// rows arrive, so the streaming fast path keeps its existing behavior.
    /// </summary>
    [Fact]
    public void MarkdownStreamedRows_WithoutAWindow_StillCapAsTheyArrive()
    {
        var options = new MarkoutWriterOptions { MaxItems = 2 };
        var output = RenderMarkdownStreaming(options, Rows(5));

        Assert.Contains("r1", output);
        Assert.DoesNotContain("r3", output);
        Assert.Contains("... and 3 more", output);
    }

    private static IEnumerable<MarkoutRowWindow> AllWindowKinds() =>
    [
        MarkoutRowWindow.Head(2),
        MarkoutRowWindow.Tail(2),
        MarkoutRowWindow.Range(2, 4),
        MarkoutRowWindow.Range(3, null),
        MarkoutRowWindow.Range(9, 12)
    ];

    /// <summary>
    /// Rows arriving one at a time cannot answer "which are the last two" until
    /// the last one lands, so the streaming path buffers when a window is set.
    /// Tail is the case that fails if that buffering is dropped, and it must
    /// agree exactly with the batch path.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllModes))]
    public void StreamedRows_HonorATailWindow(MarkoutTableMode mode)
    {
        var options = new MarkoutWriterOptions { TableMode = mode, RowWindow = MarkoutRowWindow.Tail(2) };

        Assert.Equal(
            Render(options, Rows(5)),
            RenderStreaming(options, Rows(5)));
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public void StreamedRows_AgreeWithBatchedRowsForEveryWindowKind(MarkoutTableMode mode)
    {
        foreach (var window in AllWindowKinds())
        {
            var options = new MarkoutWriterOptions { TableMode = mode, RowWindow = window };
            Assert.Equal(
                Render(options, Rows(5)),
                RenderStreaming(options, Rows(5)));
        }
    }

    // ── Options plumbing ──

    [Fact]
    public void AReadOnlyInstance_RefusesAWindow()
    {
        var options = new MarkoutWriterOptions { RowWindow = MarkoutRowWindow.Range(2, 4) };
        options.MakeReadOnly();

        Assert.Throws<InvalidOperationException>(() => options.RowWindow = MarkoutRowWindow.Head(1));
    }

    /// <summary>
    /// <c>WriteCompositeTable</c> takes the JSON identity-column path, which
    /// rebuilds the options into a fresh instance before constructing the table
    /// writer (<c>MarkoutWriter.ResolveIdentityColumns</c>). A window that the
    /// copy failed to carry would be dropped there with no error and no
    /// diagnostic — the table would simply render in full. This is the gate on
    /// that copy.
    /// </summary>
    [Fact]
    public void AWindow_SurvivesTheIdentityColumnOptionsCopy()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter(),
            new MarkoutWriterOptions
            {
                TableMode = MarkoutTableMode.Jsonl,
                RowWindow = MarkoutRowWindow.Tail(1)
            });

        writer.WriteCompositeTable(
            MarkoutCompositeRow.Scalar("first", "1"),
            MarkoutCompositeRow.Scalar("second", "2"),
            MarkoutCompositeRow.Scalar("third", "3"));

        var lines = sw.ToString().ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Single(lines);
        Assert.Contains("third", lines[0]);
        Assert.DoesNotContain("first", lines[0]);
    }

    [Fact]
    public void NoWindow_RendersEveryRow()
    {
        var output = Render(new MarkoutWriterOptions(), Rows(4));

        Assert.Contains("r1", output);
        Assert.Contains("r4", output);
    }

    [Fact]
    public void AnUnlimitedWindow_RendersEveryRow()
    {
        var output = Render(
            new MarkoutWriterOptions { RowWindow = MarkoutRowWindow.Head(-1) },
            Rows(4));

        Assert.Contains("r1", output);
        Assert.Contains("r4", output);
    }

    // ── Streaming-only formatters ──

    /// <summary>
    /// A formatter that implements only <see cref="IStreamingTableFormatter"/>.
    /// Every formatter in this repository also reaches <see cref="ITableFormatter"/>
    /// (<see cref="IDocumentFormatter"/> inherits it), so none of them exercise the
    /// buffered-replay path. The streaming-only <see cref="TableWriter"/> constructor
    /// is public API, though, so an external formatter can land there — and forcing
    /// buffering for a window would drop its entire table without that replay.
    /// </summary>
    private sealed class StreamingOnlyFormatter : IStreamingTableFormatter
    {
        public void BeginTable(TextWriter writer, ReadOnlySpan<string> headers, MarkoutWriterOptions options)
            => writer.WriteLine("begin:" + string.Join(",", headers.ToArray()));

        public void WriteRow(TextWriter writer, ReadOnlySpan<string> values)
            => writer.WriteLine("row:" + string.Join(",", values.ToArray()));

        public void EndTable(TextWriter writer, int skippedRows)
            => writer.WriteLine("end:" + skippedRows);
    }

    private static string RenderStreamingOnly(MarkoutWriterOptions options, IList<string[]> rows)
    {
        var sw = new StringWriter();
        var writer = new TableWriter(sw, new StreamingOnlyFormatter(), options);
        writer.WriteTableStart(Header);
        foreach (var row in rows)
            writer.WriteTableRow(row);
        writer.WriteTableEnd();
        return sw.ToString();
    }

    [Fact]
    public void AStreamingOnlyFormatter_StillEmitsTheWindowedTable()
    {
        var output = RenderStreamingOnly(
            new MarkoutWriterOptions { RowWindow = MarkoutRowWindow.Tail(2) },
            Rows(5));

        Assert.Contains("begin:Name", output);
        Assert.Contains("row:r4", output);
        Assert.Contains("row:r5", output);
        Assert.DoesNotContain("row:r1", output);
        Assert.Contains("end:0", output);
    }

    [Fact]
    public void AStreamingOnlyFormatter_ReportsWhatMaxItemsDroppedFromTheWindow()
    {
        var output = RenderStreamingOnly(
            new MarkoutWriterOptions { RowWindow = MarkoutRowWindow.Tail(3), MaxItems = 2 },
            Rows(6));

        Assert.Contains("row:r4", output);
        Assert.Contains("row:r5", output);
        Assert.DoesNotContain("row:r6", output);
        Assert.Contains("end:1", output);
    }

    /// <summary>
    /// The negative control: without a window the streaming-only formatter keeps
    /// writing rows straight through rather than being routed via the replay.
    /// </summary>
    [Fact]
    public void AStreamingOnlyFormatter_WithoutAWindow_StillStreamsDirectly()
    {
        var output = RenderStreamingOnly(new MarkoutWriterOptions(), Rows(3));

        Assert.Contains("row:r1", output);
        Assert.Contains("row:r3", output);
    }
}
