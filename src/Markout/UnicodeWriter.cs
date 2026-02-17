namespace Markout;

/// <summary>
/// A MarkoutWriter that renders output using Unicode box-drawing and block characters.
/// Provides a rich text presentation similar to Spectre but without dependencies or color.
/// Uses ─│├└╭╮╰╯ for borders, █▆▄▂ for bars, and other Unicode decorations.
/// </summary>
public class UnicodeWriter : MarkoutWriter
{
    private const int ColumnGap = 2;
    private int _consoleWidth = 80;

    /// <summary>
    /// Creates a Unicode writer targeting the specified TextWriter.
    /// </summary>
    public UnicodeWriter(TextWriter writer) : base(writer)
    {
    }

    /// <summary>
    /// Creates a Unicode writer with the specified options.
    /// </summary>
    public UnicodeWriter(TextWriter writer, MarkoutWriterOptions options) : base(writer, options)
    {
    }

    /// <summary>
    /// Gets or sets the console width for layout calculations. Default is 80.
    /// </summary>
    public int ConsoleWidth 
    { 
        get => _consoleWidth; 
        set => _consoleWidth = value > 0 ? value : 80; 
    }

    /// <summary>
    /// Gets or sets the character used for horizontal rules. Default is '─'.
    /// </summary>
    public char RuleCharacter { get; set; } = '─';

    /// <summary>
    /// Gets or sets the character used for bar charts. Default is '█'.
    /// </summary>
    public char BarCharacter { get; set; } = '█';

    /// <summary>
    /// Gets or sets whether to use box-drawing borders around sections. Default is false.
    /// </summary>
    public bool UseBoxBorders { get; set; } = false;

