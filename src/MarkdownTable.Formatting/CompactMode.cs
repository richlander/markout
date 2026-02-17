namespace MarkdownTable.Formatting;

/// <summary>
/// Aggressive space optimization with tight statistical parameters.
/// All three parameters are exposed for fine-tuning.
/// </summary>
public class CompactMode : IFormatterMode, IFormatterModeInfo
{
    public double Percentile { get; set; } = 0.2;
    public double Tolerance { get; set; } = 1.0;
    public int ShadowThreshold { get; set; } = 4;

    public static string Name => "compact";
    public static string Description => "Aggressive space optimization with tight statistical parameters";

    public TableFormatterOptions Options => new()
    {
        Percentile = Percentile,
        Tolerance = Tolerance,
        ShadowThreshold = ShadowThreshold
    };

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
