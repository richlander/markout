namespace MarkOut;

/// <summary>
/// Excludes a property from serialization when the containing type is rendered as a table row.
/// The property will still be rendered when the type is serialized as a document.
/// Use this to silence warnings about unsupported patterns in table context.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MarkOutIgnoreInTableAttribute : Attribute
{
}
