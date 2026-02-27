namespace Markout.Formatting;

/// <summary>
/// Capability interface for rendering fenced code blocks.
/// The base class handles nesting validation, section filtering, and state;
/// implementations only render the code fence markers in their format.
/// </summary>
public interface ICodeBlockFormatter
{
    /// <summary>
    /// Renders the opening of a code block (e.g., ``` with optional language).
    /// </summary>
    void FormatCodeStart(TextWriter writer, string? language);

    /// <summary>
    /// Renders the closing of a code block (e.g., ```).
    /// </summary>
    void FormatCodeEnd(TextWriter writer);
}
