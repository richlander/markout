namespace Markout;

/// <summary>
/// A count out of a total, rendered as <c>24/24</c>. Decomposes into <c>count</c> and
/// <c>total</c> columns (prefixed with the comparison side when nested).
/// </summary>
/// <param name="Count">The numerator.</param>
/// <param name="Total">The denominator.</param>
public readonly record struct Fraction(double Count, double Total) : IMarkoutCell, IGoalMagnitude, IDeltaCountable
{
    /// <summary>The count-over-total rate drives goal derivation (not the raw count); an undefined rate
    /// (zero total) yields <see cref="double.NaN"/> so no direction is derived.</summary>
    double IGoalMagnitude.GoalMagnitude => Total == 0 ? double.NaN : Count / Total;

    /// <summary>The numerator carries the delta noun (e.g. <c>4/6 → 6/6</c> → <c>(+2 solved)</c>).</summary>
    double IDeltaCountable.DeltaCount => Count;

    /// <inheritdoc/>
    public void FormatInline(TextWriter writer, in MarkoutCellFormat format)
    {
        writer.Write(CellText.Number(Count));
        writer.Write('/');
        writer.Write(CellText.Number(Total));
    }

    /// <inheritdoc/>
    public void Decompose(ICollection<MarkoutField> fields, string? side, in MarkoutCellFormat format)
    {
        fields.Add(new MarkoutField(CellText.SideKey(side, "count"), CellText.Number(Count)));
        fields.Add(new MarkoutField(CellText.SideKey(side, "total"), CellText.Number(Total)));
    }
}
