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
    /// Optional badge to display before the text (e.g., "🔴", "📁").
    /// </summary>
    public string? Badge { get; set; }
    
    /// <summary>
    /// Child nodes, if any.
    /// </summary>
    public List<TreeNode>? Children { get; set; }
    
    /// <summary>
    /// Creates a tree node with an optional list of children.
    /// </summary>
    public TreeNode(string text, IEnumerable<TreeNode>? children = null)
    {
        Text = text;
        Children = children?.ToList();
    }
    
    /// <summary>
    /// Creates a tree node with a badge and optional list of children.
    /// </summary>
    public TreeNode(string text, string? badge, IEnumerable<TreeNode>? children = null)
    {
        Text = text;
        Badge = badge;
        Children = children?.ToList();
    }
    
    /// <summary>
    /// Creates a tree node with string children (convenience for leaf nodes).
    /// </summary>
    public TreeNode(string text, IEnumerable<string>? children)
    {
        Text = text;
        Children = children?.Select(c => new TreeNode(c)).ToList();
    }
    
    /// <summary>
    /// Creates a tree node with a badge and string children.
    /// </summary>
    public TreeNode(string text, string? badge, IEnumerable<string>? children)
    {
        Text = text;
        Badge = badge;
        Children = children?.Select(c => new TreeNode(c)).ToList();
    }
    
    /// <summary>
    /// Creates a leaf node with no children.
    /// </summary>
    public TreeNode(string text) : this(text, (IEnumerable<TreeNode>?)null)
    {
    }
    
    /// <summary>
    /// Creates a leaf node with a badge and no children.
    /// </summary>
    public TreeNode(string text, string? badge) : this(text, badge, (IEnumerable<TreeNode>?)null)
    {
    }
}
