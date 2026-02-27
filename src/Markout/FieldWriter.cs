using Markout.Formatting;

namespace Markout;

/// <summary>
/// Writes key-value fields to a TextWriter using a field formatter.
/// Handles projection (include/exclude fields) and layout variants
/// (standard, inline, bulleted, numbered). Document state is managed
/// by the caller or <see cref="MarkoutWriter"/>.
/// </summary>
public class FieldWriter(TextWriter writer, IFieldFormatter formatter, MarkoutWriterOptions? options = null)
{
    private readonly MarkoutWriterOptions _options = options ?? new();

    /// <summary>
    /// Writes a single key-value field.
    /// </summary>
    public void WriteField(string key, string value)
    {
        formatter.FormatFieldName(writer, key, _options.BoldFieldNames);
        writer.WriteLine(value);
    }

    /// <summary>
    /// Writes multiple fields, each on its own line.
    /// </summary>
    public void WriteFields(params ReadOnlySpan<MarkoutField> fields)
    {
        if (fields.Length == 0) return;
        formatter.FormatFields(writer, fields.ToArray(), _options.BoldFieldNames);
    }

    /// <summary>
    /// Writes fields on a single line separated by pipes.
    /// </summary>
    public void WriteFieldsInline(params ReadOnlySpan<MarkoutField> fields)
    {
        if (fields.Length == 0) return;

        for (int i = 0; i < fields.Length; i++)
        {
            if (i > 0)
                writer.Write(" | ");
            formatter.FormatFieldName(writer, fields[i].Key, _options.BoldFieldNames);
            writer.Write(fields[i].Value);
        }

        writer.WriteLine();
    }

    /// <summary>
    /// Writes fields as a bulleted list.
    /// </summary>
    public void WriteFieldsBulleted(params ReadOnlySpan<MarkoutField> fields)
    {
        if (fields.Length == 0) return;

        for (int i = 0; i < fields.Length; i++)
        {
            writer.Write("- ");
            formatter.FormatFieldName(writer, fields[i].Key, _options.BoldFieldNames);
            writer.WriteLine(fields[i].Value);
        }
    }

    /// <summary>
    /// Writes fields as a numbered list.
    /// </summary>
    public void WriteFieldsNumbered(params ReadOnlySpan<MarkoutField> fields)
    {
        if (fields.Length == 0) return;

        for (int i = 0; i < fields.Length; i++)
        {
            writer.Write(i + 1);
            writer.Write(". ");
            formatter.FormatFieldName(writer, fields[i].Key, _options.BoldFieldNames);
            writer.WriteLine(fields[i].Value);
        }
    }
}
