namespace MarkdownData;

/// <summary>
/// Specifies which types should be included in an MDF serializer context.
/// Apply this attribute to a partial class that derives from MdfSerializerContext.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class MdfContextAttribute : Attribute
{
    /// <summary>
    /// Gets the type to include in the serializer context.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Initializes a new instance with the specified type.
    /// </summary>
    /// <param name="type">The type to include in the serializer context.</param>
    public MdfContextAttribute(Type type)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
    }
}
