namespace Markout.Formatting;

/// <summary>
/// Capability interface for formatters that decompose composite cells into typed columns
/// instead of rendering them densely. Structured formatters (e.g. <see cref="TableFormatter"/>
/// in TSV/JSONL modes) opt in; document formatters render the dense form.
/// </summary>
public interface ICompositeCellFormatter
{
    /// <summary>
    /// When <c>true</c>, <see cref="MarkoutWriter.WriteCompositeTable"/> decomposes each cell
    /// into typed columns; otherwise it renders a dense <c>Field | Value</c> table.
    /// </summary>
    bool DecomposesCompositeCells { get; }
}
