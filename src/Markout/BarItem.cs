namespace Markout;

/// <summary>
/// Represents a labeled value for bar chart rendering.
/// </summary>
/// <param name="Label">The bar label.</param>
/// <param name="Value">The numeric value.</param>
public readonly record struct BarItem(string Label, double Value);
