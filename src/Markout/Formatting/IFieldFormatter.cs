namespace Markout.Formatting;

/// <summary>
/// Capability interface for rendering key-value fields.
/// The base class handles section filtering, projection, and spacing;
/// implementations only render the field content in their format.
/// </summary>
public interface IFieldFormatter
{
    /// <summary>
    /// Renders a field name (key portion). Called from various field layout methods.
    /// </summary>
    /// <param name="writer">The output writer.</param>
    /// <param name="key">The field name text.</param>
    /// <param name="bold">Whether to render the field name in bold.</param>
    void FormatFieldName(TextWriter writer, string key, bool bold);

    /// <summary>
    /// Renders multiple fields, each on its own line.
    /// The caller handles section filtering, projection, blank lines, and state.
    /// </summary>
    /// <param name="writer">The output writer.</param>
    /// <param name="fields">The projected fields to render.</param>
    /// <param name="bold">Whether to render field names in bold.</param>
    void FormatFields(TextWriter writer, MarkoutField[] fields, bool bold);

    /// <summary>
    /// Renders multiple fields, each on its own line, from a span (allocation-free at the call site).
    /// The default delegates to the array overload; built-in formatters override this to avoid the copy.
    /// </summary>
    /// <param name="writer">The output writer.</param>
    /// <param name="fields">The projected fields to render.</param>
    /// <param name="bold">Whether to render field names in bold.</param>
    void FormatFields(TextWriter writer, ReadOnlySpan<MarkoutField> fields, bool bold)
        => FormatFields(writer, fields.ToArray(), bold);
}
