namespace Markout;

/// <summary>
/// Specifies custom string values to use when rendering a boolean property.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MarkoutBoolFormatAttribute : Attribute
{
    /// <summary>
    /// Gets the string to render when the value is true.
    /// </summary>
    public string TrueValue { get; }

    /// <summary>
    /// Gets the string to render when the value is false.
    /// </summary>
    public string FalseValue { get; }

    /// <summary>
    /// Initializes a new instance with custom true/false display strings.
    /// </summary>
    /// <param name="trueValue">String to display when true (e.g., "✓", "Yes", "Enabled").</param>
    /// <param name="falseValue">String to display when false (e.g., "✗", "No", "Disabled").</param>
    public MarkoutBoolFormatAttribute(string trueValue, string falseValue)
    {
        TrueValue = trueValue;
        FalseValue = falseValue;
    }
}
