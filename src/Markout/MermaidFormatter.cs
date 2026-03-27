using Markout.Formatting;

namespace Markout;

/// <summary>
/// A formatter that outputs Mermaid diagram syntax.
/// Supports headings (as comments) and trees (as <c>graph TD</c> flowcharts).
/// Use standalone with <c>--mermaid</c> for raw mermaid output, or pair with
/// <c>MarkdownFormatter</c> via code blocks for embedded mermaid in markdown.
/// </summary>
public class MermaidFormatter : IMarkoutFormatter,
    IHeadingFormatter, ITreeFormatter
{
    private int _nextNodeId;

    // ── IHeadingFormatter ──

    void IHeadingFormatter.FormatHeading(TextWriter w, int level, string text, string? context)
    {
        w.Write("%% ");
        w.Write(text);
        if (!string.IsNullOrEmpty(context))
        {
            w.Write(" (");
            w.Write(context);
            w.Write(')');
        }
    }

    // ── ITreeFormatter ──

    void ITreeFormatter.FormatTree(TextWriter w, ReadOnlySpan<TreeNode> nodes, MarkoutWriterOptions options)
    {
        _nextNodeId = 0;
        w.WriteLine("graph TD");

        for (int i = 0; i < nodes.Length; i++)
            FormatSubgraph(w, nodes[i], parentId: null, options);
    }

    void ITreeFormatter.FormatTreeNode(TextWriter w, string text, string prefix)
    {
        // Standalone tree nodes are rendered as a single-node graph.
        w.Write("graph TD");
        w.WriteLine();
        var id = "n0";
        w.Write("    ");
        w.Write(id);
        w.Write("[\"");
        w.Write(EscapeLabel(text));
        w.Write("\"]");
        w.WriteLine();
    }

    private void FormatSubgraph(TextWriter w, TreeNode node, string? parentId, MarkoutWriterOptions options)
    {
        var id = $"n{_nextNodeId++}";
        var label = BuildLabel(node, options);

        w.Write("    ");
        w.Write(id);
        w.Write("[\"");
        w.Write(EscapeLabel(label));
        w.Write("\"]");
        w.WriteLine();

        if (parentId != null)
        {
            w.Write("    ");
            w.Write(parentId);
            w.Write(" --> ");
            w.Write(id);
            w.WriteLine();
        }

        if (node.Children is { Count: > 0 })
        {
            foreach (var child in node.Children)
                FormatSubgraph(w, child, id, options);
        }
    }

    private static string BuildLabel(TreeNode node, MarkoutWriterOptions options)
    {
        if (node.Badge != null && options.IncludeBadges)
            return $"{node.Badge} {node.Text}";
        return node.Text;
    }

    /// <summary>
    /// Escapes text for use in a Mermaid node label (double-quoted form).
    /// </summary>
    public static string EscapeLabel(string text)
    {
        // Mermaid uses double-quoted labels; escape quotes and special chars.
        return text
            .Replace("\\", "\\\\")
            .Replace("\"", "#quot;");
    }
}
