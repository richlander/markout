namespace Markout;

/// <summary>
/// Represents a term with explanatory text for descriptive list rendering.
/// Renders as a bullet with a bold term: "- <b>Term:</b> text".
/// </summary>
/// <param name="Term">The bold term text.</param>
/// <param name="Text">The explanatory text after the term.</param>
/// <param name="Detail">Optional detail line, indented below the text.</param>
public readonly record struct Description(string Term, string Text, string? Detail = null);
