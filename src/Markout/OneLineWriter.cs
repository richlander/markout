using System.Runtime.InteropServices;
using Markout.Formatting;

namespace Markout;

/// <summary>
/// A writer that produces compact columnar output (docker-style).
/// Tables use space-padded columns with uppercase headers.
/// Fields are buffered and rendered as two-column tables at section boundaries.
/// Field lists are rendered inline (values only, pipe-separated).
/// </summary>
public class OneLineWriter : MarkoutWriter,
    ITableFormatter, IFieldFormatter, IListFormatter
{
    private const int ColumnGap = 2;
    private readonly bool _showHeader;
    private readonly List<MarkoutField> _fieldBuffer = new();

    /// <summary>
    /// Creates a one-line writer targeting the specified TextWriter.
    /// </summary>
    /// <param name="writer">The underlying TextWriter.</param>
    /// <param name="showHeader">Whether to display table headers. Default is true.</param>
    public OneLineWriter(TextWriter writer, bool showHeader = true) : base(writer)
    {
        _showHeader = showHeader;
    }

    /// <summary>
    /// Creates a one-line writer with the specified options.
    /// </summary>
    public OneLineWriter(TextWriter writer, MarkoutWriterOptions options, bool showHeader = true) : base(writer, options)
    {
        _showHeader = showHeader;
    }

    /// <inheritdoc/>
    public override MarkoutShape SupportedShapes => MarkoutShape.Tables | MarkoutShape.Lists | MarkoutShape.Fields;

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
        // OneLineWriter buffers fields — this is only called if the base WriteFields runs
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

    // ── Orchestration overrides (behavioral, not formatting) ──

    /// <inheritdoc/>
    public override void WriteHeading(int level, string text, string? context)
    {
        FlushFieldBuffer();
        UpdateSectionState(level, text);
    }

    /// <inheritdoc/>
    public override void WriteFields(params ReadOnlySpan<MarkoutField> fields)
    {
        // Buffer fields for table rendering
        if (SectionExcluded || fields.Length == 0)
            return;

        for (int i = 0; i < fields.Length; i++)
            _fieldBuffer.Add(fields[i]);
    }

    /// <inheritdoc/>
    public override void WriteFieldsInline(params ReadOnlySpan<MarkoutField> fields)
    {
        if (SectionExcluded || fields.Length == 0)
            return;

        var projected = ProjectFields(fields);
        if (projected.Length == 0)
            return;

        // Render inline: just values, pipe-separated
        for (int i = 0; i < projected.Length; i++)
        {
            if (i > 0)
                Writer.Write(" | ");
            Writer.Write(projected[i].Value);
        }
        Writer.WriteLine();
    }

    /// <inheritdoc/>
    public override void WriteFieldsBulleted(params ReadOnlySpan<MarkoutField> fields)
    {
        // Buffer fields for table rendering
        if (SectionExcluded || fields.Length == 0)
            return;

        for (int i = 0; i < fields.Length; i++)
            _fieldBuffer.Add(fields[i]);
    }

    /// <inheritdoc/>
    public override void WriteFieldsNumbered(params ReadOnlySpan<MarkoutField> fields)
    {
        // Buffer fields for table rendering
        if (SectionExcluded || fields.Length == 0)
            return;

        for (int i = 0; i < fields.Length; i++)
            _fieldBuffer.Add(fields[i]);
    }

    /// <summary>
    /// Flushes any buffered fields as a two-column FIELD/VALUE table.
    /// </summary>
    private void FlushFieldBuffer()
    {
        if (_fieldBuffer.Count == 0)
            return;

        var projected = ProjectFields(CollectionsMarshal.AsSpan(_fieldBuffer));
        if (projected.Length == 0)
        {
            _fieldBuffer.Clear();
            return;
        }

        // Convert to table rows
        var rows = new List<string[]>(projected.Length);
        foreach (var field in projected)
        {
            rows.Add(new[] { field.Key, field.Value });
        }

        WriteTable(new[] { "Field", "Value" }, rows);
        _fieldBuffer.Clear();
    }

    /// <inheritdoc/>
    protected override void EnsureBlankLineIfNeeded() { }

    /// <inheritdoc/>
    public override void WriteSectionEnd()
    {
        FlushFieldBuffer();
        base.WriteSectionEnd();
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        FlushFieldBuffer();
        return base.ToString();
    }
}
