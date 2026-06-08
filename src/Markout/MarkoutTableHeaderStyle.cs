namespace Markout;

/// <summary>
/// Controls which header name <see cref="TableFormatter"/> receives.
/// </summary>
public enum MarkoutTableHeaderStyle
{
    /// <summary>
    /// Use stable snake_case names for TSV tables and display labels for pretty tables.
    /// </summary>
    Auto,

    /// <summary>
    /// Use human-facing display labels.
    /// </summary>
    DisplayName,

    /// <summary>
    /// Use stable snake_case names derived from source member names.
    /// </summary>
    StableName
}
