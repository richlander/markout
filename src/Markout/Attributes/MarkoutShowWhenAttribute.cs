namespace Markout;

/// <summary>
/// Specifies that this field should only be rendered when a boolean property on the same type is <c>true</c>.
/// </summary>
/// <remarks>
/// Use this attribute to conditionally show individual fields based on the value of another property.
/// For conditional sections, use <see cref="MarkoutSectionAttribute.ShowWhenProperty"/> instead.
/// </remarks>
/// <example>
/// <code>
/// [MarkoutShowWhen(nameof(IsVerified))]
/// public string? VerifiedBy { get; set; }
///
/// public bool IsVerified { get; set; }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MarkoutShowWhenAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the boolean property that controls visibility.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Initializes a new instance with the specified boolean property name.
    /// </summary>
    /// <param name="propertyName">The name of a boolean property on the same type.</param>
    public MarkoutShowWhenAttribute(string propertyName) => PropertyName = propertyName;
}
