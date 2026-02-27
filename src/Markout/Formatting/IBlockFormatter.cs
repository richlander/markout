namespace Markout.Formatting;

/// <summary>
/// Capability interface for rendering block-level content:
/// callouts, quotations, rules, and description lists.
/// </summary>
public interface IBlockFormatter
{
    /// <summary>
    /// Renders a paragraph of text.
    /// </summary>
    void FormatParagraph(TextWriter writer, string text);

    /// <summary>
    /// Renders a callout/admonition block.
    /// </summary>
    void FormatCallout(TextWriter writer, CalloutSeverity severity, string message);

    /// <summary>
    /// Renders a prose quotation block.
    /// </summary>
    void FormatQuotation(TextWriter writer, string text);

    /// <summary>
    /// Renders a horizontal rule separator.
    /// </summary>
    void FormatRule(TextWriter writer);

    /// <summary>
    /// Renders a single description list item.
    /// </summary>
    void FormatDescription(TextWriter writer, Description item);
}
