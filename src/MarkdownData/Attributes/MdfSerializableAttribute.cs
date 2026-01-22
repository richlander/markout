namespace MarkdownData;

/// <summary>
/// Marks a type for MDF source generation.
/// Types marked with this attribute will have serialization code generated at compile time.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class MdfSerializableAttribute : Attribute
{
}
