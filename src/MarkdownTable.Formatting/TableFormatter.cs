using System.Text;

namespace MarkdownTable.Formatting;

/// <summary>
/// Formats and reformats markdown pipe tables with statistical column-width optimization.
/// </summary>
public static class TableFormatter
{
    /// <summary>
    /// Formats a table from header and row data into a markdown pipe table string.
    /// </summary>
    public static string Format(string[] headers, IReadOnlyList<string[]> rows, TableFormatterOptions? options = null)
    {
        if (headers.Length == 0)
            return string.Empty;

        var widths = ColumnWidthCalculator.Calculate(headers, rows, options);
        return Render(headers, rows, widths);
    }

    /// <summary>
    /// Reformats an existing markdown pipe table string with optimized column widths.
    /// Returns the original text unchanged if it doesn't contain a valid pipe table.
    /// </summary>
    public static string Reformat(string text, TableFormatterOptions? options = null)
    {
        if (!TableParser.TryParse(text, out var headers, out var rows))
            return text;

        return Format(headers, rows, options);
    }

    /// <summary>
    /// Renders a table with pre-calculated column widths.
    /// </summary>
    internal static string Render(string[] headers, IReadOnlyList<string[]> rows, int[] widths)
    {
        var sb = new StringBuilder();

        RenderRow(sb, headers, widths);
        RenderSeparator(sb, widths);

        foreach (var row in rows)
            RenderRow(sb, row, widths);

        return sb.ToString();
    }

    private static void RenderRow(StringBuilder sb, string[] cells, int[] widths)
    {
        sb.Append('|');
        for (int col = 0; col < widths.Length; col++)
        {
            sb.Append(' ');
            var value = col < cells.Length ? cells[col] : "";
            sb.Append(value);

            int padding = widths[col] - value.Length;
            if (padding > 0)
                sb.Append(' ', padding);

            sb.Append(" |");
        }
        sb.AppendLine();
    }

    private static void RenderSeparator(StringBuilder sb, int[] widths)
    {
        sb.Append('|');
        for (int col = 0; col < widths.Length; col++)
        {
            sb.Append(' ');
            sb.Append('-', widths[col]);
            sb.Append(" |");
        }
        sb.AppendLine();
    }
}
