namespace Markout;

/// <summary>
/// A percentage derived from a part and a whole, rendered as <c>93%</c>. Decomposes into
/// a <c>pct</c> column (prefixed with the comparison side when nested). A zero whole renders
/// the placeholder.
/// </summary>
/// <param name="Part">The part.</param>
/// <param name="Whole">The whole. A zero renders the placeholder.</param>
public readonly record struct Percent(double Part, double Whole) : IMarkoutCell
{
    /// <inheritdoc/>
    public void FormatInline(TextWriter writer, in MarkoutCellFormat format)
        => writer.Write(Whole == 0 ? CellText.Placeholder : CellText.Percent(Part / Whole * 100));

    /// <inheritdoc/>
    public void Decompose(ICollection<MarkoutField> fields, string? side, in MarkoutCellFormat format)
        => fields.Add(new MarkoutField(
            CellText.SideKey(side, "pct"),
            Whole == 0 ? CellText.Placeholder : CellText.PercentNumber(Part / Whole * 100)));
}
