using System.Buffers;
using System.Text;

namespace MarkdownTable.Formatting;

/// <summary>
/// A parsed field document backed by a UTF-8 byte buffer. Field values are
/// stored as byte ranges into the original buffer and decoded on access,
/// avoiding intermediate string allocations during parsing.
/// Analogous to <see cref="System.Text.Json.JsonDocument"/> for JSON.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// byte[] bytes = File.ReadAllBytes(path);
/// using var doc = FieldDocument.Parse(bytes);
/// string? name = doc.GetString("packageName");
/// int count = doc.GetInt32("assemblyCount");
/// string[]? tfms = doc.GetArray("targetFrameworks");
/// </code>
/// </remarks>
public sealed class FieldDocument : IDisposable
{
    private static readonly SearchValues<byte> NewlineSearch = SearchValues.Create([(byte)'\n']);

    private readonly byte[] _buffer;
    private readonly Dictionary<string, FieldEntry> _fields;

    private FieldDocument(byte[] buffer, Dictionary<string, FieldEntry> fields)
    {
        _buffer = buffer;
        _fields = fields;
    }

    /// <summary>
    /// Parses a UTF-8 byte buffer into a <see cref="FieldDocument"/>.
    /// Only scans for field boundaries; does not decode values.
    /// </summary>
    public static FieldDocument Parse(byte[] utf8)
    {
        var fields = new Dictionary<string, FieldEntry>(StringComparer.OrdinalIgnoreCase);
        var span = utf8.AsSpan();

        // Skip BOM
        int pos = 0;
        if (span.Length >= 3 && span[0] == 0xEF && span[1] == 0xBB && span[2] == 0xBF)
            pos = 3;

        int lineStart = pos;

        while (pos <= span.Length)
        {
            // Find end of line
            int lineEnd;
            int nextLineStart;
            var remaining = span[pos..];
            var nlIdx = remaining.IndexOfAny(NewlineSearch);
            if (nlIdx >= 0)
            {
                lineEnd = pos + nlIdx;
                nextLineStart = lineEnd + 1;
            }
            else
            {
                lineEnd = span.Length;
                nextLineStart = span.Length + 1; // terminate loop
            }

            // Trim \r
            int trimmedEnd = lineEnd;
            if (trimmedEnd > lineStart && span[trimmedEnd - 1] == (byte)'\r')
                trimmedEnd--;

            var line = span[lineStart..trimmedEnd];
            var trimmedLine = TrimWhitespace(line, lineStart, out int contentStart);

            if (trimmedLine.Length > 0)
            {
                // Bold field: **key:** value
                if (trimmedLine[0] == (byte)'*' && trimmedLine.Length >= 5 && trimmedLine[1] == (byte)'*')
                {
                    if (TryParseBoldField(trimmedLine, contentStart, out var key, out int valueStart, out int valueEnd))
                    {
                        AddField(fields, key, valueStart, valueEnd, span, ref nextLineStart);
                    }
                }
                // Bullet item at top level — skip (handled by array lookahead)
                // Heading, table, code fence, blockquote — skip
                else if (trimmedLine[0] != (byte)'-' && trimmedLine[0] != (byte)'#'
                    && trimmedLine[0] != (byte)'|' && trimmedLine[0] != (byte)'>'
                    && trimmedLine[0] != (byte)'`')
                {
                    // Plain field: key: value (colon + space required)
                    if (TryParsePlainField(trimmedLine, contentStart, out var key, out int valueStart, out int valueEnd))
                    {
                        AddField(fields, key, valueStart, valueEnd, span, ref nextLineStart);
                    }
                }
            }

            lineStart = nextLineStart;
            pos = nextLineStart;
        }

        return new FieldDocument(utf8, fields);
    }

    /// <summary>Gets a string value, or null if the key is not found.</summary>
    public string? GetString(string key)
    {
        if (!_fields.TryGetValue(key, out var entry))
            return null;
        if (entry.IsArray)
            return string.Join(", ", entry.Items!);
        if (entry.Length == 0)
            return "";
        return Encoding.UTF8.GetString(_buffer.AsSpan(entry.Offset, entry.Length));
    }

    /// <summary>Gets a boolean value (true if field exists and equals "true").</summary>
    public bool GetBool(string key)
    {
        if (!_fields.TryGetValue(key, out var entry) || entry.IsArray)
            return false;
        if (entry.Length != 4)
            return false;
        var span = _buffer.AsSpan(entry.Offset, 4);
        return (span[0] | 0x20) == (byte)'t'
            && (span[1] | 0x20) == (byte)'r'
            && (span[2] | 0x20) == (byte)'u'
            && (span[3] | 0x20) == (byte)'e';
    }

