using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Markout;

/// <summary>
/// Writes one typed JSON record per line when the final row shape is known.
/// Use tabular rendering for rows whose columns are selected at runtime.
/// </summary>
public static class JsonLinesWriter
{
    /// <summary>Writes each row before requesting the next one.</summary>
    /// <typeparam name="T">The final JSON record type.</typeparam>
    /// <param name="output">The destination for JSON lines.</param>
    /// <param name="rows">Records to write in order.</param>
    /// <param name="typeInfo">Serialization metadata for each record.</param>
    public static void WriteRows<T>(
        TextWriter output,
        IEnumerable<T> rows,
        JsonTypeInfo<T> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(typeInfo);

        var buffer = new ArrayBufferWriter<byte>();
        using var json = new Utf8JsonWriter(buffer, new JsonWriterOptions
        {
            Encoder = typeInfo.Options.Encoder,
        });

        foreach (T row in rows)
        {
            buffer.Clear();
            json.Reset(buffer);
            JsonSerializer.Serialize(json, row, typeInfo);
            json.Flush();
            string line = Encoding.UTF8.GetString(buffer.WrittenSpan);
            if (line.AsSpan().IndexOfAny('\r', '\n') >= 0)
            {
                // Valid JSON can contain literal CR/LF only as structural whitespace.
                line = line.Replace("\r", "").Replace("\n", "");
            }

            output.Write(line);
            output.WriteLine();
        }
    }
}
