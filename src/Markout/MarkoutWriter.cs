using System.Runtime.InteropServices;
using Markout.Formatting;

namespace Markout;

/// <summary>
/// Composes a formatter via capability interfaces, dispatching Write methods
/// to the appropriate interface when implemented by the formatter.
/// Returns <c>bool</c> from all Write methods: <c>true</c> = rendered (or filtered),
/// <c>false</c> = unsupported shape (nothing written).
/// </summary>
/// <remarks>
/// This class is not thread-safe. Use separate instances for concurrent operations.
/// </remarks>
public class MarkoutWriter
{
    private readonly TextWriter _writer;
    private readonly TextWriter _target;
    private readonly SectionBufferingWriter? _sectionBuffer;
    private readonly IMarkoutFormatter _formatter;
    private readonly MarkoutWriterOptions _options;

    // State
    private bool _hasContentValue;
    private bool _needsBlankLine;
    private bool _inTable;
    private bool _inCode;
    // Number of leading identity columns for the in-progress table write (composite decompose).
    private int _pendingJsonIdentityColumns;

    // Section tracking
    private string? _currentSectionName;
    private bool _sectionExcluded;

    // Table delegation
    private TableWriter? _tableWriter;
    private int[]? _columnMap;

    // Pending section (deferred until content written)
    private PendingSectionHeading? _pendingSection;

    /// <summary>
    /// Creates a writer that writes to the specified TextWriter.
    /// </summary>
    public MarkoutWriter(TextWriter writer, IMarkoutFormatter formatter, MarkoutWriterOptions? options = null)
    {
        var opts = options ?? new MarkoutWriterOptions();

        _target = writer;
        // A requested order can put the last section written first, so ordering cannot
        // be decided until the document is complete. Only pay for that when asked.
        _sectionBuffer = opts.SectionOrder is { Count: > 0 } ? new SectionBufferingWriter(writer) : null;
        _writer = _sectionBuffer ?? writer;
        _formatter = formatter;
        _options = opts;
    }

    /// <summary>
    /// Creates a writer that builds output in memory. Use ToString() to get the result.
    /// </summary>
    public MarkoutWriter(IMarkoutFormatter formatter, MarkoutWriterOptions? options = null)
        : this(new StringWriter(), formatter, options)
    {
    }

    /// <summary>
    /// Gets the writer options.
    /// </summary>
    public MarkoutWriterOptions Options => _options;

    /// <summary>
    /// Whether the active formatter decomposes composite cells into typed columns (TSV/JSONL). Element-table
    /// serialization uses this to decompose composite columns into typed sub-columns for structured output
    /// while keeping the dense string for document formatters.
    /// </summary>
    public bool DecomposesCompositeCells
        => _formatter is Formatting.ICompositeCellFormatter { DecomposesCompositeCells: true };

    /// <summary>
    /// Gets whether descriptions should be included in output.
    /// </summary>
    public bool IncludeDescription => _options.IncludeDescription;

    /// <summary>
    /// Gets whether badges should be included in output.
    /// </summary>
    public bool IncludeBadges => _options.IncludeBadges;

    /// <summary>
    /// Gets whether field names should be bold.
    /// </summary>
    public bool BoldFieldNames => _options.BoldFieldNames;

    // ── Headings ──

    /// <summary>
    /// Writes a heading at the specified level.
    /// </summary>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support headings.</returns>
    public bool WriteHeading(int level, string text) => WriteHeading(level, text, null);

    /// <summary>
    /// Writes a heading at the specified level with optional context.
    /// </summary>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support headings.</returns>
    public bool WriteHeading(int level, string text, string? context)
    {
        if (level < 1 || level > 6)
            throw new ArgumentOutOfRangeException(nameof(level), "Heading level must be between 1 and 6.");

        UpdateSectionState(level, text);

        if (_sectionExcluded)
            return true; // filtered, not unsupported

        if (_formatter is not IHeadingFormatter hf)
            return false;

        SeparateFromPrecedingContent();

        hf.FormatHeading(_writer, RenderHeadingLevel(level), text, context);
        _writer.WriteLine();
        _hasContent = true;
        _needsBlankLine = true;
        return true;
    }

    // ── Sections ──

    /// <summary>
    /// Begins a section with a heading. The heading may be deferred until content is written
    /// when projection is active.
    /// </summary>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support headings.</returns>
    public bool WriteSectionStart(int level, string text, string? context = null, bool headless = false)
    {
        UpdateSectionState(level, text);

        if (_sectionExcluded)
            return true;

        if (headless)
            return true;

        if (_formatter is not IHeadingFormatter)
            return false;

        if (_options.Projection != null)
        {
            _pendingSection = new PendingSectionHeading(level, text, context);
            return true;
        }

        WriteSectionHeading(level, text, context);
        return true;
    }

    /// <summary>
    /// Ends a section previously started with WriteSectionStart.
    /// </summary>
    public void WriteSectionEnd()
    {
        _pendingSection = null;
    }

    // ── Paragraphs ──

    /// <summary>
    /// Writes a paragraph of text.
    /// </summary>
    /// <returns><c>true</c> if rendered; <c>false</c> if the formatter lacks paragraph support.</returns>
    public bool WriteParagraph(string? text)
    {
        if (string.IsNullOrEmpty(text) || _sectionExcluded)
            return true;

        if (_formatter is not IBlockFormatter bf)
            return false;

        EnsureBlankLineIfNeeded();
        bf.FormatParagraph(_writer, text);
        _needsBlankLine = true;
        _hasContent = true;
        return true;
    }

    // ── Fields ──

    /// <summary>
    /// Writes a single key-value field.
    /// </summary>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support fields.</returns>
    public bool WriteField(string key, string value)
    {
        if (_sectionExcluded)
            return true;

        ReadOnlySpan<MarkoutField> field = [new(key, value)];
        ReadOnlySpan<MarkoutField> toRender = field;
        if (NeedsFieldProjection)
            toRender = ProjectFields(field);
        if (toRender.Length == 0)
            return true;

        // Cascade: IFieldFormatter → ITableFormatter → IStreamingTableFormatter
        if (_formatter is IFieldFormatter ff)
        {
            EnsureBlankLineIfNeeded();
            ff.FormatFieldName(_writer, toRender[0].Key, _options.BoldFieldNames);
            _writer.WriteLine(toRender[0].Value);
            _hasContent = true;
            return true;
        }

        return RenderFieldsAsTable(toRender);
    }

    /// <summary>
    /// Writes multiple key-value fields, each on its own line.
    /// </summary>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support fields.</returns>
    public bool WriteFields(params ReadOnlySpan<MarkoutField> fields)
    {
        if (_sectionExcluded || fields.Length == 0)
            return true;

        ReadOnlySpan<MarkoutField> toRender = fields;
        if (NeedsFieldProjection)
            toRender = ProjectFields(fields);
        if (toRender.Length == 0)
            return true;

        // Cascade: IFieldFormatter → ITableFormatter → IStreamingTableFormatter
        if (_formatter is IFieldFormatter ff)
        {
            EnsureBlankLineIfNeeded();
            ff.FormatFields(_writer, toRender, _options.BoldFieldNames);
            _needsBlankLine = true;
            _hasContent = true;
            return true;
        }

        return RenderFieldsAsTable(toRender);
    }

    /// <summary>
    /// Writes multiple key-value fields on a single line, separated by pipes.
    /// Falls back to table rendering if the formatter lacks <see cref="IFieldFormatter"/>.
    /// </summary>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support fields or tables.</returns>
    public bool WriteFieldsInline(params ReadOnlySpan<MarkoutField> fields)
    {
        if (_sectionExcluded || fields.Length == 0)
            return true;

        ReadOnlySpan<MarkoutField> toRender = fields;
        if (NeedsFieldProjection)
            toRender = ProjectFields(fields);
        if (toRender.Length == 0)
            return true;

        // Cascade: IFieldFormatter (inline) → ITableFormatter → IStreamingTableFormatter
        if (_formatter is IFieldFormatter ff)
        {
            EnsureBlankLineIfNeeded();

            for (int i = 0; i < toRender.Length; i++)
            {
                if (i > 0)
                    _writer.Write(" | ");
                ff.FormatFieldName(_writer, toRender[i].Key, _options.BoldFieldNames);
                _writer.Write(toRender[i].Value);
            }

            _writer.WriteLine();
            _needsBlankLine = true;
            _hasContent = true;
            return true;
        }

        return RenderFieldsAsTable(toRender);
    }

