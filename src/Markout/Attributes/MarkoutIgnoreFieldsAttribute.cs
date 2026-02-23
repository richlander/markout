namespace Markout;

/// <summary>
/// Specifies that fields should be silently ignored (no warning) for the specified writer type.
/// Apply to a serializable class to suppress "does not support Fields" warnings for specific writers.
/// </summary>
/// <example>
/// <code>
/// [MarkoutSerializable]
/// [MarkoutIgnoreFields(nameof(OneLineWriter))]
/// public class MyView
/// {
///     public int Count { get; set; }  // Field - ignored by OneLineWriter without warning
///
///     [MarkoutSection(Name = "Results")]
///     public List&lt;MyRow&gt;? Results { get; set; }  // Table - rendered by OneLineWriter
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class MarkoutIgnoreFieldsAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the writer type that should silently ignore fields.
    /// </summary>
    public string WriterTypeName { get; }

    /// <summary>
    /// Initializes a new instance specifying the writer type name.
    /// </summary>
    /// <param name="writerTypeName">The name of the writer type (e.g., nameof(OneLineWriter)).</param>
    public MarkoutIgnoreFieldsAttribute(string writerTypeName)
    {
        WriterTypeName = writerTypeName;
    }
}
