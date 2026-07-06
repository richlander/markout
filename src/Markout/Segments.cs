namespace Markout;

/// <summary>
/// A single labeled part of a <see cref="Segments"/> value. Labels drive the field names
/// in structured (decomposed) output.
/// </summary>
/// <param name="Label">The part label (e.g. <c>"web"</c>, <c>"bash"</c>).</param>
/// <param name="Value">The part value.</param>
public readonly record struct Segment(string Label, double Value);

/// <summary>
/// A set of independent labeled parts with no shared denominator, rendered slash-joined as
/// <c>21/171/236</c>. Each label becomes a decomposed field (as <c>{label}</c>, or
/// <c>{side}_{label}</c> when nested in a <see cref="Change{V}"/>).
/// </summary>
/// <param name="Parts">The labeled parts, rendered left-to-right in order.</param>
public readonly record struct Segments(params Segment[] Parts) : IMarkoutCell, IGoalMagnitude
{
    /// <summary>
    /// The aggregate magnitude for goal derivation is the sum of the parts' values (the breakdown's
    /// total). Opt-in via <see cref="MarkoutCellFormat.Goal"/> — <see cref="Goal.Context"/> (the default)
    /// derives nothing, so a purely compositional breakdown declines simply by not setting a goal. A
    /// constant-sum (proportion) breakdown reads as <see cref="Direction.Unchanged"/>.
    /// </summary>
    double IGoalMagnitude.GoalMagnitude
    {
        get
        {
            if (Parts is null)
                return 0;
            double sum = 0;
            foreach (var part in Parts)
                sum += part.Value;
            return sum;
        }
    }

    /// <inheritdoc/>
    public void FormatInline(TextWriter writer, in MarkoutCellFormat format)
    {
        if (Parts is null || Parts.Length == 0)
            return;
        for (int i = 0; i < Parts.Length; i++)
        {
            if (i > 0)
                writer.Write('/');
            writer.Write(CellText.Number(Parts[i].Value));
        }
    }

    /// <inheritdoc/>
    public void Decompose(ICollection<MarkoutField> fields, string? side, in MarkoutCellFormat format)
    {
        if (Parts is null)
            return;
        foreach (var part in Parts)
            fields.Add(new MarkoutField(CellText.SideKey(side, part.Label), CellText.Number(part.Value)));
    }
}
