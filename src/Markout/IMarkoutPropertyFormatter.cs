namespace Markout;

/// <summary>
/// Transforms a property value before rendering it as a table cell.
/// Implement this interface and reference it from <see cref="MarkoutSectionAttribute.Formatter"/>
/// to customize how a specific element property is displayed in a section table.
/// </summary>
/// <typeparam name="T">The type of the property value to format.</typeparam>
public interface IMarkoutPropertyFormatter<in T> where T : class
{
    string Format(T value);
}
