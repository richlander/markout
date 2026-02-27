namespace Markout.Formatting;

/// <summary>
/// Capability interface for rendering lists and arrays.
/// The base class handles section filtering and spacing;
/// implementations only render the list content in their format.
/// </summary>
public interface IListFormatter
{
    /// <summary>
    /// Renders a single list item.
    /// </summary>
    void FormatListItem(TextWriter writer, string text);

    /// <summary>
    /// Renders a labeled array as a list with a key header.
    /// </summary>
    /// <param name="writer">The output writer.</param>
    /// <param name="key">The array label.</param>
    /// <param name="items">The items to render.</param>
    /// <param name="bold">Whether to render the key in bold.</param>
    void FormatArray(TextWriter writer, string key, ReadOnlySpan<string> items, bool bold);
}
