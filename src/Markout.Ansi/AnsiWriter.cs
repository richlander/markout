using Markout.Formatting;
using Microsoft.Extensions.Terminal;

namespace Markout.Ansi;

/// <summary>
/// A formatter that renders output as rich ANSI terminal text.
/// Uses bold, color, and unicode characters for human-friendly terminal output.
/// </summary>
public class AnsiWriter : MarkoutWriter, IMarkoutFormatter,
    IDocumentFormatter, IMetricsFormatter
{
    private const int ColumnGap = 2;
    private readonly ITerminal _terminal;

    /// <summary>
    /// Creates an ANSI writer targeting the specified terminal with default options.
    /// </summary>
    public AnsiWriter(ITerminal terminal) : base(new TerminalTextWriter(terminal))
    {
        _terminal = terminal;
    }

    /// <summary>
    /// Creates an ANSI writer targeting the specified terminal with the specified options.
    /// </summary>
    public AnsiWriter(ITerminal terminal, MarkoutWriterOptions options) : base(new TerminalTextWriter(terminal), options)
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

    /// <inheritdoc/>
    public override MarkoutShape SupportedShapes => MarkoutShape.All;

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

    void IMetricsFormatter.FormatBreakdown(TextWriter w, IReadOnlyList<Breakdown> items, int? maxBarWidth, bool uniformBarWidth)
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

    void IMetricsFormatter.FormatMetrics(TextWriter w, IReadOnlyList<Metric> items, int maxBarWidth)
    {
        if (items.Count == 0) return;
        var maxValue = items.Max(m => m.Value);
        if (maxValue == 0) return;

        var labelWidth = items.Max(m => m.Label.Length);
        var valueWidth = items.Max(m => FormatBarValue(m.Value).Length);

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
            w.Write(FormatBarValue(item.Value).PadLeft(valueWidth));
            SgrReset(w);
            w.WriteLine();
        }
    }

    void IMetricsFormatter.FormatVerticalMetrics(TextWriter w, IReadOnlyList<Metric> items, int maxBarHeight, int? barWidth)
    {
        ((IMetricsFormatter)this).FormatMetrics(w, items, maxBarHeight);
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

    // ── Headings ──

    /// <inheritdoc/>
    public override void WriteHeading(int level, string text, string? context)
    {
        if (level < 1 || level > 6)
            throw new ArgumentOutOfRangeException(nameof(level), "Heading level must be between 1 and 6.");

        UpdateSectionState(level, text);

        if (SectionExcluded)
            return;

        if (HasContent)
            Writer.WriteLine();

        var fullText = string.IsNullOrEmpty(context) ? text : $"{text} ({context})";

        if (level == 1)
        {
            // H1: ─── Title ───
            WriteRule(fullText);
        }
        else
        {
            // H2+: Bold colored text
            _terminal.SetColor(TerminalColor.Cyan);
            Writer.Write(AnsiCodes.MakeBold(fullText));
            _terminal.ResetColor();
            Writer.WriteLine();
        }

        NeedsBlankLine = true;
        HasContent = true;
    }

    private void WriteRule(string title)
    {
        int width = TerminalWidth;
        string padded = $" {title} ";
        int remaining = width - padded.Length;

        if (remaining <= 0)
        {
            _terminal.SetColor(RuleLabelColor);
            Writer.Write(AnsiCodes.MakeBold(title));
            _terminal.ResetColor();
            Writer.WriteLine();
            return;
        }

        int left = remaining / 2;
        int right = remaining - left;

        WriteRuleLine(left, fadeInward: true);
        _terminal.SetColor(RuleLabelColor);
        Writer.Write(AnsiCodes.MakeBold(padded));
        _terminal.ResetColor();
        WriteRuleLine(right, fadeInward: false);
        Writer.WriteLine();
    }

    private void WriteRuleLine(int length, bool fadeInward)
    {
        if (length <= 0) return;

        if (!RuleGradient)
        {
            _terminal.SetColor(RuleLineColor);
            Writer.Write(new string('─', length));
            _terminal.ResetColor();
            return;
        }

        // Gradient: interpolate from start (bright, near label) to end (dim, at edge)
        var (r1, g1, b1) = RuleGradientStart;
        var (r2, g2, b2) = RuleGradientEnd;

        for (int i = 0; i < length; i++)
        {
            // t=0 at edge, t=1 near label
            float t = fadeInward
                ? (float)i / Math.Max(length - 1, 1)
                : 1f - (float)i / Math.Max(length - 1, 1);

            byte r = (byte)(r2 + (r1 - r2) * t);
            byte g = (byte)(g2 + (g1 - g2) * t);
            byte b = (byte)(b2 + (b1 - b2) * t);

            Writer.Write($"\x1b[38;2;{r};{g};{b}m─");
        }

        _terminal.ResetColor();
    }

    // ── Fields ──

    /// <inheritdoc/>
    protected override void WriteFieldName(string key)
    {
        Writer.Write(AnsiCodes.MakeBold(key));
        Writer.Write(": ");
    }

    // ── Code ──

    /// <inheritdoc/>
    public override void WriteCodeStart(string? language = null)
    {
        if (InCode)
            throw new InvalidOperationException("Cannot nest code regions. End the current code region before starting a new one.");

        InCode = true;

        if (SectionExcluded)
            return;

        EnsureBlankLineIfNeeded();
        _terminal.SetColor(TerminalColor.DarkGray);
        HasContent = true;
    }

    /// <inheritdoc/>
    public override void WriteCodeEnd()
    {
        if (!InCode)
            throw new InvalidOperationException("Cannot end a code region without starting one first.");

        InCode = false;

        if (SectionExcluded)
            return;

        _terminal.ResetColor();
        NeedsBlankLine = true;
    }

    // ── Tables ──

    /// <inheritdoc/>
    protected override void FlushStreamingTable(string[] headers, IList<string[]> rows, int skippedRows)
    {
        // Reuse batch WriteTable for consistent styled rendering
        WriteTable(headers, rows);
        if (skippedRows > 0)
        {
            _terminal.SetColor(TerminalColor.DarkGray);
            Writer.WriteLine($"\n... and {skippedRows} more");
            _terminal.ResetColor();
        }
    }

    /// <inheritdoc/>
    public override void WriteTable(IEnumerable<string> headers, IEnumerable<string[]> rows)
    {
        if (SectionExcluded)
            return;

        var headerArray = headers as string[] ?? headers.ToArray();
        var rowList = rows as IList<string[]> ?? rows.ToList();

        // Apply MaxItems to the visible rows (for width calculation too)
        var maxItems = Options.MaxItems;
        var visibleRows = maxItems.HasValue && rowList.Count > maxItems.Value
            ? rowList.Take(maxItems.Value).ToList()
            : rowList;
        var skipped = rowList.Count - visibleRows.Count;

        // Calculate column widths from visible rows
        var widths = new int[headerArray.Length];
        for (int i = 0; i < headerArray.Length; i++)
            widths[i] = headerArray[i].Length;
        foreach (var row in visibleRows)
        {
            for (int i = 0; i < Math.Min(row.Length, widths.Length); i++)
                widths[i] = Math.Max(widths[i], row[i].Length);
        }

        EnsureBlankLineIfNeeded();

        // Bold uppercase headers
        for (int i = 0; i < headerArray.Length; i++)
        {
            var text = headerArray[i].ToUpperInvariant();
            if (i < headerArray.Length - 1)
                Writer.Write(AnsiCodes.MakeBold(text.PadRight(widths[i] + ColumnGap)));
            else
                Writer.Write(AnsiCodes.MakeBold(text));
        }
        Writer.WriteLine();

        // Separator
        _terminal.SetColor(TerminalColor.DarkGray);
        for (int i = 0; i < headerArray.Length; i++)
        {
            if (i < headerArray.Length - 1)
                Writer.Write(new string('─', widths[i]).PadRight(widths[i] + ColumnGap));
            else
                Writer.Write(new string('─', widths[i]));
        }
        _terminal.ResetColor();
        Writer.WriteLine();

        // Rows
        foreach (var row in visibleRows)
        {
            for (int i = 0; i < row.Length; i++)
            {
                if (i < row.Length - 1)
                    Writer.Write(row[i].PadRight(widths[i] + ColumnGap));
                else
                    Writer.Write(row[i]);
            }
            Writer.WriteLine();
        }

        if (skipped > 0)
        {
            _terminal.SetColor(TerminalColor.DarkGray);
            Writer.WriteLine($"\n... and {skipped} more");
            _terminal.ResetColor();
        }

        NeedsBlankLine = true;
        HasContent = true;
    }

    // ── Arrays and lists ──

    /// <inheritdoc/>
    public override void WriteArray(string key, params ReadOnlySpan<string> items)
    {
        if (SectionExcluded)
            return;

        if (HasContent)
            NeedsBlankLine = true;
        EnsureBlankLineIfNeeded();

        Writer.Write(AnsiCodes.MakeBold(key));
        Writer.WriteLine(":");

        WriteBulletItems(items);
    }

    /// <inheritdoc/>
    public override void WriteListItem(string text)
    {
        if (SectionExcluded)
            return;

        EnsureBlankLineIfNeeded();
        _terminal.SetColor(TerminalColor.DarkGray);
        Writer.Write("  • ");
        _terminal.ResetColor();
        Writer.WriteLine(text);
        HasContent = true;
    }

    // ── Fields inline (pipe-separated) ──

    /// <inheritdoc/>
    public override void WriteFieldsInline(params ReadOnlySpan<MarkoutField> fields)
    {
        if (SectionExcluded || fields.Length == 0)
            return;

        EnsureBlankLineIfNeeded();

        for (int i = 0; i < fields.Length; i++)
        {
            if (i > 0)
            {
                _terminal.SetColor(TerminalColor.DarkGray);
                Writer.Write(" │ ");
                _terminal.ResetColor();
            }

            Writer.Write(AnsiCodes.MakeBold(fields[i].Key));
            Writer.Write(": ");
            Writer.Write(fields[i].Value ?? string.Empty);
        }

        Writer.WriteLine();
        NeedsBlankLine = true;
        HasContent = true;
    }

    // ── Trees ──

    /// <summary>
    /// Color for bar chart bars. Default is <see cref="TerminalColor.Cyan"/>.
    /// </summary>
    public TerminalColor BarColor { get; set; } = TerminalColor.Cyan;

    /// <summary>
    /// Color for bar chart value labels. Default is <see cref="TerminalColor.DarkGray"/>.
    /// </summary>
    public TerminalColor BarValueColor { get; set; } = TerminalColor.DarkGray;

    // ── Labeled lists ──

    /// <inheritdoc/>
    protected override void WriteDescription(Description item)
    {
        Writer.Write("- ");
        Writer.Write(AnsiCodes.MakeBold(item.Term));
        Writer.Write(": ");
        Writer.WriteLine(item.Text);

        if (item.Detail != null)
        {
            Writer.Write("  ");
            _terminal.SetColor(TerminalColor.DarkGray);
            Writer.WriteLine(item.Detail);
            _terminal.ResetColor();
        }
    }

    // ── Callouts ──

    /// <inheritdoc/>
    public override void WriteCallout(CalloutSeverity severity, string message)
    {
        if (SectionExcluded || ShapeUnsupported(MarkoutShape.Callouts))
            return;

        if (HasContent)
            NeedsBlankLine = true;
        EnsureBlankLineIfNeeded();

        var (label, color) = severity switch
        {
            CalloutSeverity.Note => ("NOTE", TerminalColor.Cyan),
            CalloutSeverity.Tip => ("TIP", TerminalColor.Green),
            CalloutSeverity.Important => ("IMPORTANT", TerminalColor.Magenta),
            CalloutSeverity.Warning => ("WARNING", TerminalColor.Yellow),
            CalloutSeverity.Caution => ("CAUTION", TerminalColor.Red),
            _ => (severity.ToString().ToUpperInvariant(), TerminalColor.White)
        };

        _terminal.SetColor(color);
        Writer.Write(AnsiCodes.MakeBold(label));
        _terminal.ResetColor();
        Writer.Write(": ");
        Writer.WriteLine(message);

        NeedsBlankLine = true;
        HasContent = true;
    }

    // ── Quotation ──

    /// <inheritdoc/>
    public override void WriteQuotation(string text)
    {
        if (SectionExcluded || ShapeUnsupported(MarkoutShape.Quotation))
            return;

        if (HasContent)
            NeedsBlankLine = true;
        EnsureBlankLineIfNeeded();

        _terminal.SetColor(TerminalColor.DarkGray);
        Writer.Write("│ ");
        _terminal.ResetColor();

        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
            {
                _terminal.SetColor(TerminalColor.DarkGray);
                Writer.Write("│ ");
                _terminal.ResetColor();
            }
            Writer.WriteLine(lines[i]);
        }

        NeedsBlankLine = true;
        HasContent = true;
    }

    // ── Rule ──

    /// <inheritdoc/>
    public override void WriteRule()
    {
        if (SectionExcluded)
            return;

        if (HasContent)
            NeedsBlankLine = true;
        EnsureBlankLineIfNeeded();

        _terminal.SetColor(TerminalColor.DarkGray);
        Writer.WriteLine("────────────────────────────────");
        _terminal.ResetColor();

        NeedsBlankLine = true;
        HasContent = true;
    }

    // ── Breakdowns ──

    // Colors cycle for distribution segments (most severe → least)
    private static readonly TerminalColor[] DistributionColors =
        [TerminalColor.Red, TerminalColor.Yellow, TerminalColor.Cyan, TerminalColor.Green, TerminalColor.Magenta, TerminalColor.Blue];

    /// <inheritdoc/>
    protected override void WriteBreakdownRow(Breakdown item, List<string> categories, int labelWidth, double barScale, int maxBarChars = 0)
    {
        Writer.Write(AnsiCodes.MakeBold(item.Label.PadRight(labelWidth)));
        Writer.Write("  ");

        var barWidth = 0;
        foreach (var seg in item.Segments)
        {
            var catIndex = categories.IndexOf(seg.Category);
            var color = DistributionColors[catIndex % DistributionColors.Length];
            var width = Math.Max(0, (int)Math.Round(seg.Count * barScale));
            _terminal.SetColor(color);
            Writer.Write(new string('█', width));
            _terminal.ResetColor();
            barWidth += width;
        }

        if (maxBarChars > barWidth)
            Writer.Write(new string(' ', maxBarChars - barWidth));

        Writer.Write("  ");
        _terminal.SetColor(TerminalColor.DarkGray);
        var parts = item.Segments.Where(s => s.Count > 0).Select(s => $"{s.Count} {s.Category}");
        Writer.WriteLine(string.Join(", ", parts));
        _terminal.ResetColor();
    }

    /// <inheritdoc/>
    protected override void WriteBreakdownLegend(List<string> categories)
    {
        for (int i = 0; i < categories.Count; i++)
        {
            if (i > 0) Writer.Write("  ");
            var color = DistributionColors[i % DistributionColors.Length];
            _terminal.SetColor(color);
            Writer.Write('█');
            _terminal.ResetColor();
            Writer.Write($" {categories[i]}");
        }
        Writer.WriteLine();
    }

    // ── Bar charts ──

    /// <inheritdoc/>
    protected override void WriteMetricBar(Metric item, int labelWidth, int maxBarWidth, double maxValue, int valueWidth)
    {
        // Label in bold
        Writer.Write(AnsiCodes.MakeBold(item.Label.PadRight(labelWidth)));
        Writer.Write("  ");

        // Bar in color
        var ratio = item.Value / maxValue;
        var fullBlocks = (int)(ratio * maxBarWidth);
        var fraction = (ratio * maxBarWidth) - fullBlocks;
        var halfBlock = fraction >= 0.5;

        _terminal.SetColor(BarColor);
        Writer.Write(new string('█', fullBlocks));
        if (halfBlock) Writer.Write('▌');
        _terminal.ResetColor();

        // Value label
        var barWidth = fullBlocks + (halfBlock ? 1 : 0);
        var padding = maxBarWidth - barWidth + 1;
        Writer.Write(new string(' ', padding));
        _terminal.SetColor(BarValueColor);
        Writer.WriteLine(FormatBarValue(item.Value).PadLeft(valueWidth));
        _terminal.ResetColor();
    }

    // ── Trees (continued) ──

    /// <inheritdoc/>
    protected override void WriteVerticalMetricsRow(
        (string Label, string ValueStr, int Height, int ColWidth, int BarWidth)[] cols, int row)
    {
        for (int c = 0; c < cols.Length; c++)
        {
            if (c > 0) Writer.Write("  ");
            if (cols[c].Height >= row)
            {
                var pad = cols[c].ColWidth - cols[c].BarWidth;
                var leftPad = pad / 2;
                Writer.Write(new string(' ', leftPad));
                _terminal.SetColor(BarColor);
                Writer.Write(new string('█', cols[c].BarWidth));
                _terminal.ResetColor();
                Writer.Write(new string(' ', pad - leftPad));
            }
            else
            {
                Writer.Write(new string(' ', cols[c].ColWidth));
            }
        }
        Writer.WriteLine();
    }

    /// <inheritdoc/>
    protected override void WriteVerticalMetricsValue(string valueStr, int colWidth)
    {
        var pad = colWidth - valueStr.Length;
        var leftPad = pad / 2;
        Writer.Write(new string(' ', leftPad));
        _terminal.SetColor(BarValueColor);
        Writer.Write(valueStr);
        _terminal.ResetColor();
        Writer.Write(new string(' ', pad - leftPad));
    }

    // ── Trees (node rendering) ──

    /// <inheritdoc/>
    public override void WriteTree(params ReadOnlySpan<TreeNode> nodes)
    {
        if (nodes.Length == 0 || SectionExcluded) return;

        for (int i = 0; i < nodes.Length; i++)
        {
            var isLast = i == nodes.Length - 1;
            WriteAnsiTreeNode(nodes[i], "", isLast, 0);
        }
    }

    private void WriteAnsiTreeNode(TreeNode node, string prefix, bool isLast, int depth)
    {
        if (SectionExcluded)
            return;

        EnsureBlankLineIfNeeded();

        // Dim box-drawing characters
        _terminal.SetColor(TerminalColor.DarkGray);
        Writer.Write(prefix);
        Writer.Write(isLast ? "└─ " : "├─ ");
        _terminal.ResetColor();

        // Badge (as-is) + label colored by depth
        if (node.Badge != null && Options.IncludeBadges)
        {
            Writer.Write(node.Badge);
            Writer.Write(' ');
        }

        if (depth == 0)
        {
            _terminal.SetColor(TerminalColor.Cyan);
            Writer.WriteLine(AnsiCodes.MakeBold(node.Text));
            _terminal.ResetColor();
        }
        else if (depth == 1)
        {
            Writer.WriteLine(node.Text);
        }
        else
        {
            _terminal.SetColor(TerminalColor.DarkGray);
            Writer.WriteLine(node.Text);
            _terminal.ResetColor();
        }

        HasContent = true;

        if (node.Children != null && node.Children.Count > 0)
        {
            var childPrefix = prefix + (isLast ? "   " : "│  ");
            for (int i = 0; i < node.Children.Count; i++)
            {
                var isChildLast = i == node.Children.Count - 1;
                WriteAnsiTreeNode(node.Children[i], childPrefix, isChildLast, depth + 1);
            }
        }
    }
}
