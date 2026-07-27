using Markout;
using Markout.Formatting;

namespace Markout.Tests;

[Collection("ConsoleError")]
public class GraphTests
{
    private static Graph SimpleChain() => new(
        [new GraphNode("a", "A"), new GraphNode("b", "B"), new GraphNode("c", "C")],
        [new GraphEdge("a", "b"), new GraphEdge("b", "c")]);

    // ── Shape validation ──

    [Fact]
    public void Graph_RejectsDuplicateNodeKeys()
    {
        var ex = Assert.Throws<ArgumentException>(() => new Graph(
            [new GraphNode("a", "First"), new GraphNode("a", "Second")],
            []));
        Assert.Contains("'a'", ex.Message);
    }

    [Fact]
    public void Graph_RejectsEdgeWithUnknownSource()
    {
        var ex = Assert.Throws<ArgumentException>(() => new Graph(
            [new GraphNode("a")],
            [new GraphEdge("ghost", "a")]));
        Assert.Contains("'ghost'", ex.Message);
    }

    [Fact]
    public void Graph_RejectsEdgeWithUnknownTarget()
    {
        var ex = Assert.Throws<ArgumentException>(() => new Graph(
            [new GraphNode("a")],
            [new GraphEdge("a", "ghost")]));
        Assert.Contains("'ghost'", ex.Message);
    }

    [Fact]
    public void Graph_RejectsFocusThatNamesNoNode()
    {
        var ex = Assert.Throws<ArgumentException>(() => new Graph(
            [new GraphNode("a")],
            [],
            focusKey: "ghost"));
        Assert.Contains("'ghost'", ex.Message);
    }

    [Fact]
    public void Graph_PreservesParallelEdges()
    {
        var graph = new Graph(
            [new GraphNode("a"), new GraphNode("b")],
            [new GraphEdge("a", "b"), new GraphEdge("a", "b")]);

        Assert.Equal(2, graph.Edges.Length);
    }

    [Fact]
    public void Graph_PreservesNodeOrder()
    {
        var graph = new Graph(
            [new GraphNode("z", "Z"), new GraphNode("a", "A")],
            []);

        Assert.Equal(["Z", "A"], graph.Nodes.Select(n => n.Label));
    }

    [Fact]
    public void Graph_FocusResolvesToTheNode()
    {
        var graph = new Graph([new GraphNode("a", "A"), new GraphNode("b", "B")], [], focusKey: "b");
        Assert.Equal("B", graph.Focus!.Label);
    }

    [Fact]
    public void Graph_WithoutFocus_HasNullFocus()
    {
        Assert.Null(SimpleChain().Focus);
    }

    [Fact]
    public void Graph_AllowsSelfLoop()
    {
        var graph = new Graph([new GraphNode("a")], [new GraphEdge("a", "a")]);
        Assert.Single(graph.Edges);
    }

    // ── Edge table lowering ──

    [Fact]
    public void EdgeTable_PlainGraph_HasOnlyFromAndTo()
    {
        var table = GraphLowering.ToEdgeTable(SimpleChain());

        Assert.Equal(["From", "To"], table.Headers);
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal(["A", "B"], table.Rows[0]);
        Assert.Equal(["B", "C"], table.Rows[1]);
    }

    [Fact]
    public void EdgeTable_AddsGroupColumnsOnlyWhenAGroupIsPresent()
    {
        var graph = new Graph(
            [new GraphNode("a", "A") { Group = "Lib" }, new GraphNode("b", "B")],
            [new GraphEdge("a", "b")]);

        var table = GraphLowering.ToEdgeTable(graph);

        Assert.Equal(["From", "From Group", "To", "To Group"], table.Headers);
        Assert.Equal(["A", "Lib", "B", ""], table.Rows[0]);
    }

    [Fact]
    public void EdgeTable_AddsLabelColumnOnlyWhenAnEdgeIsLabelled()
    {
        var graph = new Graph(
            [new GraphNode("a", "A"), new GraphNode("b", "B")],
            [new GraphEdge("a", "b") { Label = "3x" }]);

        var table = GraphLowering.ToEdgeTable(graph);

        Assert.Equal(["From", "To", "Label"], table.Headers);
        Assert.Equal(["A", "B", "3x"], table.Rows[0]);
    }

    [Fact]
    public void EdgeTable_UsesTheSuppliedLabelSelector()
    {
        var graph = new Graph(
            [new GraphNode("a", "A") { Emphasized = true }, new GraphNode("b", "B")],
            [new GraphEdge("a", "b")]);

        var table = GraphLowering.ToEdgeTable(graph, n => n.Emphasized ? $"**{n.Label}**" : n.Label);

        Assert.Equal(["**A**", "B"], table.Rows[0]);
    }

