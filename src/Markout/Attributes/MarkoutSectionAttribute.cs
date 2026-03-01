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

    /// <summary>
    /// Gets or sets the name of a boolean property that controls whether this section is rendered.
    /// When the property value is <c>false</c>, the section is skipped entirely.
    /// </summary>
    public string? ShowWhenProperty { get; set; }

    /// <summary>
    /// Gets or sets the name of an element property to group items by.
    /// Each distinct value becomes a subheading, with items rendered beneath.
    /// The group-by property is excluded from per-item rendering (it becomes the heading).
    /// </summary>
    public string? GroupBy { get; set; }

    /// <summary>
    /// Gets or sets whether the section heading is suppressed.
    /// When true, the section is addressable and filterable via <see cref="MarkoutWriterOptions.IncludeSections"/>
    /// but no heading (e.g., ##) is emitted. Use for preamble content that should be a named section.
    /// </summary>
    public bool Headless { get; set; }
}
