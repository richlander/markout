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
    /// Uses direct string splitting — fastest path when the content is
    /// already in memory as a string.
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
            var trimmed = lines[i].TrimEnd('\r');
            var kind = ClassifyStringLine(trimmed);

            switch (kind)
            {
                case ByteLineKind.Heading:
                {
                    FlushPending(currentSection, tableLines, fieldLines);
                    if (TryParseHeading(trimmed, out var level, out var headingText))
                    {
                        if (level == 1 && doc.Title is null)
                            doc.Title = headingText;
                        currentSection = new DocumentSection { Heading = headingText, Level = level };
                        doc.Sections.Add(currentSection);
                    }
                    break;
                }

                case ByteLineKind.PipeTable:
                {
                    if (fieldLines.Count > 0)
                        FlushFields(currentSection, fieldLines);
                    EnsureSection(doc, ref currentSection);
                    tableLines.Add(trimmed);
                    break;
                }

                case ByteLineKind.Empty:
                {
                    if (tableLines.Count > 0)
                        FlushTable(currentSection, tableLines);
                    if (fieldLines.Count > 0)
                        fieldLines.Add(trimmed);
                    break;
                }

                case ByteLineKind.Skippable:
                {
                    if (tableLines.Count > 0)
                        FlushTable(currentSection, tableLines);
                    break;
                }

                default:
                {
                    if (tableLines.Count > 0)
                        FlushTable(currentSection, tableLines);
                    EnsureSection(doc, ref currentSection);
                    fieldLines.Add(trimmed);
                    break;
                }
            }
        }

        FlushPending(currentSection, tableLines, fieldLines);
        return doc;
    }

    /// <summary>
    /// Parses a UTF-8 byte buffer into a document model.
    /// Convenience overload that wraps the buffer in a memory stream.
    /// </summary>
    public static MarkdownDocument Read(ReadOnlySpan<byte> utf8)
    {
        using var stream = new MemoryStream(utf8.ToArray());
        return ReadAsync(stream).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Parses a stream of markdown into a document model using
    /// <see cref="LineReader"/> for buffered, byte-level line scanning.
    /// This is the primary entry point — all other overloads delegate here.
    /// </summary>
    public static async Task<MarkdownDocument> ReadAsync(
        Stream stream, CancellationToken cancellationToken = default)
    {
        var doc = new MarkdownDocument();
        var reader = LineReader.Create(stream);

        DocumentSection? currentSection = null;
        var tableLines = new List<string>();
        var fieldLines = new List<string>();

        // Initial buffer fill
        await reader.AdvanceAsync(cancellationToken);

        while (!reader.IsComplete)
        {
            if (!reader.ReadLineNoConsume(out var lineBytes))
            {
                if (!await reader.AdvanceAsync(cancellationToken))
                    break;
                continue;
            }

            var kind = ByteLineClassifier.Classify(lineBytes);

            // Decode to string before consuming — the span is only valid
            // while the buffer hasn't been flipped.
            string? lineStr = kind switch
            {
                ByteLineKind.Empty or ByteLineKind.Skippable => null,
                _ => LineReader.ToString(lineBytes),
            };

            // Consume the peeked line (advances position, no buffer flip)
            reader.ConsumeNextNewline();

            switch (kind)
            {
                case ByteLineKind.Heading:
                {
                    FlushPending(currentSection, tableLines, fieldLines);
                    if (TryParseHeading(lineStr!, out var level, out var headingText))
                    {
                        if (level == 1 && doc.Title is null)
                            doc.Title = headingText;
                        currentSection = new DocumentSection { Heading = headingText, Level = level };
                        doc.Sections.Add(currentSection);
                    }
                    break;
                }

                case ByteLineKind.PipeTable:
                {
                    if (fieldLines.Count > 0)
                        FlushFields(currentSection, fieldLines);
                    EnsureSection(doc, ref currentSection);
                    tableLines.Add(lineStr!);
                    break;
                }

                case ByteLineKind.Empty:
                {
                    if (tableLines.Count > 0)
                        FlushTable(currentSection, tableLines);
                    if (fieldLines.Count > 0)
                        fieldLines.Add("");
                    break;
                }

                case ByteLineKind.Skippable:
                {
                    if (tableLines.Count > 0)
                        FlushTable(currentSection, tableLines);
                    break;
                }

                case ByteLineKind.BoldField:
                case ByteLineKind.Bullet:
                case ByteLineKind.OneLineFields:
                case ByteLineKind.Content:
                {
                    if (tableLines.Count > 0)
                        FlushTable(currentSection, tableLines);
                    EnsureSection(doc, ref currentSection);
                    fieldLines.Add(lineStr!);
                    break;
                }
            }
        }

        FlushPending(currentSection, tableLines, fieldLines);
        return doc;
    }

    private static void EnsureSection(MarkdownDocument doc, ref DocumentSection? section)
    {
        if (section is not null)
            return;
        section = new DocumentSection { Level = 0 };
        doc.Sections.Add(section);
    }

    private static void FlushPending(DocumentSection? section, List<string> tableLines, List<string> fieldLines)
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

    /// <summary>
    /// Classifies a string line into the same categories as <see cref="ByteLineClassifier"/>.
    /// Used by the <see cref="Read(string)"/> path to avoid byte conversion.
    /// </summary>
    private static ByteLineKind ClassifyStringLine(string line)
    {
        var trimmed = line.AsSpan().Trim();

        if (trimmed.IsEmpty)
            return ByteLineKind.Empty;

        char first = trimmed[0];

        if (first == '#')
        {
            int hashes = 0;
            while (hashes < trimmed.Length && trimmed[hashes] == '#') hashes++;
            if (hashes <= 6 && hashes < trimmed.Length && trimmed[hashes] == ' ')
                return ByteLineKind.Heading;
        }

        if (first == '`' || first == '>')
            return ByteLineKind.Skippable;

        if (first == '*' && trimmed.Length >= 5 && trimmed[1] == '*')
            return ByteLineKind.BoldField;

        if (first == '-' && trimmed.Length >= 2 && trimmed[1] == ' ')
            return ByteLineKind.Bullet;

        if (trimmed.Contains('|'))
        {
            if (first == '|' || trimmed[^1] == '|')
                return ByteLineKind.PipeTable;

            if (trimmed.Contains(" | ", StringComparison.Ordinal))
                return ByteLineKind.OneLineFields;
        }

        return ByteLineKind.Content;
    }
}
