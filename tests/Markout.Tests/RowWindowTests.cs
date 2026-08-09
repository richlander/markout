using System.Runtime.CompilerServices;

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

    /// <summary>
    /// A count is usually computed, and a subtraction that slips below zero should
    /// fail rather than silently widen the table to every row. "No window" is
    /// already spelled by leaving the option null, so a negative count has no
    /// legitimate reading left.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void ANegativeCount_IsRejected(int count)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MarkoutRowWindow.Head(count));
        Assert.Throws<ArgumentOutOfRangeException>(() => MarkoutRowWindow.Tail(count));
    }

    [Fact]
    public void AZeroCount_SelectsNoRows()
    {
        Assert.Equal((0, 0), MarkoutRowWindow.Head(0).Resolve(4));
        Assert.Equal((4, 4), MarkoutRowWindow.Tail(0).Resolve(4));
    }

    /// <summary>
    /// Only Tail is defined against the total, so only Tail may make a renderer wait.
    /// If an open-ended range were treated as non-positional it would buffer a table
    /// it could have streamed.
    /// </summary>
    [Fact]
    public void OnlyTail_NeedsTheTotalRowCount()
    {
        Assert.True(MarkoutRowWindow.Head(2).IsPositional);
        Assert.True(MarkoutRowWindow.Range(2, null).IsPositional);
        Assert.True(MarkoutRowWindow.Range(2, 4).IsPositional);
        Assert.False(MarkoutRowWindow.Tail(2).IsPositional);
    }

    [Fact]
    public void ATailWindow_RefusesToAnswerByPosition()
    {
        Assert.Throws<InvalidOperationException>(() => MarkoutRowWindow.Tail(2).KeepsPosition(0));
        Assert.Throws<InvalidOperationException>(() => MarkoutRowWindow.Tail(2).IsPastEnd(0));
    }

    /// <summary>
    /// The two ways of asking what a window means must give the same answer, or the
    /// streaming path has quietly acquired its own dialect. Swept rather than
    /// sampled, because a disagreement is likely to live at an edge.
    /// </summary>
    [Fact]
    public void AskingByPosition_AgreesWithResolvingAgainstATotal()
    {
        MarkoutRowWindow[] windows =
        [
            MarkoutRowWindow.Head(0), MarkoutRowWindow.Head(1), MarkoutRowWindow.Head(3),
            MarkoutRowWindow.Head(10),
            MarkoutRowWindow.Range(1, 1), MarkoutRowWindow.Range(2, 4),
            MarkoutRowWindow.Range(3, null), MarkoutRowWindow.Range(9, 12)
        ];

        foreach (var window in windows)
        {
            Assert.True(window.IsPositional);
            for (var total = 0; total <= 15; total++)
            {
                var (keepStart, keepEnd) = window.Resolve(total);
                for (var position = 0; position < total; position++)
                {
                    var expected = position >= keepStart && position < keepEnd;
                    Assert.Equal(expected, window.KeepsPosition(position));
                    Assert.Equal(position >= keepEnd, window.IsPastEnd(position));
                }
            }
        }
    }

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
            for (var n = 0; n <= 7; n++)
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
        MarkoutRowWindow.Head(0),
        MarkoutRowWindow.Head(2),
        MarkoutRowWindow.Tail(0),
        MarkoutRowWindow.Tail(2),
        MarkoutRowWindow.Range(1, 1),
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
    public void AWindowWiderThanTheTable_RendersEveryRow()
    {
        var output = Render(
            new MarkoutWriterOptions { RowWindow = MarkoutRowWindow.Head(100) },
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
        var sw = new StringWriter();
        var writer = new TableWriter(sw, new StreamingOnlyFormatter(), new MarkoutWriterOptions());
        writer.WriteTableStart(Header);
        writer.WriteTableRow(["r1"]);

        // Observed before the table ends: buffering-and-replaying would also satisfy
        // an assertion made after WriteTableEnd, so checking afterwards would not
        // distinguish direct streaming from the path this test exists to rule out.
        Assert.Contains("row:r1", sw.ToString());

        writer.WriteTableRow(["r2"]);
        writer.WriteTableEnd();
        Assert.Contains("row:r2", sw.ToString());
    }

    /// <summary>
    /// The gate on windows not costing memory. A positional window decides each row as
    /// it arrives, so rows must reach the formatter before the table ends; if they do
    /// not, the writer is holding the table, and a Head(1) over a million rows retains
    /// a million rows to emit one. Observing emission mid-table is the structural form
    /// of that claim — a memory measurement would assert the same thing less reliably.
    /// </summary>
    [Theory]
    [MemberData(nameof(PositionalWindows))]
    public void APositionalWindow_ReachesTheFormatterBeforeTheTableEnds(MarkoutRowWindow window)
    {
        var sw = new StringWriter();
        var writer = new TableWriter(sw, new StreamingOnlyFormatter(), new MarkoutWriterOptions { RowWindow = window });
        writer.WriteTableStart(Header);

        // Enough rows that every window under test has selected at least one.
        for (var i = 1; i <= 5; i++)
            writer.WriteTableRow(["r" + i]);

        Assert.Contains("row:", sw.ToString());
    }

    public static TheoryData<MarkoutRowWindow> PositionalWindows() =>
    [
        MarkoutRowWindow.Head(2),
        MarkoutRowWindow.Range(2, 4),
        MarkoutRowWindow.Range(3, null)
    ];

    /// <summary>
    /// The negative control for the test above: a Tail window is the one kind that
    /// legitimately waits, so this pins that it waits rather than that waiting is fine.
    /// </summary>
    [Fact]
    public void AWindowedStreamingOnlyFormatter_EmitsNothingUntilTheTableEnds()
    {
        var sw = new StringWriter();
        var writer = new TableWriter(
            sw,
            new StreamingOnlyFormatter(),
            new MarkoutWriterOptions { RowWindow = MarkoutRowWindow.Tail(2) });
        writer.WriteTableStart(Header);
        writer.WriteTableRow(["r1"]);

        Assert.Equal("", sw.ToString());

        writer.WriteTableEnd();
        Assert.Contains("row:r1", sw.ToString());
    }

    /// <summary>
    /// A window that keeps nothing must cost nothing per row. Tail is the only kind
    /// that retains rows at all, so it is the only one that can get this wrong, and
    /// <c>Tail(0)</c> is the boundary where retaining and copying come apart: the
    /// bound is reached before the first row, so every copy is made to be discarded.
    /// Head(0) is the control — it never had a buffer to misuse.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AWindowThatKeepsNothing_CopiesNothing(int kind)
    {
        var window = kind == 0 ? MarkoutRowWindow.Tail(0) : MarkoutRowWindow.Head(0);
        var writer = new TableWriter(
            TextWriter.Null,
            new StreamingOnlyFormatter(),
            new MarkoutWriterOptions { RowWindow = window });
        writer.WriteTableStart(Header);
        string[] row = ["r"];

        for (var i = 0; i < 200; i++)
            writer.WriteTableRow(row);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
            writer.WriteTableRow(row);

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    /// <summary>
    /// A tail window retains its bound, not the table. Allocation cannot show this —
    /// every row is copied on arrival either way, so allocated bytes are linear in row
    /// count whether the buffer is bounded or not. Reachability can: the rows a bounded
    /// buffer let go of become collectable, and the ones it holds do not.
    ///
    /// <para>
    /// The row values are freshly built rather than literals, so nothing is interned
    /// and the only thing that can keep one alive is the writer. The producer is a
    /// separate non-inlined method so its locals are out of scope and off the stack
    /// before the collection runs.
    /// </para>
    /// </summary>
    [Fact]
    public void ATailWindow_HoldsItsBoundRatherThanTheTable()
    {
        const int Rows = 1000;
        const int Bound = 7;

        var (writer, tracked) = FillTail(Rows, Bound);

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);

        var alive = tracked.Count(reference => reference.IsAlive);
        GC.KeepAlive(writer);

        Assert.Equal(Bound, alive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (TableWriter Writer, WeakReference[] Tracked) FillTail(int rows, int bound)
    {
        var writer = new TableWriter(
            TextWriter.Null,
            new StreamingOnlyFormatter(),
            new MarkoutWriterOptions { RowWindow = MarkoutRowWindow.Tail(bound) });
        writer.WriteTableStart(Header);

        var tracked = new WeakReference[rows];
        for (var i = 0; i < rows; i++)
        {
            // Built rather than written as a literal: an interned string would stay
            // reachable no matter what the writer did with it.
            var value = new string('r', 1) + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            tracked[i] = new WeakReference(value);
            writer.WriteTableRow(value);
        }

        return (writer, tracked);
    }

    /// <summary>
    /// A window that keeps everything hands back the list it was given rather than a
    /// copy of it. Only reference identity can say so: a copy holds the same elements
    /// in the same order, so every assertion about content passes either way, which is
    /// how this went ungated until a reviewer mutated the branch away and saw nothing
    /// fail.
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public void AWindowThatKeepsEveryRow_ReturnsTheSameList(int count)
    {
        IReadOnlyList<string> rows = ["r1", "r2", "r3"];

        Assert.Same(rows, MarkoutRowWindow.Head(count).Apply(rows));
        Assert.Same(rows, MarkoutRowWindow.Tail(count).Apply(rows));
        Assert.Same(rows, MarkoutRowWindow.Range(1, count).Apply(rows));
        Assert.Same(rows, MarkoutRowWindow.Range(1, null).Apply(rows));
        Assert.Same(rows, MarkoutRowWindow.Apply(null, rows));
    }

    [Fact]
    public void AWindowThatKeepsSomeRows_ReturnsACopy()
    {
        IReadOnlyList<string> rows = ["r1", "r2", "r3"];

        Assert.NotSame(rows, MarkoutRowWindow.Head(2).Apply(rows));
        Assert.NotSame(rows, MarkoutRowWindow.Tail(2).Apply(rows));
        Assert.NotSame(rows, MarkoutRowWindow.Range(2, null).Apply(rows));
    }

    [Fact]
    public void ApplyingAnAbsentWindowToANullList_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => MarkoutRowWindow.Apply<string>(null, null!));
        Assert.Throws<ArgumentNullException>(
            () => MarkoutRowWindow.Apply<string>(MarkoutRowWindow.Head(2), null!));
    }
}
