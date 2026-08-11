using Markout;

namespace Markout.Tests;

public class NoProjectionAllocationTests
{
    private static readonly MarkoutField[] Fields =
    [
        new("alpha", "1"), new("beta", "2"), new("gamma", "3"),
    ];

    /// <summary>
    /// Allocation charged to the settled state of <paramref name="op"/>: the smallest total any
    /// single batch of <paramref name="batch"/> calls costs, across up to
    /// <paramref name="attempts"/> batches.
    /// </summary>
    /// <remarks>
    /// The minimum, rather than the first batch, because a first batch is not necessarily settled.
    /// A fresh process charges the first batch for one-time work -- tiered-compilation transitions
    /// and whatever else the runtime does once -- so measuring once asks "did this batch allocate",
    /// when the property under test is "does this operation allocate". CI answered the first
    /// question with 7416 bytes on a run whose commit could not reach this code (see #201), and a
    /// gate that a loaded machine can turn red is a gate people learn to re-run.
    ///
    /// This is not a tolerance: every assertion still demands exactly zero, and an operation that
    /// really allocates per call allocates in every batch, so no minimum rescues it. It moves the
    /// demand from the first measurement to the settled one. Early exit on zero keeps the common
    /// case to a single batch.
    /// </remarks>
    private static long AllocatedPerBatch(Action op, int warmup = 200, int batch = 1000, int attempts = 5)
    {
        for (int i = 0; i < warmup; i++)
            op();

        var lowest = long.MaxValue;
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < batch; i++)
                op();
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            if (allocated <= 0)
                return 0;

            lowest = Math.Min(lowest, allocated);
        }

        return lowest;
    }

    /// <summary>
    /// AllocatedPerBatch reports a non-zero cost for an operation that does allocate.
    /// </summary>
    /// <remarks>
    /// This is the non-vacuity test for every assertion in this class. They all read
    /// <c>Assert.Equal(0, AllocatedPerBatch(...))</c>, so a helper that returned zero unconditionally
    /// -- or one whose minimum-of-attempts loop stopped measuring what it claims to -- would leave
    /// the whole file green and proving nothing. Taking a minimum makes that failure mode cheaper to
    /// reach than it was, which is why the guard arrives with it.
    /// </remarks>
    [Fact]
    public void AllocatedPerBatch_ForAnAllocatingOperation_ReportsNonZero()
    {
        Assert.NotEqual(0, AllocatedPerBatch(() => GC.KeepAlive(new object())));
    }

    [Fact]
    public void WriteField_NoProjection_DoesNotAllocate()
    {
        var w = MarkoutWriter.Create(TextWriter.Null, new MarkdownFormatter());
        Assert.Equal(0, AllocatedPerBatch(() => w.WriteField("key", "value")));
    }

    [Fact]
    public void WriteFields_NoProjection_DoesNotAllocate()
    {
        var w = MarkoutWriter.Create(TextWriter.Null, new MarkdownFormatter());
        Assert.Equal(0, AllocatedPerBatch(() => w.WriteFields(Fields)));
    }

    [Fact]
    public void WriteFieldsInline_NoProjection_DoesNotAllocate()
    {
        var w = MarkoutWriter.Create(TextWriter.Null, new MarkdownFormatter());
        Assert.Equal(0, AllocatedPerBatch(() => w.WriteFieldsInline(Fields)));
    }

    [Fact]
    public void WriteFieldsBulleted_NoProjection_DoesNotAllocate()
    {
        var w = MarkoutWriter.Create(TextWriter.Null, new MarkdownFormatter());
        Assert.Equal(0, AllocatedPerBatch(() => w.WriteFieldsBulleted(Fields)));
    }

    [Fact]
    public void WriteFieldsNumbered_NoProjection_DoesNotAllocate()
    {
        var w = MarkoutWriter.Create(TextWriter.Null, new MarkdownFormatter());
        Assert.Equal(0, AllocatedPerBatch(() => w.WriteFieldsNumbered(Fields)));
    }

    [Fact]
    public void WriteFieldsInline_WithFieldProjection_StillFilters()
    {
        // The projected path is preserved: exclude filters a field out.
        var opts = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection { ExcludeFields = new HashSet<string> { "beta" } },
        };
        var sw = new StringWriter();
        var w = MarkoutWriter.Create(sw, new MarkdownFormatter(), opts);
        w.WriteFieldsInline(Fields);
        var output = sw.ToString();

        Assert.Contains("alpha", output);
        Assert.DoesNotContain("beta", output);
        Assert.Contains("gamma", output);
    }
}
