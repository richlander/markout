namespace MarkdownTable.Formatting;

/// <summary>
/// Auto-tuned statistical formatting that hill-climbs to perfect trailing-edge alignment.
/// </summary>
public class SmoothMode : IFormatterMode, IFormatterModeInfo
{
    public static string Name => "smooth";
    public static string Description => "Auto-tuned statistical formatting for perfect trailing-edge alignment";

    public TableFormatterOptions Options => new() { AutoTune = true };
    public IReadOnlyList<IParameterDescriptor> GetParameters() => [];
}
