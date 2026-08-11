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
    /// allocates, over <paramref name="batches"/> consecutive batches.
    /// </summary>
    /// <remarks>
    /// A gate may be made harder to trip by accident, but it may never be made easier to pass. Two
    /// earlier attempts here failed that test, both by giving up coverage to buy calm:
    ///
    /// Taking the minimum of several batches is satisfied by the single cleanest batch, so a cost
    /// recurring on a period longer than one batch hides in whichever window misses it. A 4KB
    /// allocation on every 1200th call in WriteFieldsInline was caught 3 runs of 3 by one batch and
    /// missed 3 of 3 by the minimum of five.
    ///
    /// Discarding the first batch and asserting the rest fixes that, but sacrifices op's first
    /// thousand calls to the settling -- and a cost occurring there and not again is then missed
    /// where the original caught it. GPT-5.6 Sol demonstrated it with an allocation on call 1000 of
    /// every 5000: caught by the original, missed by the discard.
    ///
    /// Both attempts paid with op's call sequence because they treated the settling as something op
    /// must fund. It is not. What needs settling is this measuring frame -- the batch loop and the
    /// GC accounting around it, whose one-time cost lands wherever it is first executed, which is
    /// why the same operation reads 1464 bytes or 0 depending only on how the measuring code around
    /// it was written. That frame can be settled on a no-op instead, before op is called at all.
    ///
    /// So the prologue is the warmup and nothing else, exactly as it was, and every batch after it
    /// is asserted at exactly zero. Anything the single original batch could catch lies inside a
    /// span four times its size that begins on the same call, which makes this strictly stronger,
    /// with no window the original covered left uncovered.
    ///
    /// What still escapes is a cost that recurs on a period longer than the asserted span and
    /// misses it by phase, which was true of the original at a quarter of the span; and a cost that
    /// occurs exactly once outside the measured calls, whether during the warmup or after the last
    /// batch. The warmup case no encoding can assert on, because absorbing a one-time cost before
    /// the first measurement is precisely what a warmup is for. The tail case is a consequence of
    /// measuring a bounded number of calls at all, and moves further out as the span grows: a 4KB
    /// allocation on call 5000 escapes four batches and is caught by five.
    /// </remarks>
    private static long AllocatedPerBatch(Action op, int warmup = 200, int batch = 1000, int batches = 4)
    {
        SettleMeasurementFrame(batch);

        for (int i = 0; i < warmup; i++)
            op();

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
    /// Runs the measurement over an operation that does nothing, and throws the readings away.
    /// </summary>
    /// <remarks>
    /// This exists so that the first batch that counts is not also the first time this loop and the
    /// GC accounting around it have run. It deliberately takes no argument from the caller: the
    /// point is to spend calls that are not op's, so that op's own call sequence is asserted on
    /// from its first post-warmup call.
    /// </remarks>
    private static void SettleMeasurementFrame(int batch)
    {
        Action nothing = static () => { };
        for (int round = 0; round < 3; round++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < batch; i++)
                nothing();
            _ = GC.GetAllocatedBytesForCurrentThread() - before;
        }
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
