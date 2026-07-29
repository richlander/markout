namespace MarkdownTable.Formatting;

/// <summary>
/// Prioritizes visual alignment with relaxed statistical parameters.
/// </summary>
public class PrettyMode : IFormatterMode, IFormatterModeInfo
{
    /// <inheritdoc cref="IFormatterModeInfo.Name"/>
    public static string Name => "pretty";

    /// <inheritdoc cref="IFormatterModeInfo.Description"/>
    public static string Description => "Prioritizes visual alignment with relaxed statistical parameters";

    /// <inheritdoc/>
    public TableFormatterOptions Options => new()
    {
        Percentile = 0.8,
        Tolerance = 1.8,
        ShadowThreshold = 12
    };

    /// <inheritdoc/>
    public IReadOnlyList<IParameterDescriptor> GetParameters() => [];
}
