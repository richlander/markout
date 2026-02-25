using System.Text;

namespace Markout;

/// <summary>
/// A MarkoutWriter that renders output as Markdown.
/// Produces # headings, **bold** field names, | pipe tables |, - bullet lists,
/// ``` code fences, and trailing double-space hard line breaks.
/// </summary>
public class MarkdownWriter : MarkoutWriter
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

    /// <inheritdoc/>
    public override void WriteHeading(int level, string text, string? context)
    {
        if (level < 1 || level > 6)
            throw new ArgumentOutOfRangeException(nameof(level), "Heading level must be between 1 and 6.");

        UpdateSectionState(level, text);

        if (SectionExcluded)
            return;

        if (HasContent)
        {
            Writer.WriteLine();
        }

        Writer.Write(HeadingPrefixes[level]);
        Writer.Write(' ');
        Writer.Write(text);

        if (!string.IsNullOrEmpty(context))
        {
            Writer.Write(" (");
            Writer.Write(context);
            Writer.Write(')');
        }

        Writer.WriteLine();
        NeedsBlankLine = true;
        HasContent = true;
    }

    /// <inheritdoc/>
    protected override void WriteFieldName(string key)
    {
        if (BoldFieldNames)
        {
            Writer.Write("**");
            Writer.Write(key);
            Writer.Write(":** ");
        }
        else
        {
            Writer.Write(key);
            Writer.Write(": ");
        }
    }

    /// <inheritdoc/>
    public override void WriteFieldBareList(params ReadOnlySpan<MarkoutField> fields)
    {
        if (SectionExcluded || fields.Length == 0 || ShapeUnsupported(MarkoutShape.Fields))
            return;

        var projected = ProjectFields(fields);
        if (projected.Length == 0)
            return;

        EnsureBlankLineIfNeeded();

        for (int i = 0; i < projected.Length; i++)
        {
            WriteFieldName(projected[i].Key);
            Writer.Write(projected[i].Value);
            Writer.WriteLine("  "); // Two trailing spaces for markdown hard line break
        }

        NeedsBlankLine = true;
        HasContent = true;
    }

    /// <inheritdoc/>
    public override void WriteCodeStart(string? language = null)
    {
        if (InCode)
            throw new InvalidOperationException("Cannot nest code regions. End the current code region before starting a new one.");

        if (SectionExcluded)
        {
            InCode = true;
            return;
        }

        EnsureBlankLineIfNeeded();
        Writer.Write("```");
        if (!string.IsNullOrEmpty(language))
            Writer.Write(language);
        Writer.WriteLine();
        InCode = true;
        HasContent = true;
    }

    /// <inheritdoc/>
    public override void WriteCodeEnd()
    {
        if (!InCode)
            throw new InvalidOperationException("Cannot end a code region without starting one first.");

        InCode = false;

        if (SectionExcluded)
            return;

        Writer.WriteLine("```");
        NeedsBlankLine = true;
    }

    /// <inheritdoc/>
    protected override void FlushStreamingTable(string[] headers, IList<string[]> rows, int skippedRows)
    {
        if (Options.PrettyTables)
        {
            WritePrettyPipeTable(headers, rows, skippedRows);
        }
        else
        {
            WriteCompactPipeTable(headers, rows, skippedRows);
        }
    }

    /// <inheritdoc/>
    public override void WriteTable(IEnumerable<string> headers, IEnumerable<string[]> rows)
    {
        if (SectionExcluded || ShapeUnsupported(MarkoutShape.Tables))
            return;

        var headerArray = headers as string[] ?? headers.ToArray();
        var rowList = rows as IList<string[]> ?? rows.ToList();

        var maxItems = Options.MaxItems;
        var visibleRows = maxItems.HasValue && rowList.Count > maxItems.Value
            ? rowList.Take(maxItems.Value).ToList()
            : rowList;
        var skipped = rowList.Count - visibleRows.Count;

        if (Options.PrettyTables)
        {
            WritePrettyPipeTable(headerArray, visibleRows, skipped);
        }
        else
        {
            WriteCompactPipeTable(headerArray, visibleRows, skipped);
        }
    }

    private void WriteCompactPipeTable(string[] headers, IList<string[]> rows, int skippedRows)
    {
        EnsureBlankLineIfNeeded();

        // Header row
        Writer.Write('|');
        foreach (var header in headers)
        {
            Writer.Write(' ');
            Writer.Write(header);
            Writer.Write(" |");
        }
        Writer.WriteLine();

        // Separator row
        Writer.Write('|');
        foreach (var header in headers)
        {
            Writer.Write(' ');
            for (int i = 0; i < header.Length; i++)
                Writer.Write('-');
            Writer.Write(" |");
        }
        Writer.WriteLine();

        // Data rows
        foreach (var row in rows)
        {
            Writer.Write('|');
            foreach (var value in row)
            {
                Writer.Write(' ');
                Writer.Write(EscapeTableCell(value));
                Writer.Write(" |");
            }
            Writer.WriteLine();
        }

        if (skippedRows > 0)
            Writer.WriteLine($"\n... and {skippedRows} more");

        NeedsBlankLine = true;
        HasContent = true;
    }

    private void WritePrettyPipeTable(string[] headers, IList<string[]> rows, int skippedRows)
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

        EnsureBlankLineIfNeeded();

        // Header row
        Writer.Write('|');
        for (int i = 0; i < headers.Length; i++)
        {
            Writer.Write(' ');
            Writer.Write(headers[i].PadRight(widths[i]));
            Writer.Write(" |");
        }
        Writer.WriteLine();

        // Separator row
        Writer.Write('|');
        for (int i = 0; i < headers.Length; i++)
        {
            Writer.Write(' ');
            Writer.Write(new string('-', widths[i]));
            Writer.Write(" |");
        }
        Writer.WriteLine();

        // Data rows
        foreach (var row in rows)
        {
            Writer.Write('|');
            for (int i = 0; i < headers.Length; i++)
            {
                Writer.Write(' ');
                var value = i < row.Length ? EscapeTableCell(row[i]) : "";
                Writer.Write(value.PadRight(widths[i]));
                Writer.Write(" |");
            }
            Writer.WriteLine();
        }

        if (skippedRows > 0)
            Writer.WriteLine($"\n... and {skippedRows} more");

        NeedsBlankLine = true;
        HasContent = true;
    }

    /// <inheritdoc/>
    protected override void WriteDescription(Description item)
    {
        Writer.Write("- **");
        Writer.Write(item.Term);
        Writer.Write(":** ");
        Writer.WriteLine(item.Text);

        if (item.Detail != null)
        {
            Writer.Write("  ");
            Writer.WriteLine(item.Detail);
        }
    }

    /// <inheritdoc/>
    public override void WriteCallout(CalloutSeverity severity, string message)
    {
        if (SectionExcluded || ShapeUnsupported(MarkoutShape.Callouts))
            return;

        if (HasContent)
            NeedsBlankLine = true;
        EnsureBlankLineIfNeeded();

        var label = severity switch
        {
            CalloutSeverity.Note => "NOTE",
            CalloutSeverity.Tip => "TIP",
            CalloutSeverity.Important => "IMPORTANT",
            CalloutSeverity.Warning => "WARNING",
            CalloutSeverity.Caution => "CAUTION",
            _ => severity.ToString().ToUpperInvariant()
        };

        Writer.WriteLine($"> [!{label}]");
        Writer.Write("> ");
        Writer.WriteLine(message);

        NeedsBlankLine = true;
        HasContent = true;
    }

    /// <inheritdoc/>
    public override void WriteQuotation(string text)
    {
        if (SectionExcluded || ShapeUnsupported(MarkoutShape.Quotation))
            return;

        if (HasContent)
            NeedsBlankLine = true;
        EnsureBlankLineIfNeeded();

        foreach (var line in text.Split('\n'))
        {
            Writer.Write("> ");
            Writer.WriteLine(line);
        }

        NeedsBlankLine = true;
        HasContent = true;
    }

    /// <inheritdoc/>
    public override void WriteRule()
    {
        if (SectionExcluded)
            return;

        if (HasContent)
            NeedsBlankLine = true;
        EnsureBlankLineIfNeeded();

        Writer.WriteLine("---");

        NeedsBlankLine = true;
        HasContent = true;
    }

    /// <inheritdoc/>
    public override void WriteMatrix(string[] rowHeaders, string[] colHeaders, string?[,] values)
    {
        if (SectionExcluded || ShapeUnsupported(MarkoutShape.Matrices))
            return;

        if (rowHeaders.Length == 0 || colHeaders.Length == 0)
            return;

        if (HasContent)
            NeedsBlankLine = true;
        EnsureBlankLineIfNeeded();

        // Calculate column widths (first column is row header)
        var colWidths = new int[colHeaders.Length + 1];
        colWidths[0] = rowHeaders.Max(h => h.Length);
        for (int c = 0; c < colHeaders.Length; c++)
        {
            colWidths[c + 1] = colHeaders[c].Length;
            for (int r = 0; r < rowHeaders.Length; r++)
            {
                var val = values[r, c] ?? "";
                if (val.Length > colWidths[c + 1])
                    colWidths[c + 1] = val.Length;
            }
        }

        // Header row: | (empty) | Col1 | Col2 | ...
        Writer.Write("| ");
        Writer.Write("".PadRight(colWidths[0]));
        Writer.Write(" ");
        for (int c = 0; c < colHeaders.Length; c++)
        {
            Writer.Write("| ");
            Writer.Write(colHeaders[c].PadRight(colWidths[c + 1]));
            Writer.Write(" ");
        }
        Writer.WriteLine("|");

        // Separator row
        Writer.Write("| ");
        Writer.Write(new string('-', colWidths[0]));
        Writer.Write(" ");
        for (int c = 0; c < colHeaders.Length; c++)
        {
            Writer.Write("| ");
            Writer.Write(new string('-', colWidths[c + 1]));
            Writer.Write(" ");
        }
        Writer.WriteLine("|");

        // Data rows
        for (int r = 0; r < rowHeaders.Length; r++)
        {
            Writer.Write("| ");
            Writer.Write(rowHeaders[r].PadRight(colWidths[0]));
            Writer.Write(" ");
            for (int c = 0; c < colHeaders.Length; c++)
            {
                Writer.Write("| ");
                Writer.Write((values[r, c] ?? "").PadRight(colWidths[c + 1]));
                Writer.Write(" ");
            }
            Writer.WriteLine("|");
        }

        NeedsBlankLine = true;
        HasContent = true;
    }

    /// <inheritdoc/>
    public override void WriteArray(string key, IEnumerable<string>? items)
    {
        if (SectionExcluded)
            return;

        if (HasContent)
            NeedsBlankLine = true;
        EnsureBlankLineIfNeeded();

        if (BoldFieldNames)
        {
            Writer.Write("**");
            Writer.Write(key);
            Writer.WriteLine(":**");
        }
        else
        {
            Writer.Write(key);
            Writer.WriteLine(":");
        }

        WriteBulletItems(items);
    }

    /// <inheritdoc/>
    public override void WriteBreakdown(IReadOnlyList<Breakdown> items, int? maxBarWidth = null, bool uniformBarWidth = true)
    {
        if (items.Count == 0 || SectionExcluded || ShapeUnsupported(MarkoutShape.Breakdowns))
            return;

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

            WriteTable(headers, rows);
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

            WriteTable(headers, rows);
        }
    }

    /// <inheritdoc/>
    public override void WriteMetrics(IReadOnlyList<Metric> items, int maxBarWidth = 30)
    {
        if (items.Count == 0 || SectionExcluded || ShapeUnsupported(MarkoutShape.Metrics))
            return;

        // Render as a pipe table: Label | Value
        var headers = new[] { "Label", "Value" };
        var rows = items.Select(m => new[] { m.Label, FormatBarValue(m.Value) }).ToList();
        WriteTable(headers, rows);
    }

    /// <inheritdoc/>
    public override void WriteVerticalMetrics(IReadOnlyList<Metric> items, int maxBarHeight = 10, int? barWidth = null)
    {
        // Same as horizontal — vertical layout is a terminal concept
        WriteMetrics(items, maxBarHeight);
    }
}
