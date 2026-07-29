namespace MarkdownTable.Formatting;

/// <summary>
/// Aggressive space optimization with tight statistical parameters.
/// All three parameters are exposed for fine-tuning.
/// </summary>
public class CompactMode : IFormatterMode, IFormatterModeInfo
{
    /// <summary>
    /// Percentile threshold for column width calculation (0.0–1.0). Default: 0.2.
    /// </summary>
    public double Percentile { get; set; } = 0.2;

    /// <summary>
    /// Multiplier applied to the percentile width (0.0–10.0). Default: 1.0.
    /// </summary>
    public double Tolerance { get; set; } = 1.0;

    /// <summary>
    /// Columns narrower than this use max-width instead of statistics. Default: 4.
    /// </summary>
    public int ShadowThreshold { get; set; } = 4;

    /// <inheritdoc cref="IFormatterModeInfo.Name"/>
    public static string Name => "compact";

    /// <inheritdoc cref="IFormatterModeInfo.Description"/>
    public static string Description => "Aggressive space optimization with tight statistical parameters";

    /// <inheritdoc/>
    public TableFormatterOptions Options => new()
    {
        Percentile = Percentile,
        Tolerance = Tolerance,
        ShadowThreshold = ShadowThreshold
    };

    /// <inheritdoc/>
    public IReadOnlyList<IParameterDescriptor> GetParameters() =>
    [
        new ParameterDescriptor<double>("percentile",
            "Statistical percentile for width calculation (0.0-1.0)",
            "double", 0.2,
            value => Percentile = value,
            static (string input, out double value) =>
            {
                value = 0;
                return double.TryParse(input, out value) && value >= 0.0 && value <= 1.0;
            }),

        new ParameterDescriptor<int>("shadow-threshold",
            "Columns narrower than this use max-width instead of statistics",
            "int", 4,
            value => ShadowThreshold = value,
            static (string input, out int value) =>
            {
                value = 0;
                return int.TryParse(input, out value) && value >= 0;
            }),

        new ParameterDescriptor<double>("tolerance",
            "Multiplier applied to percentile width (0.0-10.0)",
            "double", 1.0,
            value => Tolerance = value,
            static (string input, out double value) =>
            {
                value = 0;
                return double.TryParse(input, out value) && value >= 0.0 && value <= 10.0;
            })
    ];
}
