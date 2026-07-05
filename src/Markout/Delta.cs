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
    Absolute
}
