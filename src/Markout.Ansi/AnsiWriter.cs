using Markout.Formatting;
using Microsoft.Extensions.Terminal;

namespace Markout.Ansi;

/// <summary>
/// A formatter that renders output as rich ANSI terminal text.
/// Uses bold, color, and unicode characters for human-friendly terminal output.
/// </summary>
public class AnsiWriter : IMarkoutFormatter,
    IDocumentFormatter, IMetricsFormatter
{
    private const int ColumnGap = 2;
    private readonly ITerminal _terminal;

    /// <summary>
    /// Creates an ANSI formatter targeting the specified terminal.
    /// </summary>
    public AnsiWriter(ITerminal terminal)
    {
        _terminal = terminal;
    }

    /// <summary>
    /// Gets the terminal width for layout calculations.
    /// </summary>
    private int TerminalWidth => _terminal.Width == int.MaxValue ? 80 : _terminal.Width;
    /// <summary>
    /// Color for the heading label text. Default is <see cref="TerminalColor.White"/>.
    /// </summary>
    public TerminalColor RuleLabelColor { get; set; } = TerminalColor.White;

    /// <summary>
    /// Color for rule lines when gradient is disabled. Default is <see cref="TerminalColor.DarkGray"/>.
    /// </summary>
    public TerminalColor RuleLineColor { get; set; } = TerminalColor.DarkGray;

    /// <summary>
    /// Whether to render rule lines as a gradient that fades toward the edges.
    /// Default is true.
    /// </summary>
    public bool RuleGradient { get; set; } = true;

    /// <summary>
    /// RGB color for the bright end of the gradient (nearest the label).
    /// Default is (0, 180, 180) — teal/cyan.
    /// </summary>
    public (byte R, byte G, byte B) RuleGradientStart { get; set; } = (0, 180, 180);

    /// <summary>
    /// RGB color for the dim end of the gradient (at the edges).
    /// Default is (0, 40, 50) — near-black teal.
    /// </summary>
    public (byte R, byte G, byte B) RuleGradientEnd { get; set; } = (0, 40, 50);

    // ── Formatter Interface Implementations ──
    // Pure rendering: write ANSI escape codes directly to the TextWriter w.

    private static void Sgr(TextWriter w, int code) => w.Write($"\x1b[{code}m");
    private static void SgrReset(TextWriter w) => w.Write("\x1b[m");
    private static void SgrBold(TextWriter w, string text) { w.Write("\x1b[1m"); w.Write(text); w.Write("\x1b[22m"); }
    private static void SgrRgb(TextWriter w, byte r, byte g, byte b) => w.Write($"\x1b[38;2;{r};{g};{b}m");

    // SGR color codes matching TerminalColor enum values
    private const int SgrCyan = 36;
    private const int SgrDarkGray = 90;
    private const int SgrRed = 31;
    private const int SgrGreen = 32;
    private const int SgrYellow = 33;
    private const int SgrMagenta = 35;
    private const int SgrBlue = 34;
    private const int SgrWhite = 37;

    /// <summary>
    /// Color for bar chart bars. Default is <see cref="TerminalColor.Cyan"/>.
    /// </summary>
    public TerminalColor BarColor { get; set; } = TerminalColor.Cyan;

    /// <summary>
    /// Color for bar chart values. Default is <see cref="TerminalColor.DarkGray"/>.
    /// </summary>
    public TerminalColor BarValueColor { get; set; } = TerminalColor.DarkGray;

    private static readonly int[] DistributionSgrColors = [SgrRed, SgrYellow, SgrCyan, SgrGreen, SgrMagenta, SgrBlue];

    void IHeadingFormatter.FormatHeading(TextWriter w, int level, string text, string? context)
    {
        var fullText = string.IsNullOrEmpty(context) ? text : $"{text} ({context})";
        if (level == 1)
            FormatRuleTo(w, fullText);
        else
        {
            Sgr(w, SgrCyan);
            SgrBold(w, fullText);
            SgrReset(w);
        }
    }

    void IFieldFormatter.FormatFieldName(TextWriter w, string key, bool bold)
    {
        // ANSI formatter always bolds field names for visual emphasis
        SgrBold(w, key);
        w.Write(": ");
    }

    void IFieldFormatter.FormatFields(TextWriter w, MarkoutField[] fields, bool bold)
    {
        for (int i = 0; i < fields.Length; i++)
        {
            ((IFieldFormatter)this).FormatFieldName(w, fields[i].Key, bold);
            w.Write(fields[i].Value);
            w.WriteLine();
        }
    }

    void ITableFormatter.FormatTable(TextWriter w, string[] headers, IList<string[]> rows, int skippedRows, MarkoutWriterOptions options)
    {
        var widths = new int[headers.Length];
        for (int i = 0; i < headers.Length; i++)
            widths[i] = headers[i].Length;
        foreach (var row in rows)
            for (int i = 0; i < Math.Min(row.Length, widths.Length); i++)
                widths[i] = Math.Max(widths[i], row[i].Length);

        for (int i = 0; i < headers.Length; i++)
        {
            var text = headers[i].ToUpperInvariant();
            SgrBold(w, i < headers.Length - 1 ? text.PadRight(widths[i] + ColumnGap) : text);
        }
        w.WriteLine();

        Sgr(w, SgrDarkGray);
        for (int i = 0; i < headers.Length; i++)
        {
            var sep = new string('─', widths[i]);
            w.Write(i < headers.Length - 1 ? sep.PadRight(widths[i] + ColumnGap) : sep);
        }
        SgrReset(w);
        w.WriteLine();

        foreach (var row in rows)
        {
            for (int i = 0; i < row.Length; i++)
                w.Write(i < row.Length - 1 ? row[i].PadRight(widths[i] + ColumnGap) : row[i]);
            w.WriteLine();
        }

        if (skippedRows > 0)
        {
            Sgr(w, SgrDarkGray);
            w.Write($"\n... and {skippedRows} more");
            SgrReset(w);
            w.WriteLine();
        }
    }

    void IListFormatter.FormatListItem(TextWriter w, string text)
    {
        Sgr(w, SgrDarkGray);
        w.Write("  • ");
        SgrReset(w);
        w.WriteLine(text);
    }

    void IListFormatter.FormatArray(TextWriter w, string key, ReadOnlySpan<string> items, bool bold)
    {
        if (bold)
            SgrBold(w, key);
        else
            w.Write(key);
        w.WriteLine(":");
        foreach (var item in items)
            ((IListFormatter)this).FormatListItem(w, item);
    }

    void ICodeBlockFormatter.FormatCodeStart(TextWriter w, string? language)
    {
        Sgr(w, SgrDarkGray);
    }

    void ICodeBlockFormatter.FormatCodeEnd(TextWriter w)
    {
        SgrReset(w);
    }

    void IBlockFormatter.FormatCallout(TextWriter w, CalloutSeverity severity, string message)
    {
        var (label, sgr) = severity switch
        {
            CalloutSeverity.Note => ("NOTE", SgrCyan),
            CalloutSeverity.Tip => ("TIP", SgrGreen),
            CalloutSeverity.Important => ("IMPORTANT", SgrMagenta),
            CalloutSeverity.Warning => ("WARNING", SgrYellow),
            CalloutSeverity.Caution => ("CAUTION", SgrRed),
            _ => (severity.ToString().ToUpperInvariant(), SgrWhite)
        };
        Sgr(w, sgr);
        SgrBold(w, label);
        SgrReset(w);
        w.Write(": ");
        w.WriteLine(message);
    }

    void IBlockFormatter.FormatParagraph(TextWriter w, string text)
    {
        w.WriteLine(text);
    }

    void IBlockFormatter.FormatQuotation(TextWriter w, string text)
    {
        foreach (var line in text.Split('\n'))
        {
            Sgr(w, SgrDarkGray);
            w.Write("│ ");
            SgrReset(w);
            w.WriteLine(line);
        }
    }

    void IBlockFormatter.FormatRule(TextWriter w)
    {
        Sgr(w, SgrDarkGray);
        w.WriteLine("────────────────────────────────");
        SgrReset(w);
    }

    void IBlockFormatter.FormatDescription(TextWriter w, Description item)
    {
        w.Write("- ");
        SgrBold(w, item.Term);
        w.Write(": ");
        w.WriteLine(item.Text);
        if (item.Detail != null)
        {
            w.Write("  ");
            Sgr(w, SgrDarkGray);
            w.Write(item.Detail);
            SgrReset(w);
            w.WriteLine();
        }
    }

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

    private void FormatTreeNodeRecursive(TextWriter w, TreeNode node, string prefix, bool isLast, MarkoutWriterOptions options)
    {
        w.Write(prefix);
        Sgr(w, SgrDarkGray);
        w.Write(isLast ? "└─ " : "├─ ");
        SgrReset(w);
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

    void IMetricsFormatter.FormatBreakdown(TextWriter w, IReadOnlyList<Breakdown> items, int? maxBarWidth, bool uniformBarWidth, MarkoutWriterOptions options)
    {
        var categories = items.SelectMany(b => b.Segments).Select(s => s.Category).Distinct().ToList();
        var maxTotal = items.Max(b => b.Segments.Sum(s => s.Count));
        if (maxTotal == 0) return;

        var labelWidth = items.Max(b => b.Label.Length);
        var barWidth = maxBarWidth ?? (TerminalWidth - labelWidth - 4);
        var barScale = barWidth / (double)maxTotal;
        var maxBarChars = uniformBarWidth ? (int)Math.Round(maxTotal * barScale) : 0;

        foreach (var item in items)
        {
            SgrBold(w, item.Label.PadRight(labelWidth));
            w.Write("  ");
            var bw = 0;
            foreach (var seg in item.Segments)
            {
                var catIndex = categories.IndexOf(seg.Category);
                Sgr(w, DistributionSgrColors[catIndex % DistributionSgrColors.Length]);
                var segWidth = Math.Max(0, (int)Math.Round(seg.Count * barScale));
                w.Write(new string('█', segWidth));
                bw += segWidth;
            }
            SgrReset(w);
            if (maxBarChars > bw) w.Write(new string(' ', maxBarChars - bw));
            w.Write("  ");
            Sgr(w, SgrDarkGray);
            w.Write(string.Join(", ", item.Segments.Where(s => s.Count > 0).Select(s => $"{s.Count} {s.Category}")));
            SgrReset(w);
            w.WriteLine();
        }

        for (int i = 0; i < categories.Count; i++)
        {
            if (i > 0) w.Write("  ");
            Sgr(w, DistributionSgrColors[i % DistributionSgrColors.Length]);
            w.Write('█');
            SgrReset(w);
            w.Write($" {categories[i]}");
        }
        w.WriteLine();
    }

    void IMetricsFormatter.FormatMetrics(TextWriter w, IReadOnlyList<Metric> items, int maxBarWidth, MarkoutWriterOptions options)
    {
        if (items.Count == 0) return;
        var maxValue = items.Max(m => m.Value);
        if (maxValue == 0) return;

        var labelWidth = items.Max(m => m.Label.Length);
        var valueWidth = items.Max(m => FormatHelper.FormatBarValue(m.Value).Length);

        foreach (var item in items)
        {
            SgrBold(w, item.Label.PadRight(labelWidth));
            w.Write("  ");
            var ratio = item.Value / maxValue;
            var fullBlocks = (int)(ratio * maxBarWidth);
            var halfBlock = (ratio * maxBarWidth) - fullBlocks >= 0.5;
            Sgr(w, (int)BarColor);
            w.Write(new string('█', fullBlocks));
            if (halfBlock) w.Write('▌');
            SgrReset(w);
            var bw = fullBlocks + (halfBlock ? 1 : 0);
            w.Write(new string(' ', maxBarWidth - bw + 1));
            Sgr(w, (int)BarValueColor);
            w.Write(FormatHelper.FormatBarValue(item.Value).PadLeft(valueWidth));
            SgrReset(w);
            w.WriteLine();
        }
    }

    void IMetricsFormatter.FormatVerticalMetrics(TextWriter w, IReadOnlyList<Metric> items, int maxBarHeight, int? barWidth, MarkoutWriterOptions options)
    {
        ((IMetricsFormatter)this).FormatMetrics(w, items, maxBarHeight, options);
    }

    private void FormatRuleTo(TextWriter w, string title)
    {
        int width = TerminalWidth;
        string padded = $" {title} ";
        int remaining = width - padded.Length;

        if (remaining <= 0)
        {
            Sgr(w, (int)RuleLabelColor);
            SgrBold(w, title);
            SgrReset(w);
            return;
        }

        int left = remaining / 2;
        int right = remaining - left;

        FormatGradientLine(w, left, fadeInward: true);
        Sgr(w, (int)RuleLabelColor);
        SgrBold(w, padded);
        SgrReset(w);
        FormatGradientLine(w, right, fadeInward: false);
    }

    private void FormatGradientLine(TextWriter w, int length, bool fadeInward)
    {
        if (length <= 0) return;

        if (!RuleGradient)
        {
            Sgr(w, (int)RuleLineColor);
            w.Write(new string('─', length));
            SgrReset(w);
            return;
        }

        var (r1, g1, b1) = RuleGradientStart;
        var (r2, g2, b2) = RuleGradientEnd;

        for (int i = 0; i < length; i++)
        {
            float t = fadeInward
                ? (float)i / Math.Max(length - 1, 1)
                : 1f - (float)i / Math.Max(length - 1, 1);
            SgrRgb(w, (byte)(r2 + (r1 - r2) * t), (byte)(g2 + (g1 - g2) * t), (byte)(b2 + (b1 - b2) * t));
            w.Write('─');
        }
        SgrReset(w);
    }
}
