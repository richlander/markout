using System.Buffers;

namespace Markout.Formatting;

/// <summary>
/// Shared formatting utilities for formatter implementations.
/// </summary>
public static class FormatHelper
{
    private const string CodeStart = "<code>";
    private const string CodeEnd = "</code>";
    private static readonly SearchValues<char> InlineTagSentinel = SearchValues.Create("<");

    /// <summary>
    /// Renders semantic inline tags for Markdown output.
    /// </summary>
    public static string RenderInlineMarkdown(string? value)
        => RenderInline(value, codeFormatter: FormatMarkdownCodeSpan);

    /// <summary>
    /// Renders semantic inline tags for a Markdown table cell, escaping cell-breaking characters in
    /// one code-span-aware pass. Pipes in ordinary text become <c>&amp;#124;</c>, but pipes inside a
    /// rendered code span are escaped as <c>\|</c>: GFM unescapes <c>\|</c> while splitting table
    /// rows, before code-span parsing, whereas <c>&amp;#124;</c> would render literally inside a code
    /// span.
    /// </summary>
    public static string RenderInlineMarkdownTableCell(string? value)
        => RenderInline(value, codeFormatter: FormatMarkdownCodeSpanForTableCell, textFormatter: EscapeTableCell);

    /// <summary>
    /// Renders semantic inline tags as plain text for non-Markdown output.
    /// </summary>
    public static string RenderInlinePlainText(string? value)
        => RenderInline(value, codeFormatter: static text => text);

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

    private static string RenderInline(
        string? value, Func<string, string> codeFormatter, Func<string, string>? textFormatter = null)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        var start = IndexOfCodeStart(value, 0);
        if (start < 0)
            return textFormatter is null ? value : textFormatter(value);

        var sb = new System.Text.StringBuilder(value.Length);
        var cursor = 0;
        while (start >= 0)
        {
            var contentStart = start + CodeStart.Length;
            var end = value.IndexOf(CodeEnd, contentStart, StringComparison.Ordinal);
            if (end < 0)
                break;

            AppendText(sb, value, cursor, start - cursor, textFormatter);
            var encoded = value.Substring(contentStart, end - contentStart);
            sb.Append(codeFormatter(DecodeXmlText(encoded)));

            cursor = end + CodeEnd.Length;
            start = IndexOfCodeStart(value, cursor);
        }

        if (cursor == 0)
            return textFormatter is null ? value : textFormatter(value);

        AppendText(sb, value, cursor, value.Length - cursor, textFormatter);
        return sb.ToString();
    }

    private static void AppendText(
        System.Text.StringBuilder sb, string value, int start, int length, Func<string, string>? textFormatter)
    {
        if (textFormatter is null)
            sb.Append(value, start, length);
        else
            sb.Append(textFormatter(value.Substring(start, length)));
    }

    private static int IndexOfCodeStart(string value, int startIndex)
    {
        var span = value.AsSpan(startIndex);
        while (true)
        {
            var relative = span.IndexOfAny(InlineTagSentinel);
            if (relative < 0)
                return -1;

            var absolute = startIndex + relative;
            if (value.AsSpan(absolute).StartsWith(CodeStart, StringComparison.Ordinal))
                return absolute;

            var next = relative + 1;
            startIndex += next;
            span = span[next..];
        }
    }

    private static string DecodeXmlText(string value)
        => value
            .Replace("&lt;", "<", StringComparison.Ordinal)
            .Replace("&gt;", ">", StringComparison.Ordinal)
            .Replace("&quot;", "\"", StringComparison.Ordinal)
            .Replace("&apos;", "'", StringComparison.Ordinal)
            .Replace("&amp;", "&", StringComparison.Ordinal);

    private static string FormatMarkdownCodeSpan(string value)
    {
        var delimiter = value.Contains('`')
            ? new string('`', LongestBacktickRun(value) + 1)
            : "`";
        var padding = delimiter.Length > 1 ? " " : "";
        return $"{delimiter}{padding}{value}{padding}{delimiter}";
    }

    private static string FormatMarkdownCodeSpanForTableCell(string value)
    {
        // A literal pipe inside a table-cell code span must be escaped as \| (GFM strips the
        // backslash while splitting table rows, before code-span parsing); &#124; would render
        // literally inside a code span. Newlines are collapsed to spaces as in other cell text.
        if (value.Contains('|') || value.Contains('\n') || value.Contains('\r'))
        {
            value = value
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Replace("\r", " ")
                .Replace("|", "\\|");
        }
        return FormatMarkdownCodeSpan(value);
    }

    private static int LongestBacktickRun(string value)
    {
        var longest = 0;
        var current = 0;
        foreach (var c in value)
        {
            if (c == '`')
            {
                current++;
                longest = Math.Max(longest, current);
            }
            else
            {
                current = 0;
            }
        }
        return longest;
    }
}
