using Markout;

namespace Markout.Tests;

/// <summary>
/// <see cref="TreeNodeState"/> is structural: the lowering records why a node is unexpanded and
/// each sink chooses its own spelling. These tests pin that separation.
/// </summary>
public class TreeNodeStateTests
{
    [Fact]
    public void Markdown_RendersTheRevisitGlyph()
    {
        var writer = MarkoutWriter.Create(new MarkdownFormatter());
        writer.WriteTree(Revisited());

        Assert.Contains("└─ \u21a9 B", Normalize(writer.ToString()));
    }

    [Fact]
    public void Unicode_RendersTheRevisitGlyph()
    {
        var writer = MarkoutWriter.Create(new UnicodeFormatter());
        writer.WriteTree(Revisited());

        Assert.Contains("└─ \u21a9 B", Normalize(writer.ToString()));
    }

    [Fact]
    public void PlainText_RendersAWordInsteadOfTheGlyph()
    {
        var writer = MarkoutWriter.Create(new PlainTextFormatter());
        writer.WriteTree(Revisited());
        var output = Normalize(writer.ToString());

        Assert.Contains("└─ (revisit) B", output);
        Assert.DoesNotContain("\u21a9", output);
    }

    [Fact]
    public void Mermaid_CarriesTheStateInTheNodeLabel()
    {
        var writer = MarkoutWriter.Create(new MermaidFormatter());
        writer.WriteTree(Revisited());
        var output = Normalize(writer.ToString());

        Assert.Contains("(revisit) B", output);
        Assert.DoesNotContain("\u21a9", output);
    }

    [Fact]
    public void ANormalNodeGetsNoPrefixInAnySink()
    {
        var writer = MarkoutWriter.Create(new MarkdownFormatter());
        writer.WriteTree(new TreeNode("A", [new TreeNode("B")]));
        var output = Normalize(writer.ToString());

        Assert.Contains("└─ B", output);
        Assert.DoesNotContain("\u21a9", output);
        Assert.DoesNotContain("(revisit)", output);
    }

    [Fact]
    public void TheGlyphIsConfigurable()
    {
        var options = new MarkoutWriterOptions();
        options.Glyphs = MarkoutGlyphs.Default with { Revisit = "[seen]" };

        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);
        writer.WriteTree(Revisited());

        Assert.Contains("└─ [seen] B", Normalize(writer.ToString()));
    }

    [Fact]
    public void AnEmptyGlyphSuppressesTheMarker()
    {
        var options = new MarkoutWriterOptions();
        options.Glyphs = MarkoutGlyphs.Default with { Revisit = "" };

        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);
        writer.WriteTree(Revisited());
        var output = Normalize(writer.ToString());

        Assert.Contains("└─ B", output);
        Assert.DoesNotContain("\u21a9", output);
    }

    /// <summary>
    /// A state is information about the shape of the tree, not decoration. Suppressing badges must
    /// not suppress it, or an elided subtree becomes indistinguishable from a genuine leaf.
    /// </summary>
    [Fact]
    public void IncludeBadgesDoesNotSuppressTheState()
    {
        var options = new MarkoutWriterOptions();
        options.IncludeBadges = false;

        var node = new TreeNode("B") { State = TreeNodeState.Revisit, Badge = "📁" };
        var writer = MarkoutWriter.Create(new MarkdownFormatter(), options);
        writer.WriteTree(new TreeNode("A", [node]));
        var output = Normalize(writer.ToString());

        Assert.Contains("└─ \u21a9 B", output);
        Assert.DoesNotContain("📁", output);
    }

    [Fact]
    public void TheStatePrecedesTheBadge()
    {
        var node = new TreeNode("B") { State = TreeNodeState.Revisit, Badge = "📁" };
        var writer = MarkoutWriter.Create(new MarkdownFormatter());
        writer.WriteTree(new TreeNode("A", [node]));

        Assert.Contains("└─ \u21a9 📁 B", Normalize(writer.ToString()));
    }

    /// <summary>
    /// The state travels on the node, so the text stays exactly what the caller supplied and a
    /// consumer can match on it without stripping a marker first.
    /// </summary>
    [Fact]
    public void TheLoweringLeavesNodeTextUntouched()
    {
        var graph = new Graph(
            [new GraphNode("a", "A"), new GraphNode("b", "B")],
            [new GraphEdge("a", "b"), new GraphEdge("b", "a")],
            focusKey: "a");

        var root = Assert.Single(GraphLowering.ToTree(graph));
        var back = Assert.Single(Assert.Single(root.Children!).Children!);

        Assert.Equal("A", back.Text);
        Assert.Equal(TreeNodeState.Revisit, back.State);
    }

    /// <summary>
    /// <see cref="TreeWriter"/> has no formatter to declare a capability, so it takes the stable
    /// word rather than assuming the caller can render a glyph.
    /// </summary>
    [Fact]
    public void TreeWriter_RendersAWordInsteadOfTheGlyph()
    {
        var sw = new StringWriter();
        new TreeWriter(sw).WriteTree(Revisited());
        var output = Normalize(sw.ToString());

        Assert.Contains("└─ (revisit) B", output);
        Assert.DoesNotContain("\u21a9", output);
    }

    [Fact]
    public void Diagram_RendersAWordInsteadOfTheGlyph()
    {
        var writer = MarkoutWriter.Create(new DiagramFormatter());
        writer.WriteTree(Revisited());
        var output = Normalize(writer.ToString());

        Assert.Contains("(revisit) B", output);
        Assert.DoesNotContain("\u21a9", output);
    }

    private static TreeNode Revisited()
        => new("A", [new TreeNode("B") { State = TreeNodeState.Revisit }]);

    private static string Normalize(string text) => text.Replace("\r\n", "\n");
}
