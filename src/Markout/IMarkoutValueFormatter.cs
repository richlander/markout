namespace Markout;

/// <summary>
/// Formats a property value before rendering it as a Markout field value.
/// Implement this interface and reference it from [MarkoutValueFormatter] to
/// customize how a property is displayed.
/// </summary>
public interface IMarkoutValueFormatter<in T>
{
    string Format(T value);
}
