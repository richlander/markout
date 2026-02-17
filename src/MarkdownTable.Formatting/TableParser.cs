namespace MarkdownTable.Formatting;

/// <summary>
/// Parses markdown pipe tables from text into structured header and row data.
/// </summary>
public static class TableParser
{
    /// <summary>
    /// Attempts to parse a markdown pipe table from a sequence of lines.
    /// Returns true if the lines represent a valid pipe table.
    /// </summary>
    public static bool TryParse(IReadOnlyList<string> lines, out string[] headers, out List<string[]> rows)
    {
        headers = [];
        rows = [];

        if (lines.Count < 2)
            return false;

        // Line 0: header row (must have pipes)
        if (!TryParseRow(lines[0], out headers))
            return false;

        // Line 1: separator row (must be all dashes/colons/pipes)
        if (!IsSeparatorRow(lines[1], headers.Length))
            return false;

        // Lines 2+: data rows
        rows = new List<string[]>(lines.Count - 2);
        for (int i = 2; i < lines.Count; i++)
        {
            if (TryParseRow(lines[i], out var row))
                rows.Add(row);
        }

        return true;
    }

    /// <summary>
    /// Attempts to parse a pipe table from a block of text.
    /// </summary>
    public static bool TryParse(string text, out string[] headers, out List<string[]> rows)
    {
        var lines = text.Split('\n');
        // Trim trailing empty lines
        int end = lines.Length;
        while (end > 0 && string.IsNullOrWhiteSpace(lines[end - 1]))
            end--;

        return TryParse(lines.AsSpan(0, end).ToArray(), out headers, out rows);
    }

    /// <summary>
    /// Checks whether a line looks like the start of a pipe table
    /// (contains a pipe character and is not a heading or placeholder).
    /// </summary>
    public static bool IsPipeTableLine(string line)
    {
        var trimmed = line.AsSpan().Trim();
        return trimmed.Length > 0 && trimmed.Contains('|') && !trimmed.StartsWith("{{") && !trimmed.StartsWith("#");
    }

    private static bool TryParseRow(string line, out string[] cells)
    {
        cells = [];
        var trimmed = line.Trim();

        if (!trimmed.Contains('|'))
            return false;

        // Strip leading and trailing pipes
        var span = trimmed.AsSpan();
        if (span.StartsWith("|"))
            span = span[1..];
        if (span.EndsWith("|"))
            span = span[..^1];

        // Split on pipes and trim each cell
        var parts = span.ToString().Split('|');
        cells = new string[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            cells[i] = parts[i].Trim();

        return cells.Length > 0;
    }

    private static bool IsSeparatorRow(string line, int expectedColumns)
    {
        if (!TryParseRow(line, out var cells))
            return false;

        if (cells.Length != expectedColumns)
            return false;

        foreach (var cell in cells)
        {
            var trimmed = cell.Trim();
            if (trimmed.Length == 0)
                return false;

            // Must be dashes, optionally with colons for alignment
            foreach (var ch in trimmed)
            {
                if (ch != '-' && ch != ':')
                    return false;
            }
        }

        return true;
    }
}
