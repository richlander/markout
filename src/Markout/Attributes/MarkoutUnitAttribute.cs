namespace Markout;

/// <summary>
/// Renders a <see cref="Share"/> value with a unit suffix, e.g. <c>[MarkoutUnit("s")]</c>
/// turns <c>103 (93%)</c> into <c>103s (93%)</c>. The suffix appears only in the dense form;
/// the decomposed <c>value</c> column stays numeric.
/// </summary>
/// <param name="unit">The unit suffix (e.g. <c>"s"</c>).</param>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MarkoutUnitAttribute(string unit) : Attribute
{
    /// <summary>The unit suffix appended to the value.</summary>
    public string Unit { get; } = unit;
}
