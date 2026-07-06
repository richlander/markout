namespace Markout;

/// <summary>
/// A first-class gate/verdict cell: a typed <see cref="GateStatus"/> polarity plus an optional
/// caller display label (e.g. <c>"regression"</c>, <c>"BETTER"</c>). The label — or, when absent,
/// the polarity slug — renders densely; structured output decomposes to a <c>status</c> field.
/// Lets a card carry a verdict as typed data instead of a caller-formatted string.
/// </summary>
/// <param name="Status">The outcome polarity (drives badge/color in capable renderers).</param>
/// <param name="Label">An optional caller display word; overrides the polarity slug.</param>
public readonly record struct Verdict(GateStatus Status, string? Label = null) : IMarkoutCell
{
    /// <inheritdoc/>
    public void FormatInline(TextWriter writer, in MarkoutCellFormat format)
        => writer.Write(Text);

    /// <inheritdoc/>
    public void Decompose(ICollection<MarkoutField> fields, string? side, in MarkoutCellFormat format)
        => fields.Add(new MarkoutField(CellText.SideKey(side, "status"), Text));

    private string Text => string.IsNullOrEmpty(Label) ? GateStatusText.Slug(Status) : Label!;
}
