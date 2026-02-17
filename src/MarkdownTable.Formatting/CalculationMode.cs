namespace MarkdownTable.Formatting;

/// <summary>
/// Determines how column widths are calculated.
/// </summary>
public enum CalculationMode
{
    /// <summary>
    /// Percentile-based statistical analysis with outlier handling.
    /// </summary>
    Statistical,

    /// <summary>
    /// Maximum content width per column (no statistical optimization).
    /// </summary>
    FullWidth
}
