using Markout;

namespace Markout.Tests;

public class NoProjectionAllocationTests
{
    private static readonly MarkoutField[] Fields =
    [
        new("alpha", "1"), new("beta", "2"), new("gamma", "3"),
    ];

    private static long AllocatedPerBatch(Action op, int warmup = 200, int batch = 1000)
    {
        for (int i = 0; i < warmup; i++)
            op();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < batch; i++)
            op();
        return GC.GetAllocatedBytesForCurrentThread() - before;
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
