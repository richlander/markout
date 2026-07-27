namespace Markout;

/// <summary>
/// A node in a <see cref="Graph"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Key"/> is opaque to Markout: it wires edges to nodes and nothing else. It is
/// never written to the output, and formatters allocate their own emitted identifiers, so
/// caller data never reaches a structural position in the generated syntax.
/// </para>
/// <para>
/// The type carries no status vocabulary — no "external", "truncated", or class names — and no
/// free-form property bag. A domain that needs those should map them onto <see cref="Group"/>,
/// <see cref="Emphasized"/>, and <see cref="Graph.FocusKey"/>, or render its own table, so that
/// domain styling does not leak into a general-purpose serializer.
/// </para>
/// </remarks>
public sealed class GraphNode
{
    /// <summary>
    /// The caller's opaque identity for this node, used only to match <see cref="GraphEdge.From"/>
    /// and <see cref="GraphEdge.To"/>. Never emitted.
    /// </summary>
    public string Key { get; }

    /// <summary>The display text for this node.</summary>
    public string Label { get; }

    /// <summary>
    /// Optional grouping. Formatters that can cluster (Mermaid subgraphs) use it as a container;
    /// formatters that cannot surface it as an extra column. Nodes sharing a value group together.
    /// </summary>
    public string? Group { get; init; }

    /// <summary>
    /// Marks this node as noteworthy. Emphasis augments a node, it never replaces information:
    /// sinks without a way to express it render the node unchanged.
    /// </summary>
    public bool Emphasized { get; init; }

    /// <summary>Creates a graph node.</summary>
    /// <param name="key">Opaque identity used for edge wiring. Not emitted.</param>
    /// <param name="label">Display text. Must be non-empty.</param>
    /// <remarks>
    /// An empty label is rejected rather than accepted and papered over: a diagram sink has no
    /// syntax for a label-less node (Mermaid's grammar requires text between its delimiters), so an
    /// empty label would silently produce output that does not parse.
    /// </remarks>
    public GraphNode(string key, string label)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentException.ThrowIfNullOrEmpty(label);
        Key = key;
        Label = label;
    }

    /// <summary>Creates a graph node whose display text is also its key.</summary>
    public GraphNode(string key)
        : this(key, key)
    {
    }
}
