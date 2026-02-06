namespace Markout;

/// <summary>
/// Specifies that a string collection property (e.g., <c>List&lt;string&gt;</c>, <c>string[]</c>)
/// should be rendered as a single joined field instead of a bullet list.
/// </summary>
/// <example>
///   <code>
///   [MarkoutJoin(", ")]
///   public List&lt;string&gt; Tags { get; set; }
///   // Renders as: Tags: api, web, tools
///   </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MarkoutJoinAttribute : Attribute
{
    /// <summary>
    /// Gets the separator used to join the collection values.
    /// </summary>
    public string Separator { get; }

    /// <summary>
    /// Initializes a new instance with the specified separator.
    /// </summary>
    /// <param name="separator">The string used to join collection values (e.g., ", ", " | ", " · ").</param>
    public MarkoutJoinAttribute(string separator) => Separator = separator;
}
