using Markout.Formatting;

namespace Markout;

/// <summary>
/// Writes lists and arrays to a TextWriter using a list formatter.
/// Document state is managed by the caller or <see cref="MarkoutWriter"/>.
/// </summary>
public class ListWriter(TextWriter writer, IListFormatter formatter, MarkoutWriterOptions? options = null)
{
    private readonly MarkoutWriterOptions _options = options ?? new();

    /// <summary>
    /// Writes a single bullet list item.
    /// </summary>
    public void WriteListItem(string text)
    {
        formatter.FormatListItem(writer, text);
    }

    /// <summary>
    /// Writes a sequence of strings as bullet list items.
    /// </summary>
    public void WriteList(params ReadOnlySpan<string> items)
    {
        foreach (var item in items)
            formatter.FormatListItem(writer, item);
    }

    /// <summary>
    /// Writes a labeled array as a list with a key header.
    /// </summary>
    public void WriteArray(string key, params ReadOnlySpan<string> items)
    {
        formatter.FormatArray(writer, key, items, _options.BoldFieldNames);
    }

    /// <summary>
    /// Writes items as a bullet list (no label).
    /// </summary>
    public void WriteArray(params ReadOnlySpan<string> items)
    {
        foreach (var item in items)
            formatter.FormatListItem(writer, item);
    }
}
