namespace Markout;

/// <summary>
/// A composite table cell that can render as a dense, human-readable string and
/// decompose into typed columns/fields for structured output (TSV/JSONL/JSON).
/// Implemented by the data-only shape types (<see cref="Change{V}"/>,
/// <see cref="Fraction"/>, <see cref="Share"/>, <see cref="Percent"/>,
/// <see cref="Segments"/>). The concrete type picks the rendering; formatting and
/// derivation config arrive via <see cref="MarkoutCellFormat"/>.
/// </summary>
public interface IMarkoutCell
{
    /// <summary>
    /// Writes the dense, human-readable form of the cell (e.g. <c>98555 → 61190 (−38%)</c>).
    /// </summary>
    /// <param name="writer">The output writer.</param>
    /// <param name="format">Render-time derivation/format options.</param>
    void FormatInline(TextWriter writer, in MarkoutCellFormat format);

    /// <summary>
    /// Adds the decomposed, typed fields for this cell to <paramref name="fields"/>.
    /// </summary>
    /// <param name="fields">The collection to append decomposed fields to.</param>
    /// <param name="side">
    /// When non-null, a nesting side (<c>"before"</c>/<c>"after"</c>) supplied by an
    /// enclosing <see cref="Change{V}"/>. Each shape decides how to combine the side
    /// with its own field names.
    /// </param>
    /// <param name="format">Render-time derivation/format options.</param>
    void Decompose(ICollection<MarkoutField> fields, string? side, in MarkoutCellFormat format);
}
