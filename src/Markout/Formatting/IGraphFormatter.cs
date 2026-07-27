namespace Markout.Formatting;

/// <summary>
/// Capability interface for rendering a directed <see cref="Graph"/>.
/// </summary>
/// <remarks>
/// Implementations lower the graph into whatever their format can express — a flowchart, an edge
/// table, or a tree rooted at the focus node. <see cref="GraphLowering"/> provides the shared,
/// format-neutral lowerings so an implementation only has to render, not re-derive.
/// </remarks>
public interface IGraphFormatter
{
    /// <summary>Renders a directed graph.</summary>
    void FormatGraph(TextWriter writer, Graph graph, MarkoutWriterOptions options);
}
