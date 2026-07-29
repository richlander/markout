namespace Markout;

/// <summary>
/// Specifies a custom formatter type to use when rendering a property value.
/// The formatter must implement <see cref="IMarkoutValueFormatter{T}"/> for the property type.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MarkoutValueFormatterAttribute : Attribute
{
    /// <summary>
    /// The formatter type, which must implement <see cref="IMarkoutValueFormatter{T}"/>
    /// for the annotated property's type.
    /// </summary>
    public Type FormatterType { get; }

    /// <summary>
    /// Initializes the attribute with the formatter applied to the annotated property.
    /// </summary>
    /// <param name="formatterType">
    /// A type implementing <see cref="IMarkoutValueFormatter{T}"/> for the property's type.
    /// </param>
    public MarkoutValueFormatterAttribute(Type formatterType) => FormatterType = formatterType;
}
