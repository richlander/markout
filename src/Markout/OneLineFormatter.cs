using Markout.Formatting;

namespace Markout;

/// <summary>
/// Formatter that produces compact columnar output (docker-style).
/// Tables use space-padded columns with uppercase headers.
/// Fields are rendered inline (values only, pipe-separated).
/// </summary>
public class OneLineFormatter : IMarkoutFormatter,
    ITableFormatter, IFieldFormatter, IListFormatter
{
    private const int ColumnGap = 2;
    private readonly bool _showHeader;

    /// <summary>
    /// Creates a one-line formatter.
    /// </summary>
    /// <param name="showHeader">Whether to display table headers. Default is true.</param>
    public OneLineFormatter(bool showHeader = true)
    {
        _showHeader = showHeader;
    }

    // ── ITableFormatter ──

    void ITableFormatter.FormatTable(TextWriter w, string[] headers, IList<string[]> rows, int skippedRows, MarkoutWriterOptions options)
    {
        // Calculate column widths from headers and visible data
        var widths = new int[headers.Length];
        for (int i = 0; i < headers.Length; i++)
            widths[i] = headers[i].Length;
        foreach (var row in rows)
        {
            for (int i = 0; i < Math.Min(row.Length, widths.Length); i++)
                widths[i] = Math.Max(widths[i], row[i].Length);
        }

        if (_showHeader)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                var text = headers[i].ToUpperInvariant();
                if (i < headers.Length - 1)
                    w.Write(text.PadRight(widths[i] + ColumnGap));
                else
                    w.Write(text);
            }
            w.WriteLine();
        }

        foreach (var row in rows)
        {
            for (int i = 0; i < row.Length; i++)
            {
                if (i < row.Length - 1)
                    w.Write(row[i].PadRight(widths[i] + ColumnGap));
                else
                    w.Write(row[i]);
            }
            w.WriteLine();
        }

        if (skippedRows > 0)
            w.WriteLine($"\n... and {skippedRows} more");
    }

    // ── IFieldFormatter ──

    void IFieldFormatter.FormatFieldName(TextWriter w, string key, bool bold)
    {
        w.Write(key);
        w.Write(": ");
    }

    void IFieldFormatter.FormatFields(TextWriter w, MarkoutField[] fields, bool bold)
    {
        // OneLineFormatter buffers fields — this is only called if the base WriteFields runs
        // (shouldn't happen since we override WriteFields), but provide a sensible default
        for (int i = 0; i < fields.Length; i++)
        {
            if (i > 0)
                w.Write(" | ");
            w.Write(fields[i].Value);
        }
        w.WriteLine();
    }

    // ── IListFormatter ──

    void IListFormatter.FormatListItem(TextWriter w, string text)
    {
        w.WriteLine(text);
    }

    void IListFormatter.FormatArray(TextWriter w, string key, ReadOnlySpan<string> items, bool bold)
    {
        w.Write(key);
        w.WriteLine(":");

        foreach (var item in items)
            w.WriteLine(item);
    }

}