    /// <inheritdoc/>
    public override MarkoutShape SupportedShapes => MarkoutShape.All;

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
            // H2+: Simple bold text (no color)
            Writer.WriteLine(fullText);
        }

        NeedsBlankLine = true;
        HasContent = true;
    }

    private void WriteRule(string title)
    {
        string padded = $" {title} ";
        int remaining = ConsoleWidth - padded.Length;

        if (remaining <= 0)
        {
            Writer.WriteLine(title);
            return;
        }

        int left = remaining / 2;
        int right = remaining - left;

        Writer.Write(new string(RuleCharacter, left));
        Writer.Write(padded);
        Writer.Write(new string(RuleCharacter, right));
        Writer.WriteLine();
    }

    // ── Fields ──

    /// <inheritdoc/>
    public override void WriteField(string key, string? value)
    {
        if (SectionExcluded)
            return;

        EnsureBlankLineIfNeeded();
        WriteFieldName(key);
        WriteFormattedValue(value);
        Writer.WriteLine();
        HasContent = true;
    }

    protected override void WriteFieldName(string key)
    {
        Writer.Write(key);
        Writer.Write(": ");
    }

    private void WriteFormattedValue(string? value)
    {
        Writer.Write(value ?? "");
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
            Writer.WriteLine($"\n... and {skippedRows} more");
    }

    /// <inheritdoc/>
    public override void WriteTable(IEnumerable<string> headers, IEnumerable<string[]> rows)
    {
        if (SectionExcluded)
            return;

        var headerArray = headers as string[] ?? headers.ToArray();
        var rowList = rows as IList<string[]> ?? rows.ToList();

        // Apply MaxItems
        var maxItems = Options.MaxItems;
        var visibleRows = maxItems.HasValue && rowList.Count > maxItems.Value
            ? rowList.Take(maxItems.Value).ToList()
            : rowList;
        var skipped = rowList.Count - visibleRows.Count;

        // Calculate column widths
        var widths = new int[headerArray.Length];
        for (int i = 0; i < headerArray.Length; i++)
            widths[i] = headerArray[i].Length;
        foreach (var row in visibleRows)
        {
            for (int i = 0; i < Math.Min(row.Length, widths.Length); i++)
                widths[i] = Math.Max(widths[i], row[i].Length);
        }

        EnsureBlankLineIfNeeded();

        // Headers (uppercase)
        for (int i = 0; i < headerArray.Length; i++)
        {
            var text = headerArray[i].ToUpperInvariant();
            if (i < headerArray.Length - 1)
                Writer.Write(text.PadRight(widths[i] + ColumnGap));
            else
                Writer.Write(text);
        }
        Writer.WriteLine();

        // Separator with box-drawing characters
        for (int i = 0; i < headerArray.Length; i++)
        {
            if (i < headerArray.Length - 1)
                Writer.Write(new string(RuleCharacter, widths[i]).PadRight(widths[i] + ColumnGap));
            else
                Writer.Write(new string(RuleCharacter, widths[i]));
        }
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
            Writer.WriteLine($"\n... and {skipped} more");

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

        Writer.Write(key);
        Writer.WriteLine(":");

        if (items != null)
        {
            foreach (var item in items)
            {
                Writer.Write("  • ");
                Writer.WriteLine(item);
            }
        }

        HasContent = true;
    }



    /// <inheritdoc/>
    public override void WriteListItem(string text)
    {
        if (SectionExcluded)
            return;

        Writer.Write("• ");
        Writer.WriteLine(text);
        HasContent = true;
    }

    // ── Field list ──

    /// <inheritdoc/>
    public override void WriteFieldList(params MarkoutField[] fields)
    {
        if (SectionExcluded)
            return;

        EnsureBlankLineIfNeeded();

        int maxKeyWidth = fields.Max(f => f.Key.Length);

        foreach (var field in fields)
        {
            Writer.Write(field.Key.PadRight(maxKeyWidth));
            Writer.Write(" │ ");
            Writer.WriteLine(field.Value ?? "");
        }

        NeedsBlankLine = true;
        HasContent = true;
    }

    /// <inheritdoc/>
    public override void WriteFieldList(IReadOnlyList<MarkoutField> fields)
    {
        if (SectionExcluded)
            return;

        EnsureBlankLineIfNeeded();

        int maxKeyWidth = fields.Max(f => f.Key.Length);

        foreach (var field in fields)
        {
            Writer.Write(field.Key.PadRight(maxKeyWidth));
            Writer.Write(" │ ");
            Writer.WriteLine(field.Value ?? "");
        }

        NeedsBlankLine = true;
        HasContent = true;
    }

    // ── Trees ──

    /// <inheritdoc/>
    public override void WriteTree(IEnumerable<TreeNode>? nodes)
    {
        if (SectionExcluded)
            return;

        EnsureBlankLineIfNeeded();
        WriteTreeNodes(nodes, "");
        NeedsBlankLine = true;
    }

    private void WriteTreeNodes(IEnumerable<TreeNode>? nodes, string prefix)
    {
        if (nodes == null)
            return;

        var nodeList = nodes.ToList();
        for (int i = 0; i < nodeList.Count; i++)
        {
            var node = nodeList[i];
            bool isLast = i == nodeList.Count - 1;
            WriteTreeNode(node, prefix, isLast);
        }
    }

    private void WriteTreeNode(TreeNode node, string prefix, bool isLast)
    {
        // Write the connector and text
        Writer.Write(prefix);
        Writer.Write(isLast ? "└─ " : "├─ ");
        Writer.Write(node.Text);

        if (!string.IsNullOrEmpty(node.Badge) && IncludeBadges)
        {
            Writer.Write(" [");
            Writer.Write(node.Badge);
            Writer.Write("]");
        }

        Writer.WriteLine();
        HasContent = true;

        // Recurse for children
        if (node.Children != null && node.Children.Any())
        {
            var childPrefix = prefix + (isLast ? "   " : "│  ");
            WriteTreeNodes(node.Children, childPrefix);
        }
    }

    // ── Labeled lists ──

    /// <inheritdoc/>
    public override void WriteDescriptions(IReadOnlyList<Description> items)
    {
        if (SectionExcluded)
            return;

        EnsureBlankLineIfNeeded();

        foreach (var item in items)
        {
            Writer.Write("• ");
            Writer.Write(item.Term);
            if (!string.IsNullOrEmpty(item.Text))
            {
                Writer.Write(" — ");
                Writer.Write(item.Text);
            }
            Writer.WriteLine();
        }

        NeedsBlankLine = true;
        HasContent = true;
    }

    // ── Callouts ──

    /// <inheritdoc/>
    public override void WriteCallout(CalloutSeverity severity, string message)
    {
        if (SectionExcluded)
            return;

        EnsureBlankLineIfNeeded();

        string prefix = severity switch
        {
            CalloutSeverity.Note => "ℹ ",
            CalloutSeverity.Tip => "💡 ",
            CalloutSeverity.Important => "❗ ",
            CalloutSeverity.Warning => "⚠ ",
            CalloutSeverity.Caution => "✖ ",
            _ => "• "
        };

        Writer.Write(prefix);
        Writer.WriteLine(message);

        NeedsBlankLine = true;
        HasContent = true;
    }

    // ── Quotation ──

    /// <inheritdoc/>
    public override void WriteQuotation(string text)
    {
        if (SectionExcluded)
            return;

        EnsureBlankLineIfNeeded();

        Writer.Write("│ ");
        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
            {
                Writer.WriteLine();
                Writer.Write("│ ");
            }
            Writer.Write(lines[i].TrimEnd('\r'));
        }
        Writer.WriteLine();

        NeedsBlankLine = true;
        HasContent = true;
    }

    // ── Rule ──

    /// <inheritdoc/>
    public override void WriteRule()
    {
        if (SectionExcluded)
            return;

        EnsureBlankLineIfNeeded();

        int width = Math.Min(ConsoleWidth, 80);
        Writer.WriteLine(new string(RuleCharacter, width));

        NeedsBlankLine = true;
        HasContent = true;
    }

    // ── Breakdowns ──

    /// <inheritdoc/>
    public override void WriteBreakdown(IReadOnlyList<Breakdown> items, int? maxBarWidth = null, bool uniformBarWidth = true)
    {
        if (SectionExcluded)
            return;

        EnsureBlankLineIfNeeded();

        if (items == null || items.Count == 0)
            return;

        int availableWidth = maxBarWidth ?? (ConsoleWidth - 20);
        if (availableWidth < 10)
            availableWidth = 10;

        foreach (var item in items)
        {
            int total = item.Segments.Sum(s => s.Count);
            if (total <= 0)
                continue;

            Writer.Write(item.Label.PadRight(15));
            Writer.Write(" ");

            foreach (var segment in item.Segments)
            {
                double fraction = (double)segment.Count / total;
                int width = (int)Math.Round(fraction * availableWidth);
                if (width == 0 && fraction > 0)
                    width = 1;
                Writer.Write(new string(BarCharacter, width));
            }

            Writer.WriteLine();
        }

        NeedsBlankLine = true;
        HasContent = true;
    }

    // ── Bar charts ──

    /// <inheritdoc/>
    public override void WriteMetrics(IReadOnlyList<Metric> items, int maxBarWidth = 30)
    {
        if (SectionExcluded)
            return;

        EnsureBlankLineIfNeeded();

        if (items == null || items.Count == 0)
            return;

        double maxValue = items.Max(m => m.Value);
        if (maxValue <= 0)
            return;

        int maxLabelWidth = items.Max(m => m.Label.Length);

        foreach (var item in items)
        {
            double fraction = item.Value / maxValue;
            int width = (int)Math.Round(fraction * maxBarWidth);

            Writer.Write(item.Label.PadRight(maxLabelWidth));
            Writer.Write(" ");
            Writer.Write(new string(BarCharacter, width));
            Writer.Write(" ");
            Writer.WriteLine(item.Value.ToString());
        }

        NeedsBlankLine = true;
        HasContent = true;
    }

    // ── Vertical bar charts ──

    /// <inheritdoc/>
    public override void WriteVerticalMetrics(IReadOnlyList<Metric> items, int maxBarHeight = 10, int? barWidth = null)
    {
        if (SectionExcluded)
            return;

        EnsureBlankLineIfNeeded();

        if (items == null || items.Count == 0)
            return;

        double maxValue = items.Max(m => m.Value);
        if (maxValue <= 0)
            return;

        int colWidth = barWidth ?? 8;

        // Draw bars from top to bottom
        for (int row = maxBarHeight; row > 0; row--)
        {
            foreach (var item in items)
            {
                double fraction = item.Value / maxValue;
                int height = (int)Math.Round(fraction * maxBarHeight);

                if (height >= row)
                    Writer.Write(new string(BarCharacter, colWidth - 1).PadRight(colWidth));
                else
                    Writer.Write(new string(' ', colWidth));
            }
            Writer.WriteLine();
        }

        // Draw labels
        foreach (var item in items)
        {
            string label = item.Label;
            if (label.Length > colWidth - 1)
                label = label.Substring(0, colWidth - 1);
            Writer.Write(label.PadRight(colWidth));
        }
        Writer.WriteLine();

        NeedsBlankLine = true;
        HasContent = true;
    }
}
