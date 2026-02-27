using System.Text;
using Markout.Formatting;

namespace Markout;

/// <summary>
/// A MarkoutWriter that renders output as Markdown.
/// Produces # headings, **bold** field names, | pipe tables |, - bullet lists,
/// ``` code fences, and trailing double-space hard line breaks.
/// </summary>
public class MarkdownWriter : MarkoutWriter, IMarkoutFormatter,
    IHeadingFormatter, IFieldFormatter, ITableFormatter, IListFormatter,
    ICodeBlockFormatter, IBlockFormatter, IMetricsFormatter
{
    private static readonly string[] HeadingPrefixes = ["", "#", "##", "###", "####", "#####", "######"];

    /// <summary>
    /// Creates a writer that builds Markdown output in memory with default options.
    /// Use ToString() to get the result.
    /// </summary>
    public MarkdownWriter() : base()
    {
    }

    /// <summary>
    /// Creates a writer that builds Markdown output in memory with the specified options.
    /// Use ToString() to get the result.
    /// </summary>
    public MarkdownWriter(MarkoutWriterOptions options) : base(options)
    {
    }

    /// <summary>
    /// Creates a writer that writes Markdown to the specified TextWriter with default options.
    /// </summary>
    public MarkdownWriter(TextWriter writer) : base(writer)
    {
    }

    /// <summary>
    /// Creates a writer that writes Markdown to the specified TextWriter with the specified options.
    /// </summary>
    public MarkdownWriter(TextWriter writer, MarkoutWriterOptions options) : base(writer, options)
    {
    }

    /// <summary>
    /// Creates a writer that writes Markdown to the specified Stream with default options.
    /// </summary>
    public MarkdownWriter(Stream stream) : base(stream)
    {
    }

    /// <summary>
    /// Creates a writer that writes Markdown to the specified Stream with the specified options.
    /// </summary>
    public MarkdownWriter(Stream stream, MarkoutWriterOptions options) : base(stream, options)
    {
    }

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

    void ITableFormatter.FormatTable(TextWriter w, string[] headers, IList<string[]> rows, int skippedRows, MarkoutWriterOptions options)
    {
        if (options.PrettyTables)
            WritePrettyPipeTable(w, headers, rows, skippedRows);
        else
            WriteCompactPipeTable(w, headers, rows, skippedRows);
    }

    private static void WriteCompactPipeTable(TextWriter w, string[] headers, IList<string[]> rows, int skippedRows)
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
                w.Write(EscapeTableCell(value));
                w.Write(" |");
            }
            w.WriteLine();
        }

        if (skippedRows > 0)
            w.WriteLine($"\n... and {skippedRows} more");
    }

    private static void WritePrettyPipeTable(TextWriter w, string[] headers, IList<string[]> rows, int skippedRows)
    {
        // Calculate column widths
        var widths = new int[headers.Length];
        for (int i = 0; i < headers.Length; i++)
            widths[i] = headers[i].Length;
        foreach (var row in rows)
        {
            for (int i = 0; i < Math.Min(row.Length, widths.Length); i++)
            {
                var escaped = EscapeTableCell(row[i]);
                widths[i] = Math.Max(widths[i], escaped.Length);
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
                var value = i < row.Length ? EscapeTableCell(row[i]) : "";
                w.Write(value.PadRight(widths[i]));
                w.Write(" |");
            }
            w.WriteLine();
        }

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

    // ── IMetricsFormatter ──

    void IMetricsFormatter.FormatBreakdown(TextWriter w, IReadOnlyList<Breakdown> items, int? maxBarWidth, bool uniformBarWidth)
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
                .Select(s => new[] { s.Category, s.Count.ToString(), $"{s.Count * 100 / total}" })
                .ToList();

            ((ITableFormatter)this).FormatTable(w, headers, rows, 0, Options);
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
                    rows.Add([item.Label, seg.Category, seg.Count.ToString(), $"{seg.Count * 100 / total}"]);
            }

            ((ITableFormatter)this).FormatTable(w, headers, rows, 0, Options);
        }
    }

    void IMetricsFormatter.FormatMetrics(TextWriter w, IReadOnlyList<Metric> items, int maxBarWidth)
    {
        // Render as a pipe table: Label | Value
        var headers = new[] { "Label", "Value" };
        var rows = items.Select(m => new[] { m.Label, FormatBarValue(m.Value) }).ToList();
        ((ITableFormatter)this).FormatTable(w, headers, rows, 0, Options);
    }

    void IMetricsFormatter.FormatVerticalMetrics(TextWriter w, IReadOnlyList<Metric> items, int maxBarHeight, int? barWidth)
    {
        // Same as horizontal — vertical layout is a terminal concept
        ((IMetricsFormatter)this).FormatMetrics(w, items, maxBarHeight);
    }
}
