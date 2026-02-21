namespace Markout;

/// <summary>
/// Options for configuring Markout output rendering.
/// </summary>
public class MarkoutWriterOptions
{
    private bool _isReadOnly;
    private bool _includeBadges = true;
    private bool _includeDescription = true;
    private bool _boldFieldNames;
    private bool _prettyTables;
    private int? _maxItems;
    private HashSet<string>? _includeSections;
    private HashSet<string>? _excludeSections;
    private MarkoutProjection? _projection;

    /// <summary>
    /// Gets the default options instance. This instance is read-only.
    /// </summary>
    public static MarkoutWriterOptions Default { get; } = CreateDefaultOptions();

    private static MarkoutWriterOptions CreateDefaultOptions()
    {
        var options = new MarkoutWriterOptions();
        options.MakeReadOnly();
        return options;
    }

    /// <summary>
    /// Whether to include badges in tree nodes. Default is true.
    /// </summary>
    public bool IncludeBadges
    {
        get => _includeBadges;
        set
        {
            ThrowIfReadOnly();
            _includeBadges = value;
        }
    }

    /// <summary>
    /// Whether to include the description paragraph (from DescriptionProperty). Default is true.
    /// </summary>
    public bool IncludeDescription
    {
        get => _includeDescription;
        set
        {
            ThrowIfReadOnly();
            _includeDescription = value;
        }
    }

    /// <summary>
    /// Whether to render field names in bold. Default is false.
    /// </summary>
    public bool BoldFieldNames
    {
        get => _boldFieldNames;
        set
        {
            ThrowIfReadOnly();
            _boldFieldNames = value;
        }
    }

    /// <summary>
    /// Whether to pad table columns for aligned output. When true, pipe tables
    /// and plain tables are rendered with space-padded columns. Default is false.
    /// </summary>
    public bool PrettyTables
    {
        get => _prettyTables;
        set
        {
            ThrowIfReadOnly();
            _prettyTables = value;
        }
    }

    /// <summary>
    /// Maximum number of rows to display in tables. When set, tables are
    /// truncated after this many rows with an ellipsis showing the remaining count.
    /// Default is null (no limit).
    /// </summary>
    public int? MaxItems
    {
        get => _maxItems;
        set
        {
            ThrowIfReadOnly();
            _maxItems = value;
        }
    }

    /// <summary>
    /// If set, only sections whose heading text matches are rendered.
    /// An empty set means no named sections are included (preamble only).
    /// </summary>
    public HashSet<string>? IncludeSections
    {
        get => _includeSections;
        set
        {
            ThrowIfReadOnly();
            _includeSections = value;
        }
    }

    /// <summary>
    /// If set, sections whose heading text matches are excluded from output.
    /// </summary>
    public HashSet<string>? ExcludeSections
    {
        get => _excludeSections;
        set
        {
            ThrowIfReadOnly();
            _excludeSections = value;
        }
    }

    /// <summary>
    /// Projection for trimming output to specific columns and fields.
    /// When set, the projection filters table columns and scalar fields at render time.
    /// Works across all renderers.
    /// </summary>
    public MarkoutProjection? Projection
    {
        get => _projection;
        set
        {
            ThrowIfReadOnly();
            _projection = value;
        }
    }

    /// <summary>
    /// Gets whether this instance is read-only.
    /// </summary>
    public bool IsReadOnly => _isReadOnly;

    /// <summary>
    /// Marks this instance as read-only. After calling this method, any attempt to set
    /// a property will throw <see cref="InvalidOperationException"/>.
    /// </summary>
    public void MakeReadOnly() => _isReadOnly = true;

    private void ThrowIfReadOnly()
    {
        if (_isReadOnly)
            throw new InvalidOperationException("This MarkoutWriterOptions instance is read-only.");
    }
}
