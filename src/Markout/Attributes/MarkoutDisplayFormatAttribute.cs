namespace Markout;

/// <summary>
/// Specifies a composite format string to apply when rendering a property value.
/// The format string uses <see cref="string.Format(string, object)"/> syntax where
/// <c>{0}</c> is replaced with the property value.
/// </summary>
/// <example>
///   <code>
///   [MarkoutDisplayFormat("{0:N0} downloads")]
///   public long TotalDownloads { get; set; }
///   // Renders as: TotalDownloads: 1,234,567 downloads
///
///   [MarkoutDisplayFormat("{0:P0}")]
///   public double CpuUsage { get; set; }
///   // Renders as: CpuUsage: 85%
///   </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MarkoutDisplayFormatAttribute : Attribute
{
    /// <summary>
    /// Gets the composite format string.
    /// </summary>
    public string Format { get; }

    /// <summary>
    /// Initializes a new instance with the specified format string.
    /// </summary>
    /// <param name="format">A composite format string where <c>{0}</c> represents the property value.</param>
    public MarkoutDisplayFormatAttribute(string format) => Format = format;
}
