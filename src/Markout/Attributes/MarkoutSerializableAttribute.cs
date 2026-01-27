namespace Markout;

/// <summary>
/// Marks a type for MDF source generation.
/// Types marked with this attribute will have serialization code generated at compile time.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class MarkoutSerializableAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the property to use as the document title (H1 heading).
    /// The property value will be rendered as an H1 heading at the start of the output.
    /// </summary>
    public string? TitleProperty { get; set; }

    /// <summary>
    /// Gets or sets the name of the property to use as the document description.
    /// The property value will be rendered as a paragraph after the title.
    /// </summary>
    public string? DescriptionProperty { get; set; }
}
