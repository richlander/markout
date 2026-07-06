namespace Markout;

/// <summary>
/// Implemented by composite cell shapes that expose a single "count" quantity whose delta carries a
/// caller noun (<see cref="MarkoutCellFormat.DeltaNoun"/>) — e.g. <see cref="Fraction"/> exposes its
/// <c>Count</c> so <c>4/6 → 6/6</c> renders <c>(+2 solved)</c>. Distinct from
/// <see cref="IGoalMagnitude"/> (which drives goal direction): a <see cref="Fraction"/>'s goal
/// magnitude is its <em>ratio</em>, but its delta-noun count is the numerator.
/// </summary>
public interface IDeltaCountable
{
    /// <summary>The count quantity whose before/after delta the noun is rendered on.</summary>
    double DeltaCount { get; }
}
