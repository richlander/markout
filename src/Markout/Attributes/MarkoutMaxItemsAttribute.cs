namespace Markout;

/// <summary>
/// Limits the number of items rendered from a collection property.
/// When the collection exceeds the limit, remaining items are replaced with an ellipsis message.
/// </summary>
/// <example>
///   <code>
///   [MarkoutMaxItems(5)]
///   public List&lt;string&gt; Files { get; set; }
///   // Renders first 5 items, then: "... and 10 more"
///
///   [MarkoutMaxItems(3, EllipsisFormat = "({0} more items not shown)")]
///   public List&lt;string&gt; Logs { get; set; }
///   // Renders first 3 items, then: "(7 more items not shown)"
///   </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MarkoutMaxItemsAttribute : Attribute
{
    /// <summary>
    /// Gets the maximum number of items to render.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Gets or sets the format string for the ellipsis message.
    /// <c>{0}</c> is replaced with the number of remaining items.
    /// Default is <c>"... and {0} more"</c>.
    /// </summary>
    public string EllipsisFormat { get; set; } = "... and {0} more";

    /// <summary>
    /// Initializes a new instance with the specified maximum item count.
    /// </summary>
    /// <param name="count">The maximum number of items to render before truncation.</param>
    public MarkoutMaxItemsAttribute(int count) => Count = count;
}
