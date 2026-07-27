using System.Collections.Immutable;

namespace Markout;

/// <summary>
/// The format-neutral lowerings of a <see cref="Graph"/> into shapes an existing formatter can
/// already render: an edge table, or a tree rooted at the focus node.
/// </summary>
/// <remarks>
/// These live here, once, rather than in each formatter, so that every sink agrees on which edges
/// exist, which node roots a tree, and how a revisit is spelled. A formatter is then only
/// responsible for its own syntax.
/// </remarks>
public static class GraphLowering
{
    /// <summary>The marker prefixed to a node that has already appeared earlier in a tree lowering.</summary>
    public const string RevisitMarker = "\u21a9 ";

    /// <summary>An edge list projected as a table.</summary>
    /// <param name="Headers">Column headers. Optional columns are present only when the graph uses them.</param>
    /// <param name="Rows">One row per edge, in graph order.</param>
    public readonly record struct GraphEdgeTable(ImmutableArray<string> Headers, List<string[]> Rows);

    /// <summary>
    /// Projects the graph as one row per edge.
    /// </summary>
    /// <remarks>
    /// Group columns appear only when at least one node has a <see cref="GraphNode.Group"/>, and the
    /// label column only when at least one edge has a <see cref="GraphEdge.Label"/>, so a plain graph
    /// produces a plain two-column table.
    /// </remarks>
    /// <param name="graph">The graph to project.</param>
    /// <param name="label">
    /// Optional per-node display selector, letting a sink that supports emphasis decorate a node.
    /// Defaults to <see cref="GraphNode.Label"/>.
    /// </param>
    public static GraphEdgeTable ToEdgeTable(Graph graph, Func<GraphNode, string>? label = null)
    {
        ArgumentNullException.ThrowIfNull(graph);
        label ??= static n => n.Label;

        var hasGroups = false;
        foreach (var node in graph.Nodes)
        {
            if (!string.IsNullOrEmpty(node.Group))
            {
                hasGroups = true;
                break;
            }
        }

        var hasEdgeLabels = false;
        foreach (var edge in graph.Edges)
        {
            if (!string.IsNullOrEmpty(edge.Label))
            {
                hasEdgeLabels = true;
                break;
            }
        }

        var headers = ImmutableArray.CreateBuilder<string>();
        headers.Add("From");
        if (hasGroups) headers.Add("From Group");
        headers.Add("To");
        if (hasGroups) headers.Add("To Group");
        if (hasEdgeLabels) headers.Add("Label");

        var rows = new List<string[]>(graph.Edges.Length);
        foreach (var edge in graph.Edges)
        {
            var from = graph[edge.From];
            var to = graph[edge.To];

            var row = new string[headers.Count];
            var i = 0;
            row[i++] = label(from);
            if (hasGroups) row[i++] = from.Group ?? "";
            row[i++] = label(to);
            if (hasGroups) row[i++] = to.Group ?? "";
            if (hasEdgeLabels) row[i] = edge.Label ?? "";
            rows.Add(row);
        }

        return new GraphEdgeTable(headers.ToImmutable(), rows);
    }

    /// <summary>
    /// Projects the graph as a tree by following edge direction outward from a root.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The root is <see cref="Graph.FocusKey"/> when set. Otherwise every node with no inbound edge
    /// is a root, which is the set a reader would consider a starting point. A graph whose nodes are
    /// all inside cycles has no such node, so the first node is used rather than rendering nothing.
    /// </para>
    /// <para>
    /// A node is expanded the first time it is reached; any later arrival renders as a leaf prefixed
    /// with <see cref="RevisitMarker"/>. That terminates cycles and keeps a shared node from being
    /// duplicated as though it were several distinct nodes.
    /// </para>
    /// <para>
    /// Nodes not reachable from any root are appended as additional roots, so a lowering never
    /// silently drops part of the graph.
    /// </para>
    /// </remarks>
    public static ImmutableArray<TreeNode> ToTree(Graph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        if (graph.IsEmpty)
            return [];

        var outgoing = BuildAdjacency(graph);
        var expanded = new HashSet<string>(StringComparer.Ordinal);
        var roots = ImmutableArray.CreateBuilder<TreeNode>();

        foreach (var key in RootKeys(graph))
        {
            if (expanded.Contains(key))
                continue;
            roots.Add(Expand(graph, outgoing, key, graph[key].Label, expanded));
        }

        // Anything left is unreachable from the chosen roots; surface it rather than lose it.
        foreach (var node in graph.Nodes)
        {
            if (expanded.Contains(node.Key))
                continue;
            roots.Add(Expand(graph, outgoing, node.Key, node.Label, expanded));
        }

        return roots.ToImmutable();
    }

    private static IEnumerable<string> RootKeys(Graph graph)
    {
        if (graph.FocusKey is not null)
        {
            yield return graph.FocusKey;
            yield break;
        }

        var hasInbound = new HashSet<string>(StringComparer.Ordinal);
        foreach (var edge in graph.Edges)
        {
            if (!string.Equals(edge.From, edge.To, StringComparison.Ordinal))
                hasInbound.Add(edge.To);
        }

        var any = false;
        foreach (var node in graph.Nodes)
        {
            if (hasInbound.Contains(node.Key))
                continue;
            any = true;
            yield return node.Key;
        }

        if (!any)
            yield return graph.Nodes[0].Key;
    }

    private static Dictionary<string, List<GraphEdge>> BuildAdjacency(Graph graph)
    {
        var outgoing = new Dictionary<string, List<GraphEdge>>(StringComparer.Ordinal);
        foreach (var edge in graph.Edges)
        {
            if (!outgoing.TryGetValue(edge.From, out var list))
            {
                list = [];
                outgoing[edge.From] = list;
            }
            list.Add(edge);
        }
        return outgoing;
    }

    private static TreeNode Expand(
        Graph graph,
        Dictionary<string, List<GraphEdge>> outgoing,
        string key,
        string text,
        HashSet<string> expanded)
    {
        if (!expanded.Add(key))
            return new TreeNode(RevisitMarker + text);

        var node = new TreeNode(text);
        if (!outgoing.TryGetValue(key, out var edges))
            return node;

        var children = new List<TreeNode>(edges.Count);
        foreach (var edge in edges)
        {
            var target = graph[edge.To];
            var childText = string.IsNullOrEmpty(edge.Label)
                ? target.Label
                : $"{target.Label} ({edge.Label})";
            children.Add(Expand(graph, outgoing, edge.To, childText, expanded));
        }

        node.Children = children;
        return node;
    }
}
