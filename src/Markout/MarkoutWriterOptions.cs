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
    private MarkoutRowSelection? _rowSelection;
    private HashSet<string>? _includeSections;
    private IReadOnlyList<string>? _sectionOrder;
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
    private MarkoutGlyphs _glyphs = MarkoutGlyphs.Default;
    private Func<GlyphContext, string>? _composeGlyph;
    private string _newLine = Environment.NewLine;

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
        _rowSelection = source._rowSelection;
        _includeSections = source._includeSections;
        _sectionOrder = source._sectionOrder;
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
        _glyphs = source._glyphs;
        _composeGlyph = source._composeGlyph;
        _newLine = source._newLine;
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
    /// the derived (or caller-supplied) polarity is inlined into the Change cell, a goal marker is
    /// appended to the metric label, and the separate <c>Status</c> column is dropped. On rich sinks
    /// (<see cref="Formatting.IGlyphFormatter"/>: Markdown/ANSI/Unicode) the marker and polarity are
    /// the configured <see cref="Glyphs"/> (<c>Failures ↓</c>, <c>0 → 7 ✗</c>); on plain text they are
    /// the ASCII <c>(-)</c>/<c>(+)</c> marker and the status word (<c>0 → 7 (bad)</c>). Default is
    /// <c>true</c>. Set <c>false</c> to keep the legacy <c>Metric | Change | Target | Status</c> layout.
    /// Structured (TSV/JSONL) output is unaffected and always keeps the <c>direction</c>/<c>status</c> words.
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
    /// The glyph set used to render goal (<c>↑</c>/<c>↓</c>) and polarity (<c>✓</c>/<c>✗</c>) indicators
    /// in rich document sinks (formatters implementing <see cref="Formatting.IGlyphFormatter"/>:
    /// Markdown, ANSI, Unicode). Defaults to <see cref="MarkoutGlyphs.Default"/>. Plain text and
    /// decomposing sinks (TSV/JSONL) ignore this and keep <c>direction</c>/<c>status</c> slug words.
    /// </summary>
    public MarkoutGlyphs Glyphs
    {
        get => _glyphs;
        set
        {
            ThrowIfReadOnly();
            _glyphs = value ?? MarkoutGlyphs.Default;
        }
    }

    /// <summary>
    /// An optional callback that composes the final rendered string for a goal/polarity indicator on
    /// rich sinks (formatters implementing <see cref="Formatting.IGlyphFormatter"/>). It receives the
    /// base <see cref="GlyphContext.Text"/> and the resolved <see cref="GlyphContext.Glyph"/> (from
    /// <see cref="Glyphs"/>) and returns the combined text — letting a caller replace the glyph with a
    /// word, integrate it into the text, or condition it on the <see cref="GlyphContext.Slot"/>/
    /// <see cref="GlyphContext.Status"/>. Default is <c>null</c> (append the glyph with a space via
    /// <see cref="GlyphContext.Combine"/>). Ignored by plain text and decomposing sinks (TSV/JSONL),
    /// which keep the <c>direction</c>/<c>status</c> slug words.
    /// </summary>
    public Func<GlyphContext, string>? ComposeGlyph
    {
        get => _composeGlyph;
        set
        {
            ThrowIfReadOnly();
            _composeGlyph = value;
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
    /// Which data rows tables emit, preserving headings and header rows.
    /// Default is null (every row).
    ///
    /// <para>
    /// This is <em>selection</em>, not summarization, which is what separates it
    /// from <see cref="MaxItems"/>: a windowed table emits no ellipsis row and
    /// reports no skipped count, so the output stays machine-consumable in every
    /// <see cref="MarkoutTableMode"/>. When both are set the window selects
    /// first and <see cref="MaxItems"/> then caps the selection, so any ellipsis
    /// reports only the rows the cap dropped.
    /// </para>
    ///
    /// <para>
    /// Assigning this property replaces the complete selection, including any
    /// constraints previously added through <see cref="IntersectRowWindow"/>.
    /// Assigning null clears the selection.
    /// </para>
    /// </summary>
    public MarkoutRowWindow? RowWindow
    {
        get => _rowSelection?.Primary;
        set
        {
            ThrowIfReadOnly();
            _rowSelection = value is { } window ? new MarkoutRowSelection(window) : null;
        }
    }

    /// <summary>
    /// Adds a row-window constraint to the current selection.
    /// </summary>
    /// <param name="window">The window to intersect with the current selection.</param>
    /// <remarks>
    /// Each window resolves independently against the table's original row count,
    /// so intersection never renumbers rows selected by an earlier window. If
    /// <see cref="RowWindow"/> is null, this method establishes the primary window.
    /// Assigning <see cref="RowWindow"/> afterwards replaces the complete selection.
    /// </remarks>
    public void IntersectRowWindow(MarkoutRowWindow window)
    {
        ThrowIfReadOnly();
        _rowSelection = _rowSelection?.Intersect(window) ?? new MarkoutRowSelection(window);
    }

    internal MarkoutRowSelection? RowSelection => _rowSelection;

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
    /// If set, sections named here are emitted first, in this order; every other section
    /// follows in the order it was written. Matching is case-insensitive, and naming a
    /// section that the document never writes is not an error.
    ///
    /// <para>
    /// Ordering is applied at the writer seam rather than to rendered text, so it works
    /// for every format — including TSV and JSONL, whose output carries no heading to
    /// reorder. Setting it buffers the whole document, because the last section written
    /// may be the first one emitted.
    /// </para>
    /// </summary>
    public IReadOnlyList<string>? SectionOrder
    {
        get => _sectionOrder;
        set
        {
            ThrowIfReadOnly();

            // Copied rather than referenced: MakeReadOnly would otherwise freeze only
            // the reference, and a caller still holding the list could change the
            // rendered order of a frozen options object.
            _sectionOrder = value is null ? null : [.. value];
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
    /// Records shapes the caller knows the writer does not support.
    /// </summary>
    /// <remarks>
    /// Reserved: the value is stored and copied but is not read by any current code path,
    /// because unsupported shapes are already silent — the corresponding <c>Write</c> method
    /// writes nothing and returns <c>false</c> without emitting a diagnostic.
    /// </remarks>
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
    /// Optional callback for rewriting visual table headers before they are rendered.
    /// The callback receives both the stable source name and display name. TSV and JSONL
    /// ignore this callback so presentation cannot change their structured column keys.
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
    /// The line terminator used by string-returning serialization overloads and in-memory
    /// <see cref="MarkoutWriter"/> instances. Defaults to <see cref="Environment.NewLine"/>.
    /// This setting does not modify a caller-supplied <see cref="TextWriter"/>.
    /// </summary>
    public string NewLine
    {
        get => _newLine;
        set
        {
            ThrowIfReadOnly();
            ArgumentNullException.ThrowIfNull(value);
            _newLine = value;
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
