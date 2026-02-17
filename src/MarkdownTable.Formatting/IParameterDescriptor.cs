namespace MarkdownTable.Formatting;

/// <summary>
/// A parameter descriptor that can parse and set values on its associated mode.
/// </summary>
public interface IParameterDescriptor
{
    /// <summary>
    /// Parameter name (e.g., "percentile", "shadow-threshold").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Human-readable description for help text.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Type description for help (e.g., "int", "double").
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Default value as string for display.
    /// </summary>
    string DefaultValue { get; }

    /// <summary>
    /// Parse and set the parameter value on the mode.
    /// </summary>
    bool TrySetValue(string value);
}
