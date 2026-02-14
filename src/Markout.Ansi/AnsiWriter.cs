using Microsoft.Extensions.Terminal;

namespace Markout.Ansi;

/// <summary>
/// A MarkoutWriter that renders output as rich ANSI terminal text.
/// Uses bold, color, and unicode characters for human-friendly terminal output.
/// </summary>
public class AnsiWriter : MarkoutWriter
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
            Writer.Write(AnsiCodes.MakeBold(title));
            Writer.WriteLine();
            return;
        }

        int left = remaining / 2;
        int right = remaining - left;

        _terminal.SetColor(TerminalColor.DarkGray);
        Writer.Write(new string('─', left));
        _terminal.ResetColor();
        Writer.Write(AnsiCodes.MakeBold(padded));
        _terminal.SetColor(TerminalColor.DarkGray);
        Writer.Write(new string('─', right));
        _terminal.ResetColor();
        Writer.WriteLine();
    }

    // ── Fields ──

    /// <inheritdoc/>
    protected override void WriteFieldName(string key)
    {
        Writer.Write(AnsiCodes.MakeBold(key));
        Writer.Write(": ");
    }

    /// <inheritdoc/>
    public override void WriteField(string key, string? value)
    {
        if (SectionExcluded)
            return;

        EnsureBlankLineIfNeeded();
        WriteFieldName(key);
        Writer.WriteLine(value ?? string.Empty);
        HasContent = true;
    }

    /// <inheritdoc/>
    public override void WriteField(string key, bool value)
    {
        if (SectionExcluded)
            return;

        EnsureBlankLineIfNeeded();
        WriteFieldName(key);
        _terminal.SetColor(value ? TerminalColor.Green : TerminalColor.Red);
        Writer.Write(value ? "yes" : "no");
        _terminal.ResetColor();
        Writer.WriteLine();
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
        Writer.WriteLine();
        HasContent = true;
    }

    // ── Code blocks ──

    /// <inheritdoc/>
    public override void WriteCodeBlockStart(string? language = null)
    {
        if (InCodeBlock)
            throw new InvalidOperationException("Cannot nest code blocks. End the current code block before starting a new one.");

        InCodeBlock = true;

        if (SectionExcluded)
            return;

        EnsureBlankLineIfNeeded();
        _terminal.SetColor(TerminalColor.DarkGray);
        HasContent = true;
    }

    /// <inheritdoc/>
    public override void WriteCodeBlockEnd()
    {
        if (!InCodeBlock)
            throw new InvalidOperationException("Cannot end a code block without starting one first.");

        InCodeBlock = false;

        if (SectionExcluded)
            return;

        _terminal.ResetColor();
        NeedsBlankLine = true;
    }

    // ── Tables ──

    /// <inheritdoc/>
    public override void WriteTableStart(params string[] headers)
    {
        if (InCodeBlock)
            throw new InvalidOperationException("Cannot start a table inside a code block.");

        if (SectionExcluded)
        {
            InTable = true;
            return;
        }

        if (headers.Length == 0)
            throw new ArgumentException("At least one header is required.", nameof(headers));

        EnsureBlankLineIfNeeded();
        InTable = true;
        ResetTableRowTracking();

        // Bold uppercase headers, tab-separated (streaming — no width info)
        Writer.WriteLine(AnsiCodes.MakeBold(string.Join('\t', headers.Select(h => h.ToUpperInvariant()))));
        HasContent = true;
    }

    /// <inheritdoc/>
    public override void WriteTableRow(params string[] values)
    {
        if (!InTable)
            throw new InvalidOperationException("Cannot write table row without starting a table first.");

        if (SectionExcluded)
            return;

        if (!ShouldWriteTableRow())
            return;

        Writer.WriteLine(string.Join('\t', values));
    }

    /// <inheritdoc/>
    public override void WriteTableEnd()
    {
        InTable = false;
        if (!SectionExcluded)
        {
            if (TableRowsSkipped > 0)
            {
                _terminal.SetColor(TerminalColor.DarkGray);
                Writer.WriteLine($"\n... and {TableRowsSkipped} more");
                _terminal.ResetColor();
            }
            NeedsBlankLine = true;
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
    public override void WriteArray(string key, IEnumerable<string>? items)
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

    // ── Compact fields ──

    /// <inheritdoc/>
    public override void WriteCompactFields(params MarkoutField[] fields)
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

    /// <inheritdoc/>
    public override void WriteCompactFields(IReadOnlyList<MarkoutField> fields)
    {
        if (SectionExcluded || fields.Count == 0)
            return;

        EnsureBlankLineIfNeeded();

        for (int i = 0; i < fields.Count; i++)
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

    // ── Simple pairs ──

    /// <inheritdoc/>
    public override void WriteSimplePair(string name, string value, int nameWidth = 32)
    {
        if (SectionExcluded)
            return;

        EnsureBlankLineIfNeeded();
        _terminal.SetColor(TerminalColor.DarkGray);
        Writer.Write(name.PadRight(nameWidth));
        _terminal.ResetColor();
        Writer.WriteLine(value);
        HasContent = true;
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
            WriteAnsiTreeNode(nodeList[i], "", isLast, 0);
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

        // Icon (as-is) + label colored by depth
        if (node.Icon != null && Options.IncludeIcons)
        {
            Writer.Write(node.Icon);
            Writer.Write(' ');
        }

        if (depth == 0)
            Writer.WriteLine(AnsiCodes.MakeBold(node.Label));
        else if (depth == 1)
        {
            _terminal.SetColor(TerminalColor.Cyan);
            Writer.WriteLine(node.Label);
            _terminal.ResetColor();
        }
        else
        {
            _terminal.SetColor(TerminalColor.DarkGray);
            Writer.WriteLine(node.Label);
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
