namespace Markout;

/// <summary>
/// A single segment in a distribution bar, representing a category and its count.
/// </summary>
/// <param name="Category">The category label (e.g., "Critical", "High").</param>
/// <param name="Count">The number of items in this category.</param>
public readonly record struct DistributionSegment(string Category, int Count);

/// <summary>
/// A labeled distribution bar showing the proportional breakdown of categories.
/// Used with <see cref="MarkoutWriter.WriteDistribution"/> to render stacked bars.
/// </summary>
/// <param name="Label">The row label (e.g., "Jan 2025", ".NET 9").</param>
/// <param name="Segments">The category segments, rendered left-to-right in order.</param>
public readonly record struct DistributionBar(string Label, DistributionSegment[] Segments);
