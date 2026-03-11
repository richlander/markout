using System.Collections.Generic;
using System.Diagnostics;

namespace Markout;

/// <summary>
/// Abstract base class for source-generated serializer contexts.
/// Derive from this class and apply [MarkoutContext(typeof(MyType))] attributes
/// to generate serialization code for your types.
/// </summary>
/// <example>
///   <code lang="cs" source="../../samples/Serialization/BasicUsage.cs" region="ProductViewObject" title="Context definition" />
///   <code lang="cs" source="../../samples/Serialization/SectionFiltering.cs" region="FilterViaContext" title="Context configuration" />
/// </example>
/// <seealso href="../../samples/Serialization/BasicUsage.cs">Context usage examples</seealso>
/// <seealso href="../../samples/Serialization/SectionFiltering.cs">Section filtering via context</seealso>
public abstract class MarkoutSerializerContext
{
    private const string TypeNotRegisteredMessage = 
        "Type '{0}' is not registered in this serializer context. " +
        "Add [MarkoutContext(typeof({1}))] to your context class.";

    private MarkoutWriterOptions _options;

    /// <summary>
    /// Creates a new context with default writer options.
    /// </summary>
    protected MarkoutSerializerContext() : this(new MarkoutWriterOptions())
    {
    }

    /// <summary>
    /// Creates a new context with the specified writer options.
    /// The options instance cannot be mutated once it is bound to the context.
    /// </summary>
    /// <param name="options">The writer options to bind to this context.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if <paramref name="options"/> is already read-only.</exception>
    protected MarkoutSerializerContext(MarkoutWriterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.IsReadOnly)
        {
            throw new InvalidOperationException(
                "Cannot bind a read-only MarkoutWriterOptions instance to a context. " +
                "The options instance is made read-only when bound to a context.");
        }

        options.MakeReadOnly();
        _options = options;
    }

    /// <summary>
    /// Gets the writer options bound to this context. The options instance is read-only.
    /// </summary>
    public MarkoutWriterOptions Options => _options;

    /// <summary>
    /// Gets the type info for the specified type, or null if not registered.
    /// </summary>
    /// <typeparam name="T">The type to get info for.</typeparam>
    /// <returns>The type info, or null if the type is not registered in this context.</returns>
    public abstract MarkoutTypeInfo<T>? GetTypeInfo<T>();

    /// <summary>
    /// Gets schema information describing how a type will be rendered.
    /// </summary>
    /// <typeparam name="T">The type to describe.</typeparam>
    /// <returns>The schema info, or null if the type is not registered.</returns>
    public abstract MarkoutSchemaInfo? GetSchemaInfo<T>();

    /// <summary>
    /// Gets all type infos registered in this context.
    /// Used by <see cref="Templates.MarkoutTemplate.BindContext"/> for automatic registration.
    /// </summary>
    /// <returns>An enumerable of all registered type infos.</returns>
    public abstract IEnumerable<IMarkoutTypeInfo> GetRegisteredTypes();

    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private static void ThrowTypeNotRegistered<T>()
    {
        throw new InvalidOperationException(
            string.Format(TypeNotRegisteredMessage, typeof(T).FullName, typeof(T).Name));
    }

    /// <summary>
    /// Serializes a value using this context.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The Markdown string representation.</returns>
    public string Serialize<T>(T value)
    {
        return Serialize(value, Options);
    }

    /// <summary>
    /// Serializes a value using the specified options.
    /// </summary>
    public string Serialize<T>(T value, MarkoutWriterOptions options)
    {
        var typeInfo = GetTypeInfo<T>();
        if (typeInfo == null)
            ThrowTypeNotRegistered<T>();

        var orch = new MarkoutWriter(new MarkdownFormatter(), options);
        typeInfo.Serialize(orch, value);
        return orch.ToString();
    }

    /// <summary>
    /// Serializes a value into an existing writer.
    /// Use this to interleave serialized and imperative content.
    /// </summary>
    public void Serialize<T>(T value, MarkoutWriter writer)
    {
        var typeInfo = GetTypeInfo<T>();
        if (typeInfo == null)
            ThrowTypeNotRegistered<T>();

        typeInfo.Serialize(writer, value);
    }

    /// <summary>
    /// Serializes a value to the specified TextWriter.
    /// </summary>
    public void Serialize<T>(T value, TextWriter output)
    {
        Serialize(value, output, Options);
    }

    /// <summary>
    /// Serializes a value to the specified TextWriter with options.
    /// </summary>
    public void Serialize<T>(T value, TextWriter output, MarkoutWriterOptions options)
    {
        Serialize(value, output, new MarkdownFormatter(), options);
    }

    /// <summary>
    /// Serializes a value to the specified TextWriter using the given formatter.
    /// </summary>
    public void Serialize<T>(T value, TextWriter output, Formatting.IMarkoutFormatter formatter)
    {
        Serialize(value, output, formatter, Options);
    }

    /// <summary>
    /// Serializes a value to the specified TextWriter using the given formatter and options.
    /// </summary>
    public void Serialize<T>(T value, TextWriter output, Formatting.IMarkoutFormatter formatter, MarkoutWriterOptions options)
    {
        var typeInfo = GetTypeInfo<T>();
        if (typeInfo == null)
            ThrowTypeNotRegistered<T>();

        var orch = new MarkoutWriter(output, formatter, options);
        typeInfo.Serialize(orch, value);
        orch.Flush();
    }
}
