using Markout.Formatting;

namespace Markout;

/// <summary>
/// Writes block-level content (callouts, quotations, rules, descriptions,
/// paragraphs) to a TextWriter using a block formatter.
/// Document state is managed by the caller or <see cref="MarkoutWriter"/>.
/// </summary>
public class BlockWriter(TextWriter writer, IBlockFormatter formatter)
{
    /// <summary>
    /// Writes a paragraph of text. Paragraphs are universal — no formatter needed.
    /// </summary>
    public void WriteParagraph(string text)
    {
        writer.WriteLine(text);
    }

    /// <summary>
    /// Writes a callout/admonition block.
    /// </summary>
    public void WriteCallout(CalloutSeverity severity, string message)
    {
        formatter.FormatCallout(writer, severity, message);
    }

    /// <summary>
    /// Writes a prose quotation block.
    /// </summary>
    public void WriteQuotation(string text)
    {
        formatter.FormatQuotation(writer, text);
    }

    /// <summary>
    /// Writes a horizontal rule separator.
    /// </summary>
    public void WriteRule()
    {
        formatter.FormatRule(writer);
    }

    /// <summary>
    /// Writes a list of description items.
    /// </summary>
    public void WriteDescriptions(IReadOnlyList<Description> items)
    {
        foreach (var item in items)
            formatter.FormatDescription(writer, item);
    }
}
