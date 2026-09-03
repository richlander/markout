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
public class TableFormatter : IMarkoutFormatter, ITableFormatter, IFieldFormatter, IListFormatter,
    ICompositeCellFormatter, IGraphFormatter, ITextDiffFormatter
{
    // ── IGraphFormatter ──

    /// <summary>
    /// Renders the graph as an edge table — one row per edge — which is the only graph lowering a
    /// tabular sink can express without losing edges.
    /// </summary>
    void IGraphFormatter.FormatGraph(TextWriter w, Graph graph, MarkoutWriterOptions options)
    {
        if (graph.IsEmpty)
            return;

        var table = GraphLowering.ToEdgeTable(graph);
        new TableWriter(w, (ITableFormatter)this, options).WriteTable(table.Headers.AsSpan(), table.Rows);
    }

    internal bool TryLowerGraphToTable(
        Graph graph,
        out GraphLowering.DeferredGraphEdgeTable table)
    {
        table = GraphLowering.ToDeferredEdgeTable(graph);
        return true;
    }

    // ── ITextDiffFormatter ──

    void ITextDiffFormatter.FormatTextDiff(
        TextWriter w,
        MappedTextDiff diff,
        MarkoutWriterOptions options)
    {
        var table = TextDiffFormatterHelpers.StructuredTable(
            diff,
            options.TextDiffContextLines);
        var structuredOptions = options.WithJsonIdentityColumnIndices(
            TextDiffFormatterHelpers.JsonStringColumnIndices);
        switch (options.TableMode)
        {
            case MarkoutTableMode.Tsv:
                WriteTsvTable(w, table.Headers.AsSpan(), table.Rows, skippedRows: 0, renderInline: false);
                break;
            case MarkoutTableMode.Jsonl:
                WriteJsonlTable(w, table.Headers.AsSpan(), table.Rows, structuredOptions, renderInline: false);
                break;
            default:
                WritePrettyTable(w, table.Headers.AsSpan(), table.Rows, skippedRows: 0, renderInline: false);
                break;
        }
    }

    private const int ColumnGap = 2;

    /// <summary>Structured output decomposes composite cells into typed columns.</summary>
    bool ICompositeCellFormatter.DecomposesCompositeCells => true;

    private static readonly JsonWriterOptions JsonWriterOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    private readonly bool _showHeader;

    /// <summary>
    /// Creates a table formatter.
    /// </summary>
    /// <param name="showHeader">Whether to emit the header row above the data rows.</param>
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
                WriteJsonlTable(w, headers, rows, options);
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
        => ((IFieldFormatter)this).FormatFields(w, (ReadOnlySpan<MarkoutField>)fields, bold);

    void IFieldFormatter.FormatFields(TextWriter w, ReadOnlySpan<MarkoutField> fields, bool bold)
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

    private void WriteTsvTable(
        TextWriter w,
        ReadOnlySpan<string> headers,
        IList<string[]> rows,
        int skippedRows,
        bool renderInline = true)
    {
        if (_showHeader)
            WriteTsvRow(w, headers, renderInline);

        foreach (var row in rows)
            WriteTsvRow(w, row, renderInline);

        if (skippedRows > 0)
            FormatHelper.WriteTruncationFooter(w, skippedRows);
    }

    private void WritePrettyTable(
        TextWriter w,
        ReadOnlySpan<string> headers,
        IList<string[]> rows,
        int skippedRows,
        bool renderInline = true)
    {
        var widths = new int[headers.Length];
        for (int i = 0; i < headers.Length; i++)
            widths[i] = PrepareTableCell(headers[i], renderInline).Length;

        foreach (var row in rows)
        {
            for (int i = 0; i < Math.Min(row.Length, widths.Length); i++)
                widths[i] = Math.Max(widths[i], PrepareTableCell(row[i], renderInline).Length);
        }

        if (_showHeader)
            WritePrettyRow(w, headers, widths, renderInline);

        foreach (var row in rows)
            WritePrettyRow(w, row, widths, renderInline);

        if (skippedRows > 0)
            FormatHelper.WriteTruncationFooter(w, skippedRows);
    }

    private static void WriteJsonlTable(
        TextWriter w,
        ReadOnlySpan<string> headers,
        IList<string[]> rows,
        MarkoutWriterOptions options,
        bool renderInline = true)
    {
        foreach (var row in rows)
        {
            var buffer = new ArrayBufferWriter<byte>();
            using var json = new Utf8JsonWriter(buffer, JsonWriterOptions);
            json.WriteStartObject();
            for (int i = 0; i < headers.Length; i++)
            {
                var value = i < row.Length
                    ? PrepareJsonValue(row[i], renderInline)
                    : "";

                // Heterogeneous records: drop empty fields when opted in.
                if (options.OmitEmptyJsonFields && string.IsNullOrEmpty(value))
                    continue;

                var key = headers[i] ?? "";
                var isIdentity = options.JsonIdentityColumnIndices?.Contains(i) ?? false;
                if (options.JsonTypedValues && !isIdentity)
                    WriteTypedJsonValue(json, key, value);
                else
                    json.WriteString(key, value ?? "");
            }
            json.WriteEndObject();
            json.Flush();

            w.Write(Encoding.UTF8.GetString(buffer.WrittenSpan));
            w.WriteLine();
        }
    }

    /// <summary>
    /// Writes a cell as a JSON number or boolean when its text is one; otherwise a string.
    /// Numbers are written verbatim (<see cref="Utf8JsonWriter.WriteRawValue(System.ReadOnlySpan{char}, bool)"/>)
    /// so exact digits are preserved without rounding through a CLR numeric type.
    /// Enabled by <see cref="MarkoutWriterOptions.JsonTypedValues"/>.
    /// </summary>
    private static void WriteTypedJsonValue(Utf8JsonWriter json, string key, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            json.WriteString(key, value ?? "");
        }
        else if (IsJsonNumber(value))
        {
            json.WritePropertyName(key);
            json.WriteRawValue(value, skipInputValidation: true);
        }
        else if (value == "true")
        {
            json.WriteBoolean(key, true);
        }
        else if (value == "false")
        {
            json.WriteBoolean(key, false);
        }
        else
        {
            json.WriteString(key, value);
        }
    }

    /// <summary>
    /// Returns true when <paramref name="s"/> is a valid JSON number token (RFC 8259 grammar).
    /// Rejects leading zeros, a leading '+', thousands separators, hex, NaN/Infinity, etc., so
    /// only strictly numeric text is coerced.
    /// </summary>
    private static bool IsJsonNumber(string s)
    {
        int i = 0, n = s.Length;
        if (n == 0)
            return false;

        if (s[i] == '-')
        {
            i++;
            if (i == n)
                return false;
        }

        if (s[i] == '0')
        {
            i++;
        }
        else if (s[i] >= '1' && s[i] <= '9')
        {
            i++;
            while (i < n && s[i] >= '0' && s[i] <= '9')
                i++;
        }
        else
        {
            return false;
        }

        if (i < n && s[i] == '.')
        {
            i++;
            if (i == n || s[i] < '0' || s[i] > '9')
                return false;
            while (i < n && s[i] >= '0' && s[i] <= '9')
                i++;
        }

        if (i < n && (s[i] == 'e' || s[i] == 'E'))
        {
            i++;
            if (i < n && (s[i] == '+' || s[i] == '-'))
                i++;
            if (i == n || s[i] < '0' || s[i] > '9')
                return false;
            while (i < n && s[i] >= '0' && s[i] <= '9')
                i++;
        }

        return i == n;
    }

    private static void WriteTsvRow(
        TextWriter w,
        ReadOnlySpan<string> values,
        bool renderInline)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (i > 0)
                w.Write('\t');
            w.Write(PrepareTableCell(values[i], renderInline));
        }
        w.WriteLine();
    }

    private static void WriteTsvRow(
        TextWriter w,
        string[] values,
        bool renderInline)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (i > 0)
                w.Write('\t');
            w.Write(PrepareTableCell(values[i], renderInline));
        }
        w.WriteLine();
    }

    private static void WritePrettyRow(
        TextWriter w,
        ReadOnlySpan<string> values,
        int[] widths,
        bool renderInline)
    {
        for (int i = 0; i < values.Length; i++)
        {
            var value = PrepareTableCell(values[i], renderInline);
            if (i < values.Length - 1)
                w.Write(value.PadRight(widths[i] + ColumnGap));
            else
                w.Write(value);
        }
        w.WriteLine();
    }

    private static void WritePrettyRow(
        TextWriter w,
        string[] values,
        int[] widths,
        bool renderInline)
    {
        for (int i = 0; i < values.Length; i++)
        {
            var value = PrepareTableCell(values[i], renderInline);
            if (i < values.Length - 1)
                w.Write(value.PadRight(widths[i] + ColumnGap));
            else
                w.Write(value);
        }
        w.WriteLine();
    }

    private static string PrepareTableCell(string? value, bool renderInline)
        => FormatHelper.NormalizeTableCell(
            renderInline ? FormatHelper.RenderInlinePlainText(value) : value ?? "");

    private static string PrepareJsonValue(string? value, bool renderInline)
        => renderInline ? FormatHelper.RenderInlinePlainText(value) : value ?? "";
}
