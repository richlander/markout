namespace MarkOut;

/// <summary>
/// Abstract base class for source-generated serializer contexts.
/// Derive from this class and apply [MarkOutContext(typeof(MyType))] attributes
/// to generate serialization code for your types.
/// </summary>
public abstract class MarkOutSerializerContext
{
    /// <summary>
    /// Gets or sets whether field names should be rendered in bold.
    /// When true, field names are wrapped in ** for markdown bold formatting.
    /// </summary>
    public bool BoldFieldNames { get; set; }

    /// <summary>
    /// Gets the type info for the specified type, or null if not registered.
    /// </summary>
    /// <typeparam name="T">The type to get info for.</typeparam>
    /// <returns>The type info, or null if the type is not registered in this context.</returns>
    public abstract MarkOutTypeInfo<T>? GetTypeInfo<T>();

    /// <summary>
    /// Gets schema information describing how a type will be rendered.
    /// </summary>
    /// <typeparam name="T">The type to describe.</typeparam>
    /// <returns>The schema info, or null if the type is not registered.</returns>
    public abstract MarkOutSchemaInfo? GetSchemaInfo<T>();

    /// <summary>
    /// Serializes a value using this context.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The MDF string representation.</returns>
    public string Serialize<T>(T value)
    {
        var typeInfo = GetTypeInfo<T>();
        if (typeInfo == null)
        {
            throw new InvalidOperationException(
                $"Type '{typeof(T).FullName}' is not registered in this serializer context. " +
                $"Add [MarkOutContext(typeof({typeof(T).Name}))] to your context class.");
        }

        var writer = new MarkOutWriter { BoldFieldNames = BoldFieldNames };
        typeInfo.Serialize(writer, value);
        return writer.ToString();
    }

    /// <summary>
    /// Serializes a value to the specified TextWriter.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="output">The TextWriter to write to.</param>
    public void Serialize<T>(T value, TextWriter output)
    {
        var typeInfo = GetTypeInfo<T>();
        if (typeInfo == null)
        {
            throw new InvalidOperationException(
                $"Type '{typeof(T).FullName}' is not registered in this serializer context. " +
                $"Add [MarkOutContext(typeof({typeof(T).Name}))] to your context class.");
        }

        var writer = new MarkOutWriter(output) { BoldFieldNames = BoldFieldNames };
        typeInfo.Serialize(writer, value);
        writer.Flush();
    }
}