    /// <summary>
    /// Writes multiple key-value fields as a bulleted list.
    /// Falls back to table rendering if the formatter lacks <see cref="IFieldFormatter"/>.
    /// </summary>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support fields or tables.</returns>
    public bool WriteFieldsBulleted(params ReadOnlySpan<MarkoutField> fields)
    {
        if (_sectionExcluded || fields.Length == 0)
            return true;

        ReadOnlySpan<MarkoutField> toRender = fields;
        if (NeedsFieldProjection)
            toRender = ProjectFields(fields);
        if (toRender.Length == 0)
            return true;

        // Cascade: IFieldFormatter (bulleted) → ITableFormatter → IStreamingTableFormatter
        if (_formatter is IFieldFormatter ff)
        {
            EnsureBlankLineIfNeeded();

            for (int i = 0; i < toRender.Length; i++)
            {
                _writer.Write("- ");
                ff.FormatFieldName(_writer, toRender[i].Key, _options.BoldFieldNames);
                _writer.WriteLine(toRender[i].Value);
            }

            _needsBlankLine = true;
            _hasContent = true;
            return true;
        }

        return RenderFieldsAsTable(toRender);
    }

    /// <summary>
    /// Writes multiple key-value fields as a numbered list.
    /// Falls back to table rendering if the formatter lacks <see cref="IFieldFormatter"/>.
    /// </summary>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support fields or tables.</returns>
    public bool WriteFieldsNumbered(params ReadOnlySpan<MarkoutField> fields)
    {
        if (_sectionExcluded || fields.Length == 0)
            return true;

        ReadOnlySpan<MarkoutField> toRender = fields;
        if (NeedsFieldProjection)
            toRender = ProjectFields(fields);
        if (toRender.Length == 0)
            return true;

        // Cascade: IFieldFormatter (numbered) → ITableFormatter → IStreamingTableFormatter
        if (_formatter is IFieldFormatter ff)
        {
            EnsureBlankLineIfNeeded();

            for (int i = 0; i < toRender.Length; i++)
            {
                _writer.Write(i + 1);
                _writer.Write(". ");
                ff.FormatFieldName(_writer, toRender[i].Key, _options.BoldFieldNames);
                _writer.WriteLine(toRender[i].Value);
            }

            _needsBlankLine = true;
            _hasContent = true;
            return true;
        }

        return RenderFieldsAsTable(toRender);
    }

    /// <summary>
    /// Writes fields as a two-column Field/Value table.
    /// </summary>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support tables.</returns>
    public bool WriteFieldsTable(params ReadOnlySpan<MarkoutField> fields)
    {
        if (fields.Length == 0)
            return true;

        ReadOnlySpan<MarkoutField> toRender = fields;
        if (NeedsFieldProjection)
            toRender = ProjectFields(fields);
        if (toRender.Length == 0)
            return true;

        var headers = new[] { "Field", "Value" };
        var rows = new List<string[]>(toRender.Length);
        foreach (var field in toRender)
            rows.Add([field.Key, field.Value]);

        return WriteTable(headers, ["Field", "Value"], rows);
    }

    /// <summary>
    /// Writes a composite-cell table: one row per <see cref="MarkoutCompositeRow"/>. Formatters
    /// that decompose composites (<see cref="Formatting.ICompositeCellFormatter"/>) emit one
    /// typed column per decomposed field (union across rows, blank where absent); all others
    /// render a dense two-column <c>Field | Value</c> table.
    /// </summary>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support tables.</returns>
    public bool WriteCompositeTable(params ReadOnlySpan<MarkoutCompositeRow> rows)
    {
        if (_sectionExcluded || rows.Length == 0)
            return true;

        if (_formatter is not ITableFormatter and not IStreamingTableFormatter)
            return false;

        var projected = ProjectCompositeRows(rows);
        if (projected.Length == 0)
            return true;

        if (_formatter is Formatting.ICompositeCellFormatter { DecomposesCompositeCells: true })
            return WriteDecomposedCompositeTable(projected);

        var denseRows = new List<string[]>(projected.Length);
        foreach (var row in projected)
        {
            var sw = new StringWriter();
            row.Cell?.FormatInline(sw, ApplyGlyphs(row.Format));
            denseRows.Add([row.Label, sw.ToString()]);
        }

        return WriteTable(["Field", "Value"], ["Field", "Value"], denseRows);
    }

    // Applies field projection (WithFields/WithoutFields) to composite rows by their label,
    // mirroring how WriteFieldsTable projects Field | Value rows.
    private MarkoutCompositeRow[] ProjectCompositeRows(ReadOnlySpan<MarkoutCompositeRow> rows)
    {
        if (_options.Projection == null)
            return rows.ToArray();

        var asFields = new MarkoutField[rows.Length];
        for (int i = 0; i < rows.Length; i++)
            asFields[i] = new MarkoutField(rows[i].Label, "");

        var kept = ProjectFields(asFields);
        var rowArray = rows.ToArray();
        var result = new List<MarkoutCompositeRow>(kept.Length);
        var consumed = new bool[rowArray.Length];
        foreach (var field in kept)
        {
            for (int i = 0; i < rowArray.Length; i++)
            {
                if (!consumed[i] && rowArray[i].Label == field.Key)
                {
                    result.Add(rowArray[i]);
                    consumed[i] = true;
                    break;
                }
            }
        }

        return result.ToArray();
    }

    private bool WriteDecomposedCompositeTable(ReadOnlySpan<MarkoutCompositeRow> rows)
    {
        // First pass: decompose each row and collect the ordered union of column keys.
        var keyOrder = new List<string>();
        var keyIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        var decomposedRows = new List<MarkoutField>[rows.Length];
        var labels = new string[rows.Length];

        for (int r = 0; r < rows.Length; r++)
        {
            var fields = new List<MarkoutField>();
            rows[r].Cell?.Decompose(fields, null, rows[r].Format);
            decomposedRows[r] = fields;
            labels[r] = rows[r].Label;
            foreach (var field in fields)
            {
                if (!keyIndex.ContainsKey(field.Key))
                {
                    keyIndex[field.Key] = keyOrder.Count;
                    keyOrder.Add(field.Key);
                }
            }
        }

        // Leading "field" column names the property/row; remaining columns are the union keys.
        // Stable header names are snake-cased for output, so disambiguate any that collide after
        // normalization (or with the reserved leading column) to avoid silent duplicate keys.
        var headers = new string[keyOrder.Count + 1];
        var headerNames = new string[keyOrder.Count + 1];
        headers[0] = "Field";
        headerNames[0] = "field";
        var usedStableKeys = new HashSet<string>(StringComparer.Ordinal) { "field" };

        for (int i = 0; i < keyOrder.Count; i++)
        {
            headers[i + 1] = keyOrder[i];

            // TableWriter re-applies ToSnakeCase to stable names, so use a non-empty fixed point
            // (ToSnakeCase(stable) == stable) and dedupe on it to guarantee unique output keys.
            var stable = Formatting.FormatHelper.ToSnakeCase(keyOrder[i]);
            if (string.IsNullOrEmpty(stable))
                stable = "column";
            if (!usedStableKeys.Add(stable))
            {
                int suffix = 2;
                string candidate;
                do { candidate = stable + "_" + suffix++; } while (!usedStableKeys.Add(candidate));
                stable = candidate;
            }
            headerNames[i + 1] = stable;
        }

        var outRows = new List<string[]>(rows.Length);
        for (int r = 0; r < decomposedRows.Length; r++)
        {
            var values = new string[keyOrder.Count + 1];
            values[0] = labels[r];
            for (int i = 1; i < values.Length; i++)
                values[i] = "";
            foreach (var field in decomposedRows[r])
                values[keyIndex[field.Key] + 1] = field.Value;
            outRows.Add(values);
        }

        // Column 0 is the row identity/label; keep it a JSON string even under JsonTypedValues.
        _pendingJsonIdentityColumns = 1;
        try
        {
            return WriteTable(headers, headerNames, outRows);
        }
        finally
        {
            _pendingJsonIdentityColumns = 0;
        }
    }

    /// <summary>
    /// Writes a multi-source pivot table: each <see cref="MultiSourceRow"/> carries named-role
    /// cells. Formatters that decompose composites (<see cref="Formatting.ICompositeCellFormatter"/>,
    /// i.e. TSV/JSONL) emit one flat record per row with <c>{role}_{field}</c> columns (each cell
    /// decomposed with its role as the side); all others render a wide table with one column per
    /// role — caller-supplied role order (first appearance across rows), a role absent from a row
    /// rendered as <c>-</c>, and each cell the dense render of that role's value.
    /// </summary>
    /// <param name="labelHeader">The header/identity-column name for the row labels.</param>
    /// <param name="rows">The multi-source rows.</param>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support tables.</returns>
    public bool WriteMultiSourceTable(string labelHeader, IReadOnlyList<MultiSourceRow> rows)
        => WriteMultiSourceTable(labelHeader, rows, null);

