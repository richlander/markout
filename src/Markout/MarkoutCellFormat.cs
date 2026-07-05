namespace Markout;

/// <summary>
/// Render-time configuration for a composite cell, sourced from property attributes
/// (<see cref="MarkoutDeltaAttribute"/>, <see cref="MarkoutUnitAttribute"/>).
/// Shapes are data-only; derivation/formatting options travel through this struct.
/// </summary>
/// <param name="Delta">The derived change mode for a numeric <see cref="Change{V}"/>.</param>
/// <param name="Unit">An optional unit suffix (e.g. <c>"s"</c>) for a <see cref="Share"/> value.</param>
public readonly record struct MarkoutCellFormat(Delta Delta = Delta.None, string? Unit = null);
