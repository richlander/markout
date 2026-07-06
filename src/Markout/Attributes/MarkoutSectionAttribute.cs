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

    /// <summary>
    /// Gets or sets whether decomposed (TSV/JSONL) rows for this section include a leading
    /// <c>section</c> column carrying the section <see cref="Name"/>. Lets a tool multiplex several
    /// sectioned card shapes into one structured stream and route rows by section. Off by default;
    /// Markdown output is unaffected (the section is already its heading). Applies to list shapes that
    /// decompose to typed rows (<c>List&lt;MetricChange&lt;T&gt;&gt;</c>, <c>List&lt;MultiSourceRow&gt;</c>).
    /// </summary>
    public bool IncludeSectionInStructuredRows { get; set; }

    /// Gets or sets fallback text rendered as a paragraph when the section's collection is
    /// non-null but empty. The section heading is still emitted, followed by this text in place
    /// of the table or list. When the collection is null the section is omitted entirely, so the
    /// caller controls whether the fallback appears by choosing an empty collection over null.
    /// </summary>
    public string? EmptyText { get; set; }

    /// <summary>
    /// Gets or sets how field rows in this section are ordered.
    /// Applies to field collections such as <see cref="MarkoutField"/> lists and scalar
    /// properties grouped into the same section. The default preserves input order.
    /// </summary>
    public MarkoutFieldOrder FieldOrder { get; set; } = MarkoutFieldOrder.Input;
}
