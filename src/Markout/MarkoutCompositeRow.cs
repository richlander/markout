namespace Markout;

/// <summary>
/// One row of a composite table: a labeled cell rendered densely in Markdown and
/// decomposed into typed columns/fields in structured formats. Emitted by the source
/// generator for <c>[MarkoutSerializable]</c> models whose properties are composite shapes.
/// </summary>
public readonly struct MarkoutCompositeRow
{
    /// <summary>Creates a row from a composite cell and its render format.</summary>
    public MarkoutCompositeRow(string label, IMarkoutCell cell, MarkoutCellFormat format = default)
    {
        Label = label;
        Cell = cell;
        Format = format;
    }

    /// <summary>The row label (the property display name).</summary>
    public string Label { get; }

    /// <summary>The composite cell for this row.</summary>
    public IMarkoutCell Cell { get; }

    /// <summary>Render-time derivation/format options for this row.</summary>
    public MarkoutCellFormat Format { get; }

    /// <summary>
    /// Creates a row for a plain scalar value, rendered verbatim and decomposed to a single
    /// <c>value</c> field. Lets scalar properties share one table with composite rows.
    /// </summary>
    public static MarkoutCompositeRow Scalar(string label, string? text)
        => new(label, new ScalarCell(text));
}

/// <summary>
/// Wraps a pre-rendered scalar string as an <see cref="IMarkoutCell"/> so plain properties
/// can appear alongside composite cells in the same table.
/// </summary>
internal sealed class ScalarCell(string? text) : IMarkoutCell
{
    private readonly string _text = text ?? string.Empty;

    public void FormatInline(TextWriter writer, in MarkoutCellFormat format)
        => writer.Write(_text);

    public void Decompose(ICollection<MarkoutField> fields, string? side, in MarkoutCellFormat format)
        => fields.Add(new MarkoutField(CellText.SideKey(side, "value"), _text));
}
