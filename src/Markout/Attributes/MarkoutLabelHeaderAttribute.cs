namespace Markout;

/// <summary>
/// Sets the label (identity) column header for a <c>List&lt;MultiSourceRow&gt;</c> multi-source
/// card property (the leftmost column, e.g. <c>"Metric"</c>). When absent, the header defaults to
/// <c>"Field"</c>.
/// </summary>
/// <example>
///   <code>
///   [MarkoutSection(Name = "Baseline comparison")]
///   [MarkoutLabelHeader("Metric")]
///   public List&lt;MultiSourceRow&gt; Rows { get; set; }
///   </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MarkoutLabelHeaderAttribute : Attribute
{
    /// <summary>Initializes the attribute with the label-column header text.</summary>
    /// <param name="header">The header for the leading label/identity column.</param>
    public MarkoutLabelHeaderAttribute(string header) => Header = header;

    /// <summary>The header for the leading label/identity column.</summary>
    public string Header { get; }
}
