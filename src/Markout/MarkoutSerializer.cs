namespace Markout;

/// <summary>
/// Static entry point for Markout serialization.
/// </summary>
/// <example>
///   <code lang="cs" source="../../samples/Serialization/BasicUsage.cs" region="SerializeSimpleType" title="Basic serialization" />
///   <code lang="cs" source="../../samples/Serialization/BasicUsage.cs" region="WriteToStream" title="Writing to streams" />
/// </example>
/// <seealso href="../../samples/Serialization/BasicUsage.cs">Basic serialization usage</seealso>
public static class MarkoutSerializer
{
    /// <summary>
    /// Serializes a value to Markout format using the specified context.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="context">The serializer context containing type metadata.</param>
    /// <returns>The Markdown string representation.</returns>
    public static string Serialize<T>(T value, MarkoutSerializerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Serialize(value);
    }

    /// <summary>
    /// Serializes a value to Markout format using the specified context and options.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="context">The serializer context containing type metadata.</param>
    /// <param name="options">The writer options for controlling output formatting.</param>
    /// <returns>The Markdown string representation.</returns>
    public static string Serialize<T>(T value, MarkoutSerializerContext context, MarkoutWriterOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);
        return context.Serialize(value, options);
    }

    /// <summary>
    /// Serializes a value into an existing MarkoutWriter using the specified context.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="writer">The writer to serialize into.</param>
    /// <param name="context">The serializer context containing type metadata.</param>
    public static void Serialize<T>(T value, MarkoutWriter writer, MarkoutSerializerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Serialize(value, writer);
    }

    /// <summary>
    /// Serializes a value to Markout format, writing to the specified TextWriter.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="output">The TextWriter to write to.</param>
    /// <param name="context">The serializer context containing type metadata.</param>
    public static void Serialize<T>(T value, TextWriter output, MarkoutSerializerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Serialize(value, output);
    }

    /// <summary>
    /// Serializes a value to Markout format, writing to the specified TextWriter with options.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="output">The TextWriter to write to.</param>
    /// <param name="context">The serializer context containing type metadata.</param>
    /// <param name="options">The writer options for controlling output formatting.</param>
    public static void Serialize<T>(T value, TextWriter output, MarkoutSerializerContext context, MarkoutWriterOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);
        context.Serialize(value, output, options);
    }

    /// <summary>
    /// Serializes a value to Markout format, writing to the specified Stream.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="output">The Stream to write to.</param>
    /// <param name="context">The serializer context containing type metadata.</param>
    public static void Serialize<T>(T value, Stream output, MarkoutSerializerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        using var writer = new StreamWriter(output, leaveOpen: true);
        context.Serialize(value, writer);
    }

    /// <summary>
    /// Serializes a value to Markout format, writing to the specified Stream with options.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="output">The Stream to write to.</param>
    /// <param name="context">The serializer context containing type metadata.</param>
    /// <param name="options">The writer options for controlling output formatting.</param>
    public static void Serialize<T>(T value, Stream output, MarkoutSerializerContext context, MarkoutWriterOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);
        using var writer = new StreamWriter(output, leaveOpen: true);
        context.Serialize(value, writer, options);
    }

    /// <summary>
    /// Serializes a value to Markout format using the specified type info.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="typeInfo">The type info containing serialization logic.</param>
    /// <returns>The Markdown string representation.</returns>
    public static string Serialize<T>(T value, MarkoutTypeInfo<T> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);

        var writer = new MarkdownFormatter();
        typeInfo.Serialize(writer, value);
        return writer.ToString();
    }

    /// <summary>
    /// Serializes a value to Markout format using the specified type info and options.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="typeInfo">The type info containing serialization logic.</param>
    /// <param name="options">The writer options for controlling output formatting.</param>
    /// <returns>The Markdown string representation.</returns>
    public static string Serialize<T>(T value, MarkoutTypeInfo<T> typeInfo, MarkoutWriterOptions options)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        ArgumentNullException.ThrowIfNull(options);

        var writer = new MarkdownFormatter(options);
        typeInfo.Serialize(writer, value);
        return writer.ToString();
    }

    /// <summary>
    /// Serializes a value to Markout format, writing to the specified TextWriter.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="output">The TextWriter to write to.</param>
    /// <param name="typeInfo">The type info containing serialization logic.</param>
    public static void Serialize<T>(T value, TextWriter output, MarkoutTypeInfo<T> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);

        var writer = new MarkdownFormatter(output);
        typeInfo.Serialize(writer, value);
        writer.Flush();
    }

    /// <summary>
    /// Serializes a value to Markout format, writing to the specified TextWriter with options.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="output">The TextWriter to write to.</param>
    /// <param name="typeInfo">The type info containing serialization logic.</param>
    /// <param name="options">The writer options for controlling output formatting.</param>
    public static void Serialize<T>(T value, TextWriter output, MarkoutTypeInfo<T> typeInfo, MarkoutWriterOptions options)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        ArgumentNullException.ThrowIfNull(options);

        var writer = new MarkdownFormatter(output, options);
        typeInfo.Serialize(writer, value);
        writer.Flush();
    }
}
