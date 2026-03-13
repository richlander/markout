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
    {
        if (_batchFormatter != null)
        {
            var (truncated, skipped) = TruncateRows(rows);
            _batchFormatter.FormatTable(_writer, headers, truncated, skipped, _options);
            return;
        }

        if (_streamingFormatter != null)
        {
            _streamingFormatter.BeginTable(_writer, headers, _options);
            int count = 0;
            int skipped = 0;
            foreach (var row in rows)
            {
                if (_options.MaxItems is int max && count >= max)
                {
                    skipped++;
                    continue;
                }
                _streamingFormatter.WriteRow(_writer, row);
                count++;
            }
            _streamingFormatter.EndTable(_writer, skipped);
        }
    }

    /// <summary>
    /// Starts a streaming table.
    /// </summary>
    public void WriteTableStart(params ReadOnlySpan<string> headers)
    {
        _tableRowCount = 0;
        _tableRowsSkipped = 0;
        _streamingDirect = false;

        _streamingHeaders = headers.ToArray();

        // Force buffering when TableOptions is set — statistical width
        // calculation requires all rows before rendering.
        if (_streamingFormatter != null && _options.TableOptions == null)
        {
            _streamingDirect = true;
            _streamingFormatter.BeginTable(_writer, headers, _options);
        }
        else
        {
            _streamingRows = [];
        }
    }

    /// <summary>
    /// Writes a single table row. Must be between WriteTableStart and WriteTableEnd.
    /// </summary>
    public void WriteTableRow(params ReadOnlySpan<string> values)
    {
        if (_streamingHeaders == null) return;

        if (_options.MaxItems is int max && _tableRowCount >= max)
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
        else if (_batchFormatter != null)
        {
            _batchFormatter.FormatTable(_writer, _streamingHeaders, _streamingRows ?? [], _tableRowsSkipped, _options);
        }

        _streamingHeaders = null;
        _streamingRows = null;
        _streamingDirect = false;
    }

    private (IList<string[]> rows, int skipped) TruncateRows(IList<string[]> rows)
    {
        if (_options.MaxItems is int max && rows.Count > max)
            return (rows.Take(max).ToList(), rows.Count - max);
        return (rows, 0);
    }
}
