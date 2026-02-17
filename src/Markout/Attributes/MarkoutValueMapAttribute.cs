namespace Markout;

/// <summary>
/// Maps string property values to badge-prefixed output.
/// Each entry is a "value=badge" pair. When the property value matches a key,
/// the badge is prepended: <c>"badge value"</c>. Unmatched values pass through unchanged.
/// </summary>
/// <remarks>
/// Applies to string properties in both field and table contexts.
/// </remarks>
/// <example>
/// <code>
/// [MarkoutValueMap("class=📦", "struct=🔲", "interface=🔌", "enum=🔢")]
/// public string Kind { get; set; } = "";
/// // "class" renders as "📦 class", "record" renders as "record"
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MarkoutValueMapAttribute : Attribute
{
    /// <summary>
    /// Gets the value map entries, each in "value=badge" format.
    /// </summary>
    public string[] Entries { get; }

    /// <summary>
    /// Creates a value map from "value=badge" pairs.
    /// </summary>
    /// <param name="entries">One or more "value=badge" strings.</param>
    public MarkoutValueMapAttribute(params string[] entries)
    {
        Entries = entries;
    }
}
