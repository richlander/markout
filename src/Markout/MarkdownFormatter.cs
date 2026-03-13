using System.Text;
using Markout.Formatting;
using MarkdownTable.Formatting;

namespace Markout;

/// <summary>
/// Formatter that renders Markdown output.
/// Produces # headings, **bold** field names, | pipe tables |, - bullet lists,
/// ``` code fences, and trailing double-space hard line breaks.
/// </summary>
public class MarkdownFormatter : IMarkoutFormatter,
    IDocumentFormatter, IMetricsFormatter, IStreamingTableFormatter
{
    private static readonly string[] HeadingPrefixes = ["", "#", "##", "###", "####", "#####", "######"];

    // ── IHeadingFormatter ──

    void IHeadingFormatter.FormatHeading(TextWriter w, int level, string text, string? context)
    {
        w.Write(HeadingPrefixes[level]);
        w.Write(' ');
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
        if (bold)
        {
            w.Write("**");
            w.Write(key);
            w.Write(":** ");
        }
        else
        {
            w.Write(key);
            w.Write(": ");
        }
    }

    void IFieldFormatter.FormatFields(TextWriter w, MarkoutField[] fields, bool bold)
    {
        for (int i = 0; i < fields.Length; i++)
        {
            ((IFieldFormatter)this).FormatFieldName(w, fields[i].Key, bold);
            w.Write(fields[i].Value);
            w.WriteLine("  "); // Two trailing spaces for markdown hard line break
        }
    }

    // ── ITableFormatter ──

    void ITableFormatter.FormatTable(TextWriter w, ReadOnlySpan<string> headers, IList<string[]> rows, int skippedRows, MarkoutWriterOptions options)
    {
        if (options.PrettyTables)
            WritePrettyPipeTable(w, headers, rows, skippedRows, options.TableOptions);
        else
            WriteCompactPipeTable(w, headers, rows, skippedRows);
    }

    private static void WriteCompactPipeTable(TextWriter w, ReadOnlySpan<string> headers, IList<string[]> rows, int skippedRows)
    {
        // Header row
        w.Write('|');
        foreach (var header in headers)
        {
            w.Write(' ');
            w.Write(header);
            w.Write(" |");
        }
        w.WriteLine();

        // Separator row
        w.Write('|');
        foreach (var header in headers)
        {
            w.Write(' ');
            for (int i = 0; i < header.Length; i++)
                w.Write('-');
            w.Write(" |");
        }
        w.WriteLine();

        // Data rows
        foreach (var row in rows)
        {
            w.Write('|');
            foreach (var value in row)
            {
                w.Write(' ');
                w.Write(FormatHelper.EscapeTableCell(value));
                w.Write(" |");
            }
            w.WriteLine();
        }

        if (skippedRows > 0)
            w.WriteLine($"\n... and {skippedRows} more");
    }

    private static void WritePrettyPipeTable(TextWriter w, ReadOnlySpan<string> headers, IList<string[]> rows, int skippedRows, TableFormatterOptions? tableOptions = null)
    {
        // Calculate column widths
        int[] widths;
        if (tableOptions is not null)
        {
            // Statistical width calculation — prevents outlier rows from stretching the table
            widths = ColumnWidthCalculator.Calculate(headers.ToArray(), (IReadOnlyList<string[]>)rows, tableOptions);
        }
        else
        {
            // Simple max-width calculation
            widths = new int[headers.Length];
            for (int i = 0; i < headers.Length; i++)
                widths[i] = headers[i].Length;
            foreach (var row in rows)
            {
                for (int i = 0; i < Math.Min(row.Length, widths.Length); i++)
                {
                    var escaped = FormatHelper.EscapeTableCell(row[i]);
                    widths[i] = Math.Max(widths[i], escaped.Length);
                }
            }
        }

        // Header row
        w.Write('|');
        for (int i = 0; i < headers.Length; i++)
        {
            w.Write(' ');
            w.Write(headers[i].PadRight(widths[i]));
            w.Write(" |");
        }
        w.WriteLine();

        // Separator row
        w.Write('|');
        for (int i = 0; i < headers.Length; i++)
        {
            w.Write(' ');
            w.Write(new string('-', widths[i]));
            w.Write(" |");
        }
        w.WriteLine();

        // Data rows
        foreach (var row in rows)
        {
            w.Write('|');
            for (int i = 0; i < headers.Length; i++)
            {
                w.Write(' ');
                var value = i < row.Length ? FormatHelper.EscapeTableCell(row[i]) : "";
                w.Write(value.PadRight(widths[i]));
                w.Write(" |");
            }
            w.WriteLine();
        }

        if (skippedRows > 0)
            w.WriteLine($"\n... and {skippedRows} more");
    }

    // ── IStreamingTableFormatter ──

    private int[]? _streamingWidths;

    void IStreamingTableFormatter.BeginTable(TextWriter w, ReadOnlySpan<string> headers, MarkoutWriterOptions options)
    {
        _streamingWidths = null; // Reset state in case previous table had an error

        if (options.PrettyTables)
        {
            _streamingWidths = new int[headers.Length];
            for (int i = 0; i < headers.Length; i++)
                _streamingWidths[i] = headers[i].Length;

            w.Write('|');
            for (int i = 0; i < headers.Length; i++)
            {
                w.Write(' ');
                w.Write(headers[i].PadRight(_streamingWidths[i]));
                w.Write(" |");
            }
            w.WriteLine();

            w.Write('|');
            for (int i = 0; i < headers.Length; i++)
            {
                w.Write(' ');
                w.Write(new string('-', _streamingWidths[i]));
                w.Write(" |");
            }
            w.WriteLine();
        }
        else
        {
            _streamingWidths = null;

            w.Write('|');
            foreach (var header in headers)
            {
                w.Write(' ');
                w.Write(header);
                w.Write(" |");
            }
            w.WriteLine();

            w.Write('|');
            foreach (var header in headers)
            {
                w.Write(' ');
                for (int i = 0; i < header.Length; i++)
                    w.Write('-');
                w.Write(" |");
            }
            w.WriteLine();
        }
    }

    void IStreamingTableFormatter.WriteRow(TextWriter w, ReadOnlySpan<string> values)
    {
        w.Write('|');
        if (_streamingWidths != null)
        {
            for (int i = 0; i < _streamingWidths.Length; i++)
            {
                w.Write(' ');
                var value = i < values.Length ? FormatHelper.EscapeTableCell(values[i]) : "";
                w.Write(value.PadRight(_streamingWidths[i]));
                w.Write(" |");
            }
        }
        else
        {
            foreach (var value in values)
            {
                w.Write(' ');
                w.Write(FormatHelper.EscapeTableCell(value));
                w.Write(" |");
            }
        }
        w.WriteLine();
    }

    void IStreamingTableFormatter.EndTable(TextWriter w, int skippedRows)
    {
        _streamingWidths = null;
        if (skippedRows > 0)
            w.WriteLine($"\n... and {skippedRows} more");
    }

    // ── ICodeBlockFormatter ──

    void ICodeBlockFormatter.FormatCodeStart(TextWriter w, string? language)
    {
        w.Write("```");
        if (!string.IsNullOrEmpty(language))
            w.Write(language);
        w.WriteLine();
    }

    void ICodeBlockFormatter.FormatCodeEnd(TextWriter w)
    {
        w.WriteLine("```");
    }

    // ── IBlockFormatter ──

    void IBlockFormatter.FormatParagraph(TextWriter w, string text)
    {
        w.WriteLine(text);
    }

    void IBlockFormatter.FormatCallout(TextWriter w, CalloutSeverity severity, string message)
    {
        var label = severity switch
        {
            CalloutSeverity.Note => "NOTE",
            CalloutSeverity.Tip => "TIP",
            CalloutSeverity.Important => "IMPORTANT",
            CalloutSeverity.Warning => "WARNING",
            CalloutSeverity.Caution => "CAUTION",
            _ => severity.ToString().ToUpperInvariant()
        };

        w.WriteLine($"> [!{label}]");
        w.Write("> ");
        w.WriteLine(message);
    }

    void IBlockFormatter.FormatQuotation(TextWriter w, string text)
    {
        foreach (var line in text.Split('\n'))
        {
            w.Write("> ");
            w.WriteLine(line);
        }
    }

    void IBlockFormatter.FormatRule(TextWriter w)
    {
        w.WriteLine("---");
    }

    void IBlockFormatter.FormatDescription(TextWriter w, Description item)
    {
        w.Write("- **");
        w.Write(item.Term);
        w.Write(":** ");
        w.WriteLine(item.Text);

        if (item.Detail != null)
        {
            w.Write("  ");
            w.WriteLine(item.Detail);
        }
    }

    // ── IListFormatter ──

    void IListFormatter.FormatListItem(TextWriter w, string text)
    {
        w.Write("- ");
        w.WriteLine(text);
    }

    void IListFormatter.FormatArray(TextWriter w, string key, ReadOnlySpan<string> items, bool bold)
    {
        if (bold)
        {
            w.Write("**");
            w.Write(key);
            w.WriteLine(":**");
        }
        else
        {
            w.Write(key);
            w.WriteLine(":");
        }

        foreach (var item in items)
        {
            w.Write("- ");
            w.WriteLine(item);
        }
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

    private static void FormatTreeNodeRecursive(TextWriter w, TreeNode node, string prefix, bool isLast, MarkoutWriterOptions options)
    {
        w.Write(prefix);
        w.Write(isLast ? "└─ " : "├─ ");
        if (node.Badge != null && options.IncludeBadges)
        {
            w.Write(node.Badge);
            w.Write(' ');
        }
        w.WriteLine(node.Text);

        if (node.Children is { Count: > 0 })
        {
            var childPrefix = prefix + (isLast ? "   " : "│  ");
            for (int i = 0; i < node.Children.Count; i++)
                FormatTreeNodeRecursive(w, node.Children[i], childPrefix, i == node.Children.Count - 1, options);
        }
    }

    // ── IMetricsFormatter ──

    void IMetricsFormatter.FormatBreakdown(TextWriter w, IReadOnlyList<Breakdown> items, int? maxBarWidth, bool uniformBarWidth, MarkoutWriterOptions options)
    {
        // Single breakdown: simple Category | Count | % table
        // Multiple breakdowns: include Label column to distinguish them
        if (items.Count == 1)
        {
            var item = items[0];
            var total = 0;
            foreach (var seg in item.Segments) total += seg.Count;
            if (total == 0) total = 1;

            var headers = new[] { "Category", "Count", "%" };
            var rows = item.Segments
                .Where(s => s.Count > 0)
                .Select(s => new[] { s.Category, s.Count.ToString(), $"{s.Count * 100.0 / total:F0}" })
                .ToList();

            ((ITableFormatter)this).FormatTable(w, headers, rows, 0, options);
        }
        else
        {
            var headers = new[] { "Label", "Category", "Count", "%" };
            var rows = new List<string[]>();
            foreach (var item in items)
            {
                var total = 0;
                foreach (var seg in item.Segments) total += seg.Count;
                if (total == 0) total = 1;

                foreach (var seg in item.Segments.Where(s => s.Count > 0))
                    rows.Add([item.Label, seg.Category, seg.Count.ToString(), $"{seg.Count * 100.0 / total:F0}"]);
            }

            ((ITableFormatter)this).FormatTable(w, headers, rows, 0, options);
        }
    }

    void IMetricsFormatter.FormatMetrics(TextWriter w, IReadOnlyList<Metric> items, int maxBarWidth, MarkoutWriterOptions options)
    {
        // Render as a pipe table: Label | Value
        var headers = new[] { "Label", "Value" };
        var rows = items.Select(m => new[] { m.Label, FormatHelper.FormatBarValue(m.Value) }).ToList();
        ((ITableFormatter)this).FormatTable(w, headers, rows, 0, options);
    }

    void IMetricsFormatter.FormatVerticalMetrics(TextWriter w, IReadOnlyList<Metric> items, int maxBarHeight, int? barWidth, MarkoutWriterOptions options)
    {
        // Same as horizontal — vertical layout is a terminal concept
        ((IMetricsFormatter)this).FormatMetrics(w, items, maxBarHeight, options);
    }
}
