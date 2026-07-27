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

    // ── Regression coverage from adversarial review of #163 ──

    [Fact]
    public void Graph_RejectsANullNodeElementNamingTheParameterAndIndex()
    {
        var ex = Assert.Throws<ArgumentException>(() => new Graph(
            [new GraphNode("a", "A"), null!],
            []));
        Assert.Equal("nodes", ex.ParamName);
        Assert.Contains("index 1", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Graph_RejectsANullEdgeElementNamingTheParameterAndIndex()
    {
        var ex = Assert.Throws<ArgumentException>(() => new Graph(
            [new GraphNode("a", "A")],
            [null!]));
        Assert.Equal("edges", ex.ParamName);
        Assert.Contains("index 0", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GraphNode_RejectsAnEmptyLabelBecauseNoDiagramSyntaxCanSpellOne()
    {
        Assert.Throws<ArgumentException>(() => new GraphNode("a", ""));
    }

    [Fact]
    public void Graph_TryGetNode_FindsAKnownKeyAndRejectsAnUnknownOne()
    {
        var graph = SimpleChain();

        Assert.True(graph.TryGetNode("b", out var found));
        Assert.Equal("B", found.Label);
        Assert.False(graph.TryGetNode("zzz", out var missing));
        Assert.Null(missing);
        Assert.False(graph.TryGetNode(null!, out _));
    }

    [Fact]
    public void Graph_IndexOf_ReturnsThePositionOrMinusOne()
    {
        var graph = SimpleChain();

        Assert.Equal(0, graph.IndexOf("a"));
        Assert.Equal(2, graph.IndexOf("c"));
        Assert.Equal(-1, graph.IndexOf("zzz"));
        Assert.Equal(-1, graph.IndexOf(null!));
    }

    [Fact]
    public void EdgeTable_ListsAnIsolatedNodeSoTheTableStillSeesEveryNode()
    {
        var graph = new Graph(
            [new GraphNode("a", "A"), new GraphNode("b", "B"), new GraphNode("lonely", "Lonely")],
            [new GraphEdge("a", "b")]);

        var table = GraphLowering.ToEdgeTable(graph);

        Assert.Equal(2, table.Rows.Count);
        Assert.Equal(["A", "B"], table.Rows[0]);
        // The isolated node is a From with no To rather than a row that never appears.
        Assert.Equal(["Lonely", ""], table.Rows[1]);
    }

    [Fact]
    public void EdgeTable_IsolatedNodeRowFillsEveryOptionalColumn()
    {
        var graph = new Graph(
            [
                new GraphNode("a", "A") { Group = "Core" },
                new GraphNode("b", "B") { Group = "Core" },
                new GraphNode("lonely", "Lonely") { Group = "Edge" },
            ],
            [new GraphEdge("a", "b") { Label = "calls" }]);

        var table = GraphLowering.ToEdgeTable(graph);

        Assert.Equal(["From", "From Group", "To", "To Group", "Label"], table.Headers);
        Assert.Equal(["A", "Core", "B", "Core", "calls"], table.Rows[0]);
        Assert.Equal(["Lonely", "Edge", "", "", ""], table.Rows[1]);
        Assert.All(table.Rows, row => Assert.Equal(table.Headers.Length, row.Length));
    }

    [Fact]
    public void EdgeTable_ANodeThatIsOnlyEverATargetIsNotTreatedAsIsolated()
    {
        var graph = SimpleChain();

        var table = GraphLowering.ToEdgeTable(graph);

        // "c" only ever appears as a target, so it needs no synthetic row.
        Assert.Equal(2, table.Rows.Count);
    }

    [Fact]
    public void EdgeTable_ASelectorThatReturnsNullYieldsAnEmptyCellNotANullCell()
    {
        var graph = SimpleChain();

        var table = GraphLowering.ToEdgeTable(graph, _ => null!);

        Assert.All(table.Rows, row => Assert.All(row, Assert.NotNull));
    }

    [Fact]
    public void MarkdownGraph_RendersAnIsolatedNode()
    {
        var graph = new Graph([new GraphNode("lonely", "Lonely")], []);

        var writer = MarkoutWriter.Create(new MarkdownFormatter());
        writer.WriteGraph(graph);

        Assert.Contains("Lonely", writer.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Tree_HandlesADeepChainWithoutExhaustingTheCallStack()
    {
        // The lowering walks with an explicit stack, so depth is bounded by memory. A depth this
        // large overflowed the stack while the traversal was recursive.
        const int Depth = 50_000;
        var nodes = new GraphNode[Depth];
        var edges = new GraphEdge[Depth - 1];
        for (var i = 0; i < Depth; i++)
            nodes[i] = new GraphNode(i.ToString(), "m" + i);
        for (var i = 0; i < Depth - 1; i++)
            edges[i] = new GraphEdge(i.ToString(), (i + 1).ToString());

        var roots = GraphLowering.ToTree(new Graph(nodes, edges));

        var root = Assert.Single(roots);
        var depth = 0;
        for (var node = root; node is not null; node = node.Children is [var only, ..] ? only : null)
            depth++;
        Assert.Equal(Depth, depth);
    }

    [Fact]
    public void Tree_ExpandsASelfLoopAsASingleRevisitChild()
    {
        var graph = new Graph(
            [new GraphNode("a", "A")],
            [new GraphEdge("a", "a")],
            focusKey: "a");

        var root = Assert.Single(GraphLowering.ToTree(graph));

        Assert.Equal("A", root.Text);
        var child = Assert.Single(root.Children!);
        Assert.Equal(GraphLowering.RevisitMarker + "A", child.Text);
        Assert.Null(child.Children);
    }

    [Fact]
    public void Tree_ALeafHasNoChildrenListRatherThanAnEmptyOne()
    {
        var root = Assert.Single(GraphLowering.ToTree(SimpleChain()));

        var last = root.Children![0].Children![0];
        Assert.Equal("C", last.Text);
        Assert.Null(last.Children);
    }

    [Fact]
    public void Mermaid_EscapesAGroupTitleThatTriesToCloseItsOwnSubgraph()
    {
        var graph = new Graph(
            [new GraphNode("a", "A") { Group = "evil\"]\nend\ngraph TD\n" }],
            []);

        var writer = MarkoutWriter.Create(new MermaidFormatter());
        writer.WriteGraph(graph);
        var lines = Normalize(writer.ToString()).Split('\n');

        // The payload survives as inert text inside the quoted title; what must not happen is it
        // becoming structure. Exactly one flowchart header line and one `end` line, and the whole
        // title stays on a single line because its newlines are escaped.
        Assert.Equal(1, lines.Count(line => line.Trim() == "graph TD"));
        Assert.Equal(1, lines.Count(line => line.Trim() == "end"));
        Assert.Equal(1, lines.Count(line => line.Contains("subgraph", StringComparison.Ordinal)));
    }

    [Fact]
    public void Mermaid_KeepsRepeatedGroupsTogetherInFirstSeenOrder()
    {
        var graph = new Graph(
            [
                new GraphNode("a", "A") { Group = "Second" },
                new GraphNode("b", "B") { Group = "First" },
                new GraphNode("c", "C") { Group = "Second" },
            ],
            []);

        var writer = MarkoutWriter.Create(new MermaidFormatter());
        writer.WriteGraph(graph);
        var output = Normalize(writer.ToString());

        // "Second" is declared once, at the position of its first member, and holds both nodes.
        Assert.Equal(1, CountOccurrences(output, "\"Second\""));
        Assert.True(output.IndexOf("\"Second\"", StringComparison.Ordinal) < output.IndexOf("\"First\"", StringComparison.Ordinal));
    }

    [Fact]
    public void EmptyGraph_EveryLoweringAndFormatterHandlesItDirectly()
    {
        // WriteGraph short-circuits an empty graph, so exercise the lowerings and the formatters
        // themselves rather than relying on the writer's guard.
        var empty = new Graph([], []);

        Assert.Empty(GraphLowering.ToTree(empty));
        Assert.Empty(GraphLowering.ToEdgeTable(empty).Rows);

        IMarkoutFormatter[] formatters =
        [
            new MermaidFormatter(),
            new MarkdownFormatter(),
            new TableFormatter(),
            new PlainTextFormatter(),
            new DiagramFormatter(),
            new UnicodeFormatter(),
        ];

        foreach (var formatter in formatters)
        {
            var sink = new StringWriter();
            ((IGraphFormatter)formatter).FormatGraph(sink, empty, new MarkoutWriterOptions());
            Assert.Equal("", sink.ToString());
        }
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