    [Fact]
    public void EdgeTable_KeepsARowPerParallelEdge()
    {
        var graph = new Graph(
            [new GraphNode("a", "A"), new GraphNode("b", "B")],
            [new GraphEdge("a", "b"), new GraphEdge("a", "b")]);

        Assert.Equal(2, GraphLowering.ToEdgeTable(graph).Rows.Count);
    }

    // ── Tree lowering ──

    [Fact]
    public void Tree_RootsAtTheFocusNode()
    {
        var graph = new Graph(
            [new GraphNode("a", "A"), new GraphNode("b", "B"), new GraphNode("c", "C")],
            [new GraphEdge("b", "a"), new GraphEdge("b", "c")],
            focusKey: "b");

        var tree = GraphLowering.ToTree(graph);

        var root = Assert.Single(tree);
        Assert.Equal("B", root.Text);
        Assert.Equal(["A", "C"], root.Children!.Select(c => c.Text));
    }

    [Fact]
    public void Tree_WithoutFocus_RootsAtNodesWithNoInboundEdge()
    {
        var tree = GraphLowering.ToTree(SimpleChain());

        var root = Assert.Single(tree);
        Assert.Equal("A", root.Text);
        Assert.Equal("B", root.Children![0].Text);
        Assert.Equal("C", root.Children[0].Children![0].Text);
    }

    [Fact]
    public void Tree_MarksARevisitInsteadOfExpandingItAgain()
    {
        // A -> B, A -> C, B -> C. C is reached twice; the second arrival must not re-expand.
        var graph = new Graph(
            [new GraphNode("a", "A"), new GraphNode("b", "B"), new GraphNode("c", "C"), new GraphNode("d", "D")],
            [new GraphEdge("a", "b"), new GraphEdge("a", "c"), new GraphEdge("b", "c"), new GraphEdge("c", "d")]);

        var root = Assert.Single(GraphLowering.ToTree(graph));

        var viaB = root.Children![0].Children![0];
        Assert.Equal("C", viaB.Text);
        Assert.Equal("D", Assert.Single(viaB.Children!).Text);

        var viaA = root.Children[1];
        Assert.Equal(GraphLowering.RevisitMarker + "C", viaA.Text);
        Assert.Null(viaA.Children);
    }

    [Fact]
    public void Tree_TerminatesOnACycle()
    {
        var graph = new Graph(
            [new GraphNode("a", "A"), new GraphNode("b", "B")],
            [new GraphEdge("a", "b"), new GraphEdge("b", "a")],
            focusKey: "a");

        var root = Assert.Single(GraphLowering.ToTree(graph));
        var b = Assert.Single(root.Children!);
        var back = Assert.Single(b.Children!);

        Assert.Equal(GraphLowering.RevisitMarker + "A", back.Text);
        Assert.Null(back.Children);
    }

    [Fact]
    public void Tree_AllNodesInACycleAndNoFocus_StillRendersFromTheFirstNode()
    {
        var graph = new Graph(
            [new GraphNode("a", "A"), new GraphNode("b", "B")],
            [new GraphEdge("a", "b"), new GraphEdge("b", "a")]);

        var root = Assert.Single(GraphLowering.ToTree(graph));
        Assert.Equal("A", root.Text);
    }

    [Fact]
    public void Tree_SurfacesNodesUnreachableFromTheFocus()
    {
        var graph = new Graph(
            [new GraphNode("a", "A"), new GraphNode("b", "B"), new GraphNode("orphan", "Orphan")],
            [new GraphEdge("a", "b")],
            focusKey: "a");

        var tree = GraphLowering.ToTree(graph);

        Assert.Equal(2, tree.Length);
        Assert.Equal("A", tree[0].Text);
        Assert.Equal("Orphan", tree[1].Text);
    }

    [Fact]
    public void Tree_QualifiesAChildWithItsEdgeLabel()
    {
        var graph = new Graph(
            [new GraphNode("a", "A"), new GraphNode("b", "B")],
            [new GraphEdge("a", "b") { Label = "2x" }],
            focusKey: "a");

        var root = Assert.Single(GraphLowering.ToTree(graph));
        Assert.Equal("B (2x)", Assert.Single(root.Children!).Text);
    }

    [Fact]
    public void Tree_EmptyGraphProducesNoRoots()
    {
        Assert.Empty(GraphLowering.ToTree(new Graph([], [])));
    }

    // ── Mermaid lowering ──

