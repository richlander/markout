namespace Markout.Formatting;

/// <summary>
/// Capability interface for rendering metrics, breakdowns, and charts.
/// The base class handles section filtering and spacing;
/// implementations only render the chart content in their format.
/// </summary>
public interface IMetricsFormatter
{
    /// <summary>
    /// Renders a breakdown chart showing proportional category composition.
    /// </summary>
    void FormatBreakdown(TextWriter writer, IReadOnlyList<Breakdown> items, int? maxBarWidth, bool uniformBarWidth);

    /// <summary>
    /// Renders horizontal metric bars.
    /// </summary>
    void FormatMetrics(TextWriter writer, IReadOnlyList<Metric> items, int maxBarWidth);

    /// <summary>
    /// Renders vertical metric bars.
    /// </summary>
    void FormatVerticalMetrics(TextWriter writer, IReadOnlyList<Metric> items, int maxBarHeight, int? barWidth);
}
