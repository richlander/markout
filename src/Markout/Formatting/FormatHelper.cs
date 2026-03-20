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

    /// <summary>
    /// Escapes pipe characters and newlines in a table cell value.
    /// </summary>
    public static string EscapeTableCell(string value)
    {
        if (value.Contains('|') || value.Contains('\n') || value.Contains('\r'))
        {
            return value
                .Replace("|", "\\|")
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Replace("\r", " ");
        }
        return value;
    }

    /// <summary>
    /// Formats a byte count as a human-readable size (B, KB, MB, GB).
    /// Uses binary (1024) divisors.
    /// </summary>
    public static string FormatSize(long bytes) => bytes switch
    {
        >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F1} GB",
        >= 1_048_576 => $"{bytes / 1_048_576.0:F1} MB",
        >= 1_024 => $"{bytes / 1_024.0:F1} KB",
        _ => $"{bytes} B"
    };

    /// <summary>
    /// Formats a download/count value with K/M/B suffixes.
    /// Uses decimal (1000) divisors.
    /// </summary>
    public static string FormatDownloads(long count) => count switch
    {
        >= 1_000_000_000 => $"{count / 1_000_000_000.0:F1}B",
        >= 1_000_000 => $"{count / 1_000_000.0:F1}M",
        >= 1_000 => $"{count / 1_000.0:F1}K",
        _ => count.ToString()
    };

    /// <summary>
    /// Truncates a string to a maximum length, appending "..." if truncated.
    /// Collapses newlines to spaces before truncating.
    /// </summary>
    public static string Truncate(string? text, int maxLength)
    {
        if (text is null) return "";

        // Collapse newlines to spaces
        string clean = text.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");

        return clean.Length <= maxLength ? clean : clean[..(maxLength - 3)] + "...";
    }
}
