using System.Text;
using Markout.Formatting;

namespace Markout;

/// <summary>
/// A formatter that outputs Mermaid diagram syntax.
/// Supports headings (as comments) and trees (as <c>graph TD</c> flowcharts).
/// Use standalone with <c>--mermaid</c> for raw mermaid output, or pair with
/// <c>MarkdownFormatter</c> via code blocks for embedded mermaid in markdown.
/// </summary>
public class MermaidFormatter : IMarkoutFormatter,
    IHeadingFormatter, ITreeFormatter, IGraphFormatter
{
    private int _nextNodeId;

    // ── IHeadingFormatter ──

    void IHeadingFormatter.FormatHeading(TextWriter w, int level, string text, string? context)
    {
        w.Write("%% ");
        w.Write(SingleLine(text));
        if (!string.IsNullOrEmpty(context))
        {
            w.Write(" (");
            w.Write(SingleLine(context));
            w.Write(')');
        }
    }

    // A Mermaid comment runs to end of line, so an embedded newline would end the
    // comment and let the remainder of the heading be parsed as diagram syntax.
    private static string SingleLine(string text)
        => text.Contains('\n') || text.Contains('\r')
            ? text.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ')
            : text;

    // ── ITreeFormatter ──

    void ITreeFormatter.FormatTree(TextWriter w, ReadOnlySpan<TreeNode> nodes, MarkoutWriterOptions options)
    {
        _nextNodeId = 0;
        w.WriteLine("graph TD");

        for (int i = 0; i < nodes.Length; i++)
            FormatSubgraph(w, nodes[i], parentId: null, options);
    }

    void ITreeFormatter.FormatTreeNode(TextWriter w, string text, string prefix)
    {
        // Standalone tree nodes are rendered as a single-node graph.
        w.Write("graph TD");
        w.WriteLine();
        var id = "n0";
        w.Write("    ");
        w.Write(id);
        w.Write("[\"");
        w.Write(EscapeLabel(text));
        w.Write("\"]");
        w.WriteLine();
    }

    private void FormatSubgraph(TextWriter w, TreeNode node, string? parentId, MarkoutWriterOptions options)
    {
        var id = $"n{_nextNodeId++}";
        var label = BuildLabel(node, options);

        w.Write("    ");
        w.Write(id);
        w.Write("[\"");
        w.Write(EscapeLabel(label));
        w.Write("\"]");
        w.WriteLine();

        if (parentId != null)
        {
            w.Write("    ");
            w.Write(parentId);
            w.Write(" --> ");
            w.Write(id);
            w.WriteLine();
        }

        if (node.Children is { Count: > 0 })
        {
            foreach (var child in node.Children)
                FormatSubgraph(w, child, id, options);
        }
    }

    private string BuildLabel(TreeNode node, MarkoutWriterOptions options)
    {
        var state = MarkoutGlyphs.NodeStatePrefix(node.State, options, this);
        if (node.Badge != null && options.IncludeBadges)
            return $"{state}{node.Badge} {node.Text}";
        return state + node.Text;
    }

    // ── IGraphFormatter ──

    private const string FocusClass = "markoutFocus";
    private const string EmphasisClass = "markoutEmphasis";

    /// <summary>
    /// Renders the graph as a <c>graph TD</c> flowchart.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Emitted ids are allocated here (<c>n0</c>, <c>n1</c>, …) and never derived from
    /// <see cref="GraphNode.Key"/>, so caller data cannot reach a structural position in the syntax
    /// and can only ever appear inside an escaped label.
    /// </para>
    /// <para>
    /// The focus node is declared first. Mermaid has no explicit "centre", but a flowchart ranks
    /// roughly in declaration order, so declaring the focus first anchors the diagram on it. It is
    /// also given its own class so it stays identifiable once laid out.
    /// </para>
    /// <para>
    /// Groups become subgraphs. A node can belong to at most one subgraph, so grouped nodes are
    /// declared inside their group and ungrouped nodes before them; edges are declared afterwards,
    /// which Mermaid permits and which keeps cross-group edges out of either subgraph body.
    /// </para>
    /// </remarks>
    void IGraphFormatter.FormatGraph(TextWriter w, Graph graph, MarkoutWriterOptions options)
    {
        if (graph.IsEmpty)
            return;

        var order = DisplayOrder(graph);
        var ids = new Dictionary<string, string>(graph.Nodes.Length, StringComparer.Ordinal);
        for (var i = 0; i < order.Length; i++)
            ids[graph.Nodes[order[i]].Key] = $"n{i}";

        w.WriteLine("graph TD");

        foreach (var index in order)
        {
            var node = graph.Nodes[index];
            if (string.IsNullOrEmpty(node.Group))
                WriteNode(w, ids[node.Key], node.Label, "    ");
        }

        var subgraphId = 0;
        foreach (var group in DistinctGroups(graph, order))
        {
            w.Write("    subgraph sg");
            w.Write(subgraphId++);
            w.Write("[\"");
            w.Write(EscapeLabel(group));
            w.WriteLine("\"]");

            foreach (var index in order)
            {
                var node = graph.Nodes[index];
                if (string.Equals(node.Group, group, StringComparison.Ordinal))
                    WriteNode(w, ids[node.Key], node.Label, "        ");
            }

            w.WriteLine("    end");
        }

        foreach (var edge in graph.Edges)
        {
            w.Write("    ");
            w.Write(ids[edge.From]);
            if (string.IsNullOrEmpty(edge.Label))
            {
                w.Write(" --> ");
            }
            else
            {
                // The quoted edge-label form puts the text in the same lexer state as a node label
                // (<string>, which consumes everything up to the closing quote), so the node-label
                // escape set covers it. The pipe delimiter is already escaped, so the label cannot
                // close its own edge.
                w.Write(" -->|\"");
                w.Write(EscapeLabel(edge.Label));
                w.Write("\"| ");
            }
            w.WriteLine(ids[edge.To]);
        }

        WriteClasses(w, graph, ids, order);
    }

    private static void WriteNode(TextWriter w, string id, string label, string indent)
    {
        w.Write(indent);
        w.Write(id);
        w.Write("[\"");
        w.Write(EscapeLabel(label));
        w.WriteLine("\"]");
    }

    private static void WriteClasses(TextWriter w, Graph graph, Dictionary<string, string> ids, int[] order)
    {
        var focus = graph.Focus;

        var emphasized = new List<string>();
        foreach (var index in order)
        {
            var node = graph.Nodes[index];
            if (node.Emphasized && !ReferenceEquals(node, focus))
                emphasized.Add(ids[node.Key]);
        }

        if (focus is not null)
        {
            // No '#' appears in these declarations: Mermaid pre-scans for style/classDef lines
            // containing a colour literal and strips their final character, which would corrupt them.
            w.Write("    classDef ");
            w.Write(FocusClass);
            w.WriteLine(" stroke-width:3px;");
            w.Write("    class ");
            w.Write(ids[focus.Key]);
            w.Write(' ');
            w.Write(FocusClass);
            w.WriteLine(";");
        }

        if (emphasized.Count > 0)
        {
            w.Write("    classDef ");
            w.Write(EmphasisClass);
            w.WriteLine(" stroke-dasharray:4 2;");
            w.Write("    class ");
            w.Write(string.Join(',', emphasized));
            w.Write(' ');
            w.Write(EmphasisClass);
            w.WriteLine(";");
        }
    }

    private static int[] DisplayOrder(Graph graph)
    {
        var focusIndex = graph.FocusKey is null ? -1 : graph.IndexOf(graph.FocusKey);
        var order = new int[graph.Nodes.Length];
        var next = 0;

        if (focusIndex >= 0)
            order[next++] = focusIndex;

        for (var i = 0; i < graph.Nodes.Length; i++)
        {
            if (i != focusIndex)
                order[next++] = i;
        }

        return order;
    }

    private static List<string> DistinctGroups(Graph graph, int[] order)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var groups = new List<string>();
        foreach (var index in order)
        {
            var group = graph.Nodes[index].Group;
            if (!string.IsNullOrEmpty(group) && seen.Add(group))
                groups.Add(group);
        }
        return groups;
    }

    /// <summary>
    /// Escapes text for use in a Mermaid node label (double-quoted form), so that
    /// arbitrary — including hostile — text renders literally instead of altering the
    /// diagram.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Mermaid's flowchart lexer reads a quoted label with a single rule
    /// (<c>&lt;string&gt;[^"]+</c>) and performs <em>no</em> backslash unescaping, so
    /// <c>\"</c> does not escape a quote and a doubled <c>\\</c> is not collapsed. The
    /// only portable escape is Mermaid's own entity form: <c>encodeEntities</c> rewrites
    /// <c>#NN;</c> / <c>#name;</c> into an HTML entity before rendering.
    /// </para>
    /// <para>
    /// Escaping <c>&lt;</c> and <c>&gt;</c> is required for correctness, not just safety.
    /// Flowcharts default to HTML labels, and at the default <c>securityLevel: "strict"</c>
    /// Mermaid passes label text through DOMPurify, which silently <em>drops</em> unknown
    /// tags. Unescaped text such as <c>List&lt;String&gt;</c> or <c>&lt;Foo&gt;b__0</c>
    /// therefore renders with the angle-bracket run deleted, so genuinely distinct nodes
    /// can appear identical.
    /// </para>
    /// <para>
    /// Backslash is escaped rather than passed through because Mermaid converts the
    /// two-character sequence <c>\n</c> into a line break while splitting label rows; a
    /// literal path such as <c>C:\new</c> would otherwise wrap mid-word.
    /// </para>
    /// </remarks>
    public static string EscapeLabel(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return NeedsEscaping(text) ? EscapeCore(text) : text;
    }

    private static bool NeedsEscaping(string text)
    {
        foreach (var ch in text)
        {
            if (Replacement(ch) is not null)
                return true;
        }
        return false;
    }

    private static string EscapeCore(string text)
    {
        var sb = new StringBuilder(text.Length + 16);
        foreach (var ch in text)
        {
            if (Replacement(ch) is { } entity)
                sb.Append(entity);
            else
                sb.Append(ch);
        }
        return sb.ToString();
    }

    // A single pass, not chained Replace calls: every replacement emits '#', so a
    // multi-pass scheme would re-escape its own output unless '#' were handled first.
    private static string? Replacement(char ch) => ch switch
    {
        '#' => "#35;",
        '"' => "#quot;",
        '<' => "#60;",
        '>' => "#62;",
        '&' => "#38;",
        '|' => "#124;",
        '\\' => "#92;",
        '\r' => "#13;",
        '\n' => "#10;",
        // A label is emitted as ["…], so a leading backtick forms the two-character
        // sequence ["` that Mermaid lexes as the start of a Markdown string. That rule
        // precedes (and outranks) the plain ["  rule, so the label would be parsed as
        // Markdown and the delimiters lost.
        '`' => "#96;",
        // Before decoding entities Mermaid runs two unanchored guards intended for real
        // style/classDef lines — /style.*:\S*#.*;/ and /classDef.*:\S*#.*;/ — each
        // stripping the final character of whatever they match. A label such as
        // "Lifestyle:C#" escapes to "Lifestyle:C#35;", which those guards truncate to
        // "…C#35", destroying the entity. They cannot match without a colon.
        ':' => "#58;",
        _ => null,
    };
}
