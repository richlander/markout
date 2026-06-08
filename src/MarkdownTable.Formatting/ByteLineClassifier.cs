using System.Buffers;

namespace MarkdownTable.Formatting;

/// <summary>
/// Classification of a markdown line determined at the byte level.
/// </summary>
internal enum ByteLineKind
{
    /// <summary>Blank or whitespace-only line.</summary>
    Empty,

    /// <summary>Heading line starting with # followed by space.</summary>
    Heading,

    /// <summary>Pipe table line (starts or ends with |).</summary>
    PipeTable,

    /// <summary>Bold field: **Key:** Value</summary>
    BoldField,

    /// <summary>Bullet list item: - text</summary>
    Bullet,

    /// <summary>OneLine fields: contains " | " but doesn't start/end with |</summary>
    OneLineFields,

    /// <summary>Code fence (```) or block quote (&gt;).</summary>
    Skippable,

    /// <summary>Any other content line (possible plain field or text).</summary>
    Content,
}

/// <summary>
/// SIMD-accelerated byte-level line classifier for Markout document structure.
/// Classifies lines without UTF-8 → UTF-16 string conversion by inspecting
/// raw byte patterns using <see cref="SearchValues{T}"/>.
/// </summary>
/// <remarks>
/// Inspired by MarkdownTable.IO.MarkdownLineClassifier but extended to cover
/// headings, fields (bold, inline), and bullet lists in addition to tables.
/// </remarks>
internal static class ByteLineClassifier
{
    private static readonly SearchValues<byte> Whitespace = SearchValues.Create([(byte)' ', (byte)'\t', (byte)'\r']);
    private static readonly SearchValues<byte> PipeSearch = SearchValues.Create([(byte)'|']);

    // " | " as a 3-byte pattern for inline field separators
    private static readonly byte[] PipeSeparator = [(byte)' ', (byte)'|', (byte)' '];

    // "**" prefix for bold fields
    private const byte Asterisk = (byte)'*';
    private const byte Hash = (byte)'#';
    private const byte Backtick = (byte)'`';
    private const byte GreaterThan = (byte)'>';
    private const byte Dash = (byte)'-';
    private const byte Pipe = (byte)'|';
    private const byte Space = (byte)' ';

    /// <summary>
    /// Classifies a line from its raw UTF-8 bytes.
    /// </summary>
    public static ByteLineKind Classify(ReadOnlySpan<byte> line)
    {
        // Trim leading whitespace
        var trimmed = TrimStart(line);

        if (trimmed.IsEmpty || trimmed.IndexOfAnyExcept(Whitespace) < 0)
            return ByteLineKind.Empty;

        byte first = trimmed[0];

        // Heading: # ...
        if (first == Hash)
        {
            // Validate: 1-6 hashes followed by space
            int hashes = 0;
            while (hashes < trimmed.Length && trimmed[hashes] == Hash) hashes++;
            if (hashes <= 6 && hashes < trimmed.Length && trimmed[hashes] == Space)
                return ByteLineKind.Heading;
        }

        // Code fence or block quote
        if (first == Backtick || first == GreaterThan)
            return ByteLineKind.Skippable;

        // Bold field: **Key:** Value
        if (first == Asterisk && trimmed.Length >= 5 && trimmed[1] == Asterisk)
            return ByteLineKind.BoldField;

        // Bullet list item: - text (with space after dash)
        if (first == Dash && trimmed.Length >= 2 && trimmed[1] == Space)
            return ByteLineKind.Bullet;

        // Lines with pipes — distinguish table vs inline fields
        if (trimmed.IndexOfAny(PipeSearch) >= 0)
        {
            // Table lines start or end with |
            if (first == Pipe || trimmed[^1] == Pipe)
                return ByteLineKind.PipeTable;

            // OneLine fields: contains " | " separator but not a table
            if (ContainsPipeSeparator(trimmed))
                return ByteLineKind.OneLineFields;
        }

        return ByteLineKind.Content;
    }

    private static ReadOnlySpan<byte> TrimStart(ReadOnlySpan<byte> span)
    {
        int i = 0;
        while (i < span.Length && (span[i] == Space || span[i] == (byte)'\t'))
            i++;
        return span[i..];
    }

    private static bool ContainsPipeSeparator(ReadOnlySpan<byte> span)
    {
        // Look for " | " (3-byte sequence)
        return span.IndexOf(PipeSeparator) >= 0;
    }
}
