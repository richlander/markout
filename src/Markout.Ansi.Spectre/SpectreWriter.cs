using Markout.Formatting;
using Spectre.Console;

namespace Markout.Ansi.Spectre;

/// <summary>
/// A formatter that renders output as rich ANSI terminal text using Spectre.Console.
/// Drop-in alternative to Markout.Ansi.AnsiWriter for projects already using Spectre.
/// </summary>
public class SpectreWriter : MarkoutWriter, IMarkoutFormatter,
    IDocumentFormatter, IMetricsFormatter
{
    private const int ColumnGap = 2;
    private readonly IAnsiConsole _console;

    /// <summary>
    /// Creates a Spectre writer targeting the specified console with default options.
    /// </summary>
    public SpectreWriter(IAnsiConsole console) : base(new SpectreTextWriter(console))
    {
        _console = console;
    }

    /// <summary>
    /// Creates a Spectre writer targeting the specified console with the specified options.
    /// </summary>
    public SpectreWriter(IAnsiConsole console, MarkoutWriterOptions options) : base(new SpectreTextWriter(console), options)
    {
        _console = console;
    }

    /// <summary>
    /// Gets the console width for layout calculations.
    /// </summary>
    private int ConsoleWidth => _console.Profile.Width;

    /// <summary>
    /// Color for the heading label text. Default is white.
    /// </summary>
    public Color RuleLabelColor { get; set; } = Color.White;

    /// <summary>
    /// Color for rule lines when gradient is disabled. Default is grey.
    /// </summary>
    public Color RuleLineColor { get; set; } = Color.Grey;

    /// <summary>
    /// Whether to render rule lines as a gradient that fades toward the edges.
    /// Default is true.
    /// </summary>
    public bool RuleGradient { get; set; } = true;

    /// <summary>
    /// RGB color for the bright end of the gradient (nearest the label).
    /// Default is (0, 180, 180) — teal/cyan.
    /// </summary>
    public Color RuleGradientStart { get; set; } = new Color(0, 180, 180);

    /// <summary>
    /// RGB color for the dim end of the gradient (at the edges).
    /// Default is (0, 40, 50) — near-black teal.
    /// </summary>
    public Color RuleGradientEnd { get; set; } = new Color(0, 40, 50);

    /// <summary>
    /// Color for bar chart bars. Default is cyan.
    /// </summary>
    public Color BarColor { get; set; } = Color.Cyan1;

    /// <summary>
    /// Color for bar chart value labels. Default is grey.
    /// </summary>
    public Color BarValueColor { get; set; } = Color.Grey;

    /// <inheritdoc/>
    public override MarkoutShape SupportedShapes => MarkoutShape.All;

    // ── Formatter Interface Implementations ──
    // Pure rendering: write ANSI escape codes directly to the TextWriter w.

    private static void Sgr(TextWriter w, int code) => w.Write($"\x1b[{code}m");
    private static void SgrReset(TextWriter w) => w.Write("\x1b[m");
    private static void SgrBold(TextWriter w, string text) { w.Write("\x1b[1m"); w.Write(text); w.Write("\x1b[22m"); }
    private static void SgrRgb(TextWriter w, byte r, byte g, byte b) => w.Write($"\x1b[38;2;{r};{g};{b}m");
    private static void SgrColor(TextWriter w, Color c) => SgrRgb(w, c.R, c.G, c.B);

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

    void IMetricsFormatter.FormatBreakdown(TextWriter w, IReadOnlyList<Breakdown> items, int? maxBarWidth, bool uniformBarWidth, MarkoutWriterOptions options)
    {
        var categories = items.SelectMany(b => b.Segments).Select(s => s.Category).Distinct().ToList();
        var maxTotal = items.Max(b => b.Segments.Sum(s => s.Count));
        if (maxTotal == 0) return;

        var labelWidth = items.Max(b => b.Label.Length);
        var barWidth = maxBarWidth ?? (ConsoleWidth - labelWidth - 4);
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
        var valueWidth = items.Max(m => FormatBarValue(m.Value).Length);

        foreach (var item in items)
        {
            SgrBold(w, item.Label.PadRight(labelWidth));
            w.Write("  ");
            var ratio = item.Value / maxValue;
            var fullBlocks = (int)(ratio * maxBarWidth);
            var halfBlock = (ratio * maxBarWidth) - fullBlocks >= 0.5;
            SgrColor(w, BarColor);
            w.Write(new string('█', fullBlocks));
            if (halfBlock) w.Write('▌');
            SgrReset(w);
            var bw = fullBlocks + (halfBlock ? 1 : 0);
            w.Write(new string(' ', maxBarWidth - bw + 1));
            SgrColor(w, BarValueColor);
            w.Write(FormatBarValue(item.Value).PadLeft(valueWidth));
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
        int width = ConsoleWidth;
        string padded = $" {title} ";
        int remaining = width - padded.Length;

        if (remaining <= 0)
        {
            SgrColor(w, RuleLabelColor);
            SgrBold(w, title);
            SgrReset(w);
            return;
        }

        int left = remaining / 2;
        int right = remaining - left;

        FormatGradientLine(w, left, fadeInward: true);
        SgrColor(w, RuleLabelColor);
        SgrBold(w, padded);
        SgrReset(w);
        FormatGradientLine(w, right, fadeInward: false);
    }

    private void FormatGradientLine(TextWriter w, int length, bool fadeInward)
    {
        if (length <= 0) return;

        if (!RuleGradient)
        {
            SgrColor(w, RuleLineColor);
            w.Write(new string('─', length));
            SgrReset(w);
            return;
        }

        var (r1, g1, b1) = (RuleGradientStart.R, RuleGradientStart.G, RuleGradientStart.B);
        var (r2, g2, b2) = (RuleGradientEnd.R, RuleGradientEnd.G, RuleGradientEnd.B);

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

    // ── Markup helpers ──

    private void WriteMarkup(string markup) => _console.Markup(markup);

    private void WriteMarkupLine(string markup) => _console.MarkupLine(markup);

    private static string Esc(string text) => Markup.Escape(text);

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
            _console.WriteLine();

        var fullText = string.IsNullOrEmpty(context) ? text : $"{text} ({context})";

        if (level == 1)
        {
            WriteRule(fullText);
        }
        else
        {
            WriteMarkupLine($"[bold cyan]{Esc(fullText)}[/]");
        }

        NeedsBlankLine = true;
        HasContent = true;
    }

    private void WriteRule(string title)
    {
        int width = ConsoleWidth;
        string padded = $" {title} ";
        int remaining = width - padded.Length;

        if (remaining <= 0)
        {
            WriteMarkupLine($"[bold {RuleLabelColor.ToMarkup()}]{Esc(title)}[/]");
            return;
        }

        int left = remaining / 2;
        int right = remaining - left;

        WriteRuleLine(left, fadeInward: true);
        WriteMarkup($"[bold {RuleLabelColor.ToMarkup()}]{Esc(padded)}[/]");
        WriteRuleLine(right, fadeInward: false);
        _console.WriteLine();
    }

    private void WriteRuleLine(int length, bool fadeInward, char ch = '─')
    {
        if (length <= 0) return;

        if (!RuleGradient)
        {
            WriteMarkup($"[{RuleLineColor.ToMarkup()}]{new string(ch, length)}[/]");
            return;
        }

        var (r1, g1, b1) = (RuleGradientStart.R, RuleGradientStart.G, RuleGradientStart.B);
        var (r2, g2, b2) = (RuleGradientEnd.R, RuleGradientEnd.G, RuleGradientEnd.B);

        for (int i = 0; i < length; i++)
        {
            float t = fadeInward
                ? (float)i / Math.Max(length - 1, 1)
                : 1f - (float)i / Math.Max(length - 1, 1);

            byte r = (byte)(r2 + (r1 - r2) * t);
            byte g = (byte)(g2 + (g1 - g2) * t);
            byte b = (byte)(b2 + (b1 - b2) * t);

            WriteMarkup($"[rgb({r},{g},{b})]{ch}[/]");
        }
    }

    // ── Fields ──

    /// <inheritdoc/>
    protected override void WriteFieldName(string key)
    {
        WriteMarkup($"[bold]{Esc(key)}[/]: ");
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

        NeedsBlankLine = true;
    }

    // ── Tables ──

    /// <inheritdoc/>
    protected override void FlushStreamingTable(string[] headers, IList<string[]> rows, int skippedRows)
    {
        WriteTable(headers, rows);
        if (skippedRows > 0)
        {
            _console.WriteLine();
            WriteMarkupLine($"[grey]... and {skippedRows} more[/]");
        }
    }

    /// <inheritdoc/>
    public override void WriteTable(IEnumerable<string> headers, IEnumerable<string[]> rows)
    {
        if (SectionExcluded)
            return;

        var headerArray = headers as string[] ?? headers.ToArray();
        var rowList = rows as IList<string[]> ?? rows.ToList();

        var maxItems = Options.MaxItems;
        var visibleRows = maxItems.HasValue && rowList.Count > maxItems.Value
            ? rowList.Take(maxItems.Value).ToList()
            : rowList;
        var skipped = rowList.Count - visibleRows.Count;

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
                WriteMarkup($"[bold]{Esc(text.PadRight(widths[i] + ColumnGap))}[/]");
            else
                WriteMarkup($"[bold]{Esc(text)}[/]");
        }
        _console.WriteLine();

        // Separator
        for (int i = 0; i < headerArray.Length; i++)
        {
            if (i < headerArray.Length - 1)
                WriteMarkup($"[grey]{new string('─', widths[i]).PadRight(widths[i] + ColumnGap)}[/]");
            else
                WriteMarkup($"[grey]{new string('─', widths[i])}[/]");
        }
        _console.WriteLine();

        // Rows
        foreach (var row in visibleRows)
        {
            for (int i = 0; i < row.Length; i++)
            {
                if (i < row.Length - 1)
                    _console.Write(new Text(row[i].PadRight(widths[i] + ColumnGap)));
                else
                    _console.Write(new Text(row[i]));
            }
            _console.WriteLine();
        }

        if (skipped > 0)
        {
            _console.WriteLine();
            WriteMarkupLine($"[grey]... and {skipped} more[/]");
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

        WriteMarkupLine($"[bold]{Esc(key)}[/]:");

        WriteBulletItems(items);
    }

    /// <inheritdoc/>
    public override void WriteListItem(string text)
    {
        if (SectionExcluded)
            return;

        EnsureBlankLineIfNeeded();
        WriteMarkup("[grey]  • [/]");
        _console.WriteLine(text);
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
                WriteMarkup("[grey] │ [/]");

            WriteMarkup($"[bold]{Esc(fields[i].Key)}[/]: ");
            _console.Write(new Text(fields[i].Value ?? string.Empty));
        }

        _console.WriteLine();
        NeedsBlankLine = true;
        HasContent = true;
    }

    // ── Labeled lists ──

    /// <inheritdoc/>
    protected override void WriteDescription(Description item)
    {
        WriteMarkupLine($"- [bold]{Esc(item.Term)}:[/] {Esc(item.Text)}");

        if (item.Detail != null)
        {
            WriteMarkupLine($"  [grey]{Esc(item.Detail)}[/]");
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
            CalloutSeverity.Note => ("NOTE", "cyan"),
            CalloutSeverity.Tip => ("TIP", "green"),
            CalloutSeverity.Important => ("IMPORTANT", "purple"),
            CalloutSeverity.Warning => ("WARNING", "yellow"),
            CalloutSeverity.Caution => ("CAUTION", "red"),
            _ => (severity.ToString().ToUpperInvariant(), "white")
        };

        WriteMarkupLine($"[bold {color}]{label}[/]: {Esc(message)}");

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

        foreach (var line in text.Split('\n'))
        {
            WriteMarkupLine($"[grey]│[/] {Esc(line)}");
        }

        NeedsBlankLine = true;
        HasContent = true;
    }

    // ── Breakdowns ──

    private static readonly string[] DistributionSpectreColors = ["red", "yellow", "cyan", "green", "purple", "blue"];

    /// <inheritdoc/>
    protected override void WriteBreakdownRow(Breakdown item, List<string> categories, int labelWidth, double barScale, int maxBarChars = 0)
    {
        WriteMarkup($"[bold]{Esc(item.Label.PadRight(labelWidth))}[/]  ");

        var barWidth = 0;
        foreach (var seg in item.Segments)
        {
            var catIndex = categories.IndexOf(seg.Category);
            var color = DistributionSpectreColors[catIndex % DistributionSpectreColors.Length];
            var width = Math.Max(0, (int)Math.Round(seg.Count * barScale));
            WriteMarkup($"[{color}]{new string('█', width)}[/]");
            barWidth += width;
        }

        if (maxBarChars > barWidth)
            _console.Write(new Text(new string(' ', maxBarChars - barWidth)));

        var parts = item.Segments.Where(s => s.Count > 0).Select(s => $"{s.Count} {s.Category}");
        WriteMarkupLine($"  [grey]{Esc(string.Join(", ", parts))}[/]");
    }

    /// <inheritdoc/>
    protected override void WriteBreakdownLegend(List<string> categories)
    {
        for (int i = 0; i < categories.Count; i++)
        {
            if (i > 0) WriteMarkup("  ");
            var color = DistributionSpectreColors[i % DistributionSpectreColors.Length];
            WriteMarkup($"[{color}]█[/] {Esc(categories[i])}");
        }
        _console.WriteLine();
    }

    // ── Bar charts ──

    /// <inheritdoc/>
    protected override void WriteMetricBar(Metric item, int labelWidth, int maxBarWidth, double maxValue, int valueWidth)
    {
        WriteMarkup($"[bold]{Esc(item.Label.PadRight(labelWidth))}[/]  ");

        var ratio = item.Value / maxValue;
        var fullBlocks = (int)(ratio * maxBarWidth);
        var fraction = (ratio * maxBarWidth) - fullBlocks;
        var halfBlock = fraction >= 0.5;

        var bar = new string('█', fullBlocks) + (halfBlock ? "▌" : "");
        WriteMarkup($"[{BarColor.ToMarkup()}]{bar}[/]");

        var barWidth = fullBlocks + (halfBlock ? 1 : 0);
        var padding = maxBarWidth - barWidth + 1;
        _console.Write(new Text(new string(' ', padding)));
        WriteMarkupLine($"[{BarValueColor.ToMarkup()}]{FormatBarValue(item.Value).PadLeft(valueWidth)}[/]");
    }

    /// <inheritdoc/>
    protected override void WriteVerticalMetricsRow(
        (string Label, string ValueStr, int Height, int ColWidth, int BarWidth)[] cols, int row)
    {
        for (int c = 0; c < cols.Length; c++)
        {
            if (c > 0) _console.Write(new Text("  "));
            if (cols[c].Height >= row)
            {
                var pad = cols[c].ColWidth - cols[c].BarWidth;
                var leftPad = pad / 2;
                _console.Write(new Text(new string(' ', leftPad)));
                WriteMarkup($"[{BarColor.ToMarkup()}]{new string('█', cols[c].BarWidth)}[/]");
                _console.Write(new Text(new string(' ', pad - leftPad)));
            }
            else
            {
                _console.Write(new Text(new string(' ', cols[c].ColWidth)));
            }
        }
        _console.WriteLine();
    }

    /// <inheritdoc/>
    protected override void WriteVerticalMetricsValue(string valueStr, int colWidth)
    {
        var pad = colWidth - valueStr.Length;
        var leftPad = pad / 2;
        _console.Write(new Text(new string(' ', leftPad)));
        WriteMarkup($"[{BarValueColor.ToMarkup()}]{Esc(valueStr)}[/]");
        _console.Write(new Text(new string(' ', pad - leftPad)));
    }

    // ── Trees ──

    /// <inheritdoc/>
    public override void WriteTree(params ReadOnlySpan<TreeNode> nodes)
    {
        if (nodes.Length == 0 || SectionExcluded) return;

        for (int i = 0; i < nodes.Length; i++)
        {
            var isLast = i == nodes.Length - 1;
            WriteSpectreTreeNode(nodes[i], "", isLast, 0);
        }
    }

    private void WriteSpectreTreeNode(TreeNode node, string prefix, bool isLast, int depth)
    {
        if (SectionExcluded)
            return;

        EnsureBlankLineIfNeeded();

        // Dim box-drawing characters
        WriteMarkup($"[grey]{Esc(prefix)}{Esc(isLast ? "└─ " : "├─ ")}[/]");

        // Badge
        if (node.Badge != null && Options.IncludeBadges)
        {
            _console.Write(new Text(node.Badge + " "));
        }

        // Label colored by depth
        if (depth == 0)
            WriteMarkupLine($"[bold cyan]{Esc(node.Text)}[/]");
        else if (depth == 1)
            _console.WriteLine(node.Text);
        else
            WriteMarkupLine($"[grey]{Esc(node.Text)}[/]");

        HasContent = true;

        if (node.Children != null && node.Children.Count > 0)
        {
            var childPrefix = prefix + (isLast ? "   " : "│  ");
            for (int i = 0; i < node.Children.Count; i++)
            {
                var isChildLast = i == node.Children.Count - 1;
                WriteSpectreTreeNode(node.Children[i], childPrefix, isChildLast, depth + 1);
            }
        }
    }

    // ── Section framing ──

    /// <summary>
    /// Color for section panel borders. Default is grey.
    /// </summary>
    public Color PanelBorderColor { get; set; } = Color.Grey;

    /// <summary>
    /// Alignment of the title within the panel top border.
    /// Default is <see cref="Justify.Left"/>.
    /// </summary>
    public Justify PanelTitleAlignment { get; set; } = Justify.Left;

    private int _sectionDepth;

    /// <inheritdoc/>
    public override void WriteSectionStart(int level, string text, string? context = null)
    {
        if (level == 1)
        {
            _sectionDepth++;
            var fullText = string.IsNullOrEmpty(context) ? text : $"{text} ({context})";

            UpdateSectionState(level, text);

            if (SectionExcluded)
                return;

            if (HasContent)
                _console.WriteLine();

            // Draw top border with title: ╭── Title ──────────────────╮
            int width = ConsoleWidth;
            string padded = $" {fullText} ";
            int remaining = width - padded.Length - 2; // -2 for ╭ and ╮

            if (remaining <= 0)
            {
                WriteMarkupLine($"[{PanelBorderColor.ToMarkup()}]╭─[/] [bold {RuleLabelColor.ToMarkup()}]{Esc(fullText)}[/]");
            }
            else
            {
                int left, right;
                switch (PanelTitleAlignment)
                {
                    case Justify.Right:
                        left = remaining;
                        right = 0;
                        break;
                    case Justify.Center:
                        left = remaining / 2;
                        right = remaining - left;
                        break;
                    default: // Left
                        left = Math.Min(2, remaining);
                        right = remaining - left;
                        break;
                }

                WriteMarkup($"[{PanelBorderColor.ToMarkup()}]╭[/]");
                if (left > 0)
                {
                    if (PanelTitleAlignment == Justify.Left)
                        WriteMarkup($"[{PanelBorderColor.ToMarkup()}]{new string('─', left)}[/]");
                    else
                        WriteRuleLine(left, fadeInward: true);
                }
                WriteMarkup($"[bold {RuleLabelColor.ToMarkup()}]{Esc(padded)}[/]");
                if (right > 0)
                {
                    if (PanelTitleAlignment == Justify.Right)
                        WriteMarkup($"[{PanelBorderColor.ToMarkup()}]{new string('─', right)}[/]");
                    else
                        WriteRuleLine(right, fadeInward: false);
                }
                WriteMarkupLine($"[{PanelBorderColor.ToMarkup()}]╮[/]");
            }

            NeedsBlankLine = true;
            HasContent = true;
        }
        else
        {
            // Sub-sections use the regular heading style
            WriteHeading(level, text, context);
        }
    }

    /// <inheritdoc/>
    public override void WriteSectionEnd()
    {
        if (_sectionDepth > 0)
        {
            _sectionDepth--;

            if (SectionExcluded)
                return;

            _console.WriteLine();
            int width = ConsoleWidth;
            WriteMarkup($"[{PanelBorderColor.ToMarkup()}]╰[/]");
            WriteRuleLine(width - 2, fadeInward: true);
            WriteMarkupLine($"[{PanelBorderColor.ToMarkup()}]╯[/]");
            NeedsBlankLine = true;
        }
    }
}
