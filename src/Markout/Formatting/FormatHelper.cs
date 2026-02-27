namespace Markout.Formatting;

/// <summary>
/// Shared formatting utilities for formatter implementations.
/// </summary>
public static class FormatHelper
{
    /// <summary>
    /// Formats the numeric value displayed at the end of a metric bar.
    /// </summary>
    public static string FormatBarValue(double value)
    {
        return value == Math.Floor(value) ? ((int)value).ToString() : value.ToString("0.#");
    }
}
