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
    /// Serializes a value into an existing orchestrator using the specified context.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="orchestrator">The orchestrator to serialize into.</param>
    /// <param name="context">The serializer context containing type metadata.</param>
    public static void Serialize<T>(T value, MarkoutOrchestrator orchestrator, MarkoutSerializerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Serialize(value, orchestrator);
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
    public static string Serialize<T>(T value, MarkoutTypeInfo<T> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);

        var orch = new MarkoutOrchestrator(new MarkdownFormatter());
        typeInfo.Serialize(orch, value);
        return orch.ToString();
    }

    /// <summary>
    /// Serializes a value to Markout format using the specified type info and options.
    /// </summary>
    public static string Serialize<T>(T value, MarkoutTypeInfo<T> typeInfo, MarkoutWriterOptions options)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        ArgumentNullException.ThrowIfNull(options);

        var orch = new MarkoutOrchestrator(new MarkdownFormatter(), options);
        typeInfo.Serialize(orch, value);
        return orch.ToString();
    }

    /// <summary>
    /// Serializes a value to Markout format, writing to the specified TextWriter.
    /// </summary>
    public static void Serialize<T>(T value, TextWriter output, MarkoutTypeInfo<T> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);

        var orch = new MarkoutOrchestrator(output, new MarkdownFormatter());
        typeInfo.Serialize(orch, value);
        orch.Flush();
    }

    /// <summary>
    /// Serializes a value to Markout format, writing to the specified TextWriter with options.
    /// </summary>
    public static void Serialize<T>(T value, TextWriter output, MarkoutTypeInfo<T> typeInfo, MarkoutWriterOptions options)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        ArgumentNullException.ThrowIfNull(options);

        var orch = new MarkoutOrchestrator(output, new MarkdownFormatter(), options);
        typeInfo.Serialize(orch, value);
        orch.Flush();
    }
}
