namespace Markout;

/// <summary>
/// Renders a property as a section (## heading) in Markout output.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MarkoutSectionAttribute : Attribute
{
    /// <summary>
    /// Gets or sets a custom section name. If null, uses the property name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the heading level (default is 2 for ##).
    /// </summary>
    public int Level { get; set; } = 2;

    /// <summary>
    /// Gets or sets the name of an element property to exclude from table rendering.
    /// When set, the specified property on the element type will be omitted as a column.
    /// </summary>
    public string? IgnoreProperty { get; set; }
}
