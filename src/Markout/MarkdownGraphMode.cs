namespace Markout;

/// <summary>
/// Selects how a <see cref="Graph"/> is rendered inside a Markdown document.
/// </summary>
public enum MarkdownGraphMode
{
    /// <summary>
    /// Render one Markdown table row per directed edge.
    /// </summary>
    EdgeTable,

    /// <summary>
    /// Render a rooted tree inside a fenced <c>text</c> code block.
    /// </summary>
    FencedTree,

    /// <summary>
    /// Render a Mermaid flowchart inside a fenced <c>mermaid</c> code block.
    /// </summary>
    Mermaid
}
