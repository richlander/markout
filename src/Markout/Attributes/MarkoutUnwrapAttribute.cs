namespace Markout;

/// <summary>
/// Renders a collection property's items inline at the current heading level,
/// without emitting a parent section heading.
/// <para>
/// Use this when the collection represents an object model layer that should be
/// transparent in the rendered document. Each item is rendered as a subsection
/// using its <see cref="MarkoutSerializableAttribute.TitleProperty"/> as the heading.
/// </para>
/// </summary>
/// <example>
/// <code>
/// [MarkoutSerializable(TitleProperty = nameof(Title))]
/// public class Report
/// {
///     public string Title { get; set; }
///
///     [MarkoutUnwrap]
///     public List&lt;Chapter&gt; Chapters { get; set; }
/// }
/// </code>
/// Without [MarkoutUnwrap], "Chapters" would render as:
/// <code>
/// ## Chapters
/// ### Chapter One
/// </code>
/// With [MarkoutUnwrap], the wrapper heading is omitted:
/// <code>
/// ## Chapter One
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MarkoutUnwrapAttribute : Attribute
{
}
