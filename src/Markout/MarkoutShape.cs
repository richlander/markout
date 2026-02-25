namespace Markout;

/// <summary>
/// Flags indicating which rendering shapes a writer supports.
/// Writers declare their capabilities via <see cref="MarkoutWriter.SupportedShapes"/>.
/// Unsupported shapes produce a runtime diagnostic and are skipped.
/// </summary>
[Flags]
public enum MarkoutShape
{
    /// <summary>No shapes supported.</summary>
    None = 0,

    /// <summary>Headings (H1–H6).</summary>
    Headings = 1,

    /// <summary>Paragraphs and blank lines.</summary>
    Paragraphs = 2,

    /// <summary>
    /// Key-value fields. Includes individual fields, inline field lists, and field tables.
    /// Layout (multiline, inline, tabular) is a presentation concern, not a shape distinction.
    /// </summary>
    Fields = 4,

    /// <summary>Tables (streaming and batch).</summary>
    Tables = 16,

    /// <summary>Lists and arrays.</summary>
    Lists = 32,

    /// <summary>Tree structures (TreeNode hierarchies).</summary>
    Trees = 64,

    /// <summary>Fenced code regions.</summary>
    Code = 128,

    /// <summary>Comparative labeled measurements.</summary>
    Metrics = 512,

    /// <summary>Terms with explanatory text.</summary>
    Descriptions = 1024,

    /// <summary>Callout/admonition blocks.</summary>
    Callouts = 2048,

    /// <summary>Proportional composition charts.</summary>
    Breakdowns = 4096,

    /// <summary>Prose quotation blocks.</summary>
    Quotation = 8192,

    /// <summary>2D grid with row and column headers.</summary>
    Matrices = 16384,

    /// <summary>All shapes supported.</summary>
    All = Headings | Paragraphs | Fields | Tables | Lists | Trees | Code | Metrics | Descriptions | Callouts | Breakdowns | Quotation | Matrices
}
