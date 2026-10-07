namespace Markout.Formatting;

/// <summary>
/// Capability interface for streaming tree nodes without retaining a
/// <see cref="TreeNode"/> hierarchy.
/// </summary>
/// <remarks>
/// Implement this interface directly when the formatter is stateless across
/// one tree. Stateful formatters should implement
/// <see cref="ITreeStreamingSessionFactory"/> so each tree receives isolated
/// parent, id, and traversal state.
/// </remarks>
public interface IStreamingTreeFormatter
{
    /// <summary>
    /// Begins one tree.
    /// </summary>
    /// <param name="writer">The output writer.</param>
    /// <param name="options">Writer options for format-specific settings.</param>
    void BeginTree(
        TextWriter writer,
        MarkoutWriterOptions options);

    /// <summary>
    /// Writes one tree node.
    /// </summary>
    /// <param name="writer">The output writer.</param>
    /// <param name="text">The node label.</param>
    /// <param name="state">The structural state of the node.</param>
    /// <param name="badge">An optional badge rendered before the label.</param>
    /// <param name="isLastSibling">Whether this is the last node in its sibling population.</param>
    void WriteNode(
        TextWriter writer,
        string text,
        TreeNodeState state,
        string? badge,
        bool isLastSibling);

    /// <summary>
    /// Begins the children of the node most recently written.
    /// </summary>
    void BeginChildren();

    /// <summary>
    /// Ends the current child population.
    /// </summary>
    void EndChildren();

    /// <summary>
    /// Ends the tree.
    /// </summary>
    /// <param name="writer">The output writer.</param>
    void EndTree(TextWriter writer);
}
