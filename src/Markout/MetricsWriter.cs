using Markout.Formatting;

namespace Markout;

/// <summary>
/// Writes metrics, breakdowns, and charts to a TextWriter using a metrics formatter.
/// Document state is managed by the caller or <see cref="MarkoutOrchestrator"/>.
/// </summary>
public class MetricsWriter(TextWriter writer, IMetricsFormatter formatter, MarkoutWriterOptions? options = null)
{
    private readonly MarkoutWriterOptions _options = options ?? new();

    /// <summary>
    /// Writes a breakdown chart showing proportional category composition.
    /// </summary>
    public void WriteBreakdown(IReadOnlyList<Breakdown> items, int? maxBarWidth = null, bool uniformBarWidth = true)
    {
        if (items.Count == 0) return;
        formatter.FormatBreakdown(writer, items, maxBarWidth, uniformBarWidth, _options);
    }

    /// <summary>
    /// Writes horizontal metric bars.
    /// </summary>
    public void WriteMetrics(IReadOnlyList<Metric> items, int maxBarWidth = 30)
    {
        if (items.Count == 0) return;
        formatter.FormatMetrics(writer, items, maxBarWidth, _options);
    }

    /// <summary>
    /// Writes vertical metric bars.
    /// </summary>
    public void WriteVerticalMetrics(IReadOnlyList<Metric> items, int maxBarHeight = 10, int? barWidth = null)
    {
        if (items.Count == 0) return;
        formatter.FormatVerticalMetrics(writer, items, maxBarHeight, barWidth, _options);
    }
}
