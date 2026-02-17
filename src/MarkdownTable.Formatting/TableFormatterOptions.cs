namespace MarkdownTable.Formatting;

/// <summary>
/// Options for statistical table formatting.
/// </summary>
public class TableFormatterOptions
{
    /// <summary>
    /// Percentile threshold for column width calculation (0.0–1.0).
    /// Values at or below this percentile set the baseline width.
    /// Default: 0.5 (median).
    /// </summary>
    public double Percentile { get; set; } = 0.5;

    /// <summary>
    /// Tolerance multiplier applied to the percentile width.
    /// Content up to this multiple of the percentile width is accommodated
    /// without overflow. Default: 1.5.
    /// </summary>
    public double Tolerance { get; set; } = 1.5;

    /// <summary>
    /// Shadow threshold — columns narrower than this use max-width
    /// instead of statistical analysis. Prevents over-optimization
    /// of narrow columns. Default: 12.
    /// </summary>
    public int ShadowThreshold { get; set; } = 12;

    /// <summary>
    /// Maximum allowed column width. Prevents extreme widths from
    /// dominating the table. Default: 1000.
    /// </summary>
    public int MaxColumnWidth { get; set; } = 1000;

    /// <summary>
    /// When true, uses hill-climbing to find percentile/tolerance
    /// parameters that achieve perfect trailing-edge alignment.
    /// Default: false (use fixed parameters).
    /// </summary>
    public bool AutoTune { get; set; }
}
