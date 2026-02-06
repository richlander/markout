using System.Globalization;
using System.Text;

namespace Markout;

/// <summary>
/// Low-level writer for generating Markout output.
/// </summary>
/// <example>
///   <code lang="cs" source="../../samples/Serialization/WriterUsage.cs" region="UseMarkoutWriter" title="Basic writer usage" />
///   <code lang="cs" source="../../samples/Serialization/WriterUsage.cs" region="WriteTable" title="Table output" />
///   <code lang="cs" source="../../samples/Serialization/WriterUsage.cs" region="WriteTree" title="Tree output" />
/// </example>
/// <seealso href="../../samples/Serialization/WriterUsage.cs">Direct writer usage examples</seealso>
/// <seealso href="../../samples/Serialization/SectionFiltering.cs">Section filtering examples</seealso>
public sealed class MarkoutWriter
{
    private static readonly string[] HeadingPrefixes = ["", "#", "##", "###", "####", "#####", "######"];

    private readonly TextWriter _writer;
    private readonly MarkoutWriterOptions _options;
    private bool _needsBlankLine;
    private bool _hasContent;
    private bool _inTable;
    private bool _inCodeBlock;
    private string? _currentSectionName;
    private bool _sectionExcluded;

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
    public bool IncludeIcons => _options.IncludeIcons;

    /// <summary>
    /// Gets the current rendering context, indicating what Markdown constructs are valid.
    /// </summary>
    public MarkoutRenderContext CurrentContext =>
        _inCodeBlock ? MarkoutRenderContext.CodeBlock :
        _inTable ? MarkoutRenderContext.Table :
        MarkoutRenderContext.Block;

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

    private void WriteFormattedValue<T>(T value) where T : ISpanFormattable
    {
        // Use ISO 8601 round-trip format for date/time types
        ReadOnlySpan<char> format = value is DateTime or DateTimeOffset ? "O" : default;
        Span<char> buffer = stackalloc char[64];
        if (value.TryFormat(buffer, out int charsWritten, format, CultureInfo.InvariantCulture))
            _writer.Write(buffer[..charsWritten]);
        else
            _writer.Write(value.ToString(format.ToString(), CultureInfo.InvariantCulture));
    }