    /// <summary>
    /// As <see cref="WriteMultiSourceTable(string, IReadOnlyList{MultiSourceRow})"/>, plus an optional
    /// <paramref name="structuredSection"/>: when non-null, decomposed (TSV/JSONL) rows gain a leading
    /// <c>section</c> column carrying that value. Markdown/dense output is unaffected.
    /// </summary>
    /// <param name="labelHeader">The header/identity-column name for the row labels.</param>
    /// <param name="rows">The multi-source rows.</param>
    /// <param name="structuredSection">A section discriminator prepended to decomposed rows, or <c>null</c>.</param>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support tables.</returns>
    public bool WriteMultiSourceTable(string labelHeader, IReadOnlyList<MultiSourceRow> rows, string? structuredSection)
    {
        if (_sectionExcluded || rows.Count == 0)
            return true;

        if (_formatter is not ITableFormatter and not IStreamingTableFormatter)
            return false;

        // Column axis = roles in caller (first-appearance) order across the whole row collection.
        var roleOrder = new List<string>();
        var roleIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            if (row.Sources is null)
                continue;
            foreach (var source in row.Sources)
                if (source.Role is not null && roleIndex.TryAdd(source.Role, roleOrder.Count))
                    roleOrder.Add(source.Role);
        }

        if (roleOrder.Count == 0)
            return true;

        if (_formatter is Formatting.ICompositeCellFormatter { DecomposesCompositeCells: true })
            return WriteDecomposedMultiSourceTable(labelHeader, rows, roleOrder, structuredSection);

