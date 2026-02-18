namespace MarkdownTable.Formatting;

/// <summary>
/// Parses Markout-style key-value fields from markdown text.
/// Supports all FieldLayout styles:
///   - Bold:    **Key:** Value  
///   - Plain:   Key: Value
///   - OneLine: Key: Value | Key: Value
///   - List:    - Key: Value (or - **Key:** Value)
///   - Array:   **Key:**\n- item1\n- item2
/// </summary>
public static class FieldParser
{
    /// <summary>
    /// Parses all fields from markdown text into a dictionary.
    /// Keys are case-preserved; lookup is case-insensitive.
    /// Handles both scalar values and array values (bullet lists following a field name).
    /// </summary>
    public static Dictionary<string, FieldValue> ParseToDictionary(string text)
    {
        var dict = new Dictionary<string, FieldValue>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in ParseFields(text))
        {
            dict.TryAdd(key, value);
        }
        return dict;
    }

    /// <summary>
    /// Parses all fields from markdown text, yielding key-value pairs with scalar string values.
    /// Array fields are returned with items joined by ", ".
    /// </summary>
    public static List<KeyValuePair<string, string>> Parse(string text)
    {
        var result = new List<KeyValuePair<string, string>>();
        foreach (var (key, value) in ParseFields(text))
        {
            result.Add(new(key, value.Text));
        }
        return result;
    }

    /// <summary>
    /// Parses all fields from markdown text, yielding key-FieldValue pairs.
    /// </summary>
    public static List<KeyValuePair<string, FieldValue>> ParseFields(string text)
    {
        var fields = new List<KeyValuePair<string, FieldValue>>();
        var lines = text.Split('\n');
        int i = 0;

        while (i < lines.Length)
        {
            var line = lines[i].TrimEnd('\r');
            var trimmed = line.AsSpan().Trim();

            if (trimmed.IsEmpty)
            {
                i++;
                continue;
            }

            // Skip headings, table lines, code fences, callouts
            if (trimmed[0] == '#' || trimmed[0] == '`' || trimmed.StartsWith(">"))
            {
                i++;
                continue;
            }
            if (trimmed.Contains('|') && IsPipeTableLine(trimmed))
            {
                i++;
                continue;
            }

            // OneLine: Key: Value | Key: Value | ...
            if (ContainsFieldPipeSeparator(trimmed))
            {
                ParseOneLineFields(trimmed, fields);
                i++;
                continue;
            }

            // Skip standalone list items (not associated with a preceding field)
            if (trimmed.StartsWith("- "))
            {
                i++;
                continue;
            }

            // Bold or plain field on its own line
            if (TryParseField(trimmed, out var key, out var value))
            {
                // Check if value is empty and next lines are a bullet list (array field)
                if (value.Length == 0)
                {
                    var items = CollectBulletList(lines, i + 1, out var nextI);
                    if (items.Count > 0)
                    {
                        fields.Add(new(key, FieldValue.FromItems(items.ToArray())));
                        i = nextI;
                        continue;
                    }
                }

                fields.Add(new(key, FieldValue.FromText(value)));
                i++;
                continue;
            }

            i++;
        }

        return fields;
    }

    /// <summary>
    /// Collects consecutive bullet list items starting at the given line index.
    /// </summary>
    private static List<string> CollectBulletList(string[] lines, int startIndex, out int nextIndex)
    {
        var items = new List<string>();
        int i = startIndex;

        // Skip one optional blank line between field name and list
        if (i < lines.Length && lines[i].TrimEnd('\r').Trim().Length == 0)
            i++;

        while (i < lines.Length)
        {
            var line = lines[i].TrimEnd('\r');
            var trimmed = line.AsSpan().Trim();

            if (trimmed.StartsWith("- "))
            {
                items.Add(trimmed[2..].Trim().ToString());
                i++;
            }
            else
            {
                break;
            }
        }

        nextIndex = i;
        return items;
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
        if (colonIdx <= 0)
            return false;

        // The colon must be followed by a space or be at end of line (array field)
        if (colonIdx < line.Length - 1 && line[colonIdx + 1] != ' ')
            return false;

        // Key should not look like a URL (http:, https:, ftp:)
        var possibleKey = line[..colonIdx];
        if (possibleKey.EndsWith("http", StringComparison.OrdinalIgnoreCase) ||
            possibleKey.EndsWith("https", StringComparison.OrdinalIgnoreCase) ||
            possibleKey.EndsWith("ftp", StringComparison.OrdinalIgnoreCase))
            return false;

        key = possibleKey.Trim().ToString();
        value = colonIdx < line.Length - 1
            ? line[(colonIdx + 1)..].Trim().TrimEnd(' ').ToString()
            : "";
        return key.Length > 0;
    }

    private static void ParseOneLineFields(ReadOnlySpan<char> line, List<KeyValuePair<string, FieldValue>> fields)
    {
        // Split on " | " (pipe with surrounding spaces)
        while (true)
        {
            var sepIdx = IndexOfPipeSeparator(line);
            var segment = sepIdx >= 0 ? line[..sepIdx] : line;

            if (TryParseField(segment.Trim(), out var key, out var value))
            {
                fields.Add(new(key, FieldValue.FromText(value)));
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
