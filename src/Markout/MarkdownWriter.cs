using System.Text;

namespace Markout;

/// <summary>
/// A MarkoutWriter that renders output as Markdown.
/// Produces # headings, **bold** field names, | pipe tables |, - bullet lists,
/// ``` code blocks, and trailing double-space hard line breaks.
/// </summary>
public class MarkdownWriter : MarkoutWriter
{
    private static readonly string[] HeadingPrefixes = ["", "#", "##", "###", "####", "#####", "######"];

    /// <summary>
    /// Creates a writer that builds Markdown output in memory with default options.
    /// Use ToString() to get the result.
    /// </summary>
    public MarkdownWriter() : base()
    {
    }

    /// <summary>
    /// Creates a writer that builds Markdown output in memory with the specified options.
    /// Use ToString() to get the result.
    /// </summary>
    public MarkdownWriter(MarkoutWriterOptions options) : base(options)
    {
    }

    /// <summary>
    /// Creates a writer that writes Markdown to the specified TextWriter with default options.
    /// </summary>
    public MarkdownWriter(TextWriter writer) : base(writer)
    {
    }

    /// <summary>
    /// Creates a writer that writes Markdown to the specified TextWriter with the specified options.
    /// </summary>
    public MarkdownWriter(TextWriter writer, MarkoutWriterOptions options) : base(writer, options)
    {
    }

    /// <summary>
    /// Creates a writer that writes Markdown to the specified Stream with default options.
    /// </summary>
    public MarkdownWriter(Stream stream) : base(stream)
    {
    }

    /// <summary>
    /// Creates a writer that writes Markdown to the specified Stream with the specified options.
    /// </summary>
    public MarkdownWriter(Stream stream, MarkoutWriterOptions options) : base(stream, options)
    {
    }

    /// <inheritdoc/>
    public override void WriteHeading(int level, string text, string? context)
    {
        if (level < 1 || level > 6)
            throw new ArgumentOutOfRangeException(nameof(level), "Heading level must be between 1 and 6.");

        UpdateSectionState(level, text);

        if (SectionExcluded)
            return;

        if (HasContent)
        {
            Writer.WriteLine();
        }

        Writer.Write(HeadingPrefixes[level]);
        Writer.Write(' ');
        Writer.Write(text);

        if (!string.IsNullOrEmpty(context))
        {
            Writer.Write(" (");
            Writer.Write(context);
            Writer.Write(')');
        }

        Writer.WriteLine();
        NeedsBlankLine = true;
        HasContent = true;
    }

    /// <inheritdoc/>
    protected override void WriteFieldName(string key)
    {
        if (BoldFieldNames)
        {
            Writer.Write("**");
            Writer.Write(key);
            Writer.Write(":** ");
        }
        else
        {
            Writer.Write(key);
            Writer.Write(": ");
        }
    }

    /// <inheritdoc/>
    public override void WriteField(string key, string? value)
    {
        if (SectionExcluded)
            return;

        EnsureBlankLineIfNeeded();
        WriteFieldName(key);
        Writer.Write(value ?? string.Empty);
        Writer.WriteLine("  "); // Two trailing spaces for markdown hard line break
        HasContent = true;
    }

    /// <inheritdoc/>
    public override void WriteField(string key, bool value)
    {
        if (SectionExcluded)
            return;

        EnsureBlankLineIfNeeded();
        WriteFieldName(key);
        Writer.Write(value ? "yes" : "no");
        Writer.WriteLine("  "); // Two trailing spaces for markdown hard line break
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
        Writer.WriteLine("  "); // Two trailing spaces for markdown hard line break
        HasContent = true;
    }

    /// <inheritdoc/>
    public override void WriteCodeBlockStart(string? language = null)
    {
        if (InCodeBlock)
            throw new InvalidOperationException("Cannot nest code blocks. End the current code block before starting a new one.");

        if (SectionExcluded)
        {
            InCodeBlock = true;
            return;
        }

        EnsureBlankLineIfNeeded();
        Writer.Write("```");
        if (!string.IsNullOrEmpty(language))
            Writer.Write(language);
        Writer.WriteLine();
        InCodeBlock = true;
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

        Writer.WriteLine("```");
        NeedsBlankLine = true;
    }

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

        // Header row
        Writer.Write('|');
        foreach (var header in headers)
        {
            Writer.Write(' ');
            Writer.Write(header);
            Writer.Write(" |");
        }
        Writer.WriteLine();

        // Separator row
        Writer.Write('|');
        foreach (var header in headers)
        {
            Writer.Write(' ');
            for (int i = 0; i < header.Length; i++)
                Writer.Write('-');
            Writer.Write(" |");
        }
        Writer.WriteLine();
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

        Writer.Write('|');
        foreach (var value in values)
        {
            Writer.Write(' ');
            Writer.Write(EscapeTableCell(value));
            Writer.Write(" |");
        }
        Writer.WriteLine();
    }

    /// <inheritdoc/>
    public override void WriteTableEnd()
    {
        InTable = false;
        if (!SectionExcluded)
        {
            if (TableRowsSkipped > 0)
                Writer.WriteLine($"\n... and {TableRowsSkipped} more");
            NeedsBlankLine = true;
        }
    }

    /// <inheritdoc/>
    public override void WriteArray(string key, IEnumerable<string>? items)
    {
        if (SectionExcluded)
            return;

        if (HasContent)
            NeedsBlankLine = true;
        EnsureBlankLineIfNeeded();

        if (BoldFieldNames)
        {
            Writer.Write("**");
            Writer.Write(key);
            Writer.WriteLine(":**");
        }
        else
        {
            Writer.Write(key);
            Writer.WriteLine(":");
        }

        WriteBulletItems(items);
    }

    /// <inheritdoc/>
    public override void WriteBarChart(IReadOnlyList<BarItem> items, int maxBarWidth = 30)
    {
        if (items.Count == 0 || SectionExcluded || ShapeUnsupported(MarkoutShape.BarCharts))
            return;

        EnsureBlankLineIfNeeded();
        Writer.WriteLine("```text");
        // Delegate to base rendering (which writes individual bar lines)
        var maxValue = 0.0;
        var maxLabelWidth = 0;
        var maxValueWidth = 0;
        foreach (var item in items)
        {
            if (item.Value > maxValue) maxValue = item.Value;
            if (item.Label.Length > maxLabelWidth) maxLabelWidth = item.Label.Length;
            var vw = FormatBarValue(item.Value).Length;
            if (vw > maxValueWidth) maxValueWidth = vw;
        }
        if (maxValue <= 0) maxValue = 1;

        foreach (var item in items)
            WriteBarLine(item, maxLabelWidth, maxBarWidth, maxValue, maxValueWidth);

        Writer.WriteLine("```");
        NeedsBlankLine = true;
        HasContent = true;
    }
}
