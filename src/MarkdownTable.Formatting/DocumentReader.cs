namespace MarkdownTable.Formatting;

/// <summary>
/// Parses a markdown document into a <see cref="MarkdownDocument"/> with
/// headings, fields, and tables. Suitable for both query tools and
/// programmatic data access.
/// </summary>
public static class DocumentReader
{
    /// <summary>
    /// Parses markdown text into a document model with fields and tables.
    /// </summary>
    public static MarkdownDocument Read(string text)
    {
        var doc = new MarkdownDocument();
        var lines = text.Split('\n');

        DocumentSection? currentSection = null;
        var tableLines = new List<string>();
        var fieldLines = new List<string>();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.TrimEnd('\r');

            // Check for heading
            if (TryParseHeading(trimmed, out var level, out var headingText))
            {
                FlushPending(currentSection, tableLines, fieldLines, lines, ref i);

                if (level == 1 && doc.Title is null)
                    doc.Title = headingText;

                currentSection = new DocumentSection { Heading = headingText, Level = level };
                doc.Sections.Add(currentSection);
                continue;
            }

            // Check for table lines
            if (TableParser.IsPipeTableLine(trimmed))
            {
                // Flush fields before starting table
                if (fieldLines.Count > 0)
                    FlushFields(currentSection, fieldLines);

                EnsureSection(doc, ref currentSection);
                tableLines.Add(trimmed);
                continue;
            }

            // Non-table line: flush any pending table
            if (tableLines.Count > 0)
            {
                FlushTable(currentSection, tableLines);
            }

            // Check for field-like lines (collect for batch parsing)
            var trimmedSpan = trimmed.AsSpan().Trim();
            if (!trimmedSpan.IsEmpty && !IsSkippableLine(trimmedSpan))
            {
                EnsureSection(doc, ref currentSection);
                fieldLines.Add(trimmed);
            }
            else if (trimmedSpan.IsEmpty && fieldLines.Count > 0)
            {
                // Blank line — keep it in fieldLines so FieldParser can see
                // array boundaries (field name followed by blank then bullets)
                fieldLines.Add(trimmed);
            }
        }

        // Flush final pending content
        int dummy = lines.Length;
        FlushPending(currentSection, tableLines, fieldLines, lines, ref dummy);

        return doc;
    }

    private static void EnsureSection(MarkdownDocument doc, ref DocumentSection? section)
    {
        if (section is not null)
            return;
        section = new DocumentSection { Level = 0 };
        doc.Sections.Add(section);
    }

    private static void FlushPending(DocumentSection? section, List<string> tableLines, List<string> fieldLines, string[] lines, ref int i)
    {
        if (tableLines.Count > 0)
            FlushTable(section, tableLines);
        if (fieldLines.Count > 0)
            FlushFields(section, fieldLines);
    }

    private static void FlushTable(DocumentSection? section, List<string> tableLines)
    {
        if (section is null || tableLines.Count == 0)
        {
            tableLines.Clear();
            return;
        }

        if (TableParser.TryParse(tableLines, out var headers, out var rows))
        {
            section.Table = new DocumentTable { Headers = headers, Rows = rows };
        }

        tableLines.Clear();
    }

    private static void FlushFields(DocumentSection? section, List<string> fieldLines)
    {
        if (section is null || fieldLines.Count == 0)
        {
            fieldLines.Clear();
            return;
        }

        var text = string.Join('\n', fieldLines);
        var parsed = FieldParser.ParseFields(text);

        foreach (var (key, value) in parsed)
        {
            section.Fields.TryAdd(key, value);
        }

        fieldLines.Clear();
    }

    private static bool IsSkippableLine(ReadOnlySpan<char> trimmed)
    {
        // Code fences, block quotes / callouts
        return trimmed[0] == '`' || trimmed.StartsWith(">");
    }

    internal static bool TryParseHeading(string line, out int level, out string text)
    {
        level = 0;
        text = "";

        var span = line.AsSpan().TrimStart();
        if (span.Length == 0 || span[0] != '#')
            return false;

        int hashCount = 0;
        while (hashCount < span.Length && span[hashCount] == '#')
            hashCount++;

        if (hashCount > 6 || hashCount >= span.Length || span[hashCount] != ' ')
            return false;

        level = hashCount;
        text = span[(hashCount + 1)..].Trim().ToString();
        return text.Length > 0;
    }
}
