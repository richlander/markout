namespace Markout.Formatting;

/// <summary>
/// Capability interface for streaming tabular data using Begin/Data/End pattern.
/// Each row is rendered immediately without buffering all rows first.
/// </summary>
public interface IStreamingTableFormatter
{
    /// <summary>
    /// Called once when the table begins. The formatter sees column count and header widths.
    /// </summary>
    /// <param name="writer">The output writer.</param>
    /// <param name="headers">Column headers.</param>
    /// <param name="options">Writer options for format-specific settings.</param>
    void BeginTable(TextWriter writer, string[] headers, MarkoutWriterOptions options);

    /// <summary>
    /// Called for each data row. The formatter decides padding; the row is written immediately.
    /// </summary>
    /// <param name="writer">The output writer.</param>
    /// <param name="values">Cell values for this row.</param>
    void WriteRow(TextWriter writer, string[] values);

    /// <summary>
    /// Called when the table ends. The formatter performs cleanup (trailing rules, skip counts, etc.).
    /// </summary>
    /// <param name="writer">The output writer.</param>
    /// <param name="skippedRows">Number of rows omitted by MaxItems (0 if none).</param>
    void EndTable(TextWriter writer, int skippedRows);
}
