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
public readonly record struct Source(string Role, IMarkoutCell? Value, MarkoutCellFormat Format = default);
