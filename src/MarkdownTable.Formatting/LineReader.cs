using System.Buffers;
using System.Text;

namespace MarkdownTable.Formatting;

/// <summary>
/// Byte-level line reader with buffered stream I/O and transactional position
/// management. Uses <see cref="SearchValues{T}"/> for SIMD-accelerated newline
/// search on raw UTF-8 bytes.
/// </summary>
/// <remarks>
/// Ported from MarkdownTable.IO.LineReader (smooth-markdown-table) with the
/// prefetch/double-buffering path removed. Retains:
/// <list type="bullet">
///   <item><description>Buffer flip — compacts unprocessed bytes to front, reads more from stream</description></item>
///   <item><description>Save/Rewind — transactional multi-line lookahead (table header identification)</description></item>
///   <item><description>BufferFlipVersion + Validate — span lifetime safety across buffer operations</description></item>
///   <item><description>Cached newline index — avoids redundant scans</description></item>
///   <item><description>BOM handling — skips UTF-8 BOM on first read</description></item>
/// </list>
/// Processing loop:
/// <code>
/// while (!reader.IsComplete)
/// {
///     if (!reader.ReadLine(out var line))
///     {
///         if (!await reader.AdvanceAsync()) break;
///         continue;
///     }
///     // process line
/// }
/// </code>
/// </remarks>
public class LineReader
{
    private static readonly SearchValues<byte> NewlineSearchValues = SearchValues.Create([(byte)'\n']);

    private readonly Stream _stream;
    private readonly int _bufferSize;

    private byte[] _activeBuffer;

    private int _position;
    private int _bytesInBuffer;
    private int _nextNewlineIndex;
    private bool _isEof;
    private bool _bomHandled;
    private int _savedPosition = -1;
    private int _savedNextNewlineIndex = -1;
    private int _bufferFlipVersion;

    /// <summary>Current position in buffer.</summary>
    public int Position => _position;

    /// <summary>Total valid bytes in buffer.</summary>
    public int BytesInBuffer => _bytesInBuffer;

    /// <summary>
    /// True when all data has been processed (stream EOF and buffer exhausted).
    /// </summary>
    public bool IsComplete => _position >= _bytesInBuffer && _isEof;

    /// <summary>Cached newline index (-1 if none found).</summary>
    public int NextNewlineIndex => _nextNewlineIndex;

    /// <summary>True if a position has been saved for potential rewind.</summary>
    public bool HasSavedPosition => _savedPosition != -1;

    /// <summary>True if there's a newline available from current position (cheap check).</summary>
    public bool HasNewline => _nextNewlineIndex != -1;

    /// <summary>
    /// Buffer flip version — increments when buffer contents change significantly.
    /// Use with <see cref="Validate"/> to assert span lifetime safety.
    /// </summary>
    public int BufferFlipVersion => _bufferFlipVersion;

    /// <summary>Count newlines from current position to end of buffer.</summary>
    public int NewlineCount
    {
        get
        {
            if (_position >= _bytesInBuffer) return 0;

            var remaining = _activeBuffer.AsSpan(_position, _bytesInBuffer - _position);
            int count = 0;
            int searchStart = 0;

            while (searchStart < remaining.Length)
            {
                var relativeIndex = remaining.Slice(searchStart).IndexOfAny(NewlineSearchValues);
                if (relativeIndex == -1) break;
                count++;
                searchStart += relativeIndex + 1;
            }

            return count;
        }
    }

    /// <summary>
    /// Creates a line reader over <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">The stream to read UTF-8 bytes from.</param>
    /// <param name="bufferSize">Size in bytes of the read buffer. Must be positive.</param>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="bufferSize"/> is not positive.</exception>
    public LineReader(Stream stream, int bufferSize)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (bufferSize <= 0) throw new ArgumentException("Buffer size must be positive", nameof(bufferSize));

