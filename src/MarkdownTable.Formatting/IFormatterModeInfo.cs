namespace MarkdownTable.Formatting;

/// <summary>
/// Static metadata for a formatter mode. Accessed without allocation at
/// registration time. Never used as a type argument.
/// </summary>
public interface IFormatterModeInfo
{
    /// <summary>
    /// Short name used on the command line (e.g., "smooth", "compact").
    /// </summary>
    static abstract string Name { get; }

    /// <summary>
    /// Human-readable description for help text.
    /// </summary>
    static abstract string Description { get; }
}
