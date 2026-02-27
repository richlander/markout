namespace Markout.Formatting;

/// <summary>
/// Capability interface for rendering tree structures.
/// </summary>
public interface ITreeFormatter
{
    /// <summary>
    /// Renders a tree structure from root nodes.
    /// </summary>
    void FormatTree(TextWriter writer, ReadOnlySpan<TreeNode> nodes, MarkoutWriterOptions options);

    /// <summary>
    /// Renders a single tree node with a prefix for hierarchy display.
    /// </summary>
    void FormatTreeNode(TextWriter writer, string text, string prefix);
}
