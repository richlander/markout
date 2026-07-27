using System.Collections.Immutable;

namespace Markout;

/// <summary>
/// A directed graph shape, a peer to the tree shape. Hand Markout the graph and let each
/// formatter lower it, instead of generating format-specific text per call site.
/// </summary>
/// <remarks>
/// <para>
/// A tree cannot express this: the same entity may appear in more than one relationship, so node
/// identity has to be deduplicated rather than repeated. <see cref="GraphNode.Key"/> carries that
/// identity and is used for nothing else.
/// </para>
/// <para>
/// The shape is validated on construction. Duplicate keys, an edge naming a key that has no node,
/// and a <see cref="FocusKey"/> that names no node are all errors, so a malformed graph fails
/// where it is built rather than rendering as a plausible-looking but wrong diagram.
/// </para>
/// </remarks>
public sealed class Graph
{
    private readonly Dictionary<string, int> _indexByKey;

    /// <summary>The nodes, in the order supplied.</summary>
    public ImmutableArray<GraphNode> Nodes { get; }

    /// <summary>The edges, in the order supplied. Parallel edges are preserved.</summary>
    public ImmutableArray<GraphEdge> Edges { get; }

    /// <summary>
    /// The key of the node the graph is centred on, if any. A formatter uses it to decide where to
    /// anchor a diagram, and a text lowering uses it to root a tree without having to guess.
    /// </summary>
    public string? FocusKey { get; }

    /// <summary>Creates and validates a graph.</summary>
    /// <param name="nodes">The nodes. Keys must be unique.</param>
    /// <param name="edges">The edges. Every endpoint must name a node in <paramref name="nodes"/>.</param>
    /// <param name="focusKey">Optional key of the focus node. Must name a node when supplied.</param>
    /// <exception cref="ArgumentException">
    /// A node or edge element is null, a node key is duplicated, an edge names an unknown key, or
    /// <paramref name="focusKey"/> names an unknown key.
    /// </exception>
    public Graph(IEnumerable<GraphNode> nodes, IEnumerable<GraphEdge> edges, string? focusKey = null)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(edges);

        Nodes = [.. nodes];
        Edges = [.. edges];
        FocusKey = focusKey;

        _indexByKey = new Dictionary<string, int>(Nodes.Length, StringComparer.Ordinal);
        for (var i = 0; i < Nodes.Length; i++)
        {
            var node = Nodes[i];
            if (node is null)
                throw new ArgumentException($"Node at index {i} is null.", nameof(nodes));
            if (!_indexByKey.TryAdd(node.Key, i))
                throw new ArgumentException($"Duplicate node key '{node.Key}'.", nameof(nodes));
        }

        for (var i = 0; i < Edges.Length; i++)
        {
            var edge = Edges[i];
            if (edge is null)
                throw new ArgumentException($"Edge at index {i} is null.", nameof(edges));
            if (!_indexByKey.ContainsKey(edge.From))
                throw new ArgumentException($"Edge references unknown node key '{edge.From}'.", nameof(edges));
            if (!_indexByKey.ContainsKey(edge.To))
                throw new ArgumentException($"Edge references unknown node key '{edge.To}'.", nameof(edges));
        }

        if (focusKey is not null && !_indexByKey.ContainsKey(focusKey))
            throw new ArgumentException($"Focus references unknown node key '{focusKey}'.", nameof(focusKey));
    }

    /// <summary>Whether the graph has no nodes.</summary>
    public bool IsEmpty => Nodes.Length == 0;

    /// <summary>The focus node, or <c>null</c> when no focus was supplied.</summary>
    public GraphNode? Focus => FocusKey is null ? null : Nodes[_indexByKey[FocusKey]];

    /// <summary>Gets the node with <paramref name="key"/>.</summary>
    /// <exception cref="KeyNotFoundException">No node has that key.</exception>
    public GraphNode this[string key] => Nodes[_indexByKey[key]];

    /// <summary>Gets the node with <paramref name="key"/>, if present.</summary>
    public bool TryGetNode(string key, out GraphNode node)
    {
        if (key is not null && _indexByKey.TryGetValue(key, out var index))
        {
            node = Nodes[index];
            return true;
        }

        node = null!;
        return false;
    }

    /// <summary>The position of <paramref name="key"/> in <see cref="Nodes"/>, or <c>-1</c>.</summary>
    public int IndexOf(string key)
        => key is not null && _indexByKey.TryGetValue(key, out var index) ? index : -1;
}
