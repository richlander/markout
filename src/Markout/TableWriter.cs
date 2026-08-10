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
    private int _dataPosition;
    private Queue<string[]>? _tailBuffer;
    private int _tailBound;

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
        TableHeaderValidator.Validate(headers, headerNames);
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
        TableHeaderValidator.Validate(headers, headerNames);
        _tableRowCount = 0;
        _tableRowsSkipped = 0;
        _streamingDirect = false;
        _dataPosition = 0;
        _tailBuffer = null;

        _streamingHeaders = FormatHeaders(headers, headerNames);

        // Force buffering when TableOptions is set — statistical width calculation
        // requires all rows before rendering. A Tail window forces it for its own
        // reason: which rows are the last ones is not known until the table ends.
        // Head and Range decide each row from its position, so they keep streaming
        // and retain nothing; a window is not a reason on its own to hold a table
        // in memory.
        var window = _options.RowWindow;
        if (_streamingFormatter != null && _options.TableOptions == null
            && (window == null || window.Value.IsPositional))
        {
            _streamingDirect = true;
            _streamingFormatter.BeginTable(_writer, _streamingHeaders, _options);
        }
        else
        {
            // A Tail window never needs more than its own count in hand, so it reads
            // through a queue bounded by what the window can keep rather than by the
            // size of the table. Enqueue-and-dequeue keeps that O(1) per row; trimming
            // a list from the front would make a large Tail quadratic.
            if (window is { IsPositional: false } tail)
            {
                _tailBound = tail.RetentionBound;
                _tailBuffer = new Queue<string[]>(Math.Min(_tailBound, 1024));
            }
            else
            {
                _streamingRows = [];
            }
        }
    }

    private string[] FormatHeaders(ReadOnlySpan<string> headers, ReadOnlySpan<string> headerNames)
    {
        var rendered = new string[headers.Length];
        var structured = _options.TableMode is MarkoutTableMode.Tsv or MarkoutTableMode.Jsonl;
        for (var i = 0; i < headers.Length; i++)
        {
            var displayName = headers[i];
            var name = i < headerNames.Length && !string.IsNullOrEmpty(headerNames[i])
                ? headerNames[i]
                : displayName;
            var header = new MarkoutTableHeader(name, displayName, i);
            rendered[i] = !structured && _options.FormatTableHeader is { } format
                ? format(header)
                : FormatHeader(header);
        }

        if (structured)
            ValidateStructuredHeaders(rendered);

        return rendered;
    }

    private void ValidateStructuredHeaders(ReadOnlySpan<string> headers)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < headers.Length; i++)
        {
            var key = _options.TableMode == MarkoutTableMode.Tsv
                ? Formatting.FormatHelper.NormalizeTableCell(
                    Formatting.FormatHelper.RenderInlinePlainText(headers[i]))
                : headers[i];
            if (!seen.Add(key))
            {
                throw new ArgumentException(
                    $"Two columns share the structured key '{key}'. Structured output would emit duplicate keys and lose a column.",
                    "headers");
            }
        }
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

        var position = _dataPosition++;

        // A positional window is asked about this row directly; it is the same type
        // that answers Resolve, so streaming does not get its own idea of what the
        // window means. A Tail window has no answer yet and is settled at the end.
        if (_options.RowWindow is { IsPositional: true } window && !window.KeepsPosition(position))
            return;

        // MaxItems caps the window's selection, so it counts selected rows. Under a
        // Tail window the selection is not known yet, so the cap is deferred to
        // SelectRows rather than applied to whichever rows happened to arrive first.
        if (!DefersMaxItems && _options.MaxItems is int max && _tableRowCount >= max)
        {
            _tableRowsSkipped++;
            return;
        }
        _tableRowCount++;

        if (_streamingDirect && _streamingFormatter != null)
        {
            _streamingFormatter.WriteRow(_writer, values);
        }
        else if (_tailBuffer != null)
        {
            // A zero-retention tail keeps nothing, so copying a row only to discard
            // it on the next line is pure waste. Above zero, make room before
            // copying so the queue never holds more than the window can keep.
            if (_tailBound > 0)
            {
                while (_tailBuffer.Count >= _tailBound)
                    _tailBuffer.Dequeue();
                _tailBuffer.Enqueue(values.ToArray());
            }
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
            // Buffered. A positional window already excluded its rows on arrival and
            // MaxItems already capped what survived, so re-running SelectRows here
            // would apply the window a second time to rows that are only the window's
            // output. Only a Tail window still has both to settle.
            var rows = (IList<string[]>)(_tailBuffer?.ToArray() ?? (IList<string[]>?)_streamingRows ?? []);
            var skipped = _tableRowsSkipped;
            if (_options.RowWindow is { IsPositional: false })
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
        _tailBuffer = null;
        _streamingDirect = false;
    }

    /// <summary>
    /// Whether MaxItems has to wait for the table to end. Only a Tail window makes it
    /// wait: capping on arrival would cap the rows that came first, not the rows the
    /// window selected.
    /// </summary>
    private bool DefersMaxItems => _options.RowWindow is { IsPositional: false };

    private (IList<string[]> rows, int skipped) SelectRows(IList<string[]> rows)
    {
        var selected = rows;

        // Selection runs before summarization: the window says which rows exist,
        // MaxItems then says how many of those to show. Resolving here — and only
        // here — is what keeps every table mode agreeing on what a row window means.
        if (_options.RowWindow is { } window)
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
