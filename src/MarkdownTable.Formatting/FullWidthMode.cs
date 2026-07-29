namespace MarkdownTable.Formatting;

/// <summary>
/// Maximum width per column with no statistical optimization.
/// </summary>
public class FullWidthMode : IFormatterMode, IFormatterModeInfo
{
    /// <inheritdoc cref="IFormatterModeInfo.Name"/>
    public static string Name => "full";

    /// <inheritdoc cref="IFormatterModeInfo.Description"/>
    public static string Description => "Maximum width per column (no statistical optimization)";

    /// <inheritdoc/>
    public TableFormatterOptions Options => new() { Mode = CalculationMode.FullWidth };

    /// <inheritdoc/>
    public IReadOnlyList<IParameterDescriptor> GetParameters() => [];
}