        return WriteDenseMultiSourceTable(labelHeader, rows, roleOrder, roleIndex);
    }

    // Document formatters: one column per role; each cell the dense render of that role's value.
    private bool WriteDenseMultiSourceTable(
        string labelHeader, IReadOnlyList<MultiSourceRow> rows, List<string> roleOrder, Dictionary<string, int> roleIndex)
    {
        var glyphs = SupportsGlyphs;
        var headers = new string[roleOrder.Count + 1];
        headers[0] = labelHeader;
        for (int i = 0; i < roleOrder.Count; i++)
            headers[i + 1] = roleOrder[i];

        var outRows = new List<string[]>(rows.Count);
        foreach (var row in rows)
        {
            var values = new string[roleOrder.Count + 1];
            values[0] = glyphs ? LabelWithGoalGlyph(row.Label, row.Goal) : row.Label;
            for (int i = 1; i < values.Length; i++)
                values[i] = "-";

            // Track each column's raw scalar (by role index) so a pairwise polarity glyph can compare
            // a cell to the previous populated scalar column under the row's goal.
            var scalars = glyphs && row.Goal != Goal.Context ? new double?[roleOrder.Count] : null;
            var emphasis = SupportsEmphasis ? row.Emphasis : null;

            if (row.Sources is not null)
            {
                foreach (var source in row.Sources)
                {
                    if (source.Role is null || source.Value is null || !roleIndex.TryGetValue(source.Role, out var idx))
                        continue;
                    var sw = new StringWriter();
                    source.Value.FormatInline(sw, ApplyGlyphs(source.Format));
                    var text = sw.ToString();

                    double? scalar = source.Value is ScalarSourceCell cell && CellText.TryScalarDouble(cell.RawValue, out var d)
                        ? d : null;
                    // Emphasize the value before any pairwise glyph trails it, so "**5** ✓" reads bold-then-status.
                    if (emphasis is not null && scalar is { } ev && emphasis.IsSatisfiedBy(ev))
                        text = Emphasize(text);
                    values[idx + 1] = text;
                    if (scalars is not null && scalar is { } sv)
                        scalars[idx] = sv;
                }
            }

            if (scalars is not null)
                AppendPairwisePolarity(values, scalars, row.Goal, row.Noise);

            outRows.Add(values);
        }

        return WriteTable(headers, outRows);
    }

    /// <summary>Appends the row's goal glyph to a label; nothing for <see cref="Goal.Context"/>.</summary>
    private string LabelWithGoalGlyph(string label, Goal goal)
    {
        if (goal == Goal.Context)
            return label;
        var glyph = _options.Glyphs.ForGoal(goal);
        return Compose(GlyphSlot.GoalLabel, label, glyph, goal, GateStatus.Unknown);
    }

    /// <summary>Appends a pairwise polarity glyph to each populated scalar cell, comparing it to the
    /// previous populated scalar column under <paramref name="goal"/>. The first populated column and
    /// unchanged/neutral cells get no glyph.</summary>
    private void AppendPairwisePolarity(string[] values, double?[] scalars, Goal goal, double noise)
    {
        double? previous = null;
        for (int i = 0; i < scalars.Length; i++)
        {
            if (scalars[i] is not { } current)
                continue;
            if (previous is { } prev &&
                GoalDerivation.TryDerive(prev, current, goal, noise, out _, out var status))
            {
                var glyph = _options.Glyphs.ForStatus(status);
                values[i + 1] = Compose(GlyphSlot.MovementCell, values[i + 1], glyph, goal, status);
            }
            previous = current;
        }
    }

    // Decomposing formatters (TSV/JSONL): one flat record per row, {role}_{field} columns.
    private bool WriteDecomposedMultiSourceTable(
        string labelHeader, IReadOnlyList<MultiSourceRow> rows, List<string> roleOrder, string? structuredSection)
    {
        // Decompose each source with side = role, tagging fields with their owning role so the
        // column union can be ordered role-major (caller role order), then field order within a role.
        var perRow = new List<(string Role, List<MarkoutField> Fields)>[rows.Count];
        var labels = new string[rows.Count];
        for (int r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            labels[r] = row.Label;
            var tagged = new List<(string, List<MarkoutField>)>();
            if (row.Sources is not null)
            {
                foreach (var source in row.Sources)
                {
                    if (source.Role is null || source.Value is null)
                        continue;
                    var fields = new List<MarkoutField>();
                    source.Value.Decompose(fields, source.Role, source.Format);
                    tagged.Add((source.Role, fields));
                }
            }
            perRow[r] = tagged;
        }

        // Assign every distinct (role, composed-field-key) a unique output column, deterministically
        // by global role order (roleOrder) then first-seen field order — so the same source maps to
        // the same column in EVERY row regardless of per-row source order or presence. Colliding
        // composed keys (role "a"+"b_c" vs role "a_b"+"c" → both "a_b_c") get a deterministic "_N"
        // suffix on the later role, applied globally rather than per row.
        var columnOf = new Dictionary<(string Role, string Field), string>();
        var takenColumns = new HashSet<string>(StringComparer.Ordinal);
        var keyOrder = new List<string>();
        foreach (var role in roleOrder)
            foreach (var tagged in perRow)
                foreach (var (owner, fields) in tagged)
                    if (owner == role)
                        foreach (var field in fields)
                        {
                            var id = (role, field.Key);
                            if (columnOf.ContainsKey(id))
                                continue;
                            var column = field.Key;
                            if (!takenColumns.Add(column))
                            {
                                int suffix = 2;
                                string candidate;
                                do { candidate = field.Key + "_" + suffix++; } while (!takenColumns.Add(candidate));
                                column = candidate;
                            }
                            columnOf[id] = column;
                            keyOrder.Add(column);
                        }

        // Flatten each row into (column, value) using the global map.
        var perRowFlat = new List<MarkoutField>[perRow.Length];
        for (int r = 0; r < perRow.Length; r++)
        {
            var flat = new List<MarkoutField>();
            foreach (var (role, fields) in perRow[r])
                foreach (var field in fields)
                    flat.Add(new MarkoutField(columnOf[(role, field.Key)], field.Value));
            perRowFlat[r] = flat;
        }

        return WriteDecomposedFieldTable(labelHeader, labels, perRowFlat, keyOrder, structuredSection);
    }

    // Shared emitter for flat decomposed record tables (multi-source, metric-change): a leading
    // identity column (from labelHeader) plus one column per key in keyOrder. Stable header names
    // are snake-cased and de-duped because TableWriter re-applies ToSnakeCase to header keys for
    // TSV/JSONL; the leading identity column stays a JSON string.
    private bool WriteDecomposedFieldTable(
        string labelHeader, IReadOnlyList<string> labels, IReadOnlyList<List<MarkoutField>> perRowFields, IReadOnlyList<string> keyOrder,
        string? structuredSection = null)
    {
        var keyIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < keyOrder.Count; i++)
            keyIndex[keyOrder[i]] = i;

        // Leading identity columns: an optional "section" discriminator (for multiplexed structured
        // streams), then the row label. Both stay JSON strings (never coerced under JsonTypedValues).
        int lead = structuredSection is null ? 1 : 2;
        var headers = new string[keyOrder.Count + lead];
        var headerNames = new string[keyOrder.Count + lead];
        var usedStableKeys = new HashSet<string>(StringComparer.Ordinal);

        string Dedupe(string stable)
        {
            if (usedStableKeys.Add(stable))
                return stable;
            int suffix = 2;
            string candidate;
            do { candidate = stable + "_" + suffix++; } while (!usedStableKeys.Add(candidate));
            return candidate;
        }

        int col = 0;
        if (structuredSection is not null)
        {
            headers[col] = "section";
            headerNames[col] = Dedupe("section");
            col++;
        }
        headers[col] = labelHeader;
        var labelKey = Formatting.FormatHelper.ToSnakeCase(labelHeader);
        if (string.IsNullOrEmpty(labelKey))
            labelKey = "field";
        headerNames[col] = Dedupe(labelKey);
        col++;

        for (int i = 0; i < keyOrder.Count; i++)
        {
            headers[col] = keyOrder[i];
            var stable = Formatting.FormatHelper.ToSnakeCase(keyOrder[i]);
            if (string.IsNullOrEmpty(stable))
                stable = "column";
            headerNames[col] = Dedupe(stable);
            col++;
        }

        var outRows = new List<string[]>(perRowFields.Count);
        for (int r = 0; r < perRowFields.Count; r++)
        {
            var values = new string[keyOrder.Count + lead];
            for (int i = 0; i < values.Length; i++)
                values[i] = "";
            int c = 0;
            if (structuredSection is not null)
                values[c++] = structuredSection;
            values[c] = labels[r];
            foreach (var field in perRowFields[r])
                if (keyIndex.TryGetValue(field.Key, out var idx))
                    values[lead + idx] = field.Value;
            outRows.Add(values);
        }

        _pendingJsonIdentityColumns = lead;
        try
        {
            return WriteTable(headers, headerNames, outRows);
        }
        finally
        {
            _pendingJsonIdentityColumns = 0;
        }
    }

    /// <summary>
    /// Writes an element table whose columns are already decomposed into typed fields (used by the
    /// generated element-table path for decomposing formatters). Each row is a list of <em>source
    /// columns</em>, and each source column is the list of fields it contributed (a composite column
    /// decomposes into <c>{column}_{sub}</c> fields; a scalar column contributes a single field; a null
    /// composite contributes none). Output columns are identified by <c>(source-column index, field key)</c>
    /// so a scalar column whose key collides with a composite subfield keeps its own column regardless of
    /// whether the composite is present in a given row. Columns appear in first-appearance order across rows;
    /// a row that omits a column renders blank. Display headers are the raw field keys, snake_cased and
    /// de-duplicated.
    /// </summary>
    /// <param name="rows">Per-row source columns, each source column being its decomposed fields.</param>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support tables.</returns>
    public bool WriteDecomposedRows(IReadOnlyList<IReadOnlyList<IReadOnlyList<MarkoutField>>> rows)
    {
        if (_sectionExcluded || rows.Count == 0)
            return true;

        if (_formatter is not ITableFormatter and not IStreamingTableFormatter)
            return false;

        var keyOrder = new List<string>();
        var keyIndex = new Dictionary<(int Column, string Key), int>();
        // Column identity is (source-column index, field key), not the raw key alone. The source-column
        // index is stable across rows (every row emits the same source columns in order, even when a
        // nullable composite contributes no fields), so a scalar column whose key collides with a composite
        // subfield keeps its own output column in every row. Within a source column, keys are unique.
        var resolvedRows = new List<(int Col, string Value)[]>(rows.Count);
        foreach (var row in rows)
        {
            var count = 0;
            foreach (var column in row)
                count += column.Count;
            var resolved = new (int, string)[count];
            var n = 0;
            for (int ci = 0; ci < row.Count; ci++)
            {
                foreach (var field in row[ci])
                {
                    var id = (ci, field.Key);
                    if (!keyIndex.TryGetValue(id, out var col))
                    {
                        col = keyOrder.Count;
                        keyIndex[id] = col;
                        keyOrder.Add(field.Key);
                    }
                    resolved[n++] = (col, field.Value);
                }
            }
            resolvedRows.Add(resolved);
        }

        var headers = new string[keyOrder.Count];
        var headerNames = new string[keyOrder.Count];
        var usedStableKeys = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < keyOrder.Count; i++)
        {
            headers[i] = keyOrder[i];
            var stable = Formatting.FormatHelper.ToSnakeCase(keyOrder[i]);
            if (string.IsNullOrEmpty(stable))
                stable = "column";
            if (!usedStableKeys.Add(stable))
            {
                int suffix = 2;
                string candidate;
                do { candidate = stable + "_" + suffix++; } while (!usedStableKeys.Add(candidate));
                stable = candidate;
            }
            headerNames[i] = stable;
        }

        var outRows = new List<string[]>(rows.Count);
        foreach (var resolved in resolvedRows)
        {
            var values = new string[keyOrder.Count];
            for (int i = 0; i < values.Length; i++)
                values[i] = "";
            foreach (var (col, value) in resolved)
                values[col] = value;
            outRows.Add(values);
        }

        return WriteTable(headers, headerNames, outRows);
    }

    /// <summary>
    /// Writes a gated-metric table from <see cref="MetricChange{T}"/> rows: document formatters
    /// render fixed <c>Metric | Change | Target | Status</c> columns; decomposing formatters
    /// (TSV/JSONL) emit flat typed fields (<c>before</c>, <c>after</c>, optional <c>target</c>/
    /// <c>target_label</c>, <c>status</c>). Absent targets render <c>-</c> and are omitted from
    /// structured output.
    /// </summary>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support tables.</returns>
    public bool WriteMetricChangeTable<T>(IReadOnlyList<MetricChange<T>> rows) where T : struct
        => WriteMetricChangeTable(rows, null);

    /// <summary>
    /// As <see cref="WriteMetricChangeTable{T}(IReadOnlyList{MetricChange{T}})"/>, plus an optional
    /// <paramref name="structuredSection"/>: when non-null, decomposed (TSV/JSONL) rows gain a leading
    /// <c>section</c> column carrying that value. Markdown output is unaffected.
    /// </summary>
    /// <param name="rows">The gated-metric rows.</param>
    /// <param name="structuredSection">A section discriminator prepended to decomposed rows, or <c>null</c>.</param>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support tables.</returns>
    public bool WriteMetricChangeTable<T>(IReadOnlyList<MetricChange<T>> rows, string? structuredSection) where T : struct
    {
        if (_sectionExcluded || rows.Count == 0)
            return true;

        if (_formatter is not ITableFormatter and not IStreamingTableFormatter)
            return false;

        if (_formatter is Formatting.ICompositeCellFormatter { DecomposesCompositeCells: true })
            return WriteDecomposedMetricChangeTable(rows, structuredSection);

        if (_options.InlineGoalStatus)
        {
            var glyphs = SupportsGlyphs;
            var denseRows = new List<string[]>(rows.Count);
            foreach (var metric in rows)
                denseRows.Add([MetricLabelText(metric, glyphs), DenseChangeText(metric, glyphs), TargetText(metric)]);

            return WriteTable(["Metric", "Change", "Target"], denseRows);
        }

        var outRows = new List<string[]>(rows.Count);
        foreach (var metric in rows)
            outRows.Add([metric.Name, ChangeText(metric), TargetText(metric), StatusText(metric)]);

        return WriteTable(["Metric", "Change", "Target", "Status"], outRows);
    }

    /// <summary>Whether the active formatter renders goal/polarity glyphs (opts into
    /// <see cref="Formatting.IGlyphFormatter"/>) rather than the slug words.</summary>
    private bool SupportsGlyphs => _formatter is Formatting.IGlyphFormatter;

    /// <summary>Whether the active formatter renders emphasized cell values (opts into
    /// <see cref="Formatting.IEmphasisFormatter"/>).</summary>
    private bool SupportsEmphasis => _formatter is Formatting.IEmphasisFormatter;

    /// <summary>Wraps a cell value in the formatter's emphasis; only call when <see cref="SupportsEmphasis"/>.</summary>
    private string Emphasize(string text) => ((Formatting.IEmphasisFormatter)_formatter).Emphasize(text);

    /// <summary>Composes a resolved glyph onto its base text via the caller's
    /// <see cref="MarkoutWriterOptions.ComposeGlyph"/>, or the default append-with-space.</summary>
    private string Compose(GlyphSlot slot, string text, string glyph, Goal goal, GateStatus status)
    {
        var context = new GlyphContext(slot, text, glyph, goal, status);
        return _options.ComposeGlyph is { } compose ? compose(context) : context.Combine();
    }

    /// <summary>Augments a composite-cell format with the active glyph set + composer when the sink
    /// renders glyphs, so <see cref="Change{V}"/> emits a polarity glyph instead of the status word.</summary>
    internal MarkoutCellFormat ApplyGlyphs(in MarkoutCellFormat format)
        => SupportsGlyphs ? format with { Glyphs = _options.Glyphs, Compose = _options.ComposeGlyph } : format;

    /// <summary>The metric label with a goal marker appended. With glyphs, the configured goal glyph
    /// (<c>↑</c>/<c>↓</c>); otherwise the ASCII marker <c>(-)</c>/<c>(+)</c>. Nothing for
    /// <see cref="Goal.Context"/>.</summary>
    private string MetricLabelText<T>(in MetricChange<T> metric, bool glyphs) where T : struct
    {
        if (metric.Goal == Goal.Context)
            return metric.Name;
        var marker = glyphs
            ? _options.Glyphs.ForGoal(metric.Goal)
            : (metric.Goal == Goal.Lower ? "(-)" : "(+)");
        if (!glyphs)
            return marker.Length == 0 ? metric.Name : metric.Name + " " + marker;
        return Compose(GlyphSlot.GoalLabel, metric.Name, marker, metric.Goal, GateStatus.Unknown);
    }

    /// <summary>The Change cell with the goal state inlined. With glyphs, a polarity glyph is appended
    /// (<c>0 → 7 ✗</c>) for a derived/enum <see cref="GateStatus"/>; a caller <see cref="MetricChange{T}.StatusLabel"/>
    /// stays a parenthesized word (<c>0 → 7 (regression)</c>). Without glyphs, the status word is
    /// parenthesized as before. An ungated, un-annotated row renders only the bare change.</summary>
    private string DenseChangeText<T>(in MetricChange<T> metric, bool glyphs) where T : struct
    {
        var change = MarkoutCell.ToInlineString(new Change<T>(metric.Before, metric.After));
        var (word, gate, custom) = ResolveStatus(metric);
        if (word is null)
            return change;
        if (glyphs && !custom && gate is { } g)
        {
            var glyph = _options.Glyphs.ForStatus(g);
            return Compose(GlyphSlot.MovementCell, change, glyph, metric.Goal, g);
        }
        return change + " (" + word + ")";
    }

    private bool WriteDecomposedMetricChangeTable<T>(IReadOnlyList<MetricChange<T>> rows, string? structuredSection) where T : struct
    {
        var labels = new string[rows.Count];
        var perRow = new List<MarkoutField>[rows.Count];
        var keyOrder = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        for (int r = 0; r < rows.Count; r++)
        {
            var metric = rows[r];
            labels[r] = metric.Name;
            var fields = new List<MarkoutField>();
            new Change<T>(metric.Before, metric.After).Decompose(fields, null, default);
            if (metric.Target is not null)
            {
                fields.Add(new MarkoutField("target", CellText.Scalar(metric.Target.Value)));
                if (!string.IsNullOrEmpty(metric.TargetLabel))
                    fields.Add(new MarkoutField("targetLabel", metric.TargetLabel!));
            }
            var (direction, status) = Resolve(metric);
            if (direction is not null)
                fields.Add(new MarkoutField("direction", direction));
            if (status is not null)
                fields.Add(new MarkoutField("status", status));

            perRow[r] = fields;
            foreach (var field in fields)
                if (seen.Add(field.Key))
                    keyOrder.Add(field.Key);
        }

        return WriteDecomposedFieldTable("Metric", labels, perRow, keyOrder, structuredSection);
    }

    private static string ChangeText<T>(in MetricChange<T> metric) where T : struct
        => MarkoutCell.ToInlineString(new Change<T>(metric.Before, metric.After));

    private static string TargetText<T>(in MetricChange<T> metric) where T : struct
    {
        if (metric.Target is null)
            return "-";
        var value = CellText.Scalar(metric.Target.Value);
        return string.IsNullOrEmpty(metric.TargetLabel) ? value : metric.TargetLabel + ": " + value;
    }

    private static string StatusText<T>(in MetricChange<T> metric) where T : struct
        => Resolve(metric).Status ?? "-";

    /// <summary>
    /// Resolves the row's <c>direction</c> (structural) and <c>status</c> (polarity) text. A
    /// caller-supplied <see cref="MetricChange{T}.StatusLabel"/> or <see cref="MetricChange{T}.Status"/>
    /// overrides the derived polarity; direction is derived whenever a goal is set.
    /// </summary>
    private static (string? Direction, string? Status) Resolve<T>(in MetricChange<T> metric) where T : struct
    {
        Direction? direction = null;
        var derived = GateStatus.Neutral;
        if (metric.Goal != Goal.Context &&
            GoalDerivation.TryDerive(metric.Before, metric.After, metric.Goal, metric.Noise, out var d, out derived))
            direction = d;

        string? status;
        if (!string.IsNullOrEmpty(metric.StatusLabel))
            status = metric.StatusLabel;
        else if (metric.Status != GateStatus.Unknown)
            status = GateStatusText.Slug(metric.Status);
        else if (direction is not null)
            status = GateStatusText.Slug(derived);
        else
            status = null;

        return (direction is null ? null : DirectionText.Slug(direction.Value), status);
    }

    /// <summary>
    /// Resolves the row's polarity for dense glyph rendering: the display <c>Word</c>
    /// (caller <see cref="MetricChange{T}.StatusLabel"/>, else the polarity slug), the underlying
    /// <see cref="GateStatus"/> enum when the polarity is derived or caller-set (so a glyph can be
    /// chosen), and whether the word is a caller-supplied custom label (which stays a word, not a glyph).
    /// </summary>
    private static (string? Word, GateStatus? Gate, bool Custom) ResolveStatus<T>(in MetricChange<T> metric) where T : struct
    {
        Direction? direction = null;
        var derived = GateStatus.Neutral;
        if (metric.Goal != Goal.Context &&
            GoalDerivation.TryDerive(metric.Before, metric.After, metric.Goal, metric.Noise, out var d, out derived))
            direction = d;

        if (!string.IsNullOrEmpty(metric.StatusLabel))
            return (metric.StatusLabel, null, true);
        if (metric.Status != GateStatus.Unknown)
            return (GateStatusText.Slug(metric.Status), metric.Status, false);
        if (direction is not null)
            return (GateStatusText.Slug(derived), derived, false);
        return (null, null, false);
    }

    /// <summary>
    /// Writes a single bullet list item.
    /// </summary>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support lists.</returns>
    public bool WriteListItem(string text)
    {
        if (_sectionExcluded)
            return true;

        if (_formatter is not IListFormatter lf)
            return false;

        EnsureBlankLineIfNeeded();
        lf.FormatListItem(_writer, text);
        _hasContent = true;
        return true;
    }

    /// <summary>
    /// Writes a sequence of strings as bullet list items.
    /// </summary>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support lists.</returns>
    public bool WriteList(params ReadOnlySpan<string> items)
    {
        if (items.Length == 0 || _sectionExcluded)
            return true;

        if (_formatter is not IListFormatter lf)
            return false;

        EnsureBlankLineIfNeeded();
        foreach (var item in items)
            lf.FormatListItem(_writer, item);

        _needsBlankLine = true;
        _hasContent = true;
        return true;
    }

    /// <summary>
    /// Writes an array field with string items as a labeled list.
    /// </summary>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support lists.</returns>
    public bool WriteArray(string key, params ReadOnlySpan<string> items)
    {
        if (_sectionExcluded)
            return true;

        if (_formatter is not IListFormatter lf)
            return false;

        RequireBlankLineBeforeThisBlock();
        EnsureBlankLineIfNeeded();

        lf.FormatArray(_writer, key, items, _options.BoldFieldNames);
        _needsBlankLine = true;
        _hasContent = true;
        return true;
    }

    /// <summary>
    /// Writes string items as a bullet list (no label).
    /// </summary>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support lists.</returns>
    public bool WriteArray(params ReadOnlySpan<string> items)
    {
        if (_sectionExcluded || items.Length == 0)
            return true;

        if (_formatter is not IListFormatter lf)
            return false;

        RequireBlankLineBeforeThisBlock();
        EnsureBlankLineIfNeeded();

        foreach (var item in items)
            lf.FormatListItem(_writer, item);

        _needsBlankLine = true;
        _hasContent = true;
        return true;
    }

    // ── Tables ──

    /// <summary>
    /// Writes a complete table with headers and rows.
    /// </summary>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support tables.</returns>
    public bool WriteTable(IEnumerable<string> headers, IEnumerable<string[]> rows)
        => WriteTableCore(headers, headerNames: null, rows);

    /// <summary>
    /// Writes a complete table with display headers, stable header names, and rows.
    /// </summary>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support tables.</returns>
    public bool WriteTable(IEnumerable<string> headers, IEnumerable<string> headerNames, IEnumerable<string[]> rows)
        => WriteTableCore(headers, headerNames, rows);

    private bool WriteTableCore(IEnumerable<string> headers, IEnumerable<string>? headerNames, IEnumerable<string[]> rows)
    {
        if (_sectionExcluded)
            return true;

        if (_formatter is not ITableFormatter and not IStreamingTableFormatter)
            return false;

        var headerArray = headers as string[] ?? headers.ToArray();
        var headerNameArray = headerNames == null ? null : headerNames as string[] ?? headerNames.ToArray();
        if (headerNameArray != null && headerNameArray.Length != headerArray.Length)
            throw new ArgumentException("Header names must have the same length as headers.", nameof(headerNames));

        // Apply column projection
        var columnMap = headerNameArray == null
            ? _options.Projection?.ComputeColumnMap(headerArray)
            : _options.Projection?.ComputeColumnMap(headerArray, headerNameArray);
        if (columnMap != null)
        {
            headerArray = MarkoutProjection.ProjectHeaders(headerArray, columnMap);
            if (headerNameArray != null)
                headerNameArray = MarkoutProjection.ProjectHeaders(headerNameArray, columnMap);
        }

        // Materialize and project rows
        var rowList = rows as IList<string[]> ?? rows.ToList();
        if (columnMap != null)
        {
            var projected = new List<string[]>(rowList.Count);
            foreach (var row in rowList)
                projected.Add(MarkoutProjection.ProjectRow(row, columnMap));
            rowList = projected;
        }

        // Resolve identity (never-typed) columns to their post-projection positions, since
        // projection can drop or reorder the leading identity column.
        var tableOptions = ResolveIdentityColumns(headerArray.Length, columnMap);

        EnsureBlankLineIfNeeded();
        if (headerNameArray != null)
            CreateTableWriter(tableOptions).WriteTable(headerArray, headerNameArray, rowList);
        else
            CreateTableWriter(tableOptions).WriteTable(headerArray, rowList);
        _needsBlankLine = true;
        _hasContent = true;
        return true;
    }

    // Maps the pre-projection leading identity columns through the projection map to their
    // projected indices, returning options the JSONL writer uses to keep those columns as strings.
    private MarkoutWriterOptions ResolveIdentityColumns(int projectedColumnCount, int[]? columnMap)
    {
        if (_pendingJsonIdentityColumns <= 0)
            return _options;

        var indices = new HashSet<int>();
        if (columnMap == null)
        {
            for (int i = 0; i < _pendingJsonIdentityColumns && i < projectedColumnCount; i++)
                indices.Add(i);
        }
        else
        {
            for (int j = 0; j < columnMap.Length; j++)
                if (columnMap[j] >= 0 && columnMap[j] < _pendingJsonIdentityColumns)
                    indices.Add(j);
        }

        return _options.WithJsonIdentityColumnIndices(indices);
    }

    /// <summary>
    /// Starts a streaming table with the given headers.
    /// </summary>
    /// <returns><c>true</c> if the formatter supports tables or streaming tables; <c>false</c> otherwise.</returns>
    public bool WriteTableStart(params ReadOnlySpan<string> headers)
        => WriteTableStartCore(headers, default);

    /// <summary>
    /// Starts a streaming table with display headers and stable header names.
    /// </summary>
    /// <returns><c>true</c> if the formatter supports tables or streaming tables; <c>false</c> otherwise.</returns>
    public bool WriteTableStart(ReadOnlySpan<string> headers, ReadOnlySpan<string> headerNames)
        => WriteTableStartCore(headers, headerNames);

    private bool WriteTableStartCore(ReadOnlySpan<string> headers, ReadOnlySpan<string> headerNames)
    {
        if (_inCode)
            throw new InvalidOperationException("Cannot start a table inside a code region.");

        _inTable = true;
        _columnMap = null;
        _tableWriter = null;

        if (_sectionExcluded)
            return true;

        if (_formatter is not ITableFormatter and not IStreamingTableFormatter)
            return false;

        if (headers.Length == 0)
            throw new ArgumentException("At least one header is required.", nameof(headers));
        if (headerNames.Length > 0 && headerNames.Length != headers.Length)
            throw new ArgumentException("Header names must have the same length as headers.", nameof(headerNames));

        _columnMap = headerNames.Length > 0
            ? _options.Projection?.ComputeColumnMap(headers, headerNames)
            : _options.Projection?.ComputeColumnMap(headers);
        string[]? projectedHeaderNames = null;
        if (headerNames.Length > 0)
        {
            projectedHeaderNames = headerNames.ToArray();
            if (_columnMap != null)
                projectedHeaderNames = MarkoutProjection.ProjectHeaders(projectedHeaderNames, _columnMap);
        }

        EnsureBlankLineIfNeeded();
        _tableWriter = CreateTableWriter();
        if (_columnMap != null)
        {
            var projectedHeaders = MarkoutProjection.ProjectHeaders(headers, _columnMap);
            if (projectedHeaderNames != null)
                _tableWriter.WriteTableStart(projectedHeaders, projectedHeaderNames);
            else
                _tableWriter.WriteTableStart(projectedHeaders);
        }
        else if (projectedHeaderNames != null)
        {
            _tableWriter.WriteTableStart(headers, projectedHeaderNames);
        }
        else
        {
            _tableWriter.WriteTableStart(headers);
        }
        return true;
    }

    /// <summary>
    /// Writes a table row. Must be between WriteTableStart and WriteTableEnd.
    /// </summary>
    public void WriteTableRow(params ReadOnlySpan<string> values)
    {
        if (!_inTable)
            throw new InvalidOperationException("Cannot write table row without starting a table first.");

        if (_sectionExcluded || _tableWriter == null)
            return;

        WriteTableRowCore(values);
    }

    /// <summary>
    /// Writes a table row, optionally marking it as a <c>[MarkoutChild]</c> row. When
    /// <paramref name="isChild"/> is <c>true</c> and the formatter supports glyphs, the row's first
    /// cell is prefixed with the configurable child glyph (default <c>↳</c>). Non-glyph sinks
    /// (TSV/JSONL, plain text) render the row unchanged.
    /// </summary>
    public void WriteTableRow(bool isChild, params ReadOnlySpan<string> values)
    {
        if (!_inTable)
            throw new InvalidOperationException("Cannot write table row without starting a table first.");

        if (_sectionExcluded || _tableWriter == null)
            return;

        // Project first, then prefix the glyph onto the first *displayed* cell, so a projection that
        // reorders or drops columns still lands the child marker on the leading visible column.
        var decorate = isChild && SupportsGlyphs;
        if (_columnMap != null)
        {
            var projected = MarkoutProjection.ProjectRow(values, _columnMap);
            if (decorate && projected.Length > 0)
                projected[0] = ChildLabel(projected[0]);
            _tableWriter!.WriteTableRow(projected);
        }
        else if (decorate && values.Length > 0)
        {
            var prefixed = values.ToArray();
            prefixed[0] = ChildLabel(prefixed[0]);
            _tableWriter!.WriteTableRow(prefixed);
        }
        else
        {
            _tableWriter!.WriteTableRow(values);
        }
    }

    private void WriteTableRowCore(ReadOnlySpan<string> values)
    {
        if (_columnMap != null)
            _tableWriter!.WriteTableRow(MarkoutProjection.ProjectRow(values, _columnMap));
        else
            _tableWriter!.WriteTableRow(values);
    }

    /// <summary>Prefixes a child row's first cell with the configurable child glyph. Unlike the
    /// trailing goal/polarity glyphs, the child glyph <em>leads</em> the label as a nesting marker.
    /// A custom <see cref="MarkoutWriterOptions.ComposeGlyph"/> composer takes full control.</summary>
    private string ChildLabel(string label)
    {
        var glyph = _options.Glyphs.Child;
        if (_options.ComposeGlyph is { } compose)
            return compose(new GlyphContext(GlyphSlot.ChildRow, label, glyph, Goal.Context, GateStatus.Unknown));
        return glyph.Length == 0 ? label : glyph + " " + label;
    }

    /// <summary>
    /// Ends the current streaming table.
    /// </summary>
    public void WriteTableEnd()
    {
        _inTable = false;

        if (!_sectionExcluded && _tableWriter != null)
        {
            _tableWriter.WriteTableEnd();
            _needsBlankLine = true;
            _hasContent = true;
        }

        _tableWriter = null;
        _columnMap = null;
    }

    // ── Code blocks ──

    /// <summary>
    /// Starts a code region with optional language specifier.
    /// </summary>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support code blocks.</returns>
    public bool WriteCodeStart(string? language = null)
    {
        if (_inCode)
            throw new InvalidOperationException("Cannot nest code regions. End the current code region before starting a new one.");

        _inCode = true;

        if (_sectionExcluded)
            return true;

        if (_formatter is not ICodeBlockFormatter cf)
            return false;

        EnsureBlankLineIfNeeded();
        cf.FormatCodeStart(_writer, language);
        _hasContent = true;
        return true;
    }

    /// <summary>
    /// Ends a code region.
    /// </summary>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support code blocks.</returns>
    public bool WriteCodeEnd()
    {
        if (!_inCode)
            throw new InvalidOperationException("Cannot end a code region without starting one first.");

        _inCode = false;

        if (_sectionExcluded)
            return true;

        if (_formatter is not ICodeBlockFormatter cf)
            return false;

        cf.FormatCodeEnd(_writer);
        _needsBlankLine = true;
        return true;
    }

    // ── Block content ──

    /// <summary>
    /// Writes a callout/admonition block.
    /// </summary>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support block content.</returns>
    public bool WriteCallout(CalloutSeverity severity, string message)
    {
        if (_sectionExcluded)
            return true;

        if (_formatter is not IBlockFormatter bf)
            return false;

        RequireBlankLineBeforeThisBlock();
        EnsureBlankLineIfNeeded();

        bf.FormatCallout(_writer, severity, message);
        _needsBlankLine = true;
        _hasContent = true;
        return true;
    }

    /// <summary>
    /// Writes a prose quotation block.
    /// </summary>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support block content.</returns>
    public bool WriteQuotation(string text)
    {
        if (_sectionExcluded)
            return true;

        if (_formatter is not IBlockFormatter bf)
            return false;

        RequireBlankLineBeforeThisBlock();
        EnsureBlankLineIfNeeded();

        bf.FormatQuotation(_writer, text);
        _needsBlankLine = true;
        _hasContent = true;
        return true;
    }

    /// <summary>
    /// Writes a horizontal rule separator.
    /// </summary>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support block content.</returns>
    public bool WriteRule()
    {
        if (_sectionExcluded)
            return true;

        if (_formatter is not IBlockFormatter bf)
            return false;

        RequireBlankLineBeforeThisBlock();
        EnsureBlankLineIfNeeded();

        bf.FormatRule(_writer);
        _needsBlankLine = true;
        _hasContent = true;
        return true;
    }

    /// <summary>
    /// Writes a list of description items.
    /// </summary>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support block content.</returns>
    public bool WriteDescriptions(IReadOnlyList<Description> items)
    {
        if (items.Count == 0 || _sectionExcluded)
            return true;

        if (_formatter is not IBlockFormatter bf)
            return false;

        RequireBlankLineBeforeThisBlock();
        EnsureBlankLineIfNeeded();

        foreach (var item in items)
            bf.FormatDescription(_writer, item);

        _needsBlankLine = true;
        _hasContent = true;
        return true;
    }

    // ── Metrics ──

    /// <summary>
    /// Writes a breakdown chart.
    /// </summary>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support metrics.</returns>
    public bool WriteBreakdown(IReadOnlyList<Breakdown> items, int? maxBarWidth = null, bool uniformBarWidth = true)
    {
        if (items.Count == 0 || _sectionExcluded)
            return true;

        if (_formatter is not IMetricsFormatter mf)
            return false;

        RequireBlankLineBeforeThisBlock();
        EnsureBlankLineIfNeeded();

        mf.FormatBreakdown(_writer, items, maxBarWidth, uniformBarWidth, _options);
        _needsBlankLine = true;
        _hasContent = true;
        return true;
    }

    /// <summary>
    /// Writes horizontal metric bars.
    /// </summary>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support metrics.</returns>
    public bool WriteMetrics(IReadOnlyList<Metric> items, int maxBarWidth = 30)
    {
        if (items.Count == 0 || _sectionExcluded)
            return true;

        if (_formatter is not IMetricsFormatter mf)
            return false;

        EnsureBlankLineIfNeeded();
        mf.FormatMetrics(_writer, items, maxBarWidth, _options);
        _needsBlankLine = true;
        _hasContent = true;
        return true;
    }

    /// <summary>
    /// Writes vertical metric bars.
    /// </summary>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support metrics.</returns>
    public bool WriteVerticalMetrics(IReadOnlyList<Metric> items, int maxBarHeight = 10, int? barWidth = null)
    {
        if (items.Count == 0 || _sectionExcluded)
            return true;

        if (_formatter is not IMetricsFormatter mf)
            return false;

        EnsureBlankLineIfNeeded();
        mf.FormatVerticalMetrics(_writer, items, maxBarHeight, barWidth, _options);
        _needsBlankLine = true;
        _hasContent = true;
        return true;
    }

    // ── Trees ──

    /// <summary>
    /// Writes a tree node with optional prefix for hierarchy.
    /// </summary>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support trees.</returns>
    public bool WriteTreeNode(string text, string prefix = "")
    {
        if (_sectionExcluded)
            return true;

        if (_formatter is not ITreeFormatter tf)
            return false;

        EnsureBlankLineIfNeeded();
        tf.FormatTreeNode(_writer, text, prefix);
        _hasContent = true;
        return true;
    }

    /// <summary>
    /// Writes a tree structure from a list of TreeNode objects.
    /// </summary>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support trees.</returns>
    public bool WriteTree(params ReadOnlySpan<TreeNode> nodes)
    {
        if (nodes.Length == 0 || _sectionExcluded)
            return true;

        if (_formatter is not ITreeFormatter tf)
            return false;

        EnsureBlankLineIfNeeded();
        tf.FormatTree(_writer, nodes, _options);
        _hasContent = true;
        return true;
    }

    // ── Graphs ──

    /// <summary>
    /// Writes a directed graph. Each formatter lowers the graph into what its format can express —
    /// a flowchart, an edge table, or a tree rooted at the focus node.
    /// </summary>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support graphs.</returns>
    public bool WriteGraph(Graph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        if (graph.IsEmpty || _sectionExcluded)
            return true;

        if (_formatter is not IGraphFormatter gf)
            return false;

        EnsureBlankLineIfNeeded();
        gf.FormatGraph(_writer, graph, _options);
        _needsBlankLine = true;
        _hasContent = true;
        return true;
    }

    // ── Link definitions ──

    /// <summary>
    /// Writes a block of reference-style link definitions (e.g. <c>[0]: https://example.com</c>).
    /// Ensures a blank line before the block and sets state so a blank line is inserted before subsequent content.
    /// </summary>
    public void WriteLinkDefinitions(params ReadOnlySpan<string> definitions)
    {
        if (definitions.Length == 0 || _sectionExcluded)
            return;

        EnsureBlankLineIfNeeded();

        foreach (var def in definitions)
            _writer.WriteLine(def);

        _needsBlankLine = true;
        _hasContent = true;
    }

    // ── Infrastructure ──

    /// <summary>
    /// Writes a blank line.
    /// </summary>
    public void WriteBlankLine()
    {
        if (_sectionExcluded)
            return;

        // An explicit blank line at a section boundary is part of the seam: it is the
        // caller's content and travels with the section, but it does not settle what
        // separator the seam needs, because the block after it has not been seen yet.
        if (_sectionBuffer?.TryWriteSectionOpeningBlankLine() != true)
            _writer.WriteLine();

        _needsBlankLine = false;
    }

    /// <summary>
    /// Flushes any buffered output to the underlying stream.
    /// </summary>
    public void Flush()
    {
        _sectionBuffer?.EmitOrdered(_options.SectionOrder, _needsBlankLine);

        // _writer is either _target or the buffering wrapper in front of it, and the
        // wrapper has nothing of its own to flush once it has emitted. Flushing both
        // would flush the target twice whenever ordering is off, which is every
        // caller that never asked for it.
        _target.Flush();
    }

    /// <summary>
    /// Returns the generated output. Only valid when using the constructor without a TextWriter.
    /// Trims trailing whitespace.
    /// </summary>
    public override string ToString()
    {
        // Emitting is a write, so only do it where ToString can actually return the
        // result. Against a stream target this method has nothing to return, and
        // committing the document from something a debugger calls implicitly would be
        // a side effect no caller asked for.
        if (_target is not StringWriter sw)
            return base.ToString() ?? "";

        _sectionBuffer?.EmitOrdered(_options.SectionOrder, _needsBlankLine);
        return sw.ToString().TrimEnd();
    }

    // ── Private infrastructure ──

    private int RenderHeadingLevel(int level)
    {
        var adjusted = level + _options.HeadingLevelOffset;
        if (adjusted < 1) return 1;
        if (adjusted > 6) return 6;
        return adjusted;
    }

    private void UpdateSectionState(int level, string text)
    {
        if (level == 2)
        {
            _currentSectionName = text;
            _sectionExcluded = !IsSectionIncluded();

            // The boundary the writer declares, not one re-derived from rendered text.
            // Excluded sections open a buffer too: they write nothing today, but routing
            // them into the previous section's buffer would be a silent misattribution
            // the moment any write path stops honoring _sectionExcluded.
            _sectionBuffer?.BeginSection(text, _needsBlankLine);
        }
    }

    private bool IsSectionIncluded()
    {
        if (_currentSectionName == null)
            return true;

        if (_options.IncludeSections != null && !_options.IncludeSections.Contains(_currentSectionName))
            return false;
        return true;
    }

    /// <summary>
    /// Whether a field-level projection (include/exclude) is configured. When <c>false</c>, the
    /// field-render paths iterate the caller's span directly and skip the <see cref="ProjectFields"/>
    /// array copy.
    /// </summary>
    private bool NeedsFieldProjection
        => _options.Projection is { } p && (p.IncludeFields != null || p.ExcludeFields != null);

    private MarkoutField[] ProjectFields(ReadOnlySpan<MarkoutField> fields)
    {
        var projection = _options.Projection;
        if (projection == null)
            return fields.ToArray();

        if (projection.IncludeFields != null)
        {
            var result = new List<MarkoutField>(projection.IncludeFields.Count);
            foreach (var name in projection.IncludeFields)
            {
                bool isGlob = name.Contains('*') || name.Contains('?');
                for (int i = 0; i < fields.Length; i++)
                {
                    if (projection.MatchesName(name, fields[i].Key))
                    {
                        result.Add(fields[i]);
                        if (!isGlob) break;
                    }
                }
            }
            return result.ToArray();
        }

        if (projection.ExcludeFields != null)
        {
            var result = new List<MarkoutField>(fields.Length);
            for (int i = 0; i < fields.Length; i++)
            {
                if (projection.IsFieldIncluded(fields[i].Key))
                    result.Add(fields[i]);
            }
            return result.ToArray();
        }

        return fields.ToArray();
    }

    /// <summary>
    /// Whether anything has been written that a block after it has to separate itself
    /// from. Ordering needs to know which section that content landed in, and "the
    /// section's buffer is not empty" is not the same fact: a section can hold nothing
    /// but blank lines the caller wrote, and a block the formatter does not support
    /// returns before setting this at all. Recording it in the setter rather than at
    /// the two dozen sites that assign it means a new block cannot come to count as
    /// content without also coming to be recorded as content.
    /// </summary>
    private bool _hasContent
    {
        get => _hasContentValue;
        set
        {
            _hasContentValue = value;

            if (value)
                _sectionBuffer?.NoteContent();
        }
    }

    /// <summary>
    /// Separates the heading about to be written from anything before it. A heading
    /// separates itself, whatever preceded it, which is what makes this survive
    /// reordering: an ordinary block separates only when the block before it left a
    /// blank line pending, and that is a fact about the other block.
    /// </summary>
    private void SeparateFromPrecedingContent()
    {
        // Noted whether or not one is written here. A section that opens the document
        // has nothing to separate from, but the same section moved later does.
        _sectionBuffer?.NoteSelfSeparatingOpen();

        if (_hasContent)
            WriteSeparatorLine();
    }

    /// <summary>
    /// The same claim for a block that is set off by a blank line rather than writing
    /// its own — a quotation, a rule, a callout, an array, a description list. These
    /// were the blocks the heading-only version of this missed: their separator was
    /// dropped at a section boundary and never put back, because nothing recorded that
    /// the block itself was what required it.
    /// </summary>
    private void RequireBlankLineBeforeThisBlock()
    {
        _sectionBuffer?.NoteSelfSeparatingOpen();

        if (_hasContent)
            _needsBlankLine = true;
    }

    private void EnsureBlankLineIfNeeded()
    {
        FlushPendingSection();

        if (_needsBlankLine)
        {
            WriteSeparatorLine();
            _needsBlankLine = false;
        }
    }

    /// <summary>
    /// Writes the blank line that separates two blocks, unless it would land at a
    /// section seam. A seam separator belongs to neither section, so capturing it with
    /// the section that follows makes it travel when that section moves. At a seam the
    /// buffering writer re-inserts one where the requested order actually needs it.
    /// </summary>
    private void WriteSeparatorLine()
    {
        if (_sectionBuffer is { AtSectionBoundary: true } buffer)
        {
            // This is where the separator would have been written, so this is the
            // newline it would have used — earlier than the section's first content,
            // and the one to reinstate at emit.
            buffer.NoteSeparatorNewLine();
            return;
        }

        _writer.WriteLine();
    }

    private void FlushPendingSection()
    {
        if (_pendingSection is { } pending)
        {
            _pendingSection = null;
            WriteSectionHeading(pending.Level, pending.Text, pending.Context);
        }
    }

    private void WriteSectionHeading(int level, string text, string? context)
    {
        if (string.IsNullOrEmpty(text))
            return;

        if (_formatter is not IHeadingFormatter hf)
            return;

        SeparateFromPrecedingContent();

        hf.FormatHeading(_writer, RenderHeadingLevel(level), text, context);
        _writer.WriteLine();
        _hasContent = true;
        _needsBlankLine = true;
    }

    private TableWriter CreateTableWriter() => CreateTableWriter(_options);

    private TableWriter CreateTableWriter(MarkoutWriterOptions options)
    {
        if (_formatter is ITableFormatter tf)
            return new TableWriter(_writer, tf, options);
        if (_formatter is IStreamingTableFormatter stf)
            return new TableWriter(_writer, stf, options);
        throw new InvalidOperationException("Formatter does not support tables.");
    }

    /// <summary>
    /// Cascade fallback: renders fields as a 2-column Field/Value table.
    /// </summary>
    private bool RenderFieldsAsTable(ReadOnlySpan<MarkoutField> fields)
    {
        if (_formatter is not ITableFormatter and not IStreamingTableFormatter)
            return false;

        var headers = new[] { "Field", "Value" };
        var rows = new List<string[]>(fields.Length);
        foreach (var field in fields)
            rows.Add([field.Key, field.Value]);

        EnsureBlankLineIfNeeded();
        CreateTableWriter().WriteTable(headers, ["Field", "Value"], rows);
        _needsBlankLine = true;
        _hasContent = true;
        return true;
    }

    // ── Static factories ──

    /// <summary>
    /// Creates a generic writer that writes to the specified TextWriter.
    /// The generic type enables JIT devirtualization of capability checks.
    /// </summary>
    public static MarkoutWriter<TFormatter> Create<TFormatter>(
        TextWriter writer, TFormatter formatter, MarkoutWriterOptions? options = null)
        where TFormatter : IMarkoutFormatter
        => new(writer, formatter, options);

    /// <summary>
    /// Creates a generic writer that builds output in memory. Use ToString() to get the result.
    /// </summary>
    public static MarkoutWriter<TFormatter> Create<TFormatter>(
        TFormatter formatter, MarkoutWriterOptions? options = null)
        where TFormatter : IMarkoutFormatter
        => new(formatter, options);
}

/// <summary>
/// Generic writer subclass that preserves the concrete formatter type for
/// JIT devirtualization of <c>_formatter is IHeadingFormatter</c> checks.
/// </summary>
/// <typeparam name="TFormatter">The concrete formatter type.</typeparam>
public class MarkoutWriter<TFormatter> : MarkoutWriter where TFormatter : IMarkoutFormatter
{
    /// <summary>
    /// Creates a writer that writes to the specified TextWriter.
    /// </summary>
    public MarkoutWriter(TextWriter writer, TFormatter formatter, MarkoutWriterOptions? options = null)
        : base(writer, formatter, options)
    {
    }

    /// <summary>
    /// Creates a writer that builds output in memory. Use ToString() to get the result.
    /// </summary>
    public MarkoutWriter(TFormatter formatter, MarkoutWriterOptions? options = null)
        : base(formatter, options)
    {
    }
}

internal readonly record struct PendingSectionHeading(int Level, string Text, string? Context);
