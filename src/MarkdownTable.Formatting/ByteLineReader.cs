using System.Buffers;
using System.Text;

namespace MarkdownTable.Formatting;

/// <summary>
/// Byte-level line reader using <see cref="SearchValues{T}"/> for fast newline
/// scanning. Operates on a contiguous byte buffer (no streaming) to avoid
/// string allocations during line iteration.
/// </summary>
/// <remarks>
/// Inspired by MarkdownTable.IO.LineReader but simplified for synchronous,
/// in-memory use. The key optimization is <c>SearchValues&lt;byte&gt;</c> for
/// SIMD-accelerated newline search on the raw UTF-8 bytes, deferring the
/// UTF-8 → UTF-16 decode to per-line <c>Encoding.UTF8.GetString</c> only when
/// the line is actually needed.
/// </remarks>
internal ref struct ByteLineReader
{
    private static readonly SearchValues<byte> NewlineSearch = SearchValues.Create([(byte)'\n']);

    private readonly ReadOnlySpan<byte> _buffer;
    private int _position;

    public ByteLineReader(ReadOnlySpan<byte> buffer)
    {
        _buffer = buffer;
        _position = 0;

        // Skip UTF-8 BOM
        if (_buffer.Length >= 3
            && _buffer[0] == 0xEF
            && _buffer[1] == 0xBB
            && _buffer[2] == 0xBF)
        {
            _position = 3;
        }
    }

    public readonly bool IsComplete => _position >= _buffer.Length;

    /// <summary>
    /// Reads the next line as a UTF-8 span (excluding \r\n).
    /// </summary>
    public bool ReadLine(out ReadOnlySpan<byte> line)
    {
        if (_position >= _buffer.Length)
        {
            line = default;
            return false;
        }

        var remaining = _buffer[_position..];
        var idx = remaining.IndexOfAny(NewlineSearch);

        if (idx == -1)
        {
            // Last line (no trailing newline)
            line = remaining;
            if (line.Length > 0 && line[^1] == (byte)'\r')
                line = line[..^1];
            _position = _buffer.Length;
            return true;
        }

        line = remaining[..idx];
        if (line.Length > 0 && line[^1] == (byte)'\r')
            line = line[..^1];
        _position += idx + 1;
        return true;
    }

    /// <summary>
    /// Decodes a UTF-8 line span to a string.
    /// </summary>
    public static string ToString(ReadOnlySpan<byte> line) =>
        Encoding.UTF8.GetString(line);
}
