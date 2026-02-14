namespace Markout;

/// <summary>
/// A writer that produces compact columnar output (docker-style).
/// Tables use space-padded columns with uppercase headers.
/// Suppresses headings, fields, paragraphs, and code blocks.
/// </summary>
public class OneLineWriter : MarkoutWriter
{
    private const int ColumnGap = 2;
    private readonly bool _showHeader;
    private string[]? _streamingHeaders;
    private List<string[]>? _streamingRows;

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
    public override MarkoutShape SupportedShapes => MarkoutShape.Tables | MarkoutShape.Lists;

    /// <inheritdoc/>
    public override void WriteHeading(int level, string text, string? context)
    {
        UpdateSectionState(level, text);
    }

    /// <inheritdoc/>
    public override void WriteTableStart(params string[] headers)
    {
        if (SectionExcluded)
        {
            InTable = true;
            return;
        }

        InTable = true;
        ResetTableRowTracking();
        _streamingHeaders = headers;
        _streamingRows = [];
    }

    /// <inheritdoc/>
    public override void WriteTableRow(params string[] values)
    {
        if (SectionExcluded || !ShouldWriteTableRow())
            return;

        _streamingRows?.Add(values);
    }

    /// <inheritdoc/>
    public override void WriteTableEnd()
    {
        InTable = false;
        if (SectionExcluded || _streamingHeaders == null)
            return;

        // Flush buffered rows with space-padded columns
        WriteTable(_streamingHeaders, _streamingRows ?? []);
        _streamingHeaders = null;
        _streamingRows = null;

        // WriteTable doesn't know about streaming skipped rows
        if (TableRowsSkipped > 0)
            Writer.WriteLine($"\n... and {TableRowsSkipped} more");
    }

    /// <inheritdoc/>
    public override void WriteTable(IEnumerable<string> headers, IEnumerable<string[]> rows)
    {
        if (SectionExcluded)
            return;

        var headerArray = headers as string[] ?? headers.ToArray();
        var rowList = rows as IList<string[]> ?? rows.ToList();

        // Apply MaxItems
        var maxItems = Options.MaxItems;
        var visibleRows = maxItems.HasValue && rowList.Count > maxItems.Value
            ? rowList.Take(maxItems.Value).ToList()
            : rowList;
        var skipped = rowList.Count - visibleRows.Count;

        // Calculate column widths from headers and visible data
        var widths = new int[headerArray.Length];
        for (int i = 0; i < headerArray.Length; i++)
            widths[i] = headerArray[i].Length;
        foreach (var row in visibleRows)
        {
            for (int i = 0; i < Math.Min(row.Length, widths.Length); i++)
                widths[i] = Math.Max(widths[i], row[i].Length);
        }

        if (_showHeader)
        {
            for (int i = 0; i < headerArray.Length; i++)
            {
                var text = headerArray[i].ToUpperInvariant();
                if (i < headerArray.Length - 1)
                    Writer.Write(text.PadRight(widths[i] + ColumnGap));
                else
                    Writer.Write(text);
            }
            Writer.WriteLine();
        }

        foreach (var row in visibleRows)
        {
            for (int i = 0; i < row.Length; i++)
            {
                if (i < row.Length - 1)
                    Writer.Write(row[i].PadRight(widths[i] + ColumnGap));
                else
                    Writer.Write(row[i]);
            }
            Writer.WriteLine();
        }

        if (skipped > 0)
            Writer.WriteLine($"\n... and {skipped} more");
    }

    /// <inheritdoc/>
    public override void WriteListItem(string text)
    {
        if (SectionExcluded)
            return;

        Writer.WriteLine(text);
    }

    /// <inheritdoc/>
    protected override void EnsureBlankLineIfNeeded() { }
}
