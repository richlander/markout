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
    /// Escapes Markdown table cell characters and newlines in a table cell value.
    /// </summary>
    public static string EscapeTableCell(string value)
    {
        if (value.Contains('|') || value.Contains('\n') || value.Contains('\r'))
        {
            return value
                .Replace("|", "&#124;")
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Replace("\r", " ");
        }
        return value;
    }

    /// <summary>
    /// Normalizes a table cell for line-oriented tabular output.
    /// </summary>
    public static string NormalizeTableCell(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        for (int i = 0; i < value.Length; i++)
        {
            if (NeedsTableCellReplacement(value[i]))
                return NormalizeTableCellSlow(value);
        }

        return value;
    }

    /// <summary>
    /// Converts a Pascal/camel/display name to snake_case.
    /// </summary>
    public static string ToSnakeCase(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "";

        var sb = new System.Text.StringBuilder(name.Length + 4);
        var lastWasSeparator = true;

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (!char.IsLetterOrDigit(c))
            {
                AppendSeparator(sb, ref lastWasSeparator);
                continue;
            }

            if (char.IsUpper(c))
            {
                var previousIsLowerOrDigit = i > 0 && (char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1]));
                var nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);
                if (sb.Length > 0 && !lastWasSeparator && (previousIsLowerOrDigit || nextIsLower))
                    AppendSeparator(sb, ref lastWasSeparator);
            }

            sb.Append(char.ToLowerInvariant(c));
            lastWasSeparator = false;
        }

        if (sb.Length > 0 && sb[^1] == '_')
            sb.Length--;
        return sb.ToString();
    }

    private static string NormalizeTableCellSlow(string value)
    {
        var chars = value.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (NeedsTableCellReplacement(chars[i]))
                chars[i] = ' ';
        }
        return new string(chars);
    }

    private static bool NeedsTableCellReplacement(char c) =>
        c is '\t' or '\r' or '\n' or '\u0085' or '\u2028' or '\u2029';

    private static void AppendSeparator(System.Text.StringBuilder sb, ref bool lastWasSeparator)
    {
        if (!lastWasSeparator && sb.Length > 0)
        {
            sb.Append('_');
            lastWasSeparator = true;
        }
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
