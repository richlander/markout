using Markout.Formatting;

namespace Markout;

/// <summary>
/// Writes tables to a TextWriter using a table formatter.
/// Handles MaxItems truncation, column projection, and streaming
/// (begin/row/end) pattern. Document state is managed by the caller
/// or <see cref="MarkoutWriter"/>.
/// </summary>
public class TableWriter
{
    private readonly TextWriter _writer;
    private readonly ITableFormatter? _batchFormatter;
    private readonly IStreamingTableFormatter? _streamingFormatter;
    private readonly MarkoutWriterOptions _options;

    // Streaming state
    private string[]? _streamingHeaders;
    private List<string[]>? _streamingRows;
    private bool _streamingDirect;
    private int _tableRowCount;
    private int _tableRowsSkipped;

    /// <summary>
    /// Creates a table writer with a batch table formatter.
    /// </summary>
    public TableWriter(TextWriter writer, ITableFormatter formatter, MarkoutWriterOptions? options = null)
    {
        _writer = writer;
        _batchFormatter = formatter;
        _streamingFormatter = formatter as IStreamingTableFormatter;
        _options = options ?? new();
    }

    /// <summary>
    /// Creates a table writer with a streaming table formatter.
    /// </summary>
    public TableWriter(TextWriter writer, IStreamingTableFormatter formatter, MarkoutWriterOptions? options = null)
    {
        _writer = writer;
        _batchFormatter = formatter as ITableFormatter;
        _streamingFormatter = formatter;
        _options = options ?? new();
    }

    /// <summary>
    /// Writes a complete table with headers and rows.
    /// </summary>
    public void WriteTable(ReadOnlySpan<string> headers, IList<string[]> rows)
        => WriteTableCore(headers, default, rows);

    /// <summary>
    /// Writes a complete table with display headers, stable header names, and rows.
    /// </summary>
    public void WriteTable(ReadOnlySpan<string> headers, ReadOnlySpan<string> headerNames, IList<string[]> rows)
        => WriteTableCore(headers, headerNames, rows);

    private void WriteTableCore(ReadOnlySpan<string> headers, ReadOnlySpan<string> headerNames, IList<string[]> rows)
    {
        var renderedHeaders = FormatHeaders(headers, headerNames);
        var (selected, skipped) = SelectRows(rows);

        if (_batchFormatter != null)
        {
            _batchFormatter.FormatTable(_writer, renderedHeaders, selected, skipped, _options);
            return;
        }

        if (_streamingFormatter != null)
        {
            _streamingFormatter.BeginTable(_writer, renderedHeaders, _options);
            foreach (var row in selected)
                _streamingFormatter.WriteRow(_writer, row);
            _streamingFormatter.EndTable(_writer, skipped);
        }
    }

    /// <summary>
    /// Starts a streaming table.
    /// </summary>
    public void WriteTableStart(params ReadOnlySpan<string> headers)
        => WriteTableStartCore(headers, default);

    /// <summary>
    /// Starts a streaming table with display headers and stable header names.
    /// </summary>
    public void WriteTableStart(ReadOnlySpan<string> headers, ReadOnlySpan<string> headerNames)
        => WriteTableStartCore(headers, headerNames);

    private void WriteTableStartCore(ReadOnlySpan<string> headers, ReadOnlySpan<string> headerNames)
    {
        _tableRowCount = 0;
        _tableRowsSkipped = 0;
        _streamingDirect = false;

        _streamingHeaders = FormatHeaders(headers, headerNames);

        // Force buffering when TableOptions is set — statistical width
        // calculation requires all rows before rendering. A row window forces it
        // for a different reason: Tail and an open-ended Range are defined
        // against the total row count, which is not known until the last row
        // arrives. Buffering lets every path resolve through the same
        // MarkoutRowWindow.Resolve rather than re-deriving the window positionally.
        if (_streamingFormatter != null && _options.TableOptions == null && !HasRowWindow)
        {
            _streamingDirect = true;
            _streamingFormatter.BeginTable(_writer, _streamingHeaders, _options);
        }
        else
        {
            _streamingRows = [];
        }
    }

