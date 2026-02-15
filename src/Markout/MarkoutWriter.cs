using System.Globalization;
using System.Text;

namespace Markout;

/// <summary>
/// Base writer that defines the Markout shape vocabulary.
/// Produces plain-text output by default. Subclass to create renderers
/// for specific formats (e.g., <see cref="MarkdownWriter"/> for Markdown).
/// </summary>
/// <example>
///   <code lang="cs" source="../../samples/Serialization/WriterUsage.cs" region="UseMarkoutWriter" title="Basic writer usage" />
///   <code lang="cs" source="../../samples/Serialization/WriterUsage.cs" region="WriteTable" title="Table output" />
///   <code lang="cs" source="../../samples/Serialization/WriterUsage.cs" region="WriteTree" title="Tree output" />
/// </example>
/// <seealso href="../../samples/Serialization/WriterUsage.cs">Direct writer usage examples</seealso>
/// <seealso href="../../samples/Serialization/SectionFiltering.cs">Section filtering examples</seealso>
public class MarkoutWriter
{
    private readonly TextWriter _writer;
    private readonly MarkoutWriterOptions _options;
    private bool _needsBlankLine;
    private bool _hasContent;
    private bool _inTable;
    private bool _inCodeBlock;
    private string? _currentSectionName;
    private bool _sectionExcluded;
    private int _tableRowCount;
    private int _tableRowsSkipped;
    private MarkoutShape _warnedShapes;
    private string[]? _streamingHeaders;
    private List<string[]>? _streamingRows;

    /// <summary>
    /// Creates a writer that builds output in memory with default options.
    /// Use ToString() to get the result.
    /// </summary>
    public MarkoutWriter() : this(new StringWriter(), new MarkoutWriterOptions())
    {
    }

    /// <summary>
    /// Creates a writer that builds output in memory with the specified options.
    /// Use ToString() to get the result.
    /// </summary>
    public MarkoutWriter(MarkoutWriterOptions options) : this(new StringWriter(), options)
    {
    }

    /// <summary>
    /// Creates a writer that writes to the specified TextWriter with default options.
    /// </summary>
    public MarkoutWriter(TextWriter writer) : this(writer, new MarkoutWriterOptions())
    {
    }

    /// <summary>
    /// Creates a writer that writes to the specified TextWriter with the specified options.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if both IncludeSections and ExcludeSections are set.</exception>
    public MarkoutWriter(TextWriter writer, MarkoutWriterOptions options)
    {
        if (options.IncludeSections != null && options.ExcludeSections != null)
            throw new InvalidOperationException("Cannot set both IncludeSections and ExcludeSections. Use one or the other.");

        _writer = writer;
        _options = options;
    }

    /// <summary>
    /// Creates a writer that writes to the specified Stream with default options.
    /// </summary>
    public MarkoutWriter(Stream stream) : this(new StreamWriter(stream, Encoding.UTF8, leaveOpen: true), new MarkoutWriterOptions())
    {
    }

    /// <summary>
    /// Creates a writer that writes to the specified Stream with the specified options.
    /// </summary>
    public MarkoutWriter(Stream stream, MarkoutWriterOptions options) : this(new StreamWriter(stream, Encoding.UTF8, leaveOpen: true), options)
    {
    }

    /// <summary>
    /// Gets whether field names should be rendered in bold.
    /// </summary>
    public bool BoldFieldNames => _options.BoldFieldNames;

    /// <summary>
    /// Gets the sections to include (by heading name).
    /// </summary>
    public HashSet<string>? IncludeSections => _options.IncludeSections;

    /// <summary>
    /// Gets the sections to exclude (by heading name).
    /// </summary>
    public HashSet<string>? ExcludeSections => _options.ExcludeSections;

    /// <summary>
    /// Gets whether to include the description paragraph.
    /// </summary>
    public bool IncludeDescription => _options.IncludeDescription;

    /// <summary>
    /// Gets whether to include icons in tree nodes.
    /// </summary>
    public bool IncludeBadges => _options.IncludeBadges;

    /// <summary>
    /// Gets the shapes this writer supports. Unsupported shapes produce a
    /// runtime diagnostic and are skipped. Default is <see cref="MarkoutShape.All"/>.
    /// </summary>
    public virtual MarkoutShape SupportedShapes => MarkoutShape.All;

    /// <summary>
    /// Gets the current rendering context, indicating what Markdown constructs are valid.
    /// </summary>
    public MarkoutRenderContext CurrentContext =>
        _inCodeBlock ? MarkoutRenderContext.CodeBlock :
        _inTable ? MarkoutRenderContext.Table :
        MarkoutRenderContext.Block;

    /// <summary>
    /// Gets the underlying TextWriter. Available to subclasses for custom rendering.
    /// </summary>
    protected TextWriter Writer => _writer;

    /// <summary>
    /// Gets the writer options. Available to subclasses for reading configuration.
    /// </summary>
    protected MarkoutWriterOptions Options => _options;

    /// <summary>
    /// Gets or sets whether a blank line is needed before the next content.
    /// </summary>
    protected bool NeedsBlankLine { get => _needsBlankLine; set => _needsBlankLine = value; }

