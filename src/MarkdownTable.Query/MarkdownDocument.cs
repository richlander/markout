namespace MarkdownTable.Query;

/// <summary>
/// Represents a parsed markdown document as a queryable structure:
/// an optional title, top-level fields, and named sections containing tables.
/// </summary>
public class MarkdownDocument
{
    /// <summary>
    /// The document title (from the first H1 heading), or null.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Named sections extracted from the document. Each section is keyed by
    /// its heading text and contains a table (headers + rows).
    /// </summary>
    public List<DocumentSection> Sections { get; } = [];

    /// <summary>
    /// The first (or only) table in the document, if any.
    /// Convenience accessor for documents with a single table and no headings.
    /// </summary>
    public DocumentTable? DefaultTable =>
        Sections.FirstOrDefault(s => s.Table is not null)?.Table;
}

/// <summary>
/// A named section within a markdown document, corresponding to a heading.
/// </summary>
public class DocumentSection
{
    /// <summary>
    /// The heading text for this section (e.g., "Methods", "All Releases").
    /// Null for content before the first heading.
    /// </summary>
    public string? Heading { get; set; }

    /// <summary>
    /// The heading level (1-6), or 0 for the preamble section.
    /// </summary>
    public int Level { get; set; }

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
    public string[] Headers { get; set; } = [];
    public List<string[]> Rows { get; set; } = [];
}
