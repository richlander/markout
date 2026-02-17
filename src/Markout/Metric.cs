namespace Markout;

/// <summary>
/// Represents a labeled measurement for comparative visualization.
/// </summary>
/// <param name="Label">The metric label.</param>
/// <param name="Value">The numeric value.</param>
public readonly record struct Metric(string Label, double Value);
