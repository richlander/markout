namespace Markout.Formatting;

/// <summary>
/// Capability interface for streaming tabular data as UTF-8 bytes.
/// Uses a cell-at-a-time pattern: BeginRow → (BeginCell → WriteUtf8* → EndCell)+ → EndRow.
/// Multi-part cells (e.g. markdown links) are built from multiple WriteUtf8 calls
/// within a single BeginCell/EndCell pair — no staging buffer assembly needed.
///
/// Output goes to a <see cref="Stream"/> (not TextWriter) for zero-allocation byte writes.
/// Inspired by smooth-markdown-table's ReadOnlySpan&lt;byte&gt; / Stream architecture.
/// </summary>
public interface IUtf8StreamingTableFormatter
{
    /// <summary>
    /// Called once when the table begins. Writes the header row and separator.
    /// Headers are strings because they're typically compile-time constants (not hot path).
    /// </summary>
    void BeginTable(Stream output, ReadOnlySpan<string> headers, MarkoutWriterOptions options);

    /// <summary>
    /// Begins a data row. Writes the leading pipe.
    /// </summary>
    void BeginRow(Stream output);

    /// <summary>
    /// Begins a table cell. Writes the leading space after the pipe.
    /// </summary>
    void BeginCell(Stream output);

    /// <summary>
    /// Writes raw UTF-8 bytes as cell content. May be called multiple times
    /// between BeginCell and EndCell to build composite content (e.g. markdown links)
    /// without any allocation.
    /// </summary>
    void WriteUtf8(Stream output, ReadOnlySpan<byte> content);

    /// <summary>
    /// Ends a table cell. Writes the trailing space and pipe.
    /// </summary>
    void EndCell(Stream output);

    /// <summary>
    /// Ends a data row. Writes the trailing newline.
    /// </summary>
    void EndRow(Stream output);

    /// <summary>
    /// Called when the table ends.
    /// </summary>
    void EndTable(Stream output, int skippedRows);
}
