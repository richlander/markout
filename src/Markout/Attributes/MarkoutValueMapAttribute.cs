namespace Markout;

/// <summary>
/// Specifies a mapping from property values to display labels.
/// Each entry is a "key=value" string where the key is matched against the property
/// value and the value is used as a badge or replacement in the rendered output.
/// </summary>
/// <example>
/// <code>
/// [MarkoutValueMap("CRITICAL=🔴", "HIGH=🟠", "MEDIUM=🟡", "LOW=🟢")]
/// public string? Severity { get; set; }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MarkoutValueMapAttribute : Attribute
{
    /// <summary>
    /// Gets the mapping entries in "key=value" format.
    /// </summary>
    public string[] Entries { get; }

    /// <summary>
    /// Initializes a new instance with one or more "key=value" mapping entries.
    /// </summary>
    public MarkoutValueMapAttribute(params string[] entries)
    {
        Entries = entries;
    }
}
