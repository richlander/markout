using Markout;

namespace Markout.Tests;

[MarkoutSerializable]
public class CallersContainer
{
    public string? Member { get; set; }

    [MarkoutIgnoreInTable]
    [MarkoutSection(Name = "Callers", EmptyText = "No in-assembly callers found.")]
    public List<CallerRow>? Callers { get; set; }

    [MarkoutSection(Name = "Tags", EmptyText = "None found.")]
    public List<string>? Tags { get; set; }
}

[MarkoutSerializable]
public class CallerRow
{
    public string? Caller { get; set; }
    public string? Kind { get; set; }
}

[MarkoutContext(typeof(CallersContainer))]
public partial class EmptyTextContext : MarkoutSerializerContext
{
}

public class EmptyTextFallbackTests
{
    [Fact]
    public void NonEmptyCollection_RendersTable_NotFallback()
    {
        var model = new CallersContainer
        {
            Member = "Serialize",
            Callers = new List<CallerRow> { new() { Caller = "Foo", Kind = "call" } },
        };

        var mdf = MarkoutSerializer.Serialize(model, EmptyTextContext.Default);

        Assert.Contains("## Callers", mdf);
        Assert.Contains("| Foo |", mdf);
        Assert.DoesNotContain("No in-assembly callers found.", mdf);
    }

    [Fact]
    public void EmptyCollection_RendersHeadingAndFallbackParagraph()
    {
        var model = new CallersContainer
        {
            Member = "Serialize",
            Callers = new List<CallerRow>(),
        };

        var mdf = MarkoutSerializer.Serialize(model, EmptyTextContext.Default);

        Assert.Contains("## Callers", mdf);
        Assert.Contains("No in-assembly callers found.", mdf);
    }

    [Fact]
    public void NullCollection_OmitsSectionEntirely()
    {
        var model = new CallersContainer
        {
            Member = "Serialize",
            Callers = null,
        };

        var mdf = MarkoutSerializer.Serialize(model, EmptyTextContext.Default);

        Assert.DoesNotContain("## Callers", mdf);
        Assert.DoesNotContain("No in-assembly callers found.", mdf);
    }

    [Fact]
    public void EmptyStringArray_RendersFallbackParagraph()
    {
        var model = new CallersContainer
        {
            Member = "Serialize",
            Tags = new List<string>(),
        };

        var mdf = MarkoutSerializer.Serialize(model, EmptyTextContext.Default);

        Assert.Contains("## Tags", mdf);
        Assert.Contains("None found.", mdf);
    }
}