    /// <summary>Gets an integer value, or 0 if not found or not parseable.</summary>
    public int GetInt32(string key)
    {
        if (!_fields.TryGetValue(key, out var entry) || entry.IsArray || entry.Length == 0)
            return 0;
        return Utf8Parser.TryParseInt32(_buffer.AsSpan(entry.Offset, entry.Length), out var value) ? value : 0;
    }

    /// <summary>Gets an array value, or null if the key is not found or is scalar.</summary>
    public string[]? GetArray(string key)
    {
        if (!_fields.TryGetValue(key, out var entry))
            return null;
        if (entry.IsArray)
            return entry.Items!.ToArray();
        return null;
    }

    /// <summary>Gets a <see cref="List{T}"/> of array values, or null.</summary>
    public List<string>? GetArrayList(string key)
    {
        if (!_fields.TryGetValue(key, out var entry))
            return null;
        if (entry.IsArray)
            return [.. entry.Items!];
        return null;
    }

    /// <summary>Returns true if the document contains the specified key.</summary>
    public bool ContainsKey(string key) => _fields.ContainsKey(key);

    /// <summary>Gets the keys of all fields in the document.</summary>
    public IEnumerable<string> Keys => _fields.Keys;

    public void Dispose() { /* buffer is caller-owned */ }

    // --- Static targeted lookup (early exit, zero dictionary) ---

    /// <summary>
    /// Scans a UTF-8 byte buffer for a single field by key and returns its string value.
    /// Stops scanning as soon as the key is found. No dictionary is built.
    /// For best performance, place frequently-accessed fields first in the file.
    /// </summary>
    public static string? GetString(ReadOnlySpan<byte> utf8, ReadOnlySpan<char> key)
    {
        Span<byte> keyBytes = stackalloc byte[key.Length * 3];
        int keyLen = Encoding.UTF8.GetBytes(key, keyBytes);
        var keyUtf8 = keyBytes[..keyLen];

        return ScanForField(utf8, keyUtf8, out var valueStart, out var valueEnd, out var items)
            ? (items is not null ? string.Join(", ", items) : (valueStart < valueEnd ? Encoding.UTF8.GetString(utf8[valueStart..valueEnd]) : ""))
            : null;
    }

    /// <summary>
    /// Scans for a single boolean field. Returns true if the field exists and equals "true".
    /// </summary>
    public static bool GetBool(ReadOnlySpan<byte> utf8, ReadOnlySpan<char> key)
    {
        Span<byte> keyBytes = stackalloc byte[key.Length * 3];
        int keyLen = Encoding.UTF8.GetBytes(key, keyBytes);
        var keyUtf8 = keyBytes[..keyLen];

        if (!ScanForField(utf8, keyUtf8, out var valueStart, out var valueEnd, out _))
            return false;
        int len = valueEnd - valueStart;
        if (len != 4) return false;
        return (utf8[valueStart] | 0x20) == (byte)'t'
            && (utf8[valueStart + 1] | 0x20) == (byte)'r'
            && (utf8[valueStart + 2] | 0x20) == (byte)'u'
            && (utf8[valueStart + 3] | 0x20) == (byte)'e';
    }

    /// <summary>
    /// Scans for a single integer field. Returns the parsed value, or 0 if not found.
    /// </summary>
    public static int GetInt32(ReadOnlySpan<byte> utf8, ReadOnlySpan<char> key)
    {
        Span<byte> keyBytes = stackalloc byte[key.Length * 3];
        int keyLen = Encoding.UTF8.GetBytes(key, keyBytes);
        var keyUtf8 = keyBytes[..keyLen];

        if (!ScanForField(utf8, keyUtf8, out var valueStart, out var valueEnd, out _))
            return 0;
        return Utf8Parser.TryParseInt32(utf8[valueStart..valueEnd], out var value) ? value : 0;
    }

    /// <summary>
    /// Scans for a single array field. Returns the items, or null if not found.
    /// </summary>
    public static string[]? GetArray(ReadOnlySpan<byte> utf8, ReadOnlySpan<char> key)
    {
        Span<byte> keyBytes = stackalloc byte[key.Length * 3];
        int keyLen = Encoding.UTF8.GetBytes(key, keyBytes);
        var keyUtf8 = keyBytes[..keyLen];

        if (!ScanForField(utf8, keyUtf8, out _, out _, out var items))
            return null;
        return items?.ToArray();
    }

