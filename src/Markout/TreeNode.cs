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
    /// Creates a tree node. Pass <paramref name="children"/> (a list, array, or
    /// collection expression) to add child nodes, or omit for a leaf. Set
    /// <see cref="Badge"/> via an object initializer if needed.
    /// </summary>
    public TreeNode(string text, IEnumerable<TreeNode>? children = null)
    {
        Text = text;
        Children = children?.ToList();
    }
}
