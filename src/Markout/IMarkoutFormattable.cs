namespace Markout;

/// <summary>
/// Allows a type to control its own Markout serialization.
/// Types implementing this interface can write structured Markdown content
/// directly to a <see cref="MarkoutWriter"/>.
/// </summary>
public interface IMarkoutFormattable
{
    /// <summary>
    /// Writes a structured Markdown representation of this object to the writer.
    /// Use this for block-level content (headings, fields, tables, lists).
    /// </summary>
    /// <param name="writer">The orchestrator to render to.</param>
    void WriteTo(MarkoutWriter writer);

    /// <summary>
    /// Returns a single-line string representation for use in inline contexts
    /// such as table cells and compact field values.
    /// </summary>
    /// <returns>A plain text representation, or null to use ToString().</returns>
    string? ToMarkoutString() => ToString();
}
