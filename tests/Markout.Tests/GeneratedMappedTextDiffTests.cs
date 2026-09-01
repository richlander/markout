using Markout;

namespace Markout.Tests;

[MarkoutSerializable]
public class MappedTextDiffContainer
{
    public string? Member { get; set; }

    [MarkoutSection(Name = "Source Diff", EmptyText = "No changes.")]
    public MappedTextDiff? SourceDiff { get; set; }
}

[MarkoutContext(typeof(MappedTextDiffContainer))]
public partial class MappedTextDiffContext : MarkoutSerializerContext;

public class GeneratedMappedTextDiffTests
{
    [Fact]
    public void MappedTextDiffPropertyRendersAsASection()
    {
        var model = new MappedTextDiffContainer
        {
            Member = "M",
            SourceDiff = MappedTextDiffTests.Sample()
        };

        var output = MarkoutSerializer.Serialize(model, MappedTextDiffContext.Default);

        Assert.Contains("## Source Diff", output, StringComparison.Ordinal);
        Assert.Contains("```diff", output, StringComparison.Ordinal);
        Assert.DoesNotContain("No changes.", output, StringComparison.Ordinal);
    }

    [Fact]
    public void EmptyMappedTextDiffUsesSectionFallback()
    {
        var model = new MappedTextDiffContainer
        {
            SourceDiff = new MappedTextDiff(
                new TextDiffSequence(["same"]),
                new TextDiffSequence(["same"]),
                [])
        };

        var output = MarkoutSerializer.Serialize(model, MappedTextDiffContext.Default);

        Assert.Contains("## Source Diff", output, StringComparison.Ordinal);
        Assert.Contains("No changes.", output, StringComparison.Ordinal);
        Assert.DoesNotContain("```diff", output, StringComparison.Ordinal);
    }

    [Fact]
    public void NullMappedTextDiffOmitsSection()
    {
        var output = MarkoutSerializer.Serialize(
            new MappedTextDiffContainer { SourceDiff = null },
            MappedTextDiffContext.Default);

        Assert.DoesNotContain("Source Diff", output, StringComparison.Ordinal);
    }
}