    /// <summary>
    /// Returns true if the buffer contains a field with the specified key.
    /// </summary>
    public static bool ContainsKey(ReadOnlySpan<byte> utf8, ReadOnlySpan<char> key)
    {
        Span<byte> keyBytes = stackalloc byte[key.Length * 3];
        int keyLen = Encoding.UTF8.GetBytes(key, keyBytes);
        return ScanForField(utf8, keyBytes[..keyLen], out _, out _, out _);
    }

    /// <summary>
    /// Core scanner: walks lines looking for a field whose key matches keyUtf8 (case-insensitive).
    /// Returns true if found, with value range or array items.
    /// </summary>
    private static bool ScanForField(
        ReadOnlySpan<byte> utf8,
        ReadOnlySpan<byte> keyUtf8,
        out int valueStart, out int valueEnd,
        out List<string>? items)
    {
        valueStart = valueEnd = 0;
        items = null;

        int pos = 0;
        if (utf8.Length >= 3 && utf8[0] == 0xEF && utf8[1] == 0xBB && utf8[2] == 0xBF)
            pos = 3;

        while (pos < utf8.Length)
        {
            int lineEnd = FindLineEnd(utf8, pos);
            int trimmedEnd = lineEnd;
            if (trimmedEnd > pos && utf8[trimmedEnd - 1] == (byte)'\r')
                trimmedEnd--;

            var line = TrimWhitespace(utf8[pos..trimmedEnd], pos, out int contentStart);
            int nextLineStart = lineEnd < utf8.Length ? lineEnd + 1 : utf8.Length;

            if (line.Length > 0)
            {
                ReadOnlySpan<byte> foundKey;
                int vs, ve;

                if (line[0] == (byte)'*' && line.Length >= 5 && line[1] == (byte)'*')
                {
                    if (TryParseBoldFieldSpan(line, contentStart, out foundKey, out vs, out ve)
                        && KeyEquals(foundKey, keyUtf8))
                    {
                        return ResolveValue(utf8, vs, ve, ref nextLineStart, out valueStart, out valueEnd, out items);
                    }
                }
                else if (line[0] != (byte)'-' && line[0] != (byte)'#'
                    && line[0] != (byte)'|' && line[0] != (byte)'>'
                    && line[0] != (byte)'`')
                {
                    if (TryParsePlainFieldSpan(line, contentStart, out foundKey, out vs, out ve)
                        && KeyEquals(foundKey, keyUtf8))
                    {
                        return ResolveValue(utf8, vs, ve, ref nextLineStart, out valueStart, out valueEnd, out items);
                    }
                }
            }

            pos = nextLineStart;
        }

        return false;
    }

    private static bool ResolveValue(
        ReadOnlySpan<byte> buffer, int vs, int ve, ref int nextLineStart,
        out int valueStart, out int valueEnd, out List<string>? items)
    {
        items = null;
        if (vs >= ve)
        {
            var bulletItems = CollectBulletItems(buffer, nextLineStart, out nextLineStart);
            if (bulletItems.Count > 0)
            {
                items = bulletItems;
                valueStart = valueEnd = 0;
                return true;
            }
            valueStart = valueEnd = vs;
            return true;
        }
        valueStart = vs;
        valueEnd = ve;
        return true;
    }

    /// <summary>Case-insensitive comparison of two ASCII/UTF-8 key spans.</summary>
    private static bool KeyEquals(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
        {
            if ((a[i] | 0x20) != (b[i] | 0x20))
                return false;
        }
        return true;
    }

    /// <summary>Like TryParseBoldField but returns key as a byte span (no string alloc).</summary>
    private static bool TryParseBoldFieldSpan(
        ReadOnlySpan<byte> line, int lineOffset,
        out ReadOnlySpan<byte> key, out int valueStart, out int valueEnd)
    {
        key = default;
        valueStart = valueEnd = 0;

        var afterStars = line[2..];
        int colonStarIdx = IndexOfPattern(afterStars, (byte)':', (byte)'*', (byte)'*');
        if (colonStarIdx < 0 || colonStarIdx == 0)
            return false;

        key = line.Slice(2, colonStarIdx);
        int afterPattern = 2 + colonStarIdx + 3;
        int absAfterPattern = lineOffset + afterPattern;
        if (afterPattern < line.Length && line[afterPattern] == (byte)' ')
        {
            afterPattern++;
            absAfterPattern++;
        }
        valueStart = absAfterPattern;

        int endIdx = line.Length;
        while (endIdx > afterPattern && (line[endIdx - 1] == (byte)' ' || line[endIdx - 1] == (byte)'\t'))
            endIdx--;
        valueEnd = lineOffset + endIdx;
        return true;
    }

