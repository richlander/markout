namespace MarkOut;

/// <summary>
/// Static entry point for MDF serialization.
/// </summary>
public static class MarkOutSerializer
{
    /// <summary>
    /// Serializes a value to MDF format using the specified context.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="context">The serializer context containing type metadata.</param>
    /// <returns>The MDF string representation.</returns>
    public static string Serialize<T>(T value, MarkOutSerializerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Serialize(value);
    }

    /// <summary>
    /// Serializes a value to MDF format, writing to the specified TextWriter.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="output">The TextWriter to write to.</param>
    /// <param name="context">The serializer context containing type metadata.</param>
    public static void Serialize<T>(T value, TextWriter output, MarkOutSerializerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Serialize(value, output);
    }

    /// <summary>
    /// Serializes a value to MDF format, writing to the specified Stream.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="output">The Stream to write to.</param>
    /// <param name="context">The serializer context containing type metadata.</param>
    public static void Serialize<T>(T value, Stream output, MarkOutSerializerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        using var writer = new StreamWriter(output, leaveOpen: true);
        context.Serialize(value, writer);
    }

    /// <summary>
    /// Serializes a value to MDF format using the specified type info.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="typeInfo">The type info containing serialization logic.</param>
    /// <returns>The MDF string representation.</returns>
    public static string Serialize<T>(T value, MarkOutTypeInfo<T> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);

        var writer = new MarkOutWriter();
        typeInfo.Serialize(writer, value);
        return writer.ToString();
    }

    /// <summary>
    /// Serializes a value to MDF format, writing to the specified TextWriter.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="output">The TextWriter to write to.</param>
    /// <param name="typeInfo">The type info containing serialization logic.</param>
    public static void Serialize<T>(T value, TextWriter output, MarkOutTypeInfo<T> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);

        var writer = new MarkOutWriter(output);
        typeInfo.Serialize(writer, value);
        writer.Flush();
    }
}
