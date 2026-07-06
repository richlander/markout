using MarkdownTable.Formatting;

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
    private MarkoutProjection? _projection;
    private MarkoutShape _suppressedShapes;
    private TableFormatterOptions? _tableOptions;
    private Func<MarkoutTableHeader, string>? _formatTableHeader;
    private MarkoutTableMode _tableMode;
    private MarkoutTableHeaderStyle _tableHeaderStyle;
    private bool _jsonTypedValues;
    private bool _omitEmptyJsonFields;
    private IReadOnlySet<int>? _jsonIdentityColumnIndices;
    private int _headingLevelOffset;
    private bool _inlineGoalStatus = true;

    /// <summary>Creates a new, writable options instance with default values.</summary>
    public MarkoutWriterOptions()
    {
    }

    private MarkoutWriterOptions(MarkoutWriterOptions source)
    {
        // Copies every setting into a fresh, writable instance (does not copy IsReadOnly).
        _includeBadges = source._includeBadges;
        _includeDescription = source._includeDescription;
        _boldFieldNames = source._boldFieldNames;
        _prettyTables = source._prettyTables;
        _maxItems = source._maxItems;
        _includeSections = source._includeSections;
        _projection = source._projection;
        _suppressedShapes = source._suppressedShapes;
        _tableOptions = source._tableOptions;
        _formatTableHeader = source._formatTableHeader;
        _tableMode = source._tableMode;
        _tableHeaderStyle = source._tableHeaderStyle;
        _jsonTypedValues = source._jsonTypedValues;
        _omitEmptyJsonFields = source._omitEmptyJsonFields;
        _jsonIdentityColumnIndices = source._jsonIdentityColumnIndices;
        _headingLevelOffset = source._headingLevelOffset;
        _inlineGoalStatus = source._inlineGoalStatus;
    }

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
    /// Whether a <see cref="MetricChange{T}"/> Markdown table renders goal state <em>densely</em>:
    /// the derived (or caller-supplied) status word is inlined into the Change cell
    /// (<c>0 → 7 (bad)</c>), a goal marker is appended to the metric label (<c>Failures (-)</c> for
    /// <see cref="Goal.Lower"/>, <c>(+)</c> for <see cref="Goal.Higher"/>), and the separate
    /// <c>Status</c> column is dropped. Default is <c>true</c>. Set <c>false</c> to keep the legacy
    /// <c>Metric | Change | Target | Status</c> layout. Structured (TSV/JSONL) output is unaffected.
    /// </summary>
    public bool InlineGoalStatus
    {
        get => _inlineGoalStatus;
        set
        {
            ThrowIfReadOnly();
            _inlineGoalStatus = value;
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
    /// Shapes to suppress warnings for when unsupported by the writer.
    /// Use when the caller knows the writer doesn't support certain shapes
    /// and wants to silence the diagnostic warnings globally.
    /// </summary>
    public MarkoutShape SuppressedShapes
    {
        get => _suppressedShapes;
        set
        {
            ThrowIfReadOnly();
            _suppressedShapes = value;
        }
    }

    /// <summary>
    /// Statistical table formatting options. When set with <see cref="PrettyTables"/>,
    /// column widths are calculated using percentile-based analysis instead of
    /// simple max-width, preventing outlier rows from stretching the entire table.
    /// </summary>
    public TableFormatterOptions? TableOptions
    {
        get => _tableOptions;
        set
        {
            ThrowIfReadOnly();
            _tableOptions = value;
        }
    }

    /// <summary>
    /// Optional callback for rewriting table headers before they are rendered.
    /// The callback receives both the stable source name and display name.
    /// </summary>
    public Func<MarkoutTableHeader, string>? FormatTableHeader
    {
        get => _formatTableHeader;
        set
        {
            ThrowIfReadOnly();
            _formatTableHeader = value;
        }
    }

    /// <summary>
    /// Rendering mode for <see cref="TableFormatter"/>. Default is pretty, space-padded columns.
    /// </summary>
    public MarkoutTableMode TableMode
    {
        get => _tableMode;
        set
        {
            ThrowIfReadOnly();
            _tableMode = value;
        }
    }

    /// <summary>
    /// Header naming style for table formatters. Auto uses stable names for TSV
    /// and JSONL, and display labels for pretty tables.
    /// </summary>
    public MarkoutTableHeaderStyle TableHeaderStyle
    {
        get => _tableHeaderStyle;
        set
        {
            ThrowIfReadOnly();
            _tableHeaderStyle = value;
        }
    }

    /// <summary>
    /// Offset added to every heading level at render time. Default is 0, which
    /// leaves levels unchanged.
    /// <para>
    /// Set to 1 to render a serialized document as a nested section ("print a
    /// section, elide the H1"): the document title drops from H1 to H2 and any
    /// sections shift down one level, so the output can be appended under an
    /// existing document without introducing a second H1. Rendered levels are
    /// clamped to the valid 1–6 range; logical section identity (used by
    /// <see cref="IncludeSections"/>) is unaffected.
    /// </para>
    /// </summary>
    public int HeadingLevelOffset
    {
        get => _headingLevelOffset;
        set
        {
            ThrowIfReadOnly();
            _headingLevelOffset = value;
        }
    }

    /// <summary>
    /// When rendering JSONL, emit cell values that parse as a number or boolean as JSON numbers
    /// or booleans instead of quoted strings. Default is false (all values are strings).
    /// Useful for composite-cell decomposition, where columns such as <c>before</c>/<c>after</c>/
    /// <c>count</c>/<c>pct</c> are numeric. Coercion is text-based, so opt in only when numeric-
    /// looking strings should become numbers.
    /// </summary>
    public bool JsonTypedValues
    {
        get => _jsonTypedValues;
        set
        {
            ThrowIfReadOnly();
            _jsonTypedValues = value;
        }
    }

    /// <summary>
    /// When rendering JSONL, omit fields whose value is empty so each record contains only its
    /// populated keys (heterogeneous records). Default is false (every column is emitted). Useful
    /// for composite-cell decomposition, where a card's rows have different shapes; TSV keeps the
    /// uniform column union regardless.
    /// </summary>
    public bool OmitEmptyJsonFields
    {
        get => _omitEmptyJsonFields;
        set
        {
            ThrowIfReadOnly();
            _omitEmptyJsonFields = value;
        }
    }

    /// <summary>
    /// Gets whether this instance is read-only.
    /// </summary>
    public bool IsReadOnly => _isReadOnly;

    /// <summary>
    /// Projected column indices that are identity/label columns and are always emitted as JSON
    /// strings, even when <see cref="JsonTypedValues"/> is set. Set by composite-cell decomposition
    /// after projection so the <c>field</c> column stays a stable string regardless of column order.
    /// </summary>
    internal IReadOnlySet<int>? JsonIdentityColumnIndices => _jsonIdentityColumnIndices;

    /// <summary>Returns a writable copy of these options with <see cref="JsonIdentityColumnIndices"/> set.</summary>
    internal MarkoutWriterOptions WithJsonIdentityColumnIndices(IReadOnlySet<int>? indices)
    {
        var copy = new MarkoutWriterOptions(this);
        copy._jsonIdentityColumnIndices = indices;
        return copy;
    }

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
