namespace Markout;

/// <summary>
/// Represents a labeled item for descriptive list rendering.
/// Renders as a bullet with a bold label: "- <b>Label:</b> Description".
/// </summary>
/// <param name="Label">The bold label text.</param>
/// <param name="Description">The description text after the label.</param>
/// <param name="Detail">Optional detail line, indented below the description.</param>
public readonly record struct LabeledItem(string Label, string Description, string? Detail = null);
