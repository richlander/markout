using Markout.Formatting;

namespace Markout;

/// <summary>
/// Formatter that produces plain-text output without any markup syntax.
/// Fields render as "Key: Value", tables as space-padded columns,
/// headings as plain text lines, and lists as indented items.
/// </summary>
public class PlainTextFormatter : IMarkoutFormatter,
    IHeadingFormatter, IFieldFormatter, IBlockFormatter,
    IListFormatter, ITableFormatter, ICodeBlockFormatter, ITreeFormatter, IGraphFormatter,
    ITextDiffFormatter
{
    // ── IGraphFormatter ──

    /// <summary>
    /// Renders the graph as a tree rooted at the focus node. Plain text has no way to draw a
    /// diagram, and an edge table loses the sense of walking outward from the node under
    /// examination, which is the thing a text reader is usually after.
    /// </summary>
    void IGraphFormatter.FormatGraph(TextWriter w, Graph graph, MarkoutWriterOptions options)
    {
        if (graph.IsEmpty)
            return;

        ((ITreeFormatter)this).FormatTree(w, GraphLowering.ToTree(graph).AsSpan(), options);
    }

    // ── ITextDiffFormatter ──

    void ITextDiffFormatter.FormatTextDiff(
        TextWriter w,
        MappedTextDiff diff,
        MarkoutWriterOptions options)
    {
        foreach (var line in TextDiffFormatterHelpers.UnifiedLines(diff, options.TextDiffContextLines))
            w.WriteLine(line);

        for (var address = 0; address < diff.Changes.Length; address++)
        {
            var change = diff.Changes[address];
            foreach (var annotation in change.Annotations)
            {
                w.Write("Annotation (");
                w.Write(TextDiffFormatterHelpers.AnnotationTarget(annotation, address));
                w.Write("): ");
                w.WriteLine(TextDiffEscaping.Human(annotation.Text));
            }
        }
    }

    private const int ColumnGap = 2;

    // ── IHeadingFormatter ──

    void IHeadingFormatter.FormatHeading(TextWriter w, int level, string text, string? context)
    {
        w.Write(text);
        if (!string.IsNullOrEmpty(context))
        {
            w.Write(" (");
            w.Write(context);
            w.Write(')');
        }
    }

    // ── IFieldFormatter ──

    void IFieldFormatter.FormatFieldName(TextWriter w, string key, bool bold)
    {
        w.Write(key);
        w.Write(": ");
    }

    void IFieldFormatter.FormatFields(TextWriter w, MarkoutField[] fields, bool bold)
        => ((IFieldFormatter)this).FormatFields(w, (ReadOnlySpan<MarkoutField>)fields, bold);

    void IFieldFormatter.FormatFields(TextWriter w, ReadOnlySpan<MarkoutField> fields, bool bold)
    {
        // Calculate max key width for alignment
        int maxKeyWidth = 0;
        for (int i = 0; i < fields.Length; i++)
        {
            if (fields[i].Key.Length > maxKeyWidth)
                maxKeyWidth = fields[i].Key.Length;
        }

        for (int i = 0; i < fields.Length; i++)
        {
            w.Write(fields[i].Key.PadRight(maxKeyWidth));
            w.Write("  ");
            w.WriteLine(FormatHelper.RenderInlinePlainText(fields[i].Value));
        }
    }

    // ── ITableFormatter ──

    void ITableFormatter.FormatTable(TextWriter w, ReadOnlySpan<string> headers, IList<string[]> rows, int skippedRows, MarkoutWriterOptions options)
    {
        var widths = new int[headers.Length];
        for (int i = 0; i < headers.Length; i++)
            widths[i] = headers[i].Length;
        foreach (var row in rows)
        {
            for (int i = 0; i < Math.Min(row.Length, widths.Length); i++)
                widths[i] = Math.Max(widths[i], FormatHelper.RenderInlinePlainText(row[i]).Length);
        }

        // Header
        for (int i = 0; i < headers.Length; i++)
        {
            if (i < headers.Length - 1)
                w.Write(headers[i].PadRight(widths[i] + ColumnGap));
            else
                w.Write(headers[i]);
        }
        w.WriteLine();

        // Separator
        for (int i = 0; i < headers.Length; i++)
        {
            if (i < headers.Length - 1)
                w.Write(new string('-', widths[i]).PadRight(widths[i] + ColumnGap));
            else
                w.Write(new string('-', widths[i]));
        }
        w.WriteLine();

        // Rows
        foreach (var row in rows)
        {
            for (int i = 0; i < row.Length; i++)
            {
                if (i < row.Length - 1)
                    w.Write(FormatHelper.RenderInlinePlainText(row[i]).PadRight(widths[i] + ColumnGap));
                else
                    w.Write(FormatHelper.RenderInlinePlainText(row[i]));
            }
            w.WriteLine();
        }

        if (skippedRows > 0)
            FormatHelper.WriteTruncationFooter(w, skippedRows);
    }

    // ── ICodeBlockFormatter ──

    void ICodeBlockFormatter.FormatCodeStart(TextWriter w, string? language)
    {
        // No fence markers in plain text
    }

    void ICodeBlockFormatter.FormatCodeEnd(TextWriter w)
    {
        // No fence markers in plain text
    }

    // ── IBlockFormatter ──

    void IBlockFormatter.FormatParagraph(TextWriter w, string text)
    {
        w.WriteLine(FormatHelper.RenderInlinePlainText(text));
    }

    void IBlockFormatter.FormatCallout(TextWriter w, CalloutSeverity severity, string message)
    {
        var label = severity switch
        {
            CalloutSeverity.Note => "Note",
            CalloutSeverity.Tip => "Tip",
            CalloutSeverity.Important => "Important",
            CalloutSeverity.Warning => "Warning",
            CalloutSeverity.Caution => "Caution",
            _ => severity.ToString()
        };

        w.Write(label);
        w.Write(": ");
        w.WriteLine(FormatHelper.RenderInlinePlainText(message));
    }

    void IBlockFormatter.FormatQuotation(TextWriter w, string text)
    {
        w.WriteLine(FormatHelper.RenderInlinePlainText(text));
    }

    void IBlockFormatter.FormatRule(TextWriter w)
    {
        w.WriteLine(new string('-', 40));
    }

    void IBlockFormatter.FormatDescription(TextWriter w, Description item)
    {
        w.Write(item.Term);
        w.Write(": ");
        w.WriteLine(FormatHelper.RenderInlinePlainText(item.Text));

        if (item.Detail != null)
        {
            w.Write("  ");
            w.WriteLine(FormatHelper.RenderInlinePlainText(item.Detail));
        }
    }

    // ── IListFormatter ──

    void IListFormatter.FormatListItem(TextWriter w, string text)
    {
        w.WriteLine(FormatHelper.RenderInlinePlainText(text));
    }

    void IListFormatter.FormatArray(TextWriter w, string key, ReadOnlySpan<string> items, bool bold)
    {
        w.Write(key);
        w.WriteLine(":");
        foreach (var item in items)
            w.WriteLine(FormatHelper.RenderInlinePlainText(item));
    }

    // ── ITreeFormatter ──

    void ITreeFormatter.FormatTree(TextWriter w, ReadOnlySpan<TreeNode> nodes, MarkoutWriterOptions options)
    {
        for (int i = 0; i < nodes.Length; i++)
            FormatTreeNodeRecursive(w, nodes[i], "", i == nodes.Length - 1, options);
    }

    void ITreeFormatter.FormatTreeNode(TextWriter w, string text, string prefix)
    {
        w.Write(prefix);
        w.WriteLine(text);
    }

    private void FormatTreeNodeRecursive(TextWriter w, TreeNode node, string prefix, bool isLast, MarkoutWriterOptions options)
    {
        w.Write(prefix);
        w.Write(isLast ? "└─ " : "├─ ");
        w.Write(MarkoutGlyphs.NodeStatePrefix(node.State, options, this));
        if (node.Badge != null && options.IncludeBadges)
        {
            w.Write(node.Badge);
            w.Write(' ');
        }
        w.WriteLine(FormatHelper.RenderInlinePlainText(node.Text));

        if (node.Children is { Count: > 0 })
        {
            var childPrefix = prefix + (isLast ? "   " : "│  ");
            for (int i = 0; i < node.Children.Count; i++)
                FormatTreeNodeRecursive(w, node.Children[i], childPrefix, i == node.Children.Count - 1, options);
        }
    }
}
