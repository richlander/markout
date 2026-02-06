namespace Markout;

/// <summary>
/// Options for configuring Markout output rendering.
/// </summary>
public class MarkoutWriterOptions
{
    /// <summary>
    /// Gets the default options instance.
    /// </summary>
    public static MarkoutWriterOptions Default { get; } = new();

    /// <summary>
    /// Whether to include icons in tree nodes. Default is true.
    /// </summary>
    public bool IncludeIcons { get; set; } = true;

    /// <summary>
    /// Whether to include the description paragraph (from DescriptionProperty). Default is true.
    /// </summary>
    public bool IncludeDescription { get; set; } = true;

    /// <summary>
    /// Whether to render field names in bold. Default is false.
    /// </summary>
    public bool BoldFieldNames { get; set; }

    /// <summary>
    /// If set, only sections with these indices (1-based) are rendered.
    /// </summary>
    public HashSet<int>? IncludeSections { get; set; }

    /// <summary>
    /// If set, sections with these indices (1-based) are excluded from output.
    /// </summary>
    public HashSet<int>? ExcludeSections { get; set; }
}
