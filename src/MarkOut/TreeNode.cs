namespace MarkOut;

/// <summary>
/// Represents a node in a tree structure for hierarchical rendering.
/// </summary>
public class TreeNode
{
    /// <summary>
    /// The display text for this node.
    /// </summary>
    public string Label { get; set; }
    
    /// <summary>
    /// Child nodes, if any.
    /// </summary>
    public List<TreeNode>? Children { get; set; }
    
    /// <summary>
    /// Creates a tree node with an optional list of children.
    /// </summary>
    public TreeNode(string label, IEnumerable<TreeNode>? children = null)
    {
        Label = label;
        Children = children?.ToList();
    }
    
    /// <summary>
    /// Creates a tree node with string children (convenience for leaf nodes).
    /// </summary>
    public TreeNode(string label, IEnumerable<string>? children)
    {
        Label = label;
        Children = children?.Select(c => new TreeNode(c)).ToList();
    }
    
    /// <summary>
    /// Creates a leaf node with no children.
    /// </summary>
    public TreeNode(string label) : this(label, (IEnumerable<TreeNode>?)null)
    {
    }
}