    private void WriteFieldName(string key)
    {
        if (BoldFieldNames)
        {
            _writer.Write("**");
            _writer.Write(key);
            _writer.Write(":** ");
        }
        else
        {
            _writer.Write(key);
            _writer.Write(": ");
        }
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
    public void WriteHeading(int level, string text, string? context)
    {
        if (level < 1 || level > 6)
            throw new ArgumentOutOfRangeException(nameof(level), "Heading level must be between 1 and 6.");

        // H2 starts a new section
        if (level == 2)
        {
            _currentSectionName = text;
            _sectionExcluded = !IsSectionIncluded();
        }

        if (_sectionExcluded)
            return;

        // Always add blank line before heading if there's content
        if (_hasContent)
        {
            _writer.WriteLine();
        }

        _writer.Write(HeadingPrefixes[level]);
        _writer.Write(' ');
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
    public void WriteParagraph(string? text)
    {
        if (string.IsNullOrEmpty(text) || _sectionExcluded)
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
    public void WriteCodeBlockStart(string? language = null)
    {
        if (_inCodeBlock)
            throw new InvalidOperationException("Cannot nest code blocks. End the current code block before starting a new one.");

        if (_sectionExcluded)
        {
            _inCodeBlock = true;
            return;
        }

        EnsureBlankLineIfNeeded();
        _writer.Write("```");
        if (!string.IsNullOrEmpty(language))
            _writer.Write(language);
        _writer.WriteLine();
        _inCodeBlock = true;
        _hasContent = true;
    }

    /// <summary>
    /// Ends a code block.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if not inside a code block.</exception>
    public void WriteCodeBlockEnd()
    {
        if (!_inCodeBlock)
            throw new InvalidOperationException("Cannot end a code block without starting one first.");

        _inCodeBlock = false;

        if (_sectionExcluded)
            return;

        _writer.WriteLine("```");
        _needsBlankLine = true;
    }

    /// <summary>
    /// Writes a key-value field with a string value.
    /// Uses trailing spaces for markdown hard line break.
    /// </summary>
    public void WriteField(string key, string? value)
    {
        if (_sectionExcluded)
            return;

        EnsureBlankLineIfNeeded();
        WriteFieldName(key);
        _writer.Write(value ?? string.Empty);
        _writer.WriteLine("  "); // Two trailing spaces for markdown hard line break
        _hasContent = true;
    }

    /// <summary>
    /// Writes a key-value field with a boolean value (yes/no).
    /// Uses trailing spaces for markdown hard line break.
    /// </summary>
    public void WriteField(string key, bool value)
    {
        if (_sectionExcluded)
            return;

        EnsureBlankLineIfNeeded();
        WriteFieldName(key);
        _writer.Write(value ? "yes" : "no");
        _writer.WriteLine("  "); // Two trailing spaces for markdown hard line break
        _hasContent = true;
    }

    /// <summary>
    /// Writes a key-value field with a formattable value (int, long, double, decimal, DateTime, DateTimeOffset, etc.).
    /// Uses trailing spaces for markdown hard line break.
    /// </summary>
    public void WriteField<T>(string key, T value) where T : ISpanFormattable
    {
        if (_sectionExcluded)
            return;

        EnsureBlankLineIfNeeded();
        WriteFieldName(key);
        WriteFormattedValue(value);
        _writer.WriteLine("  "); // Two trailing spaces for markdown hard line break
        _hasContent = true;
    }

    /// <summary>
    /// Writes a key-value field without trailing spaces (no markdown soft break).
    /// Use for LineBreaks layout where each field is on its own line.
    /// </summary>
    public void WriteFieldNoBreak(string key, string? value)
    {
        if (_sectionExcluded)
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
    public void WriteFieldNoBreak(string key, bool value)
    {
        if (_sectionExcluded)
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
    public void WriteFieldNoBreak<T>(string key, T value) where T : ISpanFormattable
    {
        if (_sectionExcluded)
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
    public void WriteListItem(string text)
    {
        if (_sectionExcluded)
            return;

        EnsureBlankLineIfNeeded();
        _writer.Write("- ");
        _writer.WriteLine(text);
        _hasContent = true;
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
    public void WriteCompactFields(params ReadOnlySpan<MarkoutField> fields)
    {
        if (_sectionExcluded || fields.Length == 0)
            return;

        EnsureBlankLineIfNeeded();

        for (int i = 0; i < fields.Length; i++)
        {
            if (i > 0)
                _writer.Write(" | ");

            _writer.Write(fields[i].Key);
            _writer.Write(": ");
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
    public void WriteCompactFields(IReadOnlyList<MarkoutField> fields)
    {
        if (_sectionExcluded || fields.Count == 0)
            return;

        EnsureBlankLineIfNeeded();

        for (int i = 0; i < fields.Count; i++)
        {
            if (i > 0)
                _writer.Write(" | ");

            _writer.Write(fields[i].Key);
            _writer.Write(": ");
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
    public void WriteFieldTable(IReadOnlyList<MarkoutField> fields)
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
    /// Writes an array field with string items as a markdown list.
    /// Always has a blank line before and after for proper markdown rendering.
    /// </summary>
    public void WriteArray(string key, IEnumerable<string>? items)
    {
        if (_sectionExcluded)
            return;

        // Always ensure blank line before array if there's prior content
        if (_hasContent)
            _needsBlankLine = true;
        EnsureBlankLineIfNeeded();

        if (BoldFieldNames)
        {
            _writer.Write("**");
            _writer.Write(key);
            _writer.WriteLine(":**");
        }
        else
        {
            _writer.Write(key);
            _writer.WriteLine(":");
        }

        WriteBulletItems(items);
    }

    /// <summary>
    /// Writes string items as a markdown bullet list (no label).
    /// Use after a heading when the section title serves as the label.
    /// </summary>
    public void WriteArray(IEnumerable<string>? items)
    {
        if (_sectionExcluded)
            return;

        if (_hasContent)
            _needsBlankLine = true;
        EnsureBlankLineIfNeeded();

        WriteBulletItems(items);
    }

    private void WriteBulletItems(IEnumerable<string>? items)
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
    public void WriteTableStart(params ReadOnlySpan<string> headers)
    {
        if (_inCodeBlock)
            throw new InvalidOperationException("Cannot start a table inside a code block.");

        if (_sectionExcluded)
        {
            _inTable = true; // Track state even when excluded
            return;
        }

        if (headers.Length == 0)
            throw new ArgumentException("At least one header is required.", nameof(headers));

        EnsureBlankLineIfNeeded();
        _inTable = true;

        // Header row
        _writer.Write('|');
        foreach (var header in headers)
        {
            _writer.Write(' ');
            _writer.Write(header);
            _writer.Write(" |");
        }
        _writer.WriteLine();

        // Separator row
        _writer.Write('|');
        foreach (var header in headers)
        {
            _writer.Write(' ');
            for (int i = 0; i < header.Length; i++)
                _writer.Write('-');
            _writer.Write(" |");
        }
        _writer.WriteLine();
        _hasContent = true;
    }

    /// <summary>
    /// Writes a table row with the given values.
    /// Pipe characters in values are automatically escaped.
    /// </summary>
    public void WriteTableRow(params ReadOnlySpan<string> values)
    {
        if (!_inTable)
            throw new InvalidOperationException("Cannot write table row without starting a table first.");

        if (_sectionExcluded)
            return;

        _writer.Write('|');
        foreach (var value in values)
        {
            _writer.Write(' ');
            _writer.Write(EscapeTableCell(value));
            _writer.Write(" |");
        }
        _writer.WriteLine();
    }

    private static string EscapeTableCell(string value)
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
    public void WriteTableEnd()
    {
        _inTable = false;
        if (!_sectionExcluded)
            _needsBlankLine = true;
    }

    /// <summary>
    /// Writes a complete table with headers and rows.
    /// </summary>
    /// <param name="headers">Column headers.</param>
    /// <param name="rows">Row data. Each row should have the same number of columns as headers.</param>
    public void WriteTable(IEnumerable<string> headers, IEnumerable<string[]> rows)
    {
        var headerArray = headers as string[] ?? headers.ToArray();
        WriteTableStart(headerArray);
        foreach (var row in rows)
        {
            WriteTableRow(row);
        }
        WriteTableEnd();
    }

    /// <summary>
    /// Writes a simple pair (two values separated by whitespace).
    /// </summary>
    public void WriteSimplePair(string name, string value, int nameWidth = 32)
    {
        if (_sectionExcluded)
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
    public void WriteTreeNode(string text, string prefix = "")
    {
        if (_sectionExcluded)
            return;

        EnsureBlankLineIfNeeded();
        _writer.Write(prefix);
        _writer.WriteLine(text);
        _hasContent = true;
    }

    /// <summary>
    /// Writes a tree structure from a list of TreeNode objects.
    /// </summary>
    public void WriteTree(IEnumerable<TreeNode>? nodes)
    {
        if (nodes == null || _sectionExcluded) return;
        
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
        if (node.Icon != null && _options.IncludeIcons)
        {
            _writer.Write(node.Icon);
            _writer.Write(' ');
        }
        _writer.WriteLine(node.Label);
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
    /// Writes a blank line.
    /// </summary>
    public void WriteBlankLine()
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

    private void EnsureBlankLineIfNeeded()
    {
        if (_needsBlankLine)
        {
            _writer.WriteLine();
            _needsBlankLine = false;
        }
    }
}
