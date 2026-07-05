using Markout.Formatting;
using System.Buffers;
using System.Text.Encodings.Web;
using System.Text;
using System.Text.Json;

namespace Markout;

/// <summary>
/// Formatter for compact tabular output. It can render normalized TSV or a pretty
/// space-padded table from the same row/column projection.
/// </summary>
public class TableFormatter : IMarkoutFormatter, ITableFormatter, IFieldFormatter, IListFormatter, ICompositeCellFormatter
{
    private const int ColumnGap = 2;

    /// <summary>Structured output decomposes composite cells into typed columns.</summary>
    bool ICompositeCellFormatter.DecomposesCompositeCells => true;

    private static readonly JsonWriterOptions JsonWriterOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    private readonly bool _showHeader;

    public TableFormatter(bool showHeader = true)
    {
        _showHeader = showHeader;
    }

    void ITableFormatter.FormatTable(TextWriter w, ReadOnlySpan<string> headers, IList<string[]> rows, int skippedRows, MarkoutWriterOptions options)
    {
        switch (options.TableMode)
        {
            case MarkoutTableMode.Tsv:
                WriteTsvTable(w, headers, rows, skippedRows);
                break;
            case MarkoutTableMode.Jsonl:
                WriteJsonlTable(w, headers, rows);
                break;
            default:
                WritePrettyTable(w, headers, rows, skippedRows);
                break;
        }
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
            w.Write(FormatHelper.NormalizeTableCell(FormatHelper.RenderInlinePlainText(fields[i].Value)));
        }
        w.WriteLine();
    }

    void IListFormatter.FormatListItem(TextWriter w, string text)
    {
        w.WriteLine(FormatHelper.NormalizeTableCell(FormatHelper.RenderInlinePlainText(text)));
    }

    void IListFormatter.FormatArray(TextWriter w, string key, ReadOnlySpan<string> items, bool bold)
    {
        w.Write(FormatHelper.NormalizeTableCell(key));
        w.WriteLine(":");

        foreach (var item in items)
            w.WriteLine(FormatHelper.NormalizeTableCell(FormatHelper.RenderInlinePlainText(item)));
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
                widths[i] = Math.Max(widths[i], FormatHelper.NormalizeTableCell(FormatHelper.RenderInlinePlainText(row[i])).Length);
        }

        if (_showHeader)
            WritePrettyRow(w, headers, widths);

        foreach (var row in rows)
            WritePrettyRow(w, row, widths);

        if (skippedRows > 0)
            w.WriteLine($"\n... and {skippedRows} more");
    }

    private static void WriteJsonlTable(TextWriter w, ReadOnlySpan<string> headers, IList<string[]> rows)
    {
        foreach (var row in rows)
        {
            var buffer = new ArrayBufferWriter<byte>();
            using var json = new Utf8JsonWriter(buffer, JsonWriterOptions);
            json.WriteStartObject();
            for (int i = 0; i < headers.Length; i++)
            {
                var value = i < row.Length ? FormatHelper.RenderInlinePlainText(row[i]) : "";
                json.WriteString(headers[i] ?? "", value ?? "");
            }
            json.WriteEndObject();
            json.Flush();

            w.Write(Encoding.UTF8.GetString(buffer.WrittenSpan));
            w.WriteLine();
        }
    }

    private static void WriteTsvRow(TextWriter w, ReadOnlySpan<string> values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (i > 0)
                w.Write('\t');
            w.Write(FormatHelper.NormalizeTableCell(FormatHelper.RenderInlinePlainText(values[i])));
        }
        w.WriteLine();
    }

    private static void WriteTsvRow(TextWriter w, string[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (i > 0)
                w.Write('\t');
            w.Write(FormatHelper.NormalizeTableCell(FormatHelper.RenderInlinePlainText(values[i])));
        }
        w.WriteLine();
    }

    private static void WritePrettyRow(TextWriter w, ReadOnlySpan<string> values, int[] widths)
    {
        for (int i = 0; i < values.Length; i++)
        {
            var value = FormatHelper.NormalizeTableCell(FormatHelper.RenderInlinePlainText(values[i]));
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
            var value = FormatHelper.NormalizeTableCell(FormatHelper.RenderInlinePlainText(values[i]));
            if (i < values.Length - 1)
                w.Write(value.PadRight(widths[i] + ColumnGap));
            else
                w.Write(value);
        }
        w.WriteLine();
    }
}
