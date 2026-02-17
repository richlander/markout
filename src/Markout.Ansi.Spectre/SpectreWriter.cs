using Spectre.Console;

namespace Markout.Ansi.Spectre;

/// <summary>
/// A MarkoutWriter that renders output as rich ANSI terminal text using Spectre.Console.
/// Drop-in alternative to Markout.Ansi.AnsiWriter for projects already using Spectre.
/// </summary>
public class SpectreWriter : MarkoutWriter
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

    private void WriteRuleLine(int length, bool fadeInward)
    {
        if (length <= 0) return;

        if (!RuleGradient)
        {
            WriteMarkup($"[{RuleLineColor.ToMarkup()}]{new string('─', length)}[/]");
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

            WriteMarkup($"[rgb({r},{g},{b})]─[/]");
        }
    }

    // ── Fields ──

    /// <inheritdoc/>
    protected override void WriteFieldName(string key)
    {
        WriteMarkup($"[bold]{Esc(key)}[/]: ");
    }

    /// <inheritdoc/>
    public override void WriteField(string key, string? value)
    {
        if (SectionExcluded)
            return;

        EnsureBlankLineIfNeeded();
        WriteFieldName(key);
        _console.WriteLine(value ?? string.Empty);
        HasContent = true;
    }

    /// <inheritdoc/>
    public override void WriteField(string key, bool value)
    {
        if (SectionExcluded)
            return;

        EnsureBlankLineIfNeeded();
        WriteFieldName(key);
        var color = value ? "green" : "red";
        WriteMarkupLine($"[{color}]{(value ? "yes" : "no")}[/]");
        HasContent = true;
    }

    /// <inheritdoc/>
    public override void WriteField<T>(string key, T value)
    {
        if (SectionExcluded)
            return;

        EnsureBlankLineIfNeeded();
        WriteFieldName(key);
        WriteFormattedValue(value);
        _console.WriteLine();
        HasContent = true;
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
    public override void WriteArray(string key, IEnumerable<string>? items)
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

    // ── Field list ──

    /// <inheritdoc/>
    public override void WriteFieldList(params MarkoutField[] fields)
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

    /// <inheritdoc/>
    public override void WriteFieldList(IReadOnlyList<MarkoutField> fields)
    {
        if (SectionExcluded || fields.Count == 0)
            return;

        EnsureBlankLineIfNeeded();

        for (int i = 0; i < fields.Count; i++)
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
    protected override void WriteLabeledListItem(LabeledItem item)
    {
        WriteMarkupLine($"- [bold]{Esc(item.Label)}:[/] {Esc(item.Description)}");

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

    // ── Distributions ──

    private static readonly string[] DistributionSpectreColors = ["red", "yellow", "cyan", "green", "purple", "blue"];

    /// <inheritdoc/>
    protected override void WriteDistributionRow(DistributionBar item, List<string> categories, int labelWidth, double barScale)
    {
        WriteMarkup($"[bold]{Esc(item.Label.PadRight(labelWidth))}[/]  ");

        foreach (var seg in item.Segments)
        {
            var catIndex = categories.IndexOf(seg.Category);
            var color = DistributionSpectreColors[catIndex % DistributionSpectreColors.Length];
            var width = Math.Max(0, (int)Math.Round(seg.Count * barScale));
            WriteMarkup($"[{color}]{new string('█', width)}[/]");
        }

        var parts = item.Segments.Where(s => s.Count > 0).Select(s => $"{s.Count} {s.Category}");
        WriteMarkupLine($"  [grey]{Esc(string.Join(", ", parts))}[/]");
    }

    /// <inheritdoc/>
    protected override void WriteDistributionLegend(List<string> categories)
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
    protected override void WriteBarLine(BarItem item, int labelWidth, int maxBarWidth, double maxValue, int valueWidth)
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
    protected override void WriteVerticalBarRow(
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
    protected override void WriteVerticalBarValue(string valueStr, int colWidth)
    {
        var pad = colWidth - valueStr.Length;
        var leftPad = pad / 2;
        _console.Write(new Text(new string(' ', leftPad)));
        WriteMarkup($"[{BarValueColor.ToMarkup()}]{Esc(valueStr)}[/]");
        _console.Write(new Text(new string(' ', pad - leftPad)));
    }

    // ── Trees ──

    /// <inheritdoc/>
    public override void WriteTree(IEnumerable<TreeNode>? nodes)
    {
        if (nodes == null || SectionExcluded) return;

        var nodeList = nodes as IList<TreeNode> ?? [.. nodes];
        for (int i = 0; i < nodeList.Count; i++)
        {
            var isLast = i == nodeList.Count - 1;
            WriteSpectreTreeNode(nodeList[i], "", isLast, 0);
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

        // Text colored by depth
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
}
