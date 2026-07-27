using System.Text;
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
        w.Write(SingleLine(text));
        if (!string.IsNullOrEmpty(context))
        {
            w.Write(" (");
            w.Write(SingleLine(context));
            w.Write(')');
        }
    }

    // A Mermaid comment runs to end of line, so an embedded newline would end the
    // comment and let the remainder of the heading be parsed as diagram syntax.
    private static string SingleLine(string text)
        => text.Contains('\n') || text.Contains('\r')
            ? text.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ')
            : text;

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
    /// Escapes text for use in a Mermaid node label (double-quoted form), so that
    /// arbitrary — including hostile — text renders literally instead of altering the
    /// diagram.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Mermaid's flowchart lexer reads a quoted label with a single rule
    /// (<c>&lt;string&gt;[^"]+</c>) and performs <em>no</em> backslash unescaping, so
    /// <c>\"</c> does not escape a quote and a doubled <c>\\</c> is not collapsed. The
    /// only portable escape is Mermaid's own entity form: <c>encodeEntities</c> rewrites
    /// <c>#NN;</c> / <c>#name;</c> into an HTML entity before rendering.
    /// </para>
    /// <para>
    /// Escaping <c>&lt;</c> and <c>&gt;</c> is required for correctness, not just safety.
    /// Flowcharts default to HTML labels, and at the default <c>securityLevel: "strict"</c>
    /// Mermaid passes label text through DOMPurify, which silently <em>drops</em> unknown
    /// tags. Unescaped text such as <c>List&lt;String&gt;</c> or <c>&lt;Foo&gt;b__0</c>
    /// therefore renders with the angle-bracket run deleted, so genuinely distinct nodes
    /// can appear identical.
    /// </para>
    /// <para>
    /// Backslash is escaped rather than passed through because Mermaid converts the
    /// two-character sequence <c>\n</c> into a line break while splitting label rows; a
    /// literal path such as <c>C:\new</c> would otherwise wrap mid-word.
    /// </para>
    /// </remarks>
    public static string EscapeLabel(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return NeedsEscaping(text) ? EscapeCore(text) : text;
    }

    private static bool NeedsEscaping(string text)
    {
        foreach (var ch in text)
        {
            if (Replacement(ch) is not null)
                return true;
        }
        return false;
    }

    private static string EscapeCore(string text)
    {
        var sb = new StringBuilder(text.Length + 16);
        foreach (var ch in text)
        {
            if (Replacement(ch) is { } entity)
                sb.Append(entity);
            else
                sb.Append(ch);
        }
        return sb.ToString();
    }

    // A single pass, not chained Replace calls: every replacement emits '#', so a
    // multi-pass scheme would re-escape its own output unless '#' were handled first.
    private static string? Replacement(char ch) => ch switch
    {
        '#' => "#35;",
        '"' => "#quot;",
        '<' => "#60;",
        '>' => "#62;",
        '&' => "#38;",
        '|' => "#124;",
        '\\' => "#92;",
        '\r' => "#13;",
        '\n' => "#10;",
        _ => null,
    };
}
