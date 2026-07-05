namespace Markout;

/// <summary>
/// Appends a derived change to a numeric <see cref="Change{V}"/> cell:
/// <see cref="Delta.Percent"/> appends the signed percent change
/// (<c>(After − Before) / Before × 100</c>), and <see cref="Delta.Absolute"/> appends the
/// signed difference. A zero <c>Before</c> renders a placeholder instead of infinity.
/// </summary>
/// <param name="mode">The derived-change mode.</param>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MarkoutDeltaAttribute(Delta mode) : Attribute
{
    /// <summary>The derived-change mode to append.</summary>
    public Delta Mode { get; } = mode;
}
