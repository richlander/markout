namespace Markout;

/// <summary>
/// A single slice in a breakdown: a proportional part of a shared whole.
/// </summary>
/// <param name="Category">The category label (e.g., "Critical", "High").</param>
/// <param name="Count">The number of items in this slice.</param>
public readonly record struct Slice(string Category, int Count);

/// <summary>
/// A labeled breakdown showing the proportional composition of categories.
/// Used with <see cref="MarkoutWriter.WriteBreakdown"/> to render stacked slices.
/// </summary>
/// <param name="Label">The row label (e.g., "Jan 2025", ".NET 9").</param>
/// <param name="Slices">The category slices, rendered left-to-right in order.</param>
public readonly record struct Breakdown(string Label, Slice[] Slices);
