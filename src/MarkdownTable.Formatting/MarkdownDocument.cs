namespace MarkdownTable.Formatting;

/// <summary>
/// Represents a parsed markdown document: an optional title,
/// top-level fields, and named sections containing fields and/or tables.
/// </summary>
public class MarkdownDocument
{
    /// <summary>
    /// The document title (from the first H1 heading), or null.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Named sections extracted from the document. Each section is keyed by
    /// its heading text and may contain fields, a table, or both.
    /// </summary>
    public List<DocumentSection> Sections { get; } = [];

    /// <summary>
    /// The first (or only) table in the document, if any.
    /// </summary>
    public DocumentTable? DefaultTable =>
        Sections.FirstOrDefault(s => s.Table is not null)?.Table;

    /// <summary>
    /// All fields from the preamble (before any heading) or the title section.
    /// </summary>
    public Dictionary<string, FieldValue> Fields =>
        Sections.FirstOrDefault(s => s.Level <= 1)?.Fields
        ?? new Dictionary<string, FieldValue>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// A named section within a markdown document, corresponding to a heading.
/// </summary>
public class DocumentSection
{
    /// <summary>
    /// The heading text (e.g., "Methods", "All Releases").
    /// Null for content before the first heading.
    /// </summary>
    public string? Heading { get; set; }

    /// <summary>
    /// The heading level (1-6), or 0 for the preamble section.
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Key-value fields in this section.
    /// </summary>
    public Dictionary<string, FieldValue> Fields { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The table in this section, if any.
    /// </summary>
    public DocumentTable? Table { get; set; }
}

/// <summary>
/// A parsed markdown pipe table.
/// </summary>
public class DocumentTable
{
    /// <summary>The table's header cells, left to right.</summary>
    public string[] Headers { get; set; } = [];

    /// <summary>The table's data rows, each holding one cell per header.</summary>
    public List<string[]> Rows { get; set; } = [];
}
