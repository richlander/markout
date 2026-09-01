using Markout.Formatting;

namespace Markout;

/// <summary>
/// Formatter that renders output using Unicode box-drawing and block characters.
/// Provides a rich text presentation similar to Spectre but without dependencies or color.
/// Uses ─│├└╭╮╰╯ for borders, █▆▄▂ for bars, and other Unicode decorations.
/// </summary>
public class UnicodeFormatter : IMarkoutFormatter,
    IDocumentFormatter, IMetricsFormatter, IGlyphFormatter, IGraphFormatter, ITextDiffFormatter
{
    // ── IGraphFormatter ──

    /// <summary>
    /// Renders the graph as a tree rooted at the focus node, drawn with the same box-drawing
    /// connectors as <see cref="ITreeFormatter.FormatTree"/>.
    /// </summary>
    void IGraphFormatter.FormatGraph(TextWriter w, Graph graph, MarkoutWriterOptions options)
    {
        if (graph.IsEmpty)
            return;

        ((ITreeFormatter)this).FormatTree(w, GraphLowering.ToTree(graph).AsSpan(), options);
    }

    // ── ITextDiffFormatter ──

    void ITextDiffFormatter.FormatTextDiff(
        TextWriter w,
        MappedTextDiff diff,
        MarkoutWriterOptions options)
    {
        foreach (var line in MappedTextDiffLowering.ToDisplayLines(diff, options.TextDiffContextLines))
        {
            switch (line.Kind)
            {
                case TextDiffDisplayLineKind.Context:
                    w.Write($"{line.BeforeLine!.Value + 1,4} {line.AfterLine!.Value + 1,4}   ");
                    w.WriteLine(TextDiffFormatterHelpers.EscapeIntralineMarkerLiterals(line.BeforeText!));
                    break;

                case TextDiffDisplayLineKind.Removal:
                    w.Write($"{line.BeforeLine!.Value + 1,4}      - ");
                    w.WriteLine(TextDiffFormatterHelpers.FormatMarkedLine(diff, line, "[-", "-]"));
                    break;

                case TextDiffDisplayLineKind.Addition:
                    w.Write($"     {line.AfterLine!.Value + 1,4} + ");
                    w.WriteLine(TextDiffFormatterHelpers.FormatMarkedLine(diff, line, "{+", "+}"));
                    break;

                case TextDiffDisplayLineKind.Omission:
                    w.Write("         … ");
                    w.Write(line.BeforeRange!.Value.Count);
                    w.Write(" unchanged lines (Before ");
                    WriteRange(w, line.BeforeRange.Value);
                    w.Write(", After ");
                    WriteRange(w, line.AfterRange!.Value);
                    w.WriteLine(")");
                    break;

                case TextDiffDisplayLineKind.Annotation:
                    w.Write("         ↳ ");
                    w.Write(line.Annotation!.Severity);
                    w.Write(" — ");
                    w.Write(TextDiffFormatterHelpers.AnnotationTarget(
                        line.Annotation,
                        line.ChangeAddress));
                    w.Write(": ");
                    w.WriteLine(TextDiffEscaping.Human(line.Annotation.Text));
                    break;
            }
        }
    }

    private static void WriteRange(TextWriter w, TextDiffRange range)
    {
        w.Write(range.Start + 1);
        w.Write('–');
        w.Write(range.End);
    }

    private const int ColumnGap = 2;
    private int _consoleWidth = 80;

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

    // ── Explicit interface implementations (for orchestrator dispatch) ──

    void IHeadingFormatter.FormatHeading(TextWriter w, int level, string text, string? context)
    {
        var fullText = string.IsNullOrEmpty(context) ? text : $"{text} ({context})";

        if (level == 1)
        {
            string padded = $" {fullText} ";
            int remaining = ConsoleWidth - padded.Length;
            if (remaining <= 0)
            {
                w.Write(fullText);
            }
            else
            {
                int left = remaining / 2;
                int right = remaining - left;
                w.Write(new string(RuleCharacter, left));
                w.Write(padded);
                w.Write(new string(RuleCharacter, right));
            }
        }
        else
        {
            w.Write(fullText);
        }
    }

    void IFieldFormatter.FormatFieldName(TextWriter w, string key, bool bold)
    {
        w.Write(key);
        w.Write(": ");
    }

    void IFieldFormatter.FormatFields(TextWriter w, MarkoutField[] fields, bool bold)
        => ((IFieldFormatter)this).FormatFields(w, (ReadOnlySpan<MarkoutField>)fields, bold);

    void IFieldFormatter.FormatFields(TextWriter w, ReadOnlySpan<MarkoutField> fields, bool bold)
    {
        for (int i = 0; i < fields.Length; i++)
        {
            ((IFieldFormatter)this).FormatFieldName(w, fields[i].Key, bold);
            w.WriteLine(FormatHelper.RenderInlinePlainText(fields[i].Value));
        }
    }

    void ITableFormatter.FormatTable(TextWriter w, ReadOnlySpan<string> headers, IList<string[]> rows, int skippedRows, MarkoutWriterOptions options)
    {
        var widths = new int[headers.Length];
        for (int i = 0; i < headers.Length; i++)
            widths[i] = headers[i].Length;
        foreach (var row in rows)
        {
            for (int i = 0; i < Math.Min(row.Length, widths.Length); i++)
                widths[i] = Math.Max(widths[i], FormatHelper.RenderInlinePlainText(row[i]).Length);
        }

        // Headers
        for (int i = 0; i < headers.Length; i++)
        {
            var text = headers[i];
            if (i < headers.Length - 1)
                w.Write(text.PadRight(widths[i] + ColumnGap));
            else
                w.Write(text);
        }
        w.WriteLine();

        // Separator
        for (int i = 0; i < headers.Length; i++)
        {
            if (i < headers.Length - 1)
                w.Write(new string(RuleCharacter, widths[i]).PadRight(widths[i] + ColumnGap));
            else
                w.Write(new string(RuleCharacter, widths[i]));
        }
        w.WriteLine();

        // Rows
        foreach (var row in rows)
        {
            for (int i = 0; i < row.Length; i++)
            {
                if (i < row.Length - 1)
                    w.Write(FormatHelper.RenderInlinePlainText(row[i]).PadRight(widths[i] + ColumnGap));
                else
                    w.Write(FormatHelper.RenderInlinePlainText(row[i]));
            }
            w.WriteLine();
        }

        if (skippedRows > 0)
        {
            w.WriteLine();
            w.Write("... and ");
            w.Write(skippedRows);
            w.WriteLine(" more");
        }
    }

    void IListFormatter.FormatListItem(TextWriter w, string text)
    {
        w.Write("• ");
        w.WriteLine(FormatHelper.RenderInlinePlainText(text));
    }

    void IListFormatter.FormatArray(TextWriter w, string key, ReadOnlySpan<string> items, bool bold)
    {
        w.Write(key);
        w.WriteLine(":");
        foreach (var item in items)
        {
            w.Write("  • ");
            w.WriteLine(FormatHelper.RenderInlinePlainText(item));
        }
    }

    void ICodeBlockFormatter.FormatCodeStart(TextWriter w, string? language)
    {
        // Unicode writer uses no fencing — just a blank visual separator
    }

    void ICodeBlockFormatter.FormatCodeEnd(TextWriter w)
    {
        // Unicode writer uses no fencing
    }

    void IBlockFormatter.FormatCallout(TextWriter w, CalloutSeverity severity, string message)
    {
        string prefix = severity switch
        {
            CalloutSeverity.Note => "ℹ ",
            CalloutSeverity.Tip => "💡 ",
            CalloutSeverity.Important => "❗ ",
            CalloutSeverity.Warning => "⚠ ",
            CalloutSeverity.Caution => "✖ ",
            _ => "• "
        };
        w.Write(prefix);
        w.WriteLine(FormatHelper.RenderInlinePlainText(message));
    }

    void IBlockFormatter.FormatParagraph(TextWriter w, string text)
    {
        w.WriteLine(FormatHelper.RenderInlinePlainText(text));
    }

    void IBlockFormatter.FormatQuotation(TextWriter w, string text)
    {
        w.Write("│ ");
        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
            {
                w.WriteLine();
                w.Write("│ ");
            }
            w.Write(FormatHelper.RenderInlinePlainText(lines[i].TrimEnd('\r')));
        }
        w.WriteLine();
    }

    void IBlockFormatter.FormatRule(TextWriter w)
    {
        int width = Math.Min(ConsoleWidth, 80);
        w.WriteLine(new string(RuleCharacter, width));
    }

    void IBlockFormatter.FormatDescription(TextWriter w, Description item)
    {
        w.Write("• ");
        w.Write(item.Term);
        if (!string.IsNullOrEmpty(item.Text))
        {
            w.Write(" — ");
            w.Write(FormatHelper.RenderInlinePlainText(item.Text));
        }
        w.WriteLine();
    }

    void ITreeFormatter.FormatTree(TextWriter w, ReadOnlySpan<TreeNode> nodes, MarkoutWriterOptions options)
    {
        for (int i = 0; i < nodes.Length; i++)
            FormatTreeNodeRecursive(w, nodes[i], "", i == nodes.Length - 1, options);
    }

    void ITreeFormatter.FormatTreeNode(TextWriter w, string text, string prefix)
    {
        w.Write(prefix);
        w.WriteLine(FormatHelper.RenderInlinePlainText(text));
    }

    private void FormatTreeNodeRecursive(TextWriter w, TreeNode node, string prefix, bool isLast, MarkoutWriterOptions options)
    {
        w.Write(prefix);
        w.Write(isLast ? "└─ " : "├─ ");
        w.Write(MarkoutGlyphs.NodeStatePrefix(node.State, options, this));
        if (node.Badge != null && options.IncludeBadges)
        {
            w.Write(node.Badge);
            w.Write(' ');
        }
        w.WriteLine(FormatHelper.RenderInlinePlainText(node.Text));

        if (node.Children is { Count: > 0 })
        {
            var childPrefix = prefix + (isLast ? "   " : "│  ");
            for (int i = 0; i < node.Children.Count; i++)
                FormatTreeNodeRecursive(w, node.Children[i], childPrefix, i == node.Children.Count - 1, options);
        }
    }

    void IMetricsFormatter.FormatBreakdown(TextWriter w, IReadOnlyList<Breakdown> items, int? maxBarWidth, bool uniformBarWidth, MarkoutWriterOptions options)
    {
        int availableWidth = maxBarWidth ?? (ConsoleWidth - 20);
        if (availableWidth < 10)
            availableWidth = 10;

        foreach (var item in items)
        {
            int total = item.Slices.Sum(s => s.Count);
            if (total <= 0)
                continue;

            w.Write(item.Label.PadRight(15));
            w.Write(" ");

            foreach (var segment in item.Slices)
            {
                double fraction = (double)segment.Count / total;
                int width = (int)Math.Round(fraction * availableWidth);
                if (width == 0 && fraction > 0)
                    width = 1;
                w.Write(new string(BarCharacter, width));
            }

            w.WriteLine();
        }
    }

    void IMetricsFormatter.FormatMetrics(TextWriter w, IReadOnlyList<Metric> items, int maxBarWidth, MarkoutWriterOptions options)
    {
        double maxValue = items.Max(m => m.Value);
        if (maxValue <= 0)
            return;

        int maxLabelWidth = items.Max(m => m.Label.Length);

        foreach (var item in items)
        {
            double fraction = item.Value / maxValue;
            int width = (int)Math.Round(fraction * maxBarWidth);

            w.Write(item.Label.PadRight(maxLabelWidth));
            w.Write(" ");
            w.Write(new string(BarCharacter, width));
            w.Write(" ");
            w.WriteLine(item.Value.ToString());
        }
    }

    void IMetricsFormatter.FormatVerticalMetrics(TextWriter w, IReadOnlyList<Metric> items, int maxBarHeight, int? barWidth, MarkoutWriterOptions options)
    {
        double maxValue = items.Max(m => m.Value);
        if (maxValue <= 0)
            return;

        int colWidth = barWidth ?? 8;

        for (int row = maxBarHeight; row > 0; row--)
        {
            foreach (var item in items)
            {
                double fraction = item.Value / maxValue;
                int height = (int)Math.Round(fraction * maxBarHeight);

                if (height >= row)
                    w.Write(new string(BarCharacter, colWidth - 1).PadRight(colWidth));
                else
                    w.Write(new string(' ', colWidth));
            }
            w.WriteLine();
        }

        foreach (var item in items)
        {
            string label = item.Label;
            if (label.Length > colWidth - 1)
                label = label.Substring(0, colWidth - 1);
            w.Write(label.PadRight(colWidth));
        }
        w.WriteLine();
    }
}
