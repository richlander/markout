using System.Globalization;
using System.Text;

namespace MarkdownData;

/// <summary>
/// Low-level writer for generating MDF (Markdown Data Format) output.
/// </summary>
public sealed class MdfWriter
{
    private readonly StringBuilder _sb = new();
    private bool _needsBlankLine;
    private bool _hasContent;
    private bool _inTable;

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
            _sb.AppendLine();
        }

        _sb.Append('#', level);
        _sb.Append(' ');
        _sb.Append(text);

        if (!string.IsNullOrEmpty(context))
        {
            _sb.Append(" (");
            _sb.Append(context);
            _sb.Append(')');
        }

        _sb.AppendLine();
        _needsBlankLine = true;
        _hasContent = true;
    }

    /// <summary>
    /// Writes a key-value field with a string value.
    /// </summary>
    public void WriteField(string key, string? value)
    {
        EnsureBlankLineIfNeeded();
        _sb.Append(key);
        _sb.Append(": ");
        _sb.AppendLine(value ?? string.Empty);
        _hasContent = true;
    }

    /// <summary>
    /// Writes a key-value field with a boolean value (yes/no).
    /// </summary>
    public void WriteField(string key, bool value)
    {
        EnsureBlankLineIfNeeded();
        _sb.Append(key);
        _sb.Append(": ");
        _sb.AppendLine(value ? "yes" : "no");
        _hasContent = true;
    }

    /// <summary>
    /// Writes a key-value field with an integer value.
    /// </summary>
    public void WriteField(string key, int value)
    {
        EnsureBlankLineIfNeeded();
        _sb.Append(key);
        _sb.Append(": ");
        _sb.AppendLine(value.ToString(CultureInfo.InvariantCulture));
        _hasContent = true;
    }

    /// <summary>
    /// Writes a key-value field with a long value.
    /// </summary>
    public void WriteField(string key, long value)
    {
        EnsureBlankLineIfNeeded();
        _sb.Append(key);
        _sb.Append(": ");
        _sb.AppendLine(value.ToString(CultureInfo.InvariantCulture));
        _hasContent = true;
    }

    /// <summary>
    /// Writes a key-value field with a double value.
    /// </summary>
    public void WriteField(string key, double value)
    {
        EnsureBlankLineIfNeeded();
        _sb.Append(key);
        _sb.Append(": ");
        _sb.AppendLine(value.ToString(CultureInfo.InvariantCulture));
        _hasContent = true;
    }

    /// <summary>
    /// Writes a key-value field with a decimal value.
    /// </summary>
    public void WriteField(string key, decimal value)
    {
        EnsureBlankLineIfNeeded();
        _sb.Append(key);
        _sb.Append(": ");
        _sb.AppendLine(value.ToString(CultureInfo.InvariantCulture));
        _hasContent = true;
    }

    /// <summary>
    /// Writes a key-value field with a DateTime value (ISO 8601 format).
    /// </summary>
    public void WriteField(string key, DateTime value)
    {
        EnsureBlankLineIfNeeded();
        _sb.Append(key);
        _sb.Append(": ");
        _sb.AppendLine(value.ToString("O", CultureInfo.InvariantCulture));
        _hasContent = true;
    }

    /// <summary>
    /// Writes a key-value field with a DateTimeOffset value (ISO 8601 format).
    /// </summary>
    public void WriteField(string key, DateTimeOffset value)
    {
        EnsureBlankLineIfNeeded();
        _sb.Append(key);
        _sb.Append(": ");
        _sb.AppendLine(value.ToString("O", CultureInfo.InvariantCulture));
        _hasContent = true;
    }

    /// <summary>
    /// Writes an array field with string items.
    /// </summary>
    public void WriteArray(string key, IEnumerable<string>? items)
    {
        EnsureBlankLineIfNeeded();
        _sb.Append(key);
        _sb.AppendLine(":");

        if (items != null)
        {
            foreach (var item in items)
            {
                _sb.Append("- ");
                _sb.AppendLine(item);
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
        _sb.Append('|');
        foreach (var header in headers)
        {
            _sb.Append(' ');
            _sb.Append(header);
            _sb.Append(" |");
        }
        _sb.AppendLine();

        // Separator row
        _sb.Append('|');
        foreach (var header in headers)
        {
            _sb.Append('-', header.Length + 2);
            _sb.Append('|');
        }
        _sb.AppendLine();
        _hasContent = true;
    }

    /// <summary>
    /// Writes a table row with the given values.
    /// </summary>
    public void WriteTableRow(params string[] values)
    {
        if (!_inTable)
            throw new InvalidOperationException("Cannot write table row without starting a table first.");

        _sb.Append('|');
        foreach (var value in values)
        {
            _sb.Append(' ');
            _sb.Append(value);
            _sb.Append(" |");
        }
        _sb.AppendLine();
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
        _sb.Append(name.PadRight(nameWidth));
        _sb.AppendLine(value);
        _hasContent = true;
    }

    /// <summary>
    /// Writes a blank line.
    /// </summary>
    public void WriteBlankLine()
    {
        _sb.AppendLine();
        _needsBlankLine = false;
    }

    /// <summary>
    /// Returns the generated MDF content.
    /// </summary>
    public override string ToString()
    {
        return _sb.ToString();
    }

    private void EnsureBlankLineIfNeeded()
    {
        if (_needsBlankLine)
        {
            _sb.AppendLine();
            _needsBlankLine = false;
        }
    }
}