    /// <summary>Like TryParsePlainField but returns key as a byte span (no string alloc).</summary>
    private static bool TryParsePlainFieldSpan(
        ReadOnlySpan<byte> line, int lineOffset,
        out ReadOnlySpan<byte> key, out int valueStart, out int valueEnd)
    {
        key = default;
        valueStart = valueEnd = 0;

        for (int i = 1; i < line.Length; i++)
        {
            if (line[i] == (byte)':')
            {
                var keySpan = line[..i];
                if (keySpan.IndexOf((byte)' ') >= 0)
                    return false;

                if (i == line.Length - 1)
                {
                    key = keySpan;
                    valueStart = valueEnd = lineOffset + line.Length;
                    return true;
                }

                if (line[i + 1] != (byte)' ')
                {
                    if (i + 2 < line.Length && line[i + 1] == (byte)'/' && line[i + 2] == (byte)'/')
                        return false;
                    continue;
                }

                key = keySpan;
                int afterColon = i + 2;
                valueStart = lineOffset + afterColon;

                int endIdx = line.Length;
                while (endIdx > afterColon && (line[endIdx - 1] == (byte)' ' || line[endIdx - 1] == (byte)'\t'))
                    endIdx--;
                valueEnd = lineOffset + endIdx;
                return true;
            }
        }

        return false;
    }

    // --- Parsing helpers ---

    private static void AddField(
        Dictionary<string, FieldEntry> fields, string key,
        int valueStart, int valueEnd,
        ReadOnlySpan<byte> buffer, ref int nextLineStart)
    {
        if (valueStart >= valueEnd)
        {
            // Empty value → look ahead for bullet list
            var items = CollectBulletItems(buffer, nextLineStart, out nextLineStart);
            if (items.Count > 0)
            {
                fields.TryAdd(key, FieldEntry.Array(items));
            }
            else
            {
                fields.TryAdd(key, FieldEntry.Scalar(valueStart, 0));
            }
        }
        else
        {
            fields.TryAdd(key, FieldEntry.Scalar(valueStart, valueEnd - valueStart));
        }
    }

    private static bool TryParseBoldField(
        ReadOnlySpan<byte> line, int lineOffset,
        out string key, out int valueStart, out int valueEnd)
    {
        key = "";
        valueStart = valueEnd = 0;

        // line starts with "**", find ":**"
        var afterStars = line[2..];
        int colonStarIdx = IndexOfPattern(afterStars, (byte)':', (byte)'*', (byte)'*');
        if (colonStarIdx < 0)
            return false;

        // Key is bytes [2..2+colonStarIdx]
        key = Encoding.UTF8.GetString(line.Slice(2, colonStarIdx));
        if (key.Length == 0)
            return false;

        // Value starts after ":**" + optional space
        int afterPattern = 2 + colonStarIdx + 3; // past ":**"
        int absAfterPattern = lineOffset + afterPattern;

        // Skip leading space
        if (afterPattern < line.Length && line[afterPattern] == (byte)' ')
        {
            afterPattern++;
            absAfterPattern++;
        }

        valueStart = absAfterPattern;

        // Trim trailing whitespace from value
        int endIdx = line.Length;
        while (endIdx > afterPattern && (line[endIdx - 1] == (byte)' ' || line[endIdx - 1] == (byte)'\t'))
            endIdx--;

        valueEnd = lineOffset + endIdx;
        return true;
    }

    private static bool TryParsePlainField(
        ReadOnlySpan<byte> line, int lineOffset,
        out string key, out int valueStart, out int valueEnd)
    {
        key = "";
        valueStart = valueEnd = 0;

        // Find first colon
        for (int i = 1; i < line.Length; i++)
        {
            if (line[i] == (byte)':')
            {
                // Key must not contain spaces (distinguishes from prose)
                var keySpan = line[..i];
                if (keySpan.IndexOf((byte)' ') >= 0)
                    return false;

                // Colon at end of line → empty value (array header)
                if (i == line.Length - 1)
                {
                    key = Encoding.UTF8.GetString(keySpan);
                    valueStart = valueEnd = lineOffset + line.Length;
                    return true;
                }

                // Must be followed by space (": ")
                if (line[i + 1] != (byte)' ')
                {
                    // Check for "://" — URL scheme, not a field
                    if (i + 2 < line.Length && line[i + 1] == (byte)'/' && line[i + 2] == (byte)'/')
                        return false;
                    continue;
                }

                key = Encoding.UTF8.GetString(keySpan);
                int afterColon = i + 2; // past ": "
                valueStart = lineOffset + afterColon;

                // Trim trailing whitespace
                int endIdx = line.Length;
                while (endIdx > afterColon && (line[endIdx - 1] == (byte)' ' || line[endIdx - 1] == (byte)'\t'))
                    endIdx--;

                valueEnd = lineOffset + endIdx;
                return true;
            }
        }

        return false;
    }

