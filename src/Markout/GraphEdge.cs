namespace Markout;

/// <summary>
/// A directed edge between two <see cref="GraphNode"/> keys.
/// </summary>
/// <remarks>
/// Direction is explicit and always points from <see cref="From"/> to <see cref="To"/>. A caller
/// holding an inverted relationship (such as "who calls me") is responsible for inverting it once
/// when building the graph, rather than leaving each formatter to reinterpret the direction.
/// Parallel edges are preserved: two edges with the same endpoints are two edges.
/// </remarks>
public sealed class GraphEdge
{
    /// <summary>The <see cref="GraphNode.Key"/> the edge points away from.</summary>
    public string From { get; }

    /// <summary>The <see cref="GraphNode.Key"/> the edge points to.</summary>
    public string To { get; }

    /// <summary>Optional display text for the edge.</summary>
    public string? Label { get; init; }

    /// <summary>Creates a directed edge.</summary>
    public GraphEdge(string from, string to)
    {
        ArgumentException.ThrowIfNullOrEmpty(from);
        ArgumentException.ThrowIfNullOrEmpty(to);
        From = from;
        To = to;
    }
}
