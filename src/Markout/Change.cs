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
        if (Before is IMarkoutCell beforeCell && After is IMarkoutCell afterCell)
        {
            beforeCell.FormatInline(writer, format);
            writer.Write(CellText.Arrow);
            afterCell.FormatInline(writer, format);
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
        if (Before is IMarkoutCell beforeCell && After is IMarkoutCell afterCell)
        {
            beforeCell.Decompose(fields, "before", format);
            afterCell.Decompose(fields, "after", format);
            return;
        }

        fields.Add(new MarkoutField("before", CellText.Scalar(Before)));
        fields.Add(new MarkoutField("after", CellText.Scalar(After)));

        if (format.Delta == Delta.Percent)
            fields.Add(new MarkoutField("deltaPct", DeltaValue(Delta.Percent)));
        else if (format.Delta == Delta.Absolute)
            fields.Add(new MarkoutField("deltaAbs", DeltaValue(Delta.Absolute)));
    }

    private string DeltaSuffix(Delta mode)
    {
        if (!CellText.TryScalarDouble(Before, out var before) || !CellText.TryScalarDouble(After, out var after))
            return CellText.Placeholder;
        return mode switch
        {
            Delta.Percent => before == 0 ? CellText.Placeholder : CellText.SignedPercent((after - before) / before * 100),
            Delta.Absolute => CellText.SignedNumber(after - before),
            _ => CellText.Placeholder
        };
    }

    private string DeltaValue(Delta mode)
    {
        if (!CellText.TryScalarDouble(Before, out var before) || !CellText.TryScalarDouble(After, out var after))
            return CellText.Placeholder;
        return mode switch
        {
            Delta.Percent => before == 0 ? CellText.Placeholder : CellText.PercentNumber((after - before) / before * 100),
            Delta.Absolute => CellText.Number(after - before),
            _ => CellText.Placeholder
        };
    }
}
