namespace Markout.Formatting;

/// <summary>
/// Capability interface for rendering tables (batch and streaming).
/// The base class handles section filtering, column projection, MaxItems, and spacing;
/// implementations only render the table content in their format.
/// </summary>
public interface ITableFormatter
{
    /// <summary>
    /// Renders a complete table with headers and rows.
    /// </summary>
    /// <param name="writer">The output writer.</param>
    /// <param name="headers">Column headers.</param>
    /// <param name="rows">Visible data rows (already truncated by MaxItems).</param>
    /// <param name="skippedRows">Number of rows omitted by MaxItems (0 if none).</param>
    /// <param name="options">Writer options for format-specific settings (e.g., PrettyTables).</param>
    void FormatTable(TextWriter writer, string[] headers, IList<string[]> rows, int skippedRows, MarkoutWriterOptions options);
}