        _stream = stream;
        _bufferSize = bufferSize;
        _activeBuffer = new byte[bufferSize];
        _position = 0;
        _bytesInBuffer = 0;
        _nextNewlineIndex = -1;
        _isEof = false;
        _bomHandled = false;
    }

    /// <summary>
    /// Creates a LineReader with an optimal buffer size based on stream length.
    /// </summary>
    public static LineReader Create(Stream stream)
    {
        var fileSize = stream.CanSeek ? stream.Length : 1_000_000;
        var bufferSize = fileSize switch
        {
            < 1_000_000 => 8_192,
            < 10_000_000 => 32_768,
            < 100_000_000 => 131_072,
            _ => 524_288,
        };
        return new LineReader(stream, bufferSize);
    }

    /// <summary>
    /// Reads the next complete line as <see cref="ReadOnlySpan{T}"/> of bytes,
    /// advancing position past the newline. Returns false if the buffer does
    /// not contain a complete line (call <see cref="AdvanceAsync"/> and retry).
    /// </summary>
    public bool ReadLine(out ReadOnlySpan<byte> line)
    {
        line = ReadOnlySpan<byte>.Empty;

        if (IsComplete)
            return false;

        if (_nextNewlineIndex == -1)
        {
            if (!FindNextNewline())
                return false;
        }

        var lineLength = _nextNewlineIndex - _position;

        // Strip \r from \r\n
        if (lineLength > 0 && _activeBuffer[_nextNewlineIndex - 1] == (byte)'\r')
            lineLength--;

        line = _activeBuffer.AsSpan(_position, lineLength);

        // Advance past the newline (or to end of buffer for EOF)
        var newPosition = _nextNewlineIndex == _bytesInBuffer ? _bytesInBuffer : _nextNewlineIndex + 1;
        UpdateState(new StateChange(StateChangeType.LineConsumed, NewPosition: newPosition));
        return true;
    }

    /// <summary>
    /// Peeks at the next line without advancing position. Returns false if
    /// no complete line is available.
    /// </summary>
    public bool ReadLineNoConsume(out ReadOnlySpan<byte> line)
    {
        line = ReadOnlySpan<byte>.Empty;

        if (IsComplete)
            return false;

        if (_nextNewlineIndex == -1)
        {
            if (!FindNextNewline())
                return false;
        }

        var lineLength = _nextNewlineIndex - _position;

        if (lineLength > 0 && _activeBuffer[_nextNewlineIndex - 1] == (byte)'\r')
            lineLength--;

        line = _activeBuffer.AsSpan(_position, lineLength);
        return true;
    }

    /// <summary>
    /// Consumes the current newline without returning line content. Returns false
    /// if no newline to consume.
    /// </summary>
    public bool ConsumeNextNewline()
    {
        if (IsComplete)
            return false;

        if (_nextNewlineIndex == -1)
            return false;

        var newPosition = _nextNewlineIndex == _bytesInBuffer ? _bytesInBuffer : _nextNewlineIndex + 1;
        UpdateState(new StateChange(StateChangeType.LineConsumed, NewPosition: newPosition));
        return true;
    }

    /// <summary>
    /// Saves current position for potential rewind (transactional lookahead).
    /// </summary>
    public void SavePosition()
    {
        _savedPosition = _position;
        _savedNextNewlineIndex = _nextNewlineIndex;
    }

    /// <summary>
    /// Rewinds to the last saved position. Throws if no position was saved.
    /// </summary>
    public void Rewind()
    {
        if (_savedPosition == -1)
            throw new InvalidOperationException("No saved position to rewind to");

        _position = _savedPosition;
        _nextNewlineIndex = _savedNextNewlineIndex;
        ResetSavedPosition();
    }

    /// <summary>
    /// Clears the saved position state.
    /// </summary>
    public void ResetSavedPosition()
    {
        _savedPosition = -1;
        _savedNextNewlineIndex = -1;
    }

    /// <summary>
    /// Validates that the buffer flip version matches the expected value.
    /// Throws if the buffer has been flipped since the version was captured,
    /// meaning any spans obtained before the flip are now invalid.
    /// </summary>
    public void Validate(int expectedVersion)
    {
        if (_bufferFlipVersion != expectedVersion)
            throw new InvalidOperationException(
                $"Buffer state changed unexpectedly: expected version {expectedVersion}, actual {_bufferFlipVersion}");
    }

    /// <summary>
    /// Advances the buffer: compacts unprocessed bytes to front, reads more
    /// data from the stream. Returns true if more data is available (a newline
    /// was found or EOF reached with remaining data), false otherwise.
    /// </summary>
    public async Task<bool> AdvanceAsync(CancellationToken cancellationToken = default)
    {
        if (!FlipBuffer())
            return false;

        var spaceAvailable = _activeBuffer.Length - _bytesInBuffer;
        if (spaceAvailable == 0)
            return false;

        var bytesRead = await _stream.ReadAsync(
            _activeBuffer.AsMemory(_bytesInBuffer, spaceAvailable), cancellationToken);

        if (bytesRead == 0)
        {
            UpdateState(new StateChange(StateChangeType.NewDataRead, IsEof: true));
        }
        else
        {
            HandleBomOnFirstRead();
            UpdateState(new StateChange(StateChangeType.NewDataRead,
                NewBufferSize: _bytesInBuffer + bytesRead, IsEof: false));
        }

        return _nextNewlineIndex != -1;
    }

    /// <summary>
    /// Decodes a UTF-8 line span to a string.
    /// </summary>
    public static string ToString(ReadOnlySpan<byte> line) =>
        Encoding.UTF8.GetString(line);

    // --- Private implementation ---

    private void HandleBomOnFirstRead()
    {
        if (!_bomHandled && _bytesInBuffer >= 3 && _position == 0)
        {
            if (_activeBuffer[0] == 0xEF && _activeBuffer[1] == 0xBB && _activeBuffer[2] == 0xBF)
                _position = 3;
            _bomHandled = true;
        }
    }

    private bool FindNextNewline()
    {
        var remaining = _activeBuffer.AsSpan(_position, _bytesInBuffer - _position);
        var relativeIndex = remaining.IndexOfAny(NewlineSearchValues);

        if (relativeIndex != -1)
        {
            _nextNewlineIndex = _position + relativeIndex;
            return true;
        }

        if (_isEof)
        {
            _nextNewlineIndex = _bytesInBuffer;
            return true;
        }

        _nextNewlineIndex = -1;
        return false;
    }

    private bool ShouldFlipBuffer()
    {
        if (_bytesInBuffer == 0)
            return true;

        if (_nextNewlineIndex == -1)
            return true;

        // Don't flip if we haven't consumed much
        if (_position < _bytesInBuffer / 4)
            return false;

        return true;
    }

    private bool FlipBuffer()
    {
        if (!ShouldFlipBuffer())
            return false;

        var unprocessed = _activeBuffer.AsSpan(_position, _bytesInBuffer - _position);
        unprocessed.CopyTo(_activeBuffer);
        var remainingLength = unprocessed.Length;
        var positionDelta = _position;

        ResetSavedPosition();

        UpdateState(new StateChange(StateChangeType.BufferFlipped,
            NewBufferSize: remainingLength, PositionDelta: positionDelta));
        return true;
    }

    private enum StateChangeType
    {
        BufferFlipped,
        NewDataRead,
        LineConsumed,
    }

    private record StateChange(
        StateChangeType Type,
        int? NewBufferSize = null,
        bool? IsEof = null,
        int? NewPosition = null,
        int? PositionDelta = null);

    /// <summary>
    /// Central state coordinator — the ONLY method that modifies
    /// _position, _bytesInBuffer, _nextNewlineIndex, _isEof.
    /// </summary>
    private void UpdateState(StateChange change)
    {
        switch (change.Type)
        {
            case StateChangeType.BufferFlipped:
                _bufferFlipVersion++;
                if (_nextNewlineIndex != -1 && change.PositionDelta.HasValue)
                    _nextNewlineIndex -= change.PositionDelta.Value;
                _position = 0;
                if (change.NewBufferSize.HasValue)
                    _bytesInBuffer = change.NewBufferSize.Value;
                break;

            case StateChangeType.NewDataRead:
                if (change.NewBufferSize.HasValue)
                    _bytesInBuffer = change.NewBufferSize.Value;
                if (change.IsEof.HasValue)
                    _isEof = change.IsEof.Value;
                break;

            case StateChangeType.LineConsumed:
                if (change.NewPosition.HasValue)
                    _position = change.NewPosition.Value;
                break;
        }

        RecalculateNextNewline();
    }

    private void RecalculateNextNewline()
    {
        if (_position >= _bytesInBuffer)
        {
            _nextNewlineIndex = -1;
            return;
        }

        var remaining = _activeBuffer.AsSpan(_position, _bytesInBuffer - _position);
        var relativeIndex = remaining.IndexOfAny(NewlineSearchValues);

        if (relativeIndex != -1)
            _nextNewlineIndex = _position + relativeIndex;
        else if (_isEof)
            _nextNewlineIndex = _bytesInBuffer;
        else
            _nextNewlineIndex = -1;
    }
}
