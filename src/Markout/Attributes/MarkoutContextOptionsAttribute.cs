namespace Markout;

/// <summary>
/// Specifies default writer options for a serializer context at compile time.
/// Options set here are baked into the generated context's default constructor.
/// They can still be overridden by passing a <see cref="MarkoutWriterOptions"/> instance
/// to the context constructor at runtime.
/// </summary>
/// <example>
///   <code>
///   [MarkoutContextOptions(BoldFieldNames = true, IncludeIcons = false)]
///   [MarkoutContext(typeof(MyType))]
///   public partial class MyContext : MarkoutSerializerContext { }
///   </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class MarkoutContextOptionsAttribute : Attribute
{
    /// <summary>
    /// Gets or sets whether field names should be rendered in bold.
    /// Default is false.
    /// </summary>
    public bool BoldFieldNames { get; set; }

    /// <summary>
    /// Gets or sets whether to include icons in tree nodes.
    /// Default is true.
    /// </summary>
    public bool IncludeIcons { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include the description paragraph.
    /// Default is true.
    /// </summary>
    public bool IncludeDescription { get; set; } = true;
}
