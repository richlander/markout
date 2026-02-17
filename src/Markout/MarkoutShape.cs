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

    /// <summary>Multi-field summary line.</summary>
    FieldList = 8,

    /// <summary>Tables (streaming and batch).</summary>
    Tables = 16,

    /// <summary>Lists and arrays.</summary>
    Lists = 32,

    /// <summary>Tree structures (TreeNode hierarchies).</summary>
    Trees = 64,

    /// <summary>Code regions.</summary>
    Code = 128,

    /// <summary>Horizontal bar charts.</summary>
    BarCharts = 512,

    /// <summary>Labeled descriptive lists.</summary>
    LabeledLists = 1024,

    /// <summary>Callout/admonition blocks.</summary>
    Callouts = 2048,

    /// <summary>Distribution/stacked bars.</summary>
    Distributions = 4096,

    /// <summary>All shapes supported.</summary>
    All = Headings | Paragraphs | Fields | FieldList | Tables | Lists | Trees | Code | BarCharts | LabeledLists | Callouts | Distributions
}
