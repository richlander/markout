namespace MarkdownData;

/// <summary>
/// Static entry point for MDF serialization.
/// </summary>
public static class MdfSerializer
{
    /// <summary>
    /// Serializes a value to MDF format using the specified context.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="context">The serializer context containing type metadata.</param>
    /// <returns>The MDF string representation.</returns>
    public static string Serialize<T>(T value, MdfSerializerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Serialize(value);
    }

    /// <summary>
    /// Serializes a value to MDF format using the specified type info.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="typeInfo">The type info containing serialization logic.</param>
    /// <returns>The MDF string representation.</returns>
    public static string Serialize<T>(T value, MdfTypeInfo<T> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);

        var writer = new MdfWriter();
        typeInfo.Serialize(writer, value);
        return writer.ToString();
    }
}
