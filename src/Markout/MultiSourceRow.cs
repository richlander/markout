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

    /// <summary>
    /// The optimization goal for this row's scalar series. When not <see cref="Markout.Goal.Context"/>,
    /// rich sinks append the goal glyph to the row <see cref="Label"/> and a pairwise polarity glyph to
    /// each scalar cell (comparing it to the previous populated scalar column). Set via object
    /// initializer, e.g. <c>new MultiSourceRow("Alloc", w1, w2) { Goal = Goal.Lower }</c>.
    /// </summary>
    public Goal Goal { get; init; } = Goal.Context;

    /// <summary>The tolerance (inclusive) under which a pairwise change is <see cref="Direction.Unchanged"/>; default exact.</summary>
    public double Noise { get; init; }

    /// <summary>
    /// An optional declared rule that emphasizes (bold in Markdown) each scalar cell of this row whose
    /// value clears the threshold — making "which numbers matter" a property of the data rather than
    /// hand-applied bolding. Applies to scalar cells only; ignored on plain text and structured (TSV/JSONL)
    /// sinks. Set via object initializer, e.g.
    /// <c>new MultiSourceRow("Unlocks", mini, mid, frontier) { Emphasis = MarkoutEmphasis.AtLeast(2) }</c>.
    /// </summary>
    public MarkoutEmphasis? Emphasis { get; init; }
}
