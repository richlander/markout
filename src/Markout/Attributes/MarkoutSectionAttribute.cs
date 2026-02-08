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

    /// <summary>
    /// Gets or sets the name of an element property whose value should be transformed by <see cref="Formatter"/>.
    /// Must be used together with <see cref="Formatter"/>.
    /// </summary>
    public string? FormatProperty { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="IMarkoutPropertyFormatter{T}"/> implementation type
    /// used to transform the <see cref="FormatProperty"/> value before rendering.
    /// Must be used together with <see cref="FormatProperty"/>.
    /// </summary>
    public Type? Formatter { get; set; }

    /// <summary>
    /// Gets or sets an optional column header override for the <see cref="FormatProperty"/> column.
    /// When null, the original property display name is used.
    /// </summary>
    public string? ColumnName { get; set; }
}
