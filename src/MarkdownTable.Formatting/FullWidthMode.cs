namespace MarkdownTable.Formatting;

/// <summary>
/// Maximum width per column with no statistical optimization.
/// </summary>
public class FullWidthMode : IFormatterMode, IFormatterModeInfo
{
    public static string Name => "full";
    public static string Description => "Maximum width per column (no statistical optimization)";

    public TableFormatterOptions Options => new() { Mode = CalculationMode.FullWidth };
    public IReadOnlyList<IParameterDescriptor> GetParameters() => [];
}
