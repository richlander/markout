namespace MarkdownData;

/// <summary>
/// Renders a property as a section (## heading) in MDF output.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MdfSectionAttribute : Attribute
{
    /// <summary>
    /// Gets or sets a custom section name. If null, uses the property name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the heading level (default is 2 for ##).
    /// </summary>
    public int Level { get; set; } = 2;
}
