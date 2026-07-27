using Markout;
using Markout.Formatting;

namespace Markout.Tests;

[MarkoutSerializable]
public class GraphContainer
{
    public string? Member { get; set; }

    [MarkoutSection(Name = "Call Graph", EmptyText = "No calls found.")]
    public Graph? CallGraph { get; set; }
}

[MarkoutContext(typeof(GraphContainer))]
public partial class GraphContext : MarkoutSerializerContext
{
}

/// <summary>
/// The declarative path: a <see cref="Graph"/>-typed section property must reach
/// <see cref="MarkoutWriter.WriteGraph"/> through the generated serializer, the same way a
/// <see cref="TreeNode"/> list reaches <c>WriteTree</c>. Without this a host that models its
/// document with <c>[MarkoutSerializable]</c> cannot use the graph shape at all.
/// </summary>
public class GeneratedGraphTests
{
    private static GraphContainer Sample() => new()
    {
        Member = "Run",
        CallGraph = new Graph(
            [new GraphNode("a", "A"), new GraphNode("b", "B")],
            [new GraphEdge("a", "b")],
            focusKey: "a"),
    };

    [Fact]
    public void GraphProperty_RendersAsASectionInMarkdown()
    {
        var mdf = MarkoutSerializer.Serialize(Sample(), GraphContext.Default);

        Assert.Contains("## Call Graph", mdf, StringComparison.Ordinal);
        // Markdown lowers a graph to its tree projection.
        Assert.Contains("└─ B", mdf, StringComparison.Ordinal);
        Assert.Contains("A", mdf, StringComparison.Ordinal);
        Assert.DoesNotContain("No calls found.", mdf, StringComparison.Ordinal);
    }

    [Fact]
    public void GraphProperty_RendersAsADiagramWhenTheSinkDrawsOne()
    {
        var sink = new StringWriter();
        MarkoutSerializer.Serialize(Sample(), sink, new MermaidFormatter(), GraphContext.Default);
        var output = sink.ToString();

        Assert.Contains("graph TD", output, StringComparison.Ordinal);
        Assert.Contains("n0[\"A\"]", output, StringComparison.Ordinal);
        Assert.Contains("n0 --> n1", output, StringComparison.Ordinal);
        // The caller's opaque keys never reach the output.
        Assert.DoesNotContain("\"a\"", output, StringComparison.Ordinal);
    }

    [Fact]
    public void GraphProperty_RendersAsATreeWhenTheSinkDrawsOne()
    {
        var sink = new StringWriter();
        MarkoutSerializer.Serialize(Sample(), sink, new PlainTextFormatter(), GraphContext.Default);
        var output = sink.ToString();

        Assert.Contains("A", output, StringComparison.Ordinal);
        Assert.Contains("B", output, StringComparison.Ordinal);
    }

    [Fact]
    public void EmptyGraph_FallsBackToTheSectionEmptyTextRatherThanAnEmptyHeading()
    {
        var model = new GraphContainer { Member = "Run", CallGraph = new Graph([], []) };

        var mdf = MarkoutSerializer.Serialize(model, GraphContext.Default);

        Assert.Contains("## Call Graph", mdf, StringComparison.Ordinal);
        Assert.Contains("No calls found.", mdf, StringComparison.Ordinal);
    }

    [Fact]
    public void NullGraph_OmitsTheSectionEntirely()
    {
        var model = new GraphContainer { Member = "Run", CallGraph = null };

        var mdf = MarkoutSerializer.Serialize(model, GraphContext.Default);

        Assert.DoesNotContain("Call Graph", mdf, StringComparison.Ordinal);
        Assert.DoesNotContain("No calls found.", mdf, StringComparison.Ordinal);
    }
}
