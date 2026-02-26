namespace Markout;

/// <summary>
/// Represents a node in a tree structure for hierarchical rendering.
/// </summary>
/// <example>
///   <code lang="cs" source="../../samples/Serialization/WriterUsage.cs" region="WriteTree" title="Tree rendering" />
/// </example>
/// <seealso href="../../samples/Serialization/WriterUsage.cs">Tree rendering example</seealso>
public class TreeNode
{
    /// <summary>
    /// The display text for this node.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Optional badge to display before the text (e.g., "📁", "🚢").
    /// </summary>
    public string? Badge { get; set; }

    /// <summary>
    /// Child nodes, if any.
    /// </summary>
    public List<TreeNode>? Children { get; set; }

    /// <summary>
    /// Creates a leaf node with optional badge.
    /// </summary>
    public TreeNode(string text, string? badge = null)
    {
        Text = text;
        Badge = badge;
    }

    /// <summary>
    /// Creates a tree node with children. Pass null for badge if not needed.
    /// </summary>
    public TreeNode(string text, string? badge, params ReadOnlySpan<TreeNode> children)
    {
        Text = text;
        Badge = badge;
        Children = children.Length > 0 ? [..children] : null;
    }
}