    /// <summary>
    /// Gets or sets whether any content has been written.
    /// </summary>
    protected bool HasContent { get => _hasContent; set => _hasContent = value; }

    /// <summary>
    /// Gets or sets whether output is currently inside a table.
    /// </summary>
    protected bool InTable { get => _inTable; set => _inTable = value; }

    /// <summary>
    /// Gets or sets whether output is currently inside a code block.
    /// </summary>
    protected bool InCodeBlock { get => _inCodeBlock; set => _inCodeBlock = value; }

    /// <summary>
    /// Gets whether the current section is excluded by filtering.
    /// </summary>
    protected bool SectionExcluded => _sectionExcluded;

    /// <summary>
    /// Increments the table row counter and returns true if the row should be written.
    /// When <see cref="MarkoutWriterOptions.MaxItems"/> is set, returns false once the
    /// limit is reached and tracks skipped rows for the ellipsis in <see cref="WriteTableEnd"/>.
    /// </summary>
    protected bool ShouldWriteTableRow()
    {
        if (_options.MaxItems is int max && _tableRowCount >= max)
        {
            _tableRowsSkipped++;
            return false;
        }
        _tableRowCount++;
        return true;
    }

    /// <summary>
    /// Resets the table row tracking counters. Call from <see cref="WriteTableStart"/> overrides.
    /// </summary>
    protected void ResetTableRowTracking()
    {
        _tableRowCount = 0;
        _tableRowsSkipped = 0;
    }

    /// <summary>
    /// Gets the number of table rows skipped due to <see cref="MarkoutWriterOptions.MaxItems"/>.
    /// </summary>
    protected int TableRowsSkipped => _tableRowsSkipped;

    /// <summary>
    /// Returns true if this writer supports the given shape.
    /// </summary>
    protected bool Supports(MarkoutShape shape) => (SupportedShapes & shape) != 0;

    /// <summary>
    /// Writes a diagnostic warning for an unsupported shape (once per shape) and returns true.
    /// Returns false if the shape is supported. Use as: <c>if (ShapeUnsupported(shape)) return;</c>
    /// </summary>
    protected bool ShapeUnsupported(MarkoutShape shape)
    {
        if (Supports(shape))
            return false;

        if ((_warnedShapes & shape) == 0)
        {
            _warnedShapes |= shape;
            Console.Error.WriteLine($"Warning: {GetType().Name} does not support {shape}");
        }
        return true;
    }

    /// <summary>
    /// Flushes any buffered output to the underlying stream.
    /// </summary>
    public void Flush() => _writer.Flush();

    private bool IsSectionIncluded()
    {
        // Content before first H2 (no section name) is always included
        if (_currentSectionName == null)
            return true;
        if (_options.IncludeSections != null && !_options.IncludeSections.Contains(_currentSectionName))
            return false;
        if (_options.ExcludeSections?.Contains(_currentSectionName) == true)
            return false;
        return true;
    }

