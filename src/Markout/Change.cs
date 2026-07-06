namespace Markout;

/// <summary>
/// A <c>before → after</c> change. When <typeparamref name="V"/> is a composite shape
/// (<see cref="Fraction"/>, <see cref="Share"/>, <see cref="Percent"/>, <see cref="Segments"/>)
/// the halves render and decompose recursively. When <typeparamref name="V"/> is numeric,
/// <see cref="MarkoutDeltaAttribute"/> appends a derived change, e.g. <c>98555 → 61190 (−38%)</c>.
/// </summary>
/// <typeparam name="V">The compared value type (numeric scalar or a composite shape).</typeparam>
/// <param name="Before">The value before.</param>
/// <param name="After">The value after.</param>
public readonly record struct Change<V>(V Before, V After) : IMarkoutCell
{
    /// <inheritdoc/>
    public void FormatInline(TextWriter writer, in MarkoutCellFormat format)
    {
        // If either half is a composite shape, render both as shapes (a null half writes nothing)
        // so a nullable composite side never leaks a struct ToString via the scalar path.
        if (Before is IMarkoutCell || After is IMarkoutCell)
        {
            (Before as IMarkoutCell)?.FormatInline(writer, format);
            writer.Write(CellText.Arrow);
            (After as IMarkoutCell)?.FormatInline(writer, format);
            return;
        }

        writer.Write(CellText.Scalar(Before));
        writer.Write(CellText.Arrow);
        writer.Write(CellText.Scalar(After));

        if (format.Delta == Delta.None)
            return;

        writer.Write(" (");
        writer.Write(DeltaSuffix(format.Delta));
        writer.Write(')');
    }

    /// <inheritdoc/>
    public void Decompose(ICollection<MarkoutField> fields, string? side, in MarkoutCellFormat format)
    {
        if (Before is IMarkoutCell || After is IMarkoutCell)
        {
            (Before as IMarkoutCell)?.Decompose(fields, CellText.SideKey(side, "before"), format);
            (After as IMarkoutCell)?.Decompose(fields, CellText.SideKey(side, "after"), format);
            return;
        }

        fields.Add(new MarkoutField(CellText.SideKey(side, "before"), CellText.Scalar(Before)));
        fields.Add(new MarkoutField(CellText.SideKey(side, "after"), CellText.Scalar(After)));

        if (format.Delta == Delta.Percent)
            fields.Add(new MarkoutField(CellText.SideKey(side, "deltaPct"), DeltaValue(Delta.Percent)));
        else if (format.Delta == Delta.Absolute)
            fields.Add(new MarkoutField(CellText.SideKey(side, "deltaAbs"), DeltaValue(Delta.Absolute)));

        if (format.Goal != Goal.Context &&
            GoalDerivation.TryDerive(Before, After, format.Goal, format.Noise, out var direction, out var status))
        {
            fields.Add(new MarkoutField(CellText.SideKey(side, "direction"), DirectionText.Slug(direction)));
            fields.Add(new MarkoutField(CellText.SideKey(side, "status"), GateStatusText.Slug(status)));
        }
    }

    private string DeltaSuffix(Delta mode)
    {
        if (!CellText.TryScalarDouble(Before, out var before) || !CellText.TryScalarDouble(After, out var after))
            return CellText.Placeholder;
        return mode switch
        {
            // Divide by |before| so a rise from a negative base reports as a gain, not a loss.
            Delta.Percent => before == 0 ? CellText.Placeholder : CellText.SignedPercent((after - before) / Math.Abs(before) * 100),
            Delta.Absolute => CellText.AbsoluteDelta(Before, After, signed: true),
            _ => CellText.Placeholder
        };
    }

    private string DeltaValue(Delta mode)
    {
        if (!CellText.TryScalarDouble(Before, out var before) || !CellText.TryScalarDouble(After, out var after))
            return CellText.Placeholder;
        return mode switch
        {
            Delta.Percent => before == 0 ? CellText.Placeholder : CellText.PercentNumber((after - before) / Math.Abs(before) * 100),
            Delta.Absolute => CellText.AbsoluteDelta(Before, After, signed: false),
            _ => CellText.Placeholder
        };
    }
}
