namespace MarkdownTable.Formatting;

/// <summary>
/// Prioritizes visual alignment with relaxed statistical parameters.
/// </summary>
public class PrettyMode : IFormatterMode, IFormatterModeInfo
{
    public static string Name => "pretty";
    public static string Description => "Prioritizes visual alignment with relaxed statistical parameters";

    public TableFormatterOptions Options => new()
    {
        Percentile = 0.8,
        Tolerance = 1.8,
        ShadowThreshold = 12
    };

    public IReadOnlyList<IParameterDescriptor> GetParameters() => [];
}
