namespace Markout;

/// <summary>
/// Specifies that a string property should be rendered as a Markdown link.
/// </summary>
/// <remarks>
/// When <see cref="TextProperty"/> is not set, the URL is used as both the link text and target:
/// <c>[https://example.com](https://example.com)</c>.
/// When <see cref="TextProperty"/> is set, the referenced property provides the link text:
/// <c>[My Site](https://example.com)</c>.
/// </remarks>
/// <example>
/// <code>
/// [MarkoutLink]
/// public string? Url { get; set; }
///
/// [MarkoutLink(TextProperty = nameof(Name))]
/// public string? Repository { get; set; }
/// public string Name { get; set; } = "";
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MarkoutLinkAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of a string property on the same type to use as the link text.
    /// When not set, the URL value itself is used as the link text.
    /// </summary>
    public string? TextProperty { get; set; }
}
