namespace Markout;

/// <summary>
/// Writes tree structures to a TextWriter using ASCII art connectors.
/// Trees are a universal shape — no formatter interface needed.
/// Document state is managed by the caller or <see cref="MarkoutOrchestrator"/>.
/// </summary>
public class TreeWriter(TextWriter writer, MarkoutWriterOptions? options = null)
{
    private readonly MarkoutWriterOptions _options = options ?? new();

    /// <summary>
    /// Writes a tree node with optional prefix for hierarchy.
    /// </summary>
    public void WriteTreeNode(string text, string prefix = "")
    {
        writer.Write(prefix);
        writer.WriteLine(text);
    }

    /// <summary>
    /// Writes a tree structure from a list of TreeNode objects.
    /// </summary>
    public void WriteTree(params ReadOnlySpan<TreeNode> nodes)
    {
        if (nodes.Length == 0) return;

        for (int i = 0; i < nodes.Length; i++)
        {
            var isLast = i == nodes.Length - 1;
            WriteTreeNodeRecursive(nodes[i], "", isLast);
        }
    }

    private void WriteTreeNodeRecursive(TreeNode node, string prefix, bool isLast)
    {
        writer.Write(prefix);
        writer.Write(isLast ? "└─ " : "├─ ");
        if (node.Badge != null && _options.IncludeBadges)
        {
            writer.Write(node.Badge);
            writer.Write(' ');
        }
        writer.WriteLine(node.Text);

        if (node.Children != null && node.Children.Count > 0)
        {
            var childPrefix = prefix + (isLast ? "   " : "│  ");
            for (int i = 0; i < node.Children.Count; i++)
            {
                var isChildLast = i == node.Children.Count - 1;
                WriteTreeNodeRecursive(node.Children[i], childPrefix, isChildLast);
            }
        }
    }
}
