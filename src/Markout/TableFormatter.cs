using Markout.Formatting;

namespace Markout;

/// <summary>
/// Formatter for compact tabular output. It can render normalized TSV or a pretty
/// space-padded table from the same row/column projection.
/// </summary>
public class TableFormatter : IMarkoutFormatter, ITableFormatter, IFieldFormatter, IListFormatter
{
    private const int ColumnGap = 2;
    private readonly bool _showHeader;

    public TableFormatter(bool showHeader = true)
    {
        _showHeader = showHeader;
    }

    void ITableFormatter.FormatTable(TextWriter w, ReadOnlySpan<string> headers, IList<string[]> rows, int skippedRows, MarkoutWriterOptions options)
    {
        if (options.TableMode == MarkoutTableMode.Tsv)
            WriteTsvTable(w, headers, rows, skippedRows);
        else
            WritePrettyTable(w, headers, rows, skippedRows);
    }

    void IFieldFormatter.FormatFieldName(TextWriter w, string key, bool bold)
    {
        w.Write(FormatHelper.NormalizeTableCell(key));
        w.Write(": ");
    }

    void IFieldFormatter.FormatFields(TextWriter w, MarkoutField[] fields, bool bold)
    {
        for (int i = 0; i < fields.Length; i++)
        {
            if (i > 0)
                w.Write(" | ");
            w.Write(FormatHelper.NormalizeTableCell(fields[i].Value));
        }
        w.WriteLine();
    }

    void IListFormatter.FormatListItem(TextWriter w, string text)
    {
        w.WriteLine(FormatHelper.NormalizeTableCell(text));
    }

    void IListFormatter.FormatArray(TextWriter w, string key, ReadOnlySpan<string> items, bool bold)
    {
        foreach (var item in items)
            w.WriteLine(FormatHelper.NormalizeTableCell(item));
    }

    private void WriteTsvTable(TextWriter w, ReadOnlySpan<string> headers, IList<string[]> rows, int skippedRows)
    {
        if (_showHeader)
            WriteTsvRow(w, headers);

        foreach (var row in rows)
            WriteTsvRow(w, row);

        if (skippedRows > 0)
            w.WriteLine($"\n... and {skippedRows} more");
    }

    private void WritePrettyTable(TextWriter w, ReadOnlySpan<string> headers, IList<string[]> rows, int skippedRows)
    {
        var widths = new int[headers.Length];
        for (int i = 0; i < headers.Length; i++)
            widths[i] = FormatHelper.NormalizeTableCell(headers[i]).Length;

        foreach (var row in rows)
        {
            for (int i = 0; i < Math.Min(row.Length, widths.Length); i++)
                widths[i] = Math.Max(widths[i], FormatHelper.NormalizeTableCell(row[i]).Length);
        }

        if (_showHeader)
            WritePrettyRow(w, headers, widths);

        foreach (var row in rows)
            WritePrettyRow(w, row, widths);

        if (skippedRows > 0)
            w.WriteLine($"\n... and {skippedRows} more");
    }

    private static void WriteTsvRow(TextWriter w, ReadOnlySpan<string> values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (i > 0)
                w.Write('\t');
            w.Write(FormatHelper.NormalizeTableCell(values[i]));
        }
        w.WriteLine();
    }

    private static void WriteTsvRow(TextWriter w, string[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (i > 0)
                w.Write('\t');
            w.Write(FormatHelper.NormalizeTableCell(values[i]));
        }
        w.WriteLine();
    }

    private static void WritePrettyRow(TextWriter w, ReadOnlySpan<string> values, int[] widths)
    {
        for (int i = 0; i < values.Length; i++)
        {
            var value = FormatHelper.NormalizeTableCell(values[i]);
            if (i < values.Length - 1)
                w.Write(value.PadRight(widths[i] + ColumnGap));
            else
                w.Write(value);
        }
        w.WriteLine();
    }

    private static void WritePrettyRow(TextWriter w, string[] values, int[] widths)
    {
        for (int i = 0; i < values.Length; i++)
        {
            var value = FormatHelper.NormalizeTableCell(values[i]);
            if (i < values.Length - 1)
                w.Write(value.PadRight(widths[i] + ColumnGap));
            else
                w.Write(value);
        }
        w.WriteLine();
    }
}