    private static int IndexOfPattern(ReadOnlySpan<byte> span, byte a, byte b, byte c)
    {
        for (int i = 0; i <= span.Length - 3; i++)
        {
            if (span[i] == a && span[i + 1] == b && span[i + 2] == c)
                return i;
        }
        return -1;
    }

    private static List<string> CollectBulletItems(ReadOnlySpan<byte> buffer, int pos, out int nextPos)
    {
        var items = new List<string>();
        nextPos = pos;

        // Skip one optional blank line
        if (pos < buffer.Length)
        {
            int blankEnd = FindLineEnd(buffer, pos);
            var blankLine = TrimWhitespace(buffer[pos..blankEnd], 0, out _);
            if (blankLine.IsEmpty)
                pos = SkipNewline(buffer, blankEnd);
        }

        while (pos < buffer.Length)
        {
            int lineEnd = FindLineEnd(buffer, pos);
            int trimmedEnd = lineEnd;
            if (trimmedEnd > pos && buffer[trimmedEnd - 1] == (byte)'\r')
                trimmedEnd--;

            var line = TrimWhitespace(buffer[pos..trimmedEnd], 0, out _);

            if (line.Length >= 2 && line[0] == (byte)'-' && line[1] == (byte)' ')
            {
                // Trim "- " prefix and trailing whitespace
                var itemSpan = line[2..];
                while (itemSpan.Length > 0 && (itemSpan[^1] == (byte)' ' || itemSpan[^1] == (byte)'\t'))
                    itemSpan = itemSpan[..^1];
                items.Add(Encoding.UTF8.GetString(itemSpan));
                pos = SkipNewline(buffer, lineEnd);
            }
            else
            {
                break;
            }
        }

        nextPos = pos;
        return items;
    }

    private static int FindLineEnd(ReadOnlySpan<byte> buffer, int start)
    {
        var remaining = buffer[start..];
        var idx = remaining.IndexOfAny(NewlineSearch);
        return idx >= 0 ? start + idx : buffer.Length;
    }

    private static int SkipNewline(ReadOnlySpan<byte> buffer, int pos)
    {
        if (pos < buffer.Length && buffer[pos] == (byte)'\n')
            return pos + 1;
        return pos;
    }

    private static ReadOnlySpan<byte> TrimWhitespace(ReadOnlySpan<byte> span, int baseOffset, out int contentStart)
    {
        int start = 0;
        while (start < span.Length && (span[start] == (byte)' ' || span[start] == (byte)'\t'))
            start++;
        int end = span.Length;
        while (end > start && (span[end - 1] == (byte)' ' || span[end - 1] == (byte)'\t'))
            end--;
        contentStart = baseOffset + start;
        return span[start..end];
    }

    /// <summary>
    /// A field entry: either a byte range (scalar) or a decoded string list (array).
    /// Scalars are stored as offset+length into the backing buffer and decoded on access.
    /// </summary>
    private readonly struct FieldEntry
    {
        public readonly int Offset;
        public readonly int Length;
        public readonly List<string>? Items;

        public bool IsArray => Items is not null;

        private FieldEntry(int offset, int length, List<string>? items)
        {
            Offset = offset;
            Length = length;
            Items = items;
        }

        public static FieldEntry Scalar(int offset, int length) => new(offset, length, null);
        public static FieldEntry Array(List<string> items) => new(0, 0, items);
    }

    /// <summary>
    /// Minimal UTF-8 integer parser to avoid Encoding.UTF8.GetString + int.TryParse.
    /// </summary>
    private static class Utf8Parser
    {
        public static bool TryParseInt32(ReadOnlySpan<byte> source, out int value)
        {
            value = 0;
            if (source.IsEmpty) return false;

            int i = 0;
            bool negative = false;
            if (source[0] == (byte)'-')
            {
                negative = true;
                i = 1;
            }

            if (i >= source.Length) return false;

            for (; i < source.Length; i++)
            {
                byte b = source[i];
                if (b < (byte)'0' || b > (byte)'9')
                    return false;
                value = value * 10 + (b - (byte)'0');
            }

            if (negative) value = -value;
            return true;
        }
    }
}
