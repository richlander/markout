using System.Globalization;
using System.Text;

namespace MarkOut;

/// <summary>
/// Low-level writer for generating MDF (Markdown Data Format) output.
/// </summary>
public sealed class MarkOutWriter
{
    private readonly TextWriter _writer;
    private readonly bool _ownsWriter;
    private bool _needsBlankLine;
    private bool _hasContent;
    private bool _inTable;

    /// <summary>
    /// Creates a writer that builds output in memory.
    /// Use ToString() to get the result.
    /// </summary>
    public MarkOutWriter() : this(new StringWriter(), ownsWriter: true)
    {
    }

    /// <summary>
    /// Creates a writer that writes to the specified TextWriter.
    /// </summary>
    public MarkOutWriter(TextWriter writer) : this(writer, ownsWriter: false)
    {
    }

    /// <summary>
    /// Creates a writer that writes to the specified Stream.
    /// </summary>
    public MarkOutWriter(Stream stream) : this(new StreamWriter(stream, Encoding.UTF8, leaveOpen: true), ownsWriter: true)
    {
    }

    private MarkOutWriter(TextWriter writer, bool ownsWriter)
    {
        _writer = writer;
        _ownsWriter = ownsWriter;
    }

    /// <summary>
    /// Gets or sets whether field names should be rendered in bold.
    /// When true, field names are wrapped in ** for markdown bold formatting.
    /// </summary>
    public bool BoldFieldNames { get; set; }

    /// <summary>
    /// Flushes any buffered output to the underlying stream.
    /// </summary>
    public void Flush() => _writer.Flush();

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
        if (string.IsNullOrEmpty(text))
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
        _writer.WriteLine("```");
        _needsBlankLine = true;
    }

    /// <summary>
    /// Writes a key-value field with a string value.
    /// Uses trailing spaces for markdown hard line break.
    /// </summary>
    public void WriteField(string key, string? value)
    {
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
        EnsureBlankLineIfNeeded();
        WriteFieldName(key);
        _writer.Write(value.ToString("O", CultureInfo.InvariantCulture));
        _writer.WriteLine("  "); // Two trailing spaces for markdown hard line break
        _hasContent = true;
    }

    /// <summary>
    /// Writes an array field with string items as a markdown list.
    /// Always has a blank line before and after for proper markdown rendering.
    /// </summary>
    public void WriteArray(string key, IEnumerable<string>? items)
    {
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
    /// </summary>
    public void WriteTableRow(params string[] values)
    {
        if (!_inTable)
            throw new InvalidOperationException("Cannot write table row without starting a table first.");

        _writer.Write('|');
        foreach (var value in values)
        {
            _writer.Write(' ');
            _writer.Write(value);
            _writer.Write(" |");
        }
        _writer.WriteLine();
    }

    /// <summary>
    /// Ends the current table.
    /// </summary>
    public void WriteTableEnd()
    {
        _inTable = false;
        _needsBlankLine = true;
    }

    /// <summary>
    /// Writes a simple pair (two values separated by whitespace).
    /// </summary>
    public void WriteSimplePair(string name, string value, int nameWidth = 32)
    {
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
        if (nodes == null) return;
        
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
        _writer.WriteLine();
        _needsBlankLine = false;
    }

    /// <summary>
    /// Returns the generated MDF content.
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
