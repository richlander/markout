namespace Markout;

/// <summary>
/// One named source within a <see cref="MultiSourceRow"/>. The <paramref name="Role"/> names the
/// column in the pivoted Markdown table and the decomposition prefix in structured output; the
/// <paramref name="Value"/> is any composite cell (a scalar cell, a nested <see cref="Change{V}"/>,
/// a <see cref="Verdict"/>, etc.). A <c>null</c> value renders as an empty/absent cell.
/// </summary>
/// <param name="Role">The source role (e.g. a model name, or <c>baseline</c>/<c>current</c>).</param>
/// <param name="Value">The cell for this role.</param>
/// <param name="Format">Render-time derivation/format options for this cell.</param>
public readonly record struct Source(string Role, IMarkoutCell? Value, MarkoutCellFormat Format = default)
{
    /// <summary>Creates a source with an integral scalar value (decomposes to a single <c>{role}</c> field).</summary>
    public Source(string role, long value, MarkoutCellFormat format = default)
        : this(role, new ScalarSourceCell(value), format) { }

    /// <summary>Creates a source with a floating-point scalar value (decomposes to a single <c>{role}</c> field).</summary>
    public Source(string role, double value, MarkoutCellFormat format = default)
        : this(role, new ScalarSourceCell(value), format) { }

    /// <summary>Creates a source with a decimal scalar value (decomposes to a single <c>{role}</c> field).</summary>
    public Source(string role, decimal value, MarkoutCellFormat format = default)
        : this(role, new ScalarSourceCell(value), format) { }

    /// <summary>
    /// Creates a source with a text scalar value (decomposes to a single <c>{role}</c> field). A static
    /// factory rather than a constructor so a <c>string</c> overload does not make <c>new Source(role, null)</c>
    /// ambiguous with the primary <see cref="IMarkoutCell"/> constructor.
    /// </summary>
    public static Source Text(string role, string value, MarkoutCellFormat format = default)
        => new(role, new ScalarSourceCell(value), format);
}

/// <summary>
/// Wraps a plain scalar as an <see cref="IMarkoutCell"/> for use as a <see cref="Source"/> value:
/// renders verbatim and decomposes to a single field keyed by the enclosing role (<c>{role}</c>),
/// so a card can mix scalar role cells with composite ones.
/// </summary>
internal sealed class ScalarSourceCell(object? value) : IMarkoutCell
{
    public void FormatInline(TextWriter writer, in MarkoutCellFormat format)
        => writer.Write(CellText.Scalar(value));

    public void Decompose(ICollection<MarkoutField> fields, string? side, in MarkoutCellFormat format)
        => fields.Add(new MarkoutField(side ?? "value", CellText.Scalar(value)));
}
