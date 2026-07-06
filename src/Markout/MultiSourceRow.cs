namespace Markout;

/// <summary>
/// One row of a multi-source card: a label plus named-role cells (<see cref="Source"/>). A
/// collection of these renders as a <em>pivoted</em> wide table — one column per role, each cell
/// the dense render of that role's value — in document formatters, and decomposes to one flat
/// <c>{role}_{field}</c> record per row in structured formatters (TSV/JSONL).
/// </summary>
/// <remarks>
/// Rows may be <b>heterogeneous</b>: different rows can carry different roles and different cell
/// types (e.g. metric rows hold <see cref="Change{V}"/> cells while a verdict row holds
/// <see cref="Verdict"/> cells). Column order is the caller-supplied role order (first appearance
/// across the row collection); a role absent from a given row renders as <c>-</c>.
/// </remarks>
public readonly struct MultiSourceRow
{
    /// <summary>Creates a row from a label and its named-role cells.</summary>
    /// <param name="label">The row label (the leading identity column / cell).</param>
    /// <param name="sources">The named-role cells, in caller-supplied column order.</param>
    public MultiSourceRow(string label, params Source[] sources)
    {
        Label = label;
        Sources = sources ?? [];
    }

    /// <summary>The row label (leading identity column).</summary>
    public string Label { get; }

    /// <summary>The named-role cells for this row, in caller-supplied order.</summary>
    public IReadOnlyList<Source> Sources { get; }
}
