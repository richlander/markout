using Markout;

namespace Markout.Tests;

public class NoProjectionAllocationTests
{
    private static readonly MarkoutField[] Fields =
    [
        new("alpha", "1"), new("beta", "2"), new("gamma", "3"),
    ];

    /// <summary>
    /// The most any single batch of <paramref name="batch"/> calls to <paramref name="op"/>
    /// allocates, measured over <paramref name="batches"/> batches once the operation has settled.
    /// </summary>
    /// <remarks>
    /// Two things had to be true at once, and the obvious repair only bought one of them.
    ///
    /// The measurement has to survive a one-time cost. A window is not hermetic: the same product,
    /// the same operation and the same warmup read 1464 bytes or 0 depending only on how the
    /// measuring code around it was written, so a single batch asserted at exactly zero is a gate
    /// that a settling runtime can turn red. That is what CI hit in #201, with 7416 bytes on a
    /// commit that could not reach this code. Discarding a full batch after the warmup answers it:
    /// the discarded batch absorbs whatever is one-time, and nothing one-time is ever asserted on.
    ///
    /// The measurement also has to see a cost that recurs but does not recur every call. Taking the
    /// minimum of several batches survives the transient too, and was the first repair tried here --
    /// but a minimum is satisfied by the single cleanest batch, so an allocation whose period
    /// exceeds the batch size hides in whichever window happens to miss it. That is not
    /// hypothetical: a 4KB allocation on every 1200th call, injected into WriteFieldsInline, is
    /// caught 3 runs out of 3 by one batch and missed 3 out of 3 by the minimum of five. The
    /// minimum is strictly weaker than what it replaced, which is the one thing a repair to a gate
    /// may not be.
    ///
    /// So the batches after the discarded one are all asserted, by returning the largest. Every
    /// settled batch must allocate exactly zero -- no tolerance, and four windows in which a
    /// recurring cost can appear rather than the one window the original measured.
    /// </remarks>
    private static long AllocatedPerBatch(Action op, int warmup = 200, int batch = 1000, int batches = 4)
    {
        for (int i = 0; i < warmup; i++)
            op();

        // One full batch, measured and thrown away, so that the readings that count are settled.
        var settling = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < batch; i++)
            op();
        _ = GC.GetAllocatedBytesForCurrentThread() - settling;

        long highest = 0;
        for (int b = 0; b < batches; b++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < batch; i++)
                op();
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            if (allocated > highest)
                highest = allocated;
        }

        return highest;
    }

    /// <summary>
    /// AllocatedPerBatch reports a non-zero cost for an operation that does allocate.
    /// </summary>
    /// <remarks>
    /// This is the non-vacuity test for every assertion in this class. They all read
    /// <c>Assert.Equal(0, AllocatedPerBatch(...))</c>, so a helper that returned zero unconditionally
    /// -- or one whose batch loop stopped measuring what it claims to -- would leave the whole file
    /// green and proving nothing.
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