    private string[] FormatHeaders(ReadOnlySpan<string> headers, ReadOnlySpan<string> headerNames)
    {
        var rendered = new string[headers.Length];
        for (var i = 0; i < headers.Length; i++)
        {
            var displayName = headers[i];
            var name = i < headerNames.Length && !string.IsNullOrEmpty(headerNames[i])
                ? headerNames[i]
                : displayName;
            var header = new MarkoutTableHeader(name, displayName, i);
            rendered[i] = _options.FormatTableHeader?.Invoke(header)
                ?? FormatHeader(header);
        }
        return rendered;
    }

    private string FormatHeader(MarkoutTableHeader header)
    {
        return _options.TableHeaderStyle switch
        {
            MarkoutTableHeaderStyle.DisplayName => header.DisplayName,
            MarkoutTableHeaderStyle.StableName => header.Key,
            _ when _options.TableMode is MarkoutTableMode.Tsv or MarkoutTableMode.Jsonl => header.Key,
            _ => header.DisplayName
        };
    }

    /// <summary>
    /// Writes a single table row. Must be between WriteTableStart and WriteTableEnd.
    /// </summary>
    public void WriteTableRow(params ReadOnlySpan<string> values)
    {
        if (_streamingHeaders == null) return;

        // With a window active every row must be buffered: which rows survive is
        // not decidable until the total is known, so capping here would cap the
        // wrong set. SelectRows applies both the window and MaxItems at end.
        if (!HasRowWindow && _options.MaxItems is int max && _tableRowCount >= max)
        {
            _tableRowsSkipped++;
            return;
        }
        _tableRowCount++;

        if (_streamingDirect && _streamingFormatter != null)
        {
            _streamingFormatter.WriteRow(_writer, values);
        }
        else
        {
            _streamingRows?.Add(values.ToArray());
        }
    }

    /// <summary>
    /// Ends the current streaming table.
    /// </summary>
    public void WriteTableEnd()
    {
        if (_streamingHeaders == null) return;

        if (_streamingDirect && _streamingFormatter != null)
        {
            _streamingFormatter.EndTable(_writer, _tableRowsSkipped);
        }
        else
        {
            // Buffered. MaxItems was deferred while a window was active, so both
            // are resolved here; without a window the rows were capped on arrival.
            var rows = (IList<string[]>)(_streamingRows ?? []);
            var skipped = _tableRowsSkipped;
            if (HasRowWindow)
                (rows, skipped) = SelectRows(rows);

            if (_batchFormatter != null)
            {
                _batchFormatter.FormatTable(_writer, _streamingHeaders, rows, skipped, _options);
            }
            else if (_streamingFormatter != null)
            {
                // A streaming-only formatter still has to emit what was buffered,
                // or forcing buffering would silently drop the entire table.
                _streamingFormatter.BeginTable(_writer, _streamingHeaders, _options);
                foreach (var row in rows)
                    _streamingFormatter.WriteRow(_writer, row);
                _streamingFormatter.EndTable(_writer, skipped);
            }
        }

        _streamingHeaders = null;
        _streamingRows = null;
        _streamingDirect = false;
    }

    private bool HasRowWindow => _options.RowWindow is { IsUnlimited: false };

    private (IList<string[]> rows, int skipped) SelectRows(IList<string[]> rows)
    {
        var selected = rows;

        // Selection runs before summarization: the window says which rows exist,
        // MaxItems then says how many of those to show. Resolving here — and only
        // here — is what keeps every table mode agreeing on what a row window means.
        if (_options.RowWindow is { IsUnlimited: false } window)
        {
            var (keepStart, keepEnd) = window.Resolve(rows.Count);
            if (keepStart != 0 || keepEnd != rows.Count)
            {
                var kept = new List<string[]>(keepEnd - keepStart);
                for (var i = keepStart; i < keepEnd; i++)
                    kept.Add(rows[i]);
                selected = kept;
            }
        }

        if (_options.MaxItems is int max && selected.Count > max)
            return (selected.Take(max).ToList(), selected.Count - max);

        return (selected, 0);
    }
}