    [Fact]
    public void Mermaid_RendersAFlowchart()
    {
        var orch = MarkoutWriter.Create(new MermaidFormatter());
        Assert.True(orch.WriteGraph(SimpleChain()));

        var expected =
            "graph TD\n" +
            "    n0[\"A\"]\n" +
            "    n1[\"B\"]\n" +
            "    n2[\"C\"]\n" +
            "    n0 --> n1\n" +
            "    n1 --> n2";

        Assert.Equal(expected, Normalize(orch.ToString()));
    }

    [Fact]
    public void Mermaid_DeclaresTheFocusNodeFirstAndClassesIt()
    {
        var graph = new Graph(
            [new GraphNode("a", "A"), new GraphNode("b", "B")],
            [new GraphEdge("a", "b")],
            focusKey: "b");

        var orch = MarkoutWriter.Create(new MermaidFormatter());
        orch.WriteGraph(graph);
        var output = Normalize(orch.ToString());

        // Focus takes id n0 despite being second in node order.
        Assert.Contains("    n0[\"B\"]\n    n1[\"A\"]\n", output);
        Assert.Contains("    n1 --> n0\n", output);
        Assert.Contains("class n0 markoutFocus;", output);
    }

    [Fact]
    public void Mermaid_NeverEmitsTheCallerSuppliedKey()
    {
        var graph = new Graph(
            [new GraphNode("n0[\"pwned\"]", "Safe")],
            []);

        var orch = MarkoutWriter.Create(new MermaidFormatter());
        orch.WriteGraph(graph);

        Assert.DoesNotContain("pwned", orch.ToString());
    }

    [Fact]
    public void Mermaid_EscapesNodeLabels()
    {
        var graph = new Graph([new GraphNode("a", "List<T>")], []);

        var orch = MarkoutWriter.Create(new MermaidFormatter());
        orch.WriteGraph(graph);

        Assert.Contains("n0[\"List#60;T#62;\"]", orch.ToString());
    }

    [Fact]
    public void Mermaid_EscapesEdgeLabelsSoTheyCannotCloseTheirOwnEdge()
    {
        var graph = new Graph(
            [new GraphNode("a", "A"), new GraphNode("b", "B")],
            [new GraphEdge("a", "b") { Label = "\"| n0 --> n1 |\"" }]);

        var orch = MarkoutWriter.Create(new MermaidFormatter());
        orch.WriteGraph(graph);
        var output = orch.ToString();

        // The quote and pipe delimiters are escaped, and '>' is escaped too, so the injected
        // arrow cannot survive as syntax.
        Assert.Contains("-->|\"#quot;#124; n0 --#62; n1 #124;#quot;\"|", output);
        Assert.Equal(1, CountOccurrences(output, "-->"));
    }

    [Fact]
    public void Mermaid_EscapesGroupNames()
    {
        var graph = new Graph([new GraphNode("a", "A") { Group = "Sys<T>" }], []);

        var orch = MarkoutWriter.Create(new MermaidFormatter());
        orch.WriteGraph(graph);

        Assert.Contains("subgraph sg0[\"Sys#60;T#62;\"]", orch.ToString());
    }

    [Fact]
    public void Mermaid_PutsGroupedNodesInSubgraphsAndEdgesOutside()
    {
        var graph = new Graph(
            [
                new GraphNode("a", "A") { Group = "One" },
                new GraphNode("b", "B") { Group = "Two" },
                new GraphNode("c", "C"),
            ],
            [new GraphEdge("a", "b")]);

        var orch = MarkoutWriter.Create(new MermaidFormatter());
        orch.WriteGraph(graph);

        var expected =
            "graph TD\n" +
            "    n2[\"C\"]\n" +
            "    subgraph sg0[\"One\"]\n" +
            "        n0[\"A\"]\n" +
            "    end\n" +
            "    subgraph sg1[\"Two\"]\n" +
            "        n1[\"B\"]\n" +
            "    end\n" +
            "    n0 --> n1";

        Assert.Equal(expected, Normalize(orch.ToString()));
    }

    [Fact]
    public void Mermaid_ClassesEmphasizedNodesTogether()
    {
        var graph = new Graph(
            [
                new GraphNode("a", "A") { Emphasized = true },
                new GraphNode("b", "B"),
                new GraphNode("c", "C") { Emphasized = true },
            ],
            []);

        var orch = MarkoutWriter.Create(new MermaidFormatter());
        orch.WriteGraph(graph);

        Assert.Contains("class n0,n2 markoutEmphasis;", Normalize(orch.ToString()));
    }

