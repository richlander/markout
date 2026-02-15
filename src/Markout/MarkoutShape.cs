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

    /// <summary>Key-value fields (scalar and formatted).</summary>
    Fields = 4,

    /// <summary>Compact multi-field layout.</summary>
    CompactFields = 8,

    /// <summary>Tables (streaming and batch).</summary>
    Tables = 16,

    /// <summary>Lists and arrays.</summary>
    Lists = 32,

    /// <summary>Tree structures (TreeNode hierarchies).</summary>
    Trees = 64,

    /// <summary>Code blocks (fenced regions).</summary>
    CodeBlocks = 128,

    /// <summary>Aligned name-value pairs.</summary>
    Pairs = 256,

    /// <summary>Comparative labeled measurements.</summary>
    Metrics = 512,

    /// <summary>Terms with explanatory text.</summary>
    Descriptions = 1024,

    /// <summary>Callout/admonition blocks.</summary>
    Callouts = 2048,

    /// <summary>Proportional composition charts.</summary>
    Breakdowns = 4096,

    /// <summary>Prose quotation blocks.</summary>
    Blockquotes = 8192,

    /// <summary>2D grid with row and column headers.</summary>
    Matrices = 16384,

    /// <summary>All shapes supported.</summary>
    All = Headings | Paragraphs | Fields | CompactFields | Tables | Lists | Trees | CodeBlocks | Pairs | Metrics | Descriptions | Callouts | Breakdowns | Blockquotes | Matrices
}
