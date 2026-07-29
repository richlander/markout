namespace MarkdownTable.Formatting;

/// <summary>
/// Auto-tuned statistical formatting that hill-climbs to perfect trailing-edge alignment.
/// </summary>
public class SmoothMode : IFormatterMode, IFormatterModeInfo
{
    /// <inheritdoc cref="IFormatterModeInfo.Name"/>
    public static string Name => "smooth";

    /// <inheritdoc cref="IFormatterModeInfo.Description"/>
    public static string Description => "Auto-tuned statistical formatting for perfect trailing-edge alignment";

    /// <inheritdoc/>
    public TableFormatterOptions Options => new() { AutoTune = true };

    /// <inheritdoc/>
    public IReadOnlyList<IParameterDescriptor> GetParameters() => [];
}
