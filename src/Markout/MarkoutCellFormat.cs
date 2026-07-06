namespace Markout;

/// <summary>
/// Render-time configuration for a composite cell, sourced from property attributes
/// (<see cref="MarkoutDeltaAttribute"/>, <see cref="MarkoutUnitAttribute"/>).
/// Shapes are data-only; derivation/formatting options travel through this struct.
/// </summary>
/// <param name="Delta">The derived change mode for a numeric <see cref="Change{V}"/>.</param>
/// <param name="Unit">An optional unit suffix (e.g. <c>"s"</c>) for a <see cref="Share"/> value.</param>
/// <param name="Goal">The optimization goal; when not <see cref="Markout.Goal.Context"/>, a numeric
/// <see cref="Change{V}"/> derives a structural <c>direction</c> and a polarity <c>status</c>.</param>
/// <param name="Noise">The tolerance (inclusive) under which a change counts as
/// <see cref="Direction.Unchanged"/>; defaults to <c>0</c> (exact).</param>
public readonly record struct MarkoutCellFormat(
    Delta Delta = Delta.None,
    string? Unit = null,
    Goal Goal = Goal.Context,
    double Noise = 0);
