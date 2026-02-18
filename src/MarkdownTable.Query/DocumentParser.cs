using MarkdownTable.Formatting;

namespace MarkdownTable.Query;

/// <summary>
/// Parses a markdown document into a queryable <see cref="MarkdownDocument"/> structure.
/// Extracts headings and tables; other content is ignored.
/// </summary>
public static class DocumentParser
{
    /// <summary>
    /// Parses markdown text into a document model.
    /// </summary>
    public static MarkdownDocument Parse(string text)
    {
        var doc = new MarkdownDocument();
        var lines = text.Split('\n');

        // Current section being built
        DocumentSection? currentSection = null;
        var tableLines = new List<string>();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.TrimEnd('\r');

            // Check for heading
            if (TryParseHeading(trimmed, out var level, out var headingText))
            {
                // Flush any pending table
                FlushTable(currentSection, tableLines);

                if (level == 1 && doc.Title is null)
                {
                    doc.Title = headingText;
                }

                currentSection = new DocumentSection { Heading = headingText, Level = level };
                doc.Sections.Add(currentSection);
                continue;
            }

            // Check for table lines
            if (TableParser.IsPipeTableLine(trimmed))
            {
                // Ensure we have a section to attach to
                if (currentSection is null)
                {
                    currentSection = new DocumentSection { Level = 0 };
                    doc.Sections.Add(currentSection);
                }

                tableLines.Add(trimmed);
                continue;
            }

            // Non-table, non-heading line: flush any pending table
            if (tableLines.Count > 0)
            {
                FlushTable(currentSection, tableLines);
            }
        }

        // Flush final table
        FlushTable(currentSection, tableLines);

        return doc;
    }

    private static void FlushTable(DocumentSection? section, List<string> tableLines)
    {
        if (tableLines.Count == 0 || section is null)
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

    private static bool TryParseHeading(string line, out int level, out string text)
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
