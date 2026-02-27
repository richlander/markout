using Markout.Formatting;

namespace Markout;

/// <summary>
/// A formatter for structural and hierarchical visualizations.
/// Supports headings, trees, and metrics with plain-text rendering.
/// </summary>
public class DiagramFormatter : IMarkoutFormatter,
    IHeadingFormatter, ITreeFormatter, IMetricsFormatter
{
    // ── IHeadingFormatter ──

    void IHeadingFormatter.FormatHeading(TextWriter w, int level, string text, string? context)
    {
        w.Write(text);
        if (!string.IsNullOrEmpty(context))
        {
            w.Write(" (");
            w.Write(context);
            w.Write(')');
        }
    }

    // ── ITreeFormatter ──

    void ITreeFormatter.FormatTree(TextWriter w, ReadOnlySpan<TreeNode> nodes, MarkoutWriterOptions options)
    {
        for (int i = 0; i < nodes.Length; i++)
            FormatTreeNodeRecursive(w, nodes[i], "", i == nodes.Length - 1, options);
    }

    void ITreeFormatter.FormatTreeNode(TextWriter w, string text, string prefix)
    {
        w.Write(prefix);
        w.WriteLine(text);
    }

    private static void FormatTreeNodeRecursive(TextWriter w, TreeNode node, string prefix, bool isLast, MarkoutWriterOptions options)
    {
        w.Write(prefix);
        w.Write(isLast ? "└─ " : "├─ ");
        if (node.Badge != null && options.IncludeBadges)
        {
            w.Write(node.Badge);
            w.Write(' ');
        }
        w.WriteLine(node.Text);

        if (node.Children is { Count: > 0 })
        {
            var childPrefix = prefix + (isLast ? "   " : "│  ");
            for (int i = 0; i < node.Children.Count; i++)
                FormatTreeNodeRecursive(w, node.Children[i], childPrefix, i == node.Children.Count - 1, options);
        }
    }

    // ── IMetricsFormatter ──

    void IMetricsFormatter.FormatMetrics(TextWriter w, IReadOnlyList<Metric> items, int maxBarWidth, MarkoutWriterOptions options)
    {
        var maxValue = 0.0;
        var maxLabelWidth = 0;
        var maxValueWidth = 0;
        foreach (var item in items)
        {
            if (item.Value > maxValue) maxValue = item.Value;
            if (item.Label.Length > maxLabelWidth) maxLabelWidth = item.Label.Length;
            var vw = FormatHelper.FormatBarValue(item.Value).Length;
            if (vw > maxValueWidth) maxValueWidth = vw;
        }

        if (maxValue <= 0) maxValue = 1;

        foreach (var item in items)
        {
            w.Write(item.Label.PadRight(maxLabelWidth));
            w.Write("  ");

            var ratio = item.Value / maxValue;
            var fullBlocks = (int)(ratio * maxBarWidth);
            var fraction = (ratio * maxBarWidth) - fullBlocks;
            var halfBlock = fraction >= 0.5;

            w.Write(new string('█', fullBlocks));
            if (halfBlock) w.Write('▌');

            var barWidth = fullBlocks + (halfBlock ? 1 : 0);
            var padding = maxBarWidth - barWidth + 1;
            w.Write(new string(' ', padding));
            w.WriteLine(FormatHelper.FormatBarValue(item.Value).PadLeft(maxValueWidth));
        }
    }

    void IMetricsFormatter.FormatBreakdown(TextWriter w, IReadOnlyList<Breakdown> items, int? maxBarWidth, bool uniformBarWidth, MarkoutWriterOptions options)
    {
        // DiagramFormatter does not render breakdowns
    }

    void IMetricsFormatter.FormatVerticalMetrics(TextWriter w, IReadOnlyList<Metric> items, int maxBarHeight, int? barWidth, MarkoutWriterOptions options)
    {
        // DiagramFormatter does not render vertical metrics
    }
}
