namespace MarkdownTable.Formatting;

/// <summary>
/// Instance configuration for a formatter mode. Created when a mode is
/// selected and needs configuration. Safe to use as a type argument.
/// </summary>
public interface IFormatterMode
{
    /// <summary>
    /// Configured formatting options for this mode.
    /// </summary>
    TableFormatterOptions Options { get; }

    /// <summary>
    /// Parameter descriptors for mode-specific configuration.
    /// </summary>
    IReadOnlyList<IParameterDescriptor> GetParameters();
}
