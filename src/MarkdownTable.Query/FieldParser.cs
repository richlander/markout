namespace MarkdownTable.Query;

/// <summary>
/// Parses Markout-style key-value fields from markdown text.
/// Supports all FieldLayout styles:
///   - Bold:    **Key:** Value  
///   - Plain:   Key: Value
///   - OneLine: Key: Value | Key: Value
///   - List:    - Key: Value (or - **Key:** Value)
/// </summary>
public static class FieldParser
{
    /// <summary>
    /// Parses all fields from markdown text into a dictionary.
    /// Keys are case-preserved; lookup is case-insensitive.
    /// </summary>
    public static Dictionary<string, string> ParseToDictionary(string text)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in Parse(text))
        {
            dict.TryAdd(key, value);
        }
        return dict;
    }

    /// <summary>
    /// Parses all fields from markdown text, yielding key-value pairs.
    /// </summary>
    public static List<KeyValuePair<string, string>> Parse(string text)
    {
        var fields = new List<KeyValuePair<string, string>>();

        foreach (var rawLine in text.AsSpan().EnumerateLines())
        {
            var line = rawLine.Trim();
            if (line.IsEmpty)
                continue;

            // Skip headings, table lines, code fences, callouts
            if (line[0] == '#' || line[0] == '`' || line.StartsWith(">"))
                continue;
            if (line.Contains('|') && IsPipeTableLine(line))
                continue;

            // OneLine: Key: Value | Key: Value | ...
            if (ContainsFieldPipeSeparator(line))
            {
                ParseOneLineFields(line, fields);
                continue;
            }

            // List item: - Key: Value or - **Key:** Value
            if (line.StartsWith("- "))
            {
                var item = line[2..].Trim();
                if (TryParseField(item, out var k, out var v))
                {
                    fields.Add(new(k, v));
                }
                continue;
            }

            // Bold or plain field on its own line
            if (TryParseField(line, out var key, out var value))
            {
                fields.Add(new(key, value));
            }
        }

        return fields;
    }

    /// <summary>
    /// Tries to parse a single field from a line.
    /// Handles **Key:** Value (bold) and Key: Value (plain).
    /// </summary>
    internal static bool TryParseField(ReadOnlySpan<char> line, out string key, out string value)
    {
        key = "";
        value = "";

        // Bold field: **Key:** Value
        if (line.StartsWith("**"))
        {
            var closeIdx = line[2..].IndexOf(":**");
            if (closeIdx < 0)
                return false;

            key = line.Slice(2, closeIdx).ToString();
            var valueStart = 2 + closeIdx + 3; // skip past ":**"
            value = valueStart < line.Length
                ? line[valueStart..].Trim().TrimEnd(' ').ToString()
                : "";
            return key.Length > 0;
        }

        // Plain field: Key: Value
        var colonIdx = line.IndexOf(':');
        if (colonIdx <= 0 || colonIdx >= line.Length - 1)
            return false;

        // Key must not contain spaces before the colon (simple heuristic)
        // Actually, Markout keys can have spaces (e.g., "Latest Major")
        // But the colon must be followed by a space
        if (line[colonIdx + 1] != ' ')
            return false;

        // Key should not look like a URL (http:, https:, ftp:)
        var possibleKey = line[..colonIdx];
        if (possibleKey.EndsWith("http", StringComparison.OrdinalIgnoreCase) ||
            possibleKey.EndsWith("https", StringComparison.OrdinalIgnoreCase) ||
            possibleKey.EndsWith("ftp", StringComparison.OrdinalIgnoreCase))
            return false;

        key = possibleKey.Trim().ToString();
        value = line[(colonIdx + 1)..].Trim().TrimEnd(' ').ToString();
        return key.Length > 0;
    }

    private static void ParseOneLineFields(ReadOnlySpan<char> line, List<KeyValuePair<string, string>> fields)
    {
        // Split on " | " (pipe with surrounding spaces)
        while (true)
        {
            var sepIdx = IndexOfPipeSeparator(line);
            var segment = sepIdx >= 0 ? line[..sepIdx] : line;

            if (TryParseField(segment.Trim(), out var key, out var value))
            {
                fields.Add(new(key, value));
            }

            if (sepIdx < 0)
                break;

            line = line[(sepIdx + 3)..]; // skip " | "
        }
    }

    private static bool ContainsFieldPipeSeparator(ReadOnlySpan<char> line)
    {
        // Look for " | " that separates fields (not table pipes)
        // Table lines start/end with |, field lines don't
        if (line.Length > 0 && line[0] == '|')
            return false;

        return IndexOfPipeSeparator(line) >= 0;
    }

    private static int IndexOfPipeSeparator(ReadOnlySpan<char> line)
    {
        var idx = line.IndexOf(" | ");
        return idx;
    }

    private static bool IsPipeTableLine(ReadOnlySpan<char> line)
    {
        // Table lines start or end with |
        return line[0] == '|' || line[^1] == '|';
    }
}
