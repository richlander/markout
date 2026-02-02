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
    private readonly TextWriter _writer;
    private bool _needsBlankLine;
    private bool _hasContent;
    private bool _inTable;
    private int _currentSection;
    private bool _sectionExcluded;

    /// <summary>
    /// Creates a writer that builds output in memory.
    /// Use ToString() to get the result.
    /// </summary>
    public MarkoutWriter() : this(new StringWriter())
    {
    }

    /// <summary>
    /// Creates a writer that writes to the specified TextWriter.
    /// </summary>
    public MarkoutWriter(TextWriter writer)
    {
        _writer = writer;
    }

    /// <summary>
    /// Creates a writer that writes to the specified Stream.
    /// </summary>
    public MarkoutWriter(Stream stream) : this(new StreamWriter(stream, Encoding.UTF8, leaveOpen: true))
    {
    }

    /// <summary>
    /// Gets or sets whether field names should be rendered in bold.
    /// When true, field names are wrapped in ** for markdown bold formatting.
    /// </summary>
    public bool BoldFieldNames { get; set; }

    /// <summary>
    /// Gets or sets the sections to include (1-based, H2 boundaries).
    /// If set, only these sections are written. If null, all sections are included.
    /// </summary>
    public HashSet<int>? IncludeSections { get; set; }

    /// <summary>
    /// Gets or sets the sections to exclude (1-based, H2 boundaries).
    /// These sections are skipped even if in IncludeSections.
    /// </summary>
    public HashSet<int>? ExcludeSections { get; set; }

    /// <summary>
    /// Gets or sets whether to include the description paragraph.
    /// When false, the description is suppressed. Default is true.
    /// </summary>
    public bool IncludeDescription { get; set; } = true;

    /// <summary>
    /// Flushes any buffered output to the underlying stream.
    /// </summary>
    public void Flush() => _writer.Flush();

    private bool IsSectionIncluded()
    {
        // Content before first H2 (section 0) is always included
        if (_currentSection == 0)
            return true;
        if (IncludeSections?.Count > 0 && !IncludeSections.Contains(_currentSection))
            return false;
        if (ExcludeSections?.Contains(_currentSection) == true)
            return false;
        return true;
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
            _currentSection++;
            _sectionExcluded = !IsSectionIncluded();
        }

        if (_sectionExcluded)
            return;

        // Always add blank line before heading if there's content
        if (_hasContent)
        {
            _writer.WriteLine();
        }

        _writer.Write(new string('#', level));
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
    public void WriteCodeBlockStart(string? language = null)
    {
        if (_sectionExcluded)
            return;

        EnsureBlankLineIfNeeded();
        _writer.Write("```");
        if (!string.IsNullOrEmpty(language))
            _writer.Write(language);
        _writer.WriteLine();
        _hasContent = true;
    }

    /// <summary>
    /// Ends a code block.
    /// </summary>
    public void WriteCodeBlockEnd()
    {
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
    /// Writes a key-value field with an integer value.
    /// Uses trailing spaces for markdown hard line break.
    /// </summary>
    public void WriteField(string key, int value)
    {
        if (_sectionExcluded)
            return;

        EnsureBlankLineIfNeeded();
        WriteFieldName(key);
        _writer.Write(value.ToString(CultureInfo.InvariantCulture));
        _writer.WriteLine("  "); // Two trailing spaces for markdown hard line break
        _hasContent = true;
    }

    /// <summary>
    /// Writes a key-value field with a long value.
    /// Uses trailing spaces for markdown hard line break.
    /// </summary>
    public void WriteField(string key, long value)
    {
        if (_sectionExcluded)
            return;

        EnsureBlankLineIfNeeded();
        WriteFieldName(key);
        _writer.Write(value.ToString(CultureInfo.InvariantCulture));
        _writer.WriteLine("  "); // Two trailing spaces for markdown hard line break
        _hasContent = true;
    }

    /// <summary>
    /// Writes a key-value field with a double value.
    /// Uses trailing spaces for markdown hard line break.
    /// </summary>
    public void WriteField(string key, double value)
    {
        if (_sectionExcluded)
            return;

        EnsureBlankLineIfNeeded();
        WriteFieldName(key);
        _writer.Write(value.ToString(CultureInfo.InvariantCulture));
        _writer.WriteLine("  "); // Two trailing spaces for markdown hard line break
        _hasContent = true;
    }

    /// <summary>
    /// Writes a key-value field with a decimal value.
    /// Uses trailing spaces for markdown hard line break.
    /// </summary>
    public void WriteField(string key, decimal value)
    {
        if (_sectionExcluded)
            return;

        EnsureBlankLineIfNeeded();
        WriteFieldName(key);
        _writer.Write(value.ToString(CultureInfo.InvariantCulture));
        _writer.WriteLine("  "); // Two trailing spaces for markdown hard line break
        _hasContent = true;
    }

    /// <summary>
    /// Writes a key-value field with a DateTime value (ISO 8601 format).
    /// Uses trailing spaces for markdown hard line break.
    /// </summary>
    public void WriteField(string key, DateTime value)
    {
        if (_sectionExcluded)
            return;

        EnsureBlankLineIfNeeded();
        WriteFieldName(key);
        _writer.Write(value.ToString("O", CultureInfo.InvariantCulture));
        _writer.WriteLine("  "); // Two trailing spaces for markdown hard line break
        _hasContent = true;
    }

    /// <summary>
    /// Writes a key-value field with a DateTimeOffset value (ISO 8601 format).
    /// Uses trailing spaces for markdown hard line break.
    /// </summary>
    public void WriteField(string key, DateTimeOffset value)
    {
        if (_sectionExcluded)
            return;

        EnsureBlankLineIfNeeded();
        WriteFieldName(key);
        _writer.Write(value.ToString("O", CultureInfo.InvariantCulture));
        _writer.WriteLine("  "); // Two trailing spaces for markdown hard line break
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
    public void WriteCompactFields(params MarkoutField[] fields)
        => WriteCompactFields((IReadOnlyList<MarkoutField>)fields);

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
    public void WriteTableStart(params string[] headers)
    {
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
            _writer.Write(new string('-', header.Length + 2));
            _writer.Write('|');
        }
        _writer.WriteLine();
        _hasContent = true;
    }

    /// <summary>
    /// Writes a table row with the given values.
    /// Pipe characters in values are automatically escaped.
    /// </summary>
    public void WriteTableRow(params string[] values)
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
        
        var nodeList = nodes.ToList();
        for (int i = 0; i < nodeList.Count; i++)
        {
            var isLast = i == nodeList.Count - 1;
            WriteTreeNodeRecursive(nodeList[i], "", isLast);
        }
    }

    private void WriteTreeNodeRecursive(TreeNode node, string prefix, bool isLast)
    {
        var connector = isLast ? "└─ " : "├─ ";
        WriteTreeNode(node.Label, prefix + connector);
        
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
            return sw.ToString();
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
