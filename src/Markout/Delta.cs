namespace Markout;

/// <summary>
/// Controls the derived change appended to a numeric <see cref="Change{V}"/>
/// via <see cref="MarkoutDeltaAttribute"/>.
/// </summary>
public enum Delta
{
    /// <summary>No derived change is appended.</summary>
    None,

    /// <summary>Append the signed percent change: <c>(After − Before) / Before × 100</c>.</summary>
    Percent,

    /// <summary>Append the signed absolute difference: <c>After − Before</c>.</summary>
    Absolute,

    /// <summary>
    /// Append the multiplicative factor between the two values with a direction word:
    /// <c>15 → 5 (3× fewer)</c>, <c>5 → 15 (3× more)</c>. The factor is <c>max/min</c> of the
    /// magnitudes; a zero endpoint (no finite multiple) renders the placeholder.
    /// </summary>
    Multiple
}
