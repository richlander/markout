namespace Markout;

/// <summary>
/// A single segment in a breakdown, representing a category and its count.
/// </summary>
/// <param name="Category">The category label (e.g., "Critical", "High").</param>
/// <param name="Count">The number of items in this category.</param>
public readonly record struct Segment(string Category, int Count);

/// <summary>
/// A labeled breakdown showing the proportional composition of categories.
/// Used with <see cref="MarkoutWriter.WriteBreakdown"/> to render stacked segments.
/// </summary>
/// <param name="Label">The row label (e.g., "Jan 2025", ".NET 9").</param>
/// <param name="Segments">The category segments, rendered left-to-right in order.</param>
public readonly record struct Breakdown(string Label, Segment[] Segments);
