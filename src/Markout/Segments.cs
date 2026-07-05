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
/// <c>{label}_{side}</c> when nested in a <see cref="Change{V}"/>).
/// </summary>
/// <param name="Parts">The labeled parts, rendered left-to-right in order.</param>
public readonly record struct Segments(params Segment[] Parts) : IMarkoutCell
{
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
            fields.Add(new MarkoutField(CellText.LabelKey(part.Label, side), CellText.Number(part.Value)));
    }
}
