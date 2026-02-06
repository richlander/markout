namespace Markout;

/// <summary>
/// Specifies a custom format string for rendering an <see cref="ISpanFormattable"/> property
/// (e.g., DateTime, DateTimeOffset, numeric types).
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MarkoutFormatAttribute : Attribute
{
    /// <summary>
    /// Gets the format string to use when rendering the value.
    /// </summary>
    public string Format { get; }

    /// <summary>
    /// Initializes a new instance with the specified format string.
    /// </summary>
    /// <param name="format">A standard or custom format string (e.g., "yyyy-MM-dd", "N0", "P1").</param>
    public MarkoutFormatAttribute(string format) => Format = format;
}