    [Fact]
    public void Mermaid_StyleDeclarationsCarryNoHashSoMermaidsGuardCannotTruncateThem()
    {
        var graph = new Graph(
            [new GraphNode("a", "A") { Emphasized = true }],
            [],
            focusKey: "a");

        var orch = MarkoutWriter.Create(new MermaidFormatter());
        orch.WriteGraph(graph);

        foreach (var line in Normalize(orch.ToString()).Split('\n'))
        {
            if (line.Contains("classDef") || line.TrimStart().StartsWith("style", StringComparison.Ordinal))
                Assert.DoesNotContain('#', line);
        }
    }

    [Fact]
    public void Mermaid_FocusOutranksEmphasisForTheSameNode()
    {
        var graph = new Graph(
            [new GraphNode("a", "A") { Emphasized = true }],
            [],
            focusKey: "a");

        var orch = MarkoutWriter.Create(new MermaidFormatter());
        orch.WriteGraph(graph);
        var output = Normalize(orch.ToString());

        Assert.Contains("class n0 markoutFocus;", output);
        Assert.DoesNotContain("markoutEmphasis", output);
    }

    // ── Cross-formatter behavior ──

    [Fact]
    public void Markdown_RendersAnEdgeTable()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        Assert.True(orch.WriteGraph(SimpleChain()));

        var output = orch.ToString();
        Assert.Contains("| From | To |", output);
        Assert.Contains("| A | B |", output);
        Assert.Contains("| B | C |", output);
    }

    [Fact]
    public void Markdown_EmphasizesANodeWithoutReplacingIt()
    {
        var graph = new Graph(
            [new GraphNode("a", "A") { Emphasized = true }, new GraphNode("b", "B")],
            [new GraphEdge("a", "b")]);

        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        orch.WriteGraph(graph);

        Assert.Contains("**A**", orch.ToString());
    }

    [Fact]
    public void PlainText_RendersATreeRootedAtTheFocus()
    {
        var graph = new Graph(
            [new GraphNode("a", "A"), new GraphNode("b", "B")],
            [new GraphEdge("a", "b")],
            focusKey: "a");

        var orch = MarkoutWriter.Create(new PlainTextFormatter());
        Assert.True(orch.WriteGraph(graph));

        var output = orch.ToString();
        Assert.Contains("A", output);
        Assert.Contains("B", output);
        Assert.DoesNotContain("graph TD", output);
    }

    [Fact]
    public void EveryLoweringSeesEveryNode()
    {
        var graph = new Graph(
            [new GraphNode("a", "Alpha"), new GraphNode("b", "Beta"), new GraphNode("c", "Gamma")],
            [new GraphEdge("a", "b"), new GraphEdge("c", "b")]);

        foreach (var formatter in new IMarkoutFormatter[]
        {
            new MermaidFormatter(), new MarkdownFormatter(), new PlainTextFormatter(),
            new DiagramFormatter(), new UnicodeFormatter(), new TableFormatter(),
        })
        {
            var orch = MarkoutWriter.Create(formatter);
            Assert.True(orch.WriteGraph(graph), $"{formatter.GetType().Name} did not render a graph.");

            var output = orch.ToString();
            foreach (var label in new[] { "Alpha", "Beta", "Gamma" })
                Assert.True(output.Contains(label), $"{formatter.GetType().Name} dropped '{label}'.");
        }
    }

    // ── Writer integration ──

    [Fact]
    public void WriteGraph_EmptyGraphIsANoOpAndStillReportsSuccess()
    {
        var orch = MarkoutWriter.Create(new MermaidFormatter());
        Assert.True(orch.WriteGraph(new Graph([], [])));
        Assert.Equal("", orch.ToString());
    }

    [Fact]
    public void WriteGraph_ReturnsFalseWhenTheFormatterCannotRenderGraphs()
    {
        var orch = MarkoutWriter.Create(new GraphlessFormatter());
        Assert.False(orch.WriteGraph(SimpleChain()));
    }

    [Fact]
    public void WriteGraph_RejectsNull()
    {
        var orch = MarkoutWriter.Create(new MermaidFormatter());
        Assert.Throws<ArgumentNullException>(() => orch.WriteGraph(null!));
    }

    [Fact]
    public void MarkoutShape_AllIncludesGraphs()
    {
        Assert.True(MarkoutShape.All.HasFlag(MarkoutShape.Graphs));
    }

    private sealed class GraphlessFormatter : IMarkoutFormatter, IHeadingFormatter
    {
        void IHeadingFormatter.FormatHeading(TextWriter w, int level, string text, string? context)
            => w.Write(text);
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n");

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }
        return count;
    }
}
