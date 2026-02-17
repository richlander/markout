namespace Markout;

/// <summary>
/// Describes the current rendering context of a <see cref="MarkoutWriter"/>.
/// Used to determine what Markdown constructs are valid at the current position.
/// </summary>
public enum MarkoutRenderContext
{
    /// <summary>
    /// Top-level block context. Any Markdown construct is valid.
    /// </summary>
    Block,

    /// <summary>
    /// Inside a table. Only inline content and escaped values are valid.
    /// Block-level elements (headings, code regions, lists) are not allowed.
    /// </summary>
    Table,

    /// <summary>
    /// Inside a code region. Content is rendered literally, not as Markdown.
    /// Nested code regions are not allowed.
    /// </summary>
    Code
}
