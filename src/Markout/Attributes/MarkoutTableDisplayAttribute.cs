namespace Markout;

/// <summary>
/// Specifies an alternate format string for a property when rendered in a table column.
/// In block/field context, the property uses its normal rendering.
/// In table context, the value is formatted using <see cref="string.Format(string, object)"/>
/// where <c>{0}</c> is replaced with the property value.
/// </summary>
/// <example>
///   <code>
///   // Without [MarkoutTableDisplay]: table shows raw count "42"
///   // With [MarkoutTableDisplay]: table shows "42 items", block shows "42"
///   [MarkoutTableDisplay("{0} items")]
///   public int Count { get; set; }
///   </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MarkoutTableDisplayAttribute : Attribute
{
    /// <summary>
    /// Gets the format string to use when rendering in a table column.
    /// <c>{0}</c> is replaced with the property value.
    /// </summary>
    public string Format { get; }

    /// <summary>
    /// Initializes a new instance with the specified table display format.
    /// </summary>
    /// <param name="format">A composite format string where <c>{0}</c> represents the property value.</param>
    public MarkoutTableDisplayAttribute(string format) => Format = format;
}
