namespace Markout.Formatting;

/// <summary>
/// Capability interface for rendering headings (H1–H6).
/// The base class handles validation, section tracking, and spacing;
/// implementations only render the heading text in their format.
/// </summary>
public interface IHeadingFormatter
{
    /// <summary>
    /// Renders a heading at the specified level.
    /// The caller handles the trailing newline and state updates.
    /// </summary>
    void FormatHeading(TextWriter writer, int level, string text, string? context);
}
