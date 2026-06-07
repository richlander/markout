namespace Markout;

/// <summary>
/// Controls how <see cref="TableFormatter"/> renders tabular data.
/// </summary>
public enum MarkoutTableMode
{
    /// <summary>
    /// Render compact space-padded columns.
    /// </summary>
    Pretty,

    /// <summary>
    /// Render normalized tab-separated values.
    /// </summary>
    Tsv
}
