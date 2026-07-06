namespace Markout;

/// <summary>
/// A value together with its share of a (hidden) whole, rendered as <c>5056 (24%)</c>.
/// With <see cref="MarkoutUnitAttribute"/> the value carries a unit suffix, e.g. <c>103s (93%)</c>.
/// Decomposes into <c>value</c> and <c>pct</c> columns (prefixed with the comparison side when nested).
/// </summary>
/// <param name="Value">The value shown before the derived percent.</param>
/// <param name="Whole">The whole the value is a share of. A zero renders the placeholder.</param>
public readonly record struct Share(double Value, double Whole) : IMarkoutCell, IGoalMagnitude
{
    /// <summary>The raw value drives goal derivation (the share percent is secondary context).</summary>
    double IGoalMagnitude.GoalMagnitude => Value;

    /// <inheritdoc/>
    public void FormatInline(TextWriter writer, in MarkoutCellFormat format)
    {
        writer.Write(CellText.Number(Value));
        if (!string.IsNullOrEmpty(format.Unit))
            writer.Write(format.Unit);
        writer.Write(" (");
        writer.Write(Whole == 0 ? CellText.Placeholder : CellText.Percent(Value / Whole * 100));
        writer.Write(')');
    }

    /// <inheritdoc/>
    public void Decompose(ICollection<MarkoutField> fields, string? side, in MarkoutCellFormat format)
    {
        fields.Add(new MarkoutField(CellText.SideKey(side, "value"), CellText.Number(Value)));
        fields.Add(new MarkoutField(
            CellText.SideKey(side, "pct"),
            Whole == 0 ? CellText.Placeholder : CellText.PercentNumber(Value / Whole * 100)));
    }
}
