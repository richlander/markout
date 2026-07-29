namespace Markout;

/// <summary>
/// Formats a property value before rendering it as a Markout field value.
/// Implement this interface and reference it from [MarkoutValueFormatter] to
/// customize how a property is displayed.
/// </summary>
/// <typeparam name="T">The type of the property value to format.</typeparam>
public interface IMarkoutValueFormatter<in T>
{
    /// <summary>Formats <paramref name="value"/> as the rendered field value.</summary>
    /// <param name="value">The property value to format.</param>
    /// <returns>The string to render.</returns>
    string Format(T value);
}
