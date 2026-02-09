namespace Markout;

/// <summary>
/// Specifies a custom formatter type to use when rendering a property value.
/// The formatter must implement <see cref="IMarkoutValueFormatter{T}"/> for the property type.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MarkoutValueFormatterAttribute : Attribute
{
    public Type FormatterType { get; }
    public MarkoutValueFormatterAttribute(Type formatterType) => FormatterType = formatterType;
}
