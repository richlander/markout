using System.Text;

namespace Markout;

/// <summary>
/// A MarkoutWriter that renders output as Markdown.
/// Produces # headings, **bold** field names, | pipe tables |, - bullet lists,
/// ``` code blocks, and trailing double-space hard line breaks.
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
    public override void WriteField(string key, string? value)
    {
        if (SectionExcluded)
            return;

        EnsureBlankLineIfNeeded();
        WriteFieldName(key);
        Writer.Write(value ?? string.Empty);
        Writer.WriteLine("  "); // Two trailing spaces for markdown hard line break
        HasContent = true;
    }

    /// <inheritdoc/>
    public override void WriteField(string key, bool value)
    {
        if (SectionExcluded)
            return;

        EnsureBlankLineIfNeeded();
        WriteFieldName(key);
        Writer.Write(value ? "yes" : "no");
        Writer.WriteLine("  "); // Two trailing spaces for markdown hard line break
        HasContent = true;
    }

    /// <inheritdoc/>
    public override void WriteField<T>(string key, T value)
    {
        if (SectionExcluded)
            return;

        EnsureBlankLineIfNeeded();
        WriteFieldName(key);
        WriteFormattedValue(value);
        Writer.WriteLine("  "); // Two trailing spaces for markdown hard line break
        HasContent = true;
    }

    /// <inheritdoc/>
    public override void WriteCodeBlockStart(string? language = null)
    {
        if (InCodeBlock)
            throw new InvalidOperationException("Cannot nest code blocks. End the current code block before starting a new one.");

        if (SectionExcluded)
        {
            InCodeBlock = true;
            return;
        }

        EnsureBlankLineIfNeeded();
        Writer.Write("```");
        if (!string.IsNullOrEmpty(language))
            Writer.Write(language);
        Writer.WriteLine();
        InCodeBlock = true;
        HasContent = true;
    }

    /// <inheritdoc/>
    public override void WriteCodeBlockEnd()
    {
        if (!InCodeBlock)
            throw new InvalidOperationException("Cannot end a code block without starting one first.");

        InCodeBlock = false;

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
    protected override void WriteLabeledListItem(LabeledItem item)
    {
        Writer.Write("- **");
        Writer.Write(item.Label);
        Writer.Write(":** ");
        Writer.WriteLine(item.Description);

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
    public override void WriteDistribution(IReadOnlyList<DistributionBar> items, int? maxBarWidth = null)
    {
        if (items.Count == 0 || SectionExcluded || ShapeUnsupported(MarkoutShape.Distributions))
            return;

        EnsureBlankLineIfNeeded();
        Writer.WriteLine("```text");

        // Reuse base rendering logic for the content
        var categories = new List<string>();
        foreach (var item in items)
            foreach (var seg in item.Segments)
                if (!categories.Contains(seg.Category))
                    categories.Add(seg.Category);

        var maxLabelWidth = 0;
        var maxTotal = 0;
        foreach (var item in items)
        {
            if (item.Label.Length > maxLabelWidth) maxLabelWidth = item.Label.Length;
            var total = 0;
            foreach (var seg in item.Segments) total += seg.Count;
            if (total > maxTotal) maxTotal = total;
        }
        if (maxTotal == 0) maxTotal = 1;
        var barScale = maxBarWidth.HasValue ? (double)maxBarWidth.Value / maxTotal : 1.0;

        foreach (var item in items)
            WriteDistributionRow(item, categories, maxLabelWidth, barScale);

        Writer.WriteLine();
        WriteDistributionLegend(categories);
        Writer.WriteLine("```");
        NeedsBlankLine = true;
        HasContent = true;
    }

    /// <inheritdoc/>
    public override void WriteBarChart(IReadOnlyList<BarItem> items, int maxBarWidth = 30)
    {
        if (items.Count == 0 || SectionExcluded || ShapeUnsupported(MarkoutShape.BarCharts))
            return;

        EnsureBlankLineIfNeeded();
        Writer.WriteLine("```text");
        // Delegate to base rendering (which writes individual bar lines)
        var maxValue = 0.0;
        var maxLabelWidth = 0;
        var maxValueWidth = 0;
        foreach (var item in items)
        {
            if (item.Value > maxValue) maxValue = item.Value;
            if (item.Label.Length > maxLabelWidth) maxLabelWidth = item.Label.Length;
            var vw = FormatBarValue(item.Value).Length;
            if (vw > maxValueWidth) maxValueWidth = vw;
        }
        if (maxValue <= 0) maxValue = 1;

        foreach (var item in items)
            WriteBarLine(item, maxLabelWidth, maxBarWidth, maxValue, maxValueWidth);

        Writer.WriteLine("```");
        NeedsBlankLine = true;
        HasContent = true;
    }

    /// <inheritdoc/>
    public override void WriteVerticalBarChart(IReadOnlyList<BarItem> items, int maxBarHeight = 10, int? barWidth = null)
    {
        if (items.Count == 0 || SectionExcluded || ShapeUnsupported(MarkoutShape.BarCharts))
            return;

        EnsureBlankLineIfNeeded();
        Writer.WriteLine("```text");
        WriteVerticalBarBody(items, maxBarHeight, barWidth);
        Writer.WriteLine("```");
        NeedsBlankLine = true;
        HasContent = true;
    }
}