    /// <summary>
    /// Writes a formatted value using ISpanFormattable.
    /// Available to subclasses for custom rendering.
    /// </summary>
    protected void WriteFormattedValue<T>(T value) where T : ISpanFormattable
    {
        // Use ISO 8601 round-trip format for date/time types
        ReadOnlySpan<char> format = value is DateTime or DateTimeOffset ? "O" : default;
        Span<char> buffer = stackalloc char[64];
        if (value.TryFormat(buffer, out int charsWritten, format, CultureInfo.InvariantCulture))
            _writer.Write(buffer[..charsWritten]);
        else
            _writer.Write(value.ToString(format.ToString(), CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Writes a field name.
    /// Override to customize how field names are rendered.
    /// </summary>
    protected virtual void WriteFieldName(string key)
    {
        _writer.Write(key);
        _writer.Write(": ");
    }

    /// <summary>
    /// Writes a heading at the specified level.
    /// </summary>
    /// <param name="level">Heading level (1-6).</param>
    /// <param name="text">Heading text.</param>
    public void WriteHeading(int level, string text)
    {
        WriteHeading(level, text, null);
    }

    /// <summary>
    /// Writes a heading at the specified level with optional context.
    /// </summary>
    /// <param name="level">Heading level (1-6).</param>
    /// <param name="text">Heading text.</param>
    /// <param name="context">Optional context to append in parentheses.</param>
    public virtual void WriteHeading(int level, string text, string? context)
    {
        if (level < 1 || level > 6)
            throw new ArgumentOutOfRangeException(nameof(level), "Heading level must be between 1 and 6.");

        UpdateSectionState(level, text);

        if (_sectionExcluded || ShapeUnsupported(MarkoutShape.Headings))
            return;

        // Always add blank line before heading if there's content
        if (_hasContent)
        {
            _writer.WriteLine();
        }

        _writer.Write(text);

        if (!string.IsNullOrEmpty(context))
        {
            _writer.Write(" (");
            _writer.Write(context);
            _writer.Write(')');
        }

        _writer.WriteLine();
        _needsBlankLine = true;
        _hasContent = true;
    }

    /// <summary>
    /// Writes a paragraph of text.
    /// </summary>
    /// <param name="text">The paragraph text.</param>
    public virtual void WriteParagraph(string? text)
    {
        if (string.IsNullOrEmpty(text) || _sectionExcluded || ShapeUnsupported(MarkoutShape.Paragraphs))
            return;

        EnsureBlankLineIfNeeded();
        _writer.WriteLine(text);
        _needsBlankLine = true;
        _hasContent = true;
    }

    /// <summary>
    /// Starts a code block with optional language specifier.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if already inside a code block.</exception>
    public virtual void WriteCodeBlockStart(string? language = null)
    {
        if (_inCodeBlock)
            throw new InvalidOperationException("Cannot nest code blocks. End the current code block before starting a new one.");

        _inCodeBlock = true;

        if (_sectionExcluded || ShapeUnsupported(MarkoutShape.CodeBlocks))
            return;

        EnsureBlankLineIfNeeded();
        _hasContent = true;
    }

    /// <summary>
    /// Ends a code block.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if not inside a code block.</exception>
    public virtual void WriteCodeBlockEnd()
    {
        if (!_inCodeBlock)
            throw new InvalidOperationException("Cannot end a code block without starting one first.");

        _inCodeBlock = false;

        if (_sectionExcluded)
            return;

        _needsBlankLine = true;
    }

    /// <summary>
    /// Writes a key-value field with a string value.
    /// </summary>
    public virtual void WriteField(string key, string? value)
    {
        if (_sectionExcluded || ShapeUnsupported(MarkoutShape.Fields))
            return;

        EnsureBlankLineIfNeeded();
        WriteFieldName(key);
        _writer.WriteLine(value ?? string.Empty);
        _hasContent = true;
    }

    /// <summary>
    /// Writes a key-value field with a boolean value (yes/no).
    /// </summary>
    public virtual void WriteField(string key, bool value)
    {
        if (_sectionExcluded || ShapeUnsupported(MarkoutShape.Fields))
            return;

        EnsureBlankLineIfNeeded();
        WriteFieldName(key);
        _writer.WriteLine(value ? "yes" : "no");
        _hasContent = true;
    }

    /// <summary>
    /// Writes a key-value field with a formattable value (int, long, double, decimal, DateTime, DateTimeOffset, etc.).
    /// </summary>
    public virtual void WriteField<T>(string key, T value) where T : ISpanFormattable
    {
        if (_sectionExcluded || ShapeUnsupported(MarkoutShape.Fields))
            return;

        EnsureBlankLineIfNeeded();
        WriteFieldName(key);
        WriteFormattedValue(value);
        _writer.WriteLine();
        _hasContent = true;
    }

    /// <summary>
    /// Writes a key-value field without trailing spaces (no markdown soft break).
    /// Use for LineBreaks layout where each field is on its own line.
    /// </summary>
    public virtual void WriteFieldNoBreak(string key, string? value)
    {
        if (_sectionExcluded || ShapeUnsupported(MarkoutShape.Fields))
            return;

        EnsureBlankLineIfNeeded();
        WriteFieldName(key);
        _writer.WriteLine(value ?? string.Empty);
        _hasContent = true;
    }

    /// <summary>
    /// Writes a key-value field without trailing spaces (no markdown soft break).
    /// Use for LineBreaks layout where each field is on its own line.
    /// </summary>
    public virtual void WriteFieldNoBreak(string key, bool value)
    {
        if (_sectionExcluded || ShapeUnsupported(MarkoutShape.Fields))
            return;

        EnsureBlankLineIfNeeded();
        WriteFieldName(key);
        _writer.WriteLine(value ? "yes" : "no");
        _hasContent = true;
    }

    /// <summary>
    /// Writes a key-value field without trailing spaces (no markdown soft break).
    /// Use for LineBreaks layout where each field is on its own line.
    /// </summary>
    public virtual void WriteFieldNoBreak<T>(string key, T value) where T : ISpanFormattable
    {
        if (_sectionExcluded || ShapeUnsupported(MarkoutShape.Fields))
            return;

        EnsureBlankLineIfNeeded();
        WriteFieldName(key);
        WriteFormattedValue(value);
        _writer.WriteLine();
        _hasContent = true;
    }

    /// <summary>
    /// Writes a single bullet list item.
    /// </summary>
    public virtual void WriteListItem(string text)
    {
        if (_sectionExcluded || ShapeUnsupported(MarkoutShape.Lists))
            return;

        EnsureBlankLineIfNeeded();
        _writer.Write("- ");
        _writer.WriteLine(text);
        _hasContent = true;
    }

    /// <summary>
    /// Writes a sequence of strings as bullet list items.
    /// </summary>
    public void WriteList(IEnumerable<string> items)
    {
        foreach (var item in items)
            WriteListItem(item);
    }

    /// <summary>
    /// Writes multiple key-value fields on a single line, separated by pipes.
    /// Useful for compact summary lines with essential metadata.
    /// </summary>
    /// <param name="fields">Fields to write.</param>
    /// <example>
    /// <code>
    /// writer.WriteCompactFields(
    ///     new MarkoutField("Type", "Library"),
    ///     new MarkoutField("TFM", "net8.0"),
    ///     new MarkoutField("Updated", "2026-01-15"));
    /// // Output: Type: Library | TFM: net8.0 | Updated: 2026-01-15
    /// </code>
    /// </example>
    public virtual void WriteCompactFields(params MarkoutField[] fields)
    {
        if (_sectionExcluded || fields.Length == 0 || ShapeUnsupported(MarkoutShape.CompactFields))
            return;

        EnsureBlankLineIfNeeded();

        for (int i = 0; i < fields.Length; i++)
        {
            if (i > 0)
                _writer.Write(" | ");

            WriteFieldName(fields[i].Key);
            _writer.Write(fields[i].Value ?? string.Empty);
        }

        _writer.WriteLine();
        _needsBlankLine = true;
        _hasContent = true;
    }

    /// <summary>
    /// Writes multiple key-value fields on a single line, separated by pipes.
    /// Useful for compact summary lines with essential metadata.
    /// </summary>
    /// <param name="fields">Fields to write.</param>
    public virtual void WriteCompactFields(IReadOnlyList<MarkoutField> fields)
    {
        if (_sectionExcluded || fields.Count == 0 || ShapeUnsupported(MarkoutShape.CompactFields))
            return;

        EnsureBlankLineIfNeeded();

        for (int i = 0; i < fields.Count; i++)
        {
            if (i > 0)
                _writer.Write(" | ");

            WriteFieldName(fields[i].Key);
            _writer.Write(fields[i].Value ?? string.Empty);
        }

        _writer.WriteLine();
        _needsBlankLine = true;
        _hasContent = true;
    }

    /// <summary>
    /// Writes fields as a two-column Property/Value table.
    /// </summary>
    /// <param name="fields">Fields to write as table rows.</param>
    public virtual void WriteFieldTable(IReadOnlyList<MarkoutField> fields)
    {
        if (fields.Count == 0)
            return;
        
        WriteTableStart("Property", "Value");
        for (int i = 0; i < fields.Count; i++)
        {
            WriteTableRow(fields[i].Key, fields[i].Value ?? string.Empty);
        }
        WriteTableEnd();
    }

    /// <summary>
    /// Writes an array field with string items as a list.
    /// </summary>
    public virtual void WriteArray(string key, IEnumerable<string>? items)
    {
        if (_sectionExcluded || ShapeUnsupported(MarkoutShape.Lists))
            return;

        if (_hasContent)
            _needsBlankLine = true;
        EnsureBlankLineIfNeeded();

        _writer.Write(key);
        _writer.WriteLine(":");

        WriteBulletItems(items);
    }

    /// <summary>
    /// Writes string items as a bullet list (no label).
    /// Use after a heading when the section title serves as the label.
    /// </summary>
    public virtual void WriteArray(IEnumerable<string>? items)
    {
        if (_sectionExcluded || ShapeUnsupported(MarkoutShape.Lists))
            return;

        if (_hasContent)
            _needsBlankLine = true;
        EnsureBlankLineIfNeeded();

        WriteBulletItems(items);
    }

    /// <summary>
    /// Writes items as a bullet list. Available to subclasses.
    /// </summary>
    protected void WriteBulletItems(IEnumerable<string>? items)
    {
        if (items != null)
        {
            foreach (var item in items)
            {
                _writer.Write("- ");
                _writer.WriteLine(item);
            }
        }

        _needsBlankLine = true;
        _hasContent = true;
    }

    /// <summary>
    /// Starts a table with the given headers.
    /// </summary>
    public virtual void WriteTableStart(params string[] headers)
    {
        if (_inCodeBlock)
            throw new InvalidOperationException("Cannot start a table inside a code block.");

        _inTable = true;
        _tableRowCount = 0;
        _tableRowsSkipped = 0;

        if (SectionExcluded || ShapeUnsupported(MarkoutShape.Tables))
            return;

        if (headers.Length == 0)
            throw new ArgumentException("At least one header is required.", nameof(headers));

        _streamingHeaders = headers;
        _streamingRows = [];
    }

    /// <summary>
    /// Writes a table row with the given values.
    /// </summary>
    public virtual void WriteTableRow(params string[] values)
    {
        if (!_inTable)
            throw new InvalidOperationException("Cannot write table row without starting a table first.");

        if (_sectionExcluded || !Supports(MarkoutShape.Tables))
            return;

        if (!ShouldWriteTableRow())
            return;

        _streamingRows?.Add(values);
    }

    /// <summary>
    /// Escapes pipe characters and newlines in table cell values.
    /// </summary>
    protected static string EscapeTableCell(string value)
    {
        // Escape pipe characters and newlines in table cells
        if (value.Contains('|') || value.Contains('\n') || value.Contains('\r'))
        {
            return value
                .Replace("|", "\\|")
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Replace("\r", " ");
        }
        return value;
    }

    /// <summary>
    /// Ends the current table.
    /// </summary>
    public virtual void WriteTableEnd()
    {
        _inTable = false;
        if (!_sectionExcluded && _streamingHeaders != null)
        {
            FlushStreamingTable(_streamingHeaders, _streamingRows ?? [], _tableRowsSkipped);
            _streamingHeaders = null;
            _streamingRows = null;
            _needsBlankLine = true;
        }
    }

    /// <summary>
    /// Renders buffered streaming table rows with space-padded columns.
    /// Rows have already been filtered by MaxItems via <see cref="ShouldWriteTableRow"/>.
    /// Override to customize streaming table rendering (e.g., add separators or colors).
    /// </summary>
    protected virtual void FlushStreamingTable(string[] headers, IList<string[]> rows, int skippedRows)
    {
        // Calculate column widths
        var widths = new int[headers.Length];
        for (int i = 0; i < headers.Length; i++)
            widths[i] = headers[i].Length;
        foreach (var row in rows)
        {
            for (int i = 0; i < Math.Min(row.Length, widths.Length); i++)
                widths[i] = Math.Max(widths[i], row[i].Length);
        }

        EnsureBlankLineIfNeeded();

        // Headers
        for (int i = 0; i < headers.Length; i++)
        {
            if (i < headers.Length - 1)
                _writer.Write(headers[i].PadRight(widths[i] + 2));
            else
                _writer.Write(headers[i]);
        }
        _writer.WriteLine();

        // Rows
        foreach (var row in rows)
        {
            for (int i = 0; i < row.Length; i++)
            {
                if (i < row.Length - 1)
                    _writer.Write(row[i].PadRight(widths[i] + 2));
                else
                    _writer.Write(row[i]);
            }
            _writer.WriteLine();
        }

        if (skippedRows > 0)
            _writer.WriteLine($"\n... and {skippedRows} more");

        _hasContent = true;
    }

    /// <summary>
    /// Writes a complete table with headers and rows.
    /// </summary>
    /// <param name="headers">Column headers.</param>
    /// <param name="rows">Row data. Each row should have the same number of columns as headers.</param>
    public virtual void WriteTable(IEnumerable<string> headers, IEnumerable<string[]> rows)
    {
        if (ShapeUnsupported(MarkoutShape.Tables))
            return;

        var headerArray = headers as string[] ?? headers.ToArray();
        var rowList = rows as IList<string[]> ?? rows.ToList();

        // Calculate column widths
        var widths = new int[headerArray.Length];
        for (int i = 0; i < headerArray.Length; i++)
            widths[i] = headerArray[i].Length;
        foreach (var row in rowList)
        {
            for (int i = 0; i < Math.Min(row.Length, widths.Length); i++)
                widths[i] = Math.Max(widths[i], row[i].Length);
        }

        // Apply MaxItems
        var maxItems = _options.MaxItems;
        var visibleRows = maxItems.HasValue && rowList.Count > maxItems.Value
            ? rowList.Take(maxItems.Value).ToList()
            : rowList;
        var skipped = rowList.Count - visibleRows.Count;

        EnsureBlankLineIfNeeded();

        // Headers
        for (int i = 0; i < headerArray.Length; i++)
        {
            if (i < headerArray.Length - 1)
                _writer.Write(headerArray[i].PadRight(widths[i] + 2));
            else
                _writer.Write(headerArray[i]);
        }
        _writer.WriteLine();

        // Rows
        foreach (var row in visibleRows)
        {
            for (int i = 0; i < row.Length; i++)
            {
                if (i < row.Length - 1)
                    _writer.Write(row[i].PadRight(widths[i] + 2));
                else
                    _writer.Write(row[i]);
            }
            _writer.WriteLine();
        }

        if (skipped > 0)
            _writer.WriteLine($"\n... and {skipped} more");

        _needsBlankLine = true;
        _hasContent = true;
    }

    /// <summary>
    /// Writes a simple pair (two values separated by whitespace).
    /// </summary>
    public virtual void WritePair(string name, string value, int nameWidth = 32)
    {
        if (_sectionExcluded || ShapeUnsupported(MarkoutShape.Pairs))
            return;

        EnsureBlankLineIfNeeded();
        _writer.Write(name.PadRight(nameWidth));
        _writer.WriteLine(value);
        _hasContent = true;
    }

    /// <summary>
    /// Writes a tree node with optional prefix for hierarchy.
    /// </summary>
    /// <param name="text">The node text.</param>
    /// <param name="prefix">The prefix for tree structure (e.g., "├─ ", "│  ").</param>
    public virtual void WriteTreeNode(string text, string prefix = "")
    {
        if (_sectionExcluded || ShapeUnsupported(MarkoutShape.Trees))
            return;

        EnsureBlankLineIfNeeded();
        _writer.Write(prefix);
        _writer.WriteLine(text);
        _hasContent = true;
    }

    /// <summary>
    /// Writes a tree structure from a list of TreeNode objects.
    /// </summary>
    public virtual void WriteTree(IEnumerable<TreeNode>? nodes)
    {
        if (nodes == null || _sectionExcluded || ShapeUnsupported(MarkoutShape.Trees)) return;
        
        var nodeList = nodes as IList<TreeNode> ?? [.. nodes];
        for (int i = 0; i < nodeList.Count; i++)
        {
            var isLast = i == nodeList.Count - 1;
            WriteTreeNodeRecursive(nodeList[i], "", isLast);
        }
    }

    private void WriteTreeNodeRecursive(TreeNode node, string prefix, bool isLast)
    {
        if (_sectionExcluded)
            return;

        EnsureBlankLineIfNeeded();
        _writer.Write(prefix);
        _writer.Write(isLast ? "└─ " : "├─ ");
        if (node.Badge != null && _options.IncludeBadges)
        {
            _writer.Write(node.Badge);
            _writer.Write(' ');
        }
        _writer.WriteLine(node.Text);
        _hasContent = true;
        
        if (node.Children != null && node.Children.Count > 0)
        {
            var childPrefix = prefix + (isLast ? "   " : "│  ");
            for (int i = 0; i < node.Children.Count; i++)
            {
                var isChildLast = i == node.Children.Count - 1;
                WriteTreeNodeRecursive(node.Children[i], childPrefix, isChildLast);
            }
        }
    }

    /// <summary>
    /// Writes a list of descriptions as a bullet list with bold terms.
    /// Each item renders as "- Label: Description" with an optional indented detail line.
    /// </summary>
    public virtual void WriteDescriptions(IReadOnlyList<Description> items)
    {
        if (items.Count == 0 || _sectionExcluded || ShapeUnsupported(MarkoutShape.Descriptions))
            return;

        if (_hasContent)
            _needsBlankLine = true;
        EnsureBlankLineIfNeeded();

        foreach (var item in items)
            WriteDescription(item);

        _needsBlankLine = true;
        _hasContent = true;
    }

    /// <summary>
    /// Renders a single description item. Override for custom styling (e.g., bold, color).
    /// </summary>
    protected virtual void WriteDescription(Description item)
    {
        _writer.Write("- ");
        _writer.Write(item.Term);
        _writer.Write(": ");
        _writer.WriteLine(item.Text);

        if (item.Detail != null)
        {
            _writer.Write("  ");
            _writer.WriteLine(item.Detail);
        }
    }

    /// <summary>
    /// Writes a callout/admonition block with a severity level and message.
    /// </summary>
    public virtual void WriteCallout(CalloutSeverity severity, string message)
    {
        if (_sectionExcluded || ShapeUnsupported(MarkoutShape.Callouts))
            return;

        if (_hasContent)
            _needsBlankLine = true;
        EnsureBlankLineIfNeeded();

        var label = severity.ToString().ToUpperInvariant();
        _writer.Write(label);
        _writer.Write(": ");
        _writer.WriteLine(message);

        _needsBlankLine = true;
        _hasContent = true;
    }

    // ── Blockquotes ──

    /// <summary>
    /// Writes a blockquote — a prose quotation distinct from code blocks.
    /// </summary>
    public virtual void WriteBlockquote(string text)
    {
        if (_sectionExcluded || ShapeUnsupported(MarkoutShape.Blockquotes))
            return;

        if (_hasContent)
            _needsBlankLine = true;
        EnsureBlankLineIfNeeded();

        foreach (var line in text.Split('\n'))
        {
            _writer.Write("  ");
            _writer.WriteLine(line);
        }

        _needsBlankLine = true;
        _hasContent = true;
    }

    // ── Horizontal Rule ──

    /// <summary>
    /// Writes a horizontal rule separator between content sections.
    /// </summary>
    public virtual void WriteHorizontalRule()
    {
        if (_sectionExcluded)
            return;

        if (_hasContent)
            _needsBlankLine = true;
        EnsureBlankLineIfNeeded();

        _writer.WriteLine("────────────────────────────────");

        _needsBlankLine = true;
        _hasContent = true;
    }

    // ── Matrices ──

    /// <summary>
    /// Writes a 2D matrix with row and column headers.
    /// </summary>
    /// <param name="rowHeaders">Labels for each row.</param>
    /// <param name="colHeaders">Labels for each column.</param>
    /// <param name="values">2D array of cell values [row, col]. Null cells render as empty.</param>
    public virtual void WriteMatrix(string[] rowHeaders, string[] colHeaders, string?[,] values)
    {
        if (_sectionExcluded || ShapeUnsupported(MarkoutShape.Matrices))
            return;

        if (rowHeaders.Length == 0 || colHeaders.Length == 0)
            return;

        if (_hasContent)
            _needsBlankLine = true;
        EnsureBlankLineIfNeeded();

        // Calculate column widths
        var colWidths = new int[colHeaders.Length + 1]; // +1 for row header column
        colWidths[0] = rowHeaders.Max(h => h.Length);
        for (int c = 0; c < colHeaders.Length; c++)
        {
            colWidths[c + 1] = colHeaders[c].Length;
            for (int r = 0; r < rowHeaders.Length; r++)
            {
                var val = values[r, c] ?? "";
                if (val.Length > colWidths[c + 1])
                    colWidths[c + 1] = val.Length;
            }
        }

        // Header row
        _writer.Write("".PadRight(colWidths[0]));
        for (int c = 0; c < colHeaders.Length; c++)
        {
            _writer.Write("  ");
            _writer.Write(colHeaders[c].PadRight(colWidths[c + 1]));
        }
        _writer.WriteLine();

        // Data rows
        for (int r = 0; r < rowHeaders.Length; r++)
        {
            _writer.Write(rowHeaders[r].PadRight(colWidths[0]));
            for (int c = 0; c < colHeaders.Length; c++)
            {
                _writer.Write("  ");
                _writer.Write((values[r, c] ?? "").PadRight(colWidths[c + 1]));
            }
            _writer.WriteLine();
        }

        _needsBlankLine = true;
        _hasContent = true;
    }

    // Shade characters for breakdown segments (decreasing intensity)
    private static readonly char[] BreakdownFills = ['█', '▓', '▒', '░'];

    /// <summary>
    /// Writes a breakdown chart showing proportional category composition per row.
    /// Each row is a stacked bar with segments rendered using shade characters.
    /// </summary>
    /// <param name="items">The breakdown rows.</param>
    /// <param name="maxBarWidth">Maximum bar width in characters. When null, 1 char = 1 count.</param>
    public virtual void WriteBreakdown(IReadOnlyList<Breakdown> items, int? maxBarWidth = null)
    {
        if (items.Count == 0 || _sectionExcluded || ShapeUnsupported(MarkoutShape.Breakdowns))
            return;

        if (_hasContent)
            _needsBlankLine = true;
        EnsureBlankLineIfNeeded();

        // Build category order from first appearance across all items
        var categories = new List<string>();
        foreach (var item in items)
        {
            foreach (var seg in item.Segments)
            {
                if (!categories.Contains(seg.Category))
                    categories.Add(seg.Category);
            }
        }

        // Calculate label width and max total count
        var maxLabelWidth = 0;
        var maxTotal = 0;
        foreach (var item in items)
        {
            if (item.Label.Length > maxLabelWidth) maxLabelWidth = item.Label.Length;
            var total = 0;
            foreach (var seg in item.Segments) total += seg.Count;
            if (total > maxTotal) maxTotal = total;
        }

        if (maxTotal == 0) maxTotal = 1;
        var barScale = maxBarWidth.HasValue ? (double)maxBarWidth.Value / maxTotal : 1.0;

        // Render rows
        foreach (var item in items)
            WriteBreakdownRow(item, categories, maxLabelWidth, barScale);

        // Legend
        _writer.WriteLine();
        WriteBreakdownLegend(categories);

        _needsBlankLine = true;
        _hasContent = true;
    }

    /// <summary>
    /// Renders a single breakdown row. Override for custom styling.
    /// </summary>
    protected virtual void WriteBreakdownRow(Breakdown item, List<string> categories, int labelWidth, double barScale)
    {
        _writer.Write(item.Label.PadRight(labelWidth));
        _writer.Write("  ");

        // Draw stacked bar
        foreach (var seg in item.Segments)
        {
            var catIndex = categories.IndexOf(seg.Category);
            var fill = BreakdownFills[catIndex % BreakdownFills.Length];
            var width = Math.Max(0, (int)Math.Round(seg.Count * barScale));
            _writer.Write(new string(fill, width));
        }

        // Summary
        _writer.Write("  ");
        var parts = item.Segments.Where(s => s.Count > 0).Select(s => $"{s.Count} {s.Category}");
        _writer.WriteLine(string.Join(", ", parts));
    }

    /// <summary>
    /// Renders the breakdown legend. Override for custom styling.
    /// </summary>
    protected virtual void WriteBreakdownLegend(List<string> categories)
    {
        var parts = new List<string>();
        for (int i = 0; i < categories.Count; i++)
        {
            var fill = BreakdownFills[i % BreakdownFills.Length];
            parts.Add($"{fill} {categories[i]}");
        }
        _writer.WriteLine(string.Join("  ", parts));
    }

    /// <summary>
    /// Writes horizontal metric bars from labeled measurements.
    /// Bars are scaled proportionally to the maximum value using block characters.
    /// </summary>
    /// <param name="items">The metrics to render.</param>
    /// <param name="maxBarWidth">Maximum width of the longest bar in characters. Default is 30.</param>
    public virtual void WriteMetrics(IReadOnlyList<Metric> items, int maxBarWidth = 30)
    {
        if (items.Count == 0 || _sectionExcluded || ShapeUnsupported(MarkoutShape.Metrics))
            return;

        EnsureBlankLineIfNeeded();

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
        {
            WriteMetricBar(item, maxLabelWidth, maxBarWidth, maxValue, maxValueWidth);
        }

        _needsBlankLine = true;
        _hasContent = true;
    }

    /// <summary>
    /// Renders a single bar line. Override for custom bar styling.
    /// </summary>
    protected virtual void WriteMetricBar(Metric item, int labelWidth, int maxBarWidth, double maxValue, int valueWidth)
    {
        _writer.Write(item.Label.PadRight(labelWidth));
        _writer.Write("  ");

        var ratio = item.Value / maxValue;
        var fullBlocks = (int)(ratio * maxBarWidth);
        var fraction = (ratio * maxBarWidth) - fullBlocks;
        var halfBlock = fraction >= 0.5;

        _writer.Write(new string('█', fullBlocks));
        if (halfBlock) _writer.Write('▌');

        // Pad to align the value column
        var barWidth = fullBlocks + (halfBlock ? 1 : 0);
        var padding = maxBarWidth - barWidth + 1;
        _writer.Write(new string(' ', padding));
        _writer.WriteLine(FormatBarValue(item.Value).PadLeft(valueWidth));
    }

    /// <summary>
    /// Formats the numeric value displayed at the end of a bar.
    /// </summary>
    protected static string FormatBarValue(double value)
    {
        return value == Math.Floor(value) ? ((int)value).ToString() : value.ToString("0.#");
    }

    /// <summary>
    /// Writes vertical metric bars from labeled measurements.
    /// Bars grow upward, with labels and values below.
    /// </summary>
    /// <param name="items">The metrics to render.</param>
    /// <param name="maxBarHeight">Maximum height of the tallest bar in rows. Default is 10.</param>
    /// <param name="barWidth">Width of each bar in characters. Null (default) fills the column width.</param>
    public virtual void WriteVerticalMetrics(IReadOnlyList<Metric> items, int maxBarHeight = 10, int? barWidth = null)
    {
        if (items.Count == 0 || _sectionExcluded || ShapeUnsupported(MarkoutShape.Metrics))
            return;

        EnsureBlankLineIfNeeded();
        WriteVerticalMetricsBody(items, maxBarHeight, barWidth);
        _needsBlankLine = true;
        _hasContent = true;
    }

    /// <summary>
    /// Renders the vertical metrics body (bars, values, labels).
    /// Override for custom styling of the entire vertical chart.
    /// </summary>
    protected virtual void WriteVerticalMetricsBody(IReadOnlyList<Metric> items, int maxBarHeight, int? barWidth)
    {
        var maxValue = 0.0;
        foreach (var item in items)
            if (item.Value > maxValue) maxValue = item.Value;
        if (maxValue <= 0) maxValue = 1;

        // Pre-compute column data
        var cols = new (string Label, string ValueStr, int Height, int ColWidth, int BarWidth)[items.Count];
        for (int i = 0; i < items.Count; i++)
        {
            var valueStr = FormatBarValue(items[i].Value);
            var height = (int)Math.Round(items[i].Value / maxValue * maxBarHeight);
            if (items[i].Value > 0 && height == 0) height = 1;
            var colWidth = Math.Max(items[i].Label.Length, Math.Max(valueStr.Length, barWidth ?? 1));
            var bw = barWidth ?? colWidth;
            cols[i] = (items[i].Label, valueStr, height, colWidth, bw);
        }

        // Bar rows (top to bottom)
        for (int row = maxBarHeight; row >= 1; row--)
        {
            WriteVerticalMetricsRow(cols, row);
        }

        // Value row
        for (int c = 0; c < cols.Length; c++)
        {
            if (c > 0) _writer.Write("  ");
            WriteVerticalMetricsValue(cols[c].ValueStr, cols[c].ColWidth);
        }
        _writer.WriteLine();

        // Label row
        for (int c = 0; c < cols.Length; c++)
        {
            if (c > 0) _writer.Write("  ");
            _writer.Write(cols[c].Label.PadRight(cols[c].ColWidth));
        }
        _writer.WriteLine();
    }

    /// <summary>
    /// Renders one row of vertical bars. Override for custom bar colors.
    /// </summary>
    protected virtual void WriteVerticalMetricsRow(
        (string Label, string ValueStr, int Height, int ColWidth, int BarWidth)[] cols, int row)
    {
        for (int c = 0; c < cols.Length; c++)
        {
            if (c > 0) _writer.Write("  ");
            if (cols[c].Height >= row)
            {
                var pad = cols[c].ColWidth - cols[c].BarWidth;
                var leftPad = pad / 2;
                _writer.Write(new string(' ', leftPad));
                _writer.Write(new string('█', cols[c].BarWidth));
                _writer.Write(new string(' ', pad - leftPad));
            }
            else
            {
                _writer.Write(new string(' ', cols[c].ColWidth));
            }
        }
        _writer.WriteLine();
    }

    /// <summary>
    /// Renders a single value label in the vertical chart value row.
    /// </summary>
    protected virtual void WriteVerticalMetricsValue(string valueStr, int colWidth)
    {
        var pad = colWidth - valueStr.Length;
        var leftPad = pad / 2;
        _writer.Write(new string(' ', leftPad));
        _writer.Write(valueStr);
        _writer.Write(new string(' ', pad - leftPad));
    }

    /// <summary>
    /// Writes a blank line.
    /// </summary>
    public virtual void WriteBlankLine()
    {
        if (_sectionExcluded)
            return;

        _writer.WriteLine();
        _needsBlankLine = false;
    }

    /// <summary>
    /// Returns the generated Markdown content.
    /// Only valid when using the default constructor (in-memory writer).
    /// </summary>
    public override string ToString()
    {
        if (_writer is StringWriter sw)
            return sw.ToString().TrimEnd();
        return base.ToString() ?? "";
    }

    /// <summary>
    /// Ensures a blank line is written before the next content if needed.
    /// Override to customize blank line behavior.
    /// </summary>
    protected virtual void EnsureBlankLineIfNeeded()
    {
        if (_needsBlankLine)
        {
            _writer.WriteLine();
            _needsBlankLine = false;
        }
    }

    /// <summary>
    /// Updates section tracking state for heading-based section filtering.
    /// Subclasses that override <see cref="WriteHeading(int, string, string?)"/> should call this
    /// at the start of their override to maintain section include/exclude behavior.
    /// </summary>
    /// <param name="level">Heading level (1-6).</param>
    /// <param name="text">Heading text (used as section name for H2 headings).</param>
    protected void UpdateSectionState(int level, string text)
    {
        if (level == 2)
        {
            _currentSectionName = text;
            _sectionExcluded = !IsSectionIncluded();
        }
    }
}
