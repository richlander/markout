using System.Runtime.InteropServices;
using Markout.Formatting;

namespace Markout;

/// <summary>
/// Composes a formatter via capability interfaces, dispatching Write methods
/// to the appropriate interface when implemented by the formatter.
/// Returns <c>bool</c> from all Write methods: <c>true</c> = rendered (or filtered),
/// <c>false</c> = unsupported shape (nothing written).
/// </summary>
public class MarkoutOrchestrator
{
    private readonly TextWriter _writer;
    private readonly IMarkoutFormatter _formatter;
    private readonly MarkoutWriterOptions _options;

    // State
    private bool _hasContent;
    private bool _needsBlankLine;
    private bool _inTable;
    private bool _inCode;

    // Section tracking
    private string? _currentSectionName;
    private bool _sectionExcluded;
    private bool _projectionSectionActive;

    // Streaming tables
    private int _tableRowCount;
    private int _tableRowsSkipped;
    private string[]? _streamingHeaders;
    private List<string[]>? _streamingRows;
    private int[]? _columnMap;
    private bool _streamingDirect;

    // Pending section (deferred until content written)
    private PendingSectionHeading? _pendingSection;

    /// <summary>
    /// Creates an orchestrator that writes to the specified TextWriter.
    /// </summary>
    public MarkoutOrchestrator(TextWriter writer, IMarkoutFormatter formatter, MarkoutWriterOptions? options = null)
    {
        var opts = options ?? new MarkoutWriterOptions();
        if (opts.IncludeSections != null && opts.ExcludeSections != null)
            throw new InvalidOperationException("Cannot set both IncludeSections and ExcludeSections. Use one or the other.");

        _writer = writer;
        _formatter = formatter;
        _options = opts;
    }

    /// <summary>
    /// Creates an orchestrator that builds output in memory. Use ToString() to get the result.
    /// </summary>
    public MarkoutOrchestrator(IMarkoutFormatter formatter, MarkoutWriterOptions? options = null)
        : this(new StringWriter(), formatter, options)
    {
    }

    /// <summary>
    /// Gets the writer options.
    /// </summary>
    public MarkoutWriterOptions Options => _options;

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

        if (_hasContent)
            _writer.WriteLine();

        hf.FormatHeading(_writer, level, text, context);
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
    public bool WriteSectionStart(int level, string text, string? context = null)
    {
        UpdateSectionState(level, text);

        if (_sectionExcluded)
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
    /// <returns><c>true</c> always (paragraphs are universal).</returns>
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
        var projected = ProjectFields(field);
        if (projected.Length == 0)
            return true;

        // Cascade: IFieldFormatter → ITableFormatter → IStreamingTableFormatter
        if (_formatter is IFieldFormatter ff)
        {
            EnsureBlankLineIfNeeded();
            ff.FormatFieldName(_writer, projected[0].Key, _options.BoldFieldNames);
            _writer.WriteLine(projected[0].Value);
            _hasContent = true;
            return true;
        }

        return RenderFieldsAsTable(projected);
    }

    /// <summary>
    /// Writes multiple key-value fields, each on its own line.
    /// </summary>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support fields.</returns>
    public bool WriteFields(params ReadOnlySpan<MarkoutField> fields)
    {
        if (_sectionExcluded || fields.Length == 0)
            return true;

        var projected = ProjectFields(fields);
        if (projected.Length == 0)
            return true;

        // Cascade: IFieldFormatter → ITableFormatter → IStreamingTableFormatter
        if (_formatter is IFieldFormatter ff)
        {
            EnsureBlankLineIfNeeded();
            ff.FormatFields(_writer, projected, _options.BoldFieldNames);
            _needsBlankLine = true;
            _hasContent = true;
            return true;
        }

        return RenderFieldsAsTable(projected);
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

        var projected = ProjectFields(fields);
        if (projected.Length == 0)
            return true;

        // Cascade: IFieldFormatter (inline) → ITableFormatter → IStreamingTableFormatter
        if (_formatter is IFieldFormatter ff)
        {
            EnsureBlankLineIfNeeded();

            for (int i = 0; i < projected.Length; i++)
            {
                if (i > 0)
                    _writer.Write(" | ");
                ff.FormatFieldName(_writer, projected[i].Key, _options.BoldFieldNames);
                _writer.Write(projected[i].Value);
            }

            _writer.WriteLine();
            _needsBlankLine = true;
            _hasContent = true;
            return true;
        }

        return RenderFieldsAsTable(projected);
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

        var projected = ProjectFields(fields);
        if (projected.Length == 0)
            return true;

        // Cascade: IFieldFormatter (bulleted) → ITableFormatter → IStreamingTableFormatter
        if (_formatter is IFieldFormatter ff)
        {
            EnsureBlankLineIfNeeded();

            for (int i = 0; i < projected.Length; i++)
            {
                _writer.Write("- ");
                ff.FormatFieldName(_writer, projected[i].Key, _options.BoldFieldNames);
                _writer.WriteLine(projected[i].Value);
            }

            _needsBlankLine = true;
            _hasContent = true;
            return true;
        }

        return RenderFieldsAsTable(projected);
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

        var projected = ProjectFields(fields);
        if (projected.Length == 0)
            return true;

        // Cascade: IFieldFormatter (numbered) → ITableFormatter → IStreamingTableFormatter
        if (_formatter is IFieldFormatter ff)
        {
            EnsureBlankLineIfNeeded();

            for (int i = 0; i < projected.Length; i++)
            {
                _writer.Write(i + 1);
                _writer.Write(". ");
                ff.FormatFieldName(_writer, projected[i].Key, _options.BoldFieldNames);
                _writer.WriteLine(projected[i].Value);
            }

            _needsBlankLine = true;
            _hasContent = true;
            return true;
        }

        return RenderFieldsAsTable(projected);
    }

    /// <summary>
    /// Writes fields as a two-column Field/Value table.
    /// </summary>
    /// <returns><c>true</c> if rendered or filtered; <c>false</c> if the formatter does not support tables.</returns>
    public bool WriteFieldsTable(params ReadOnlySpan<MarkoutField> fields)
    {
        if (fields.Length == 0)
            return true;

        var projected = ProjectFields(fields);
        if (projected.Length == 0)
            return true;

        var headers = new[] { "Field", "Value" };
        var rows = new List<string[]>(projected.Length);
        foreach (var field in projected)
            rows.Add([field.Key, field.Value]);

        return WriteTable(headers, rows);
    }

    // ── Lists ──

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
        if (_sectionExcluded)
            return true;

        if (_formatter is not IListFormatter lf)
            return false;

        EnsureBlankLineIfNeeded();
        foreach (var item in items)
            lf.FormatListItem(_writer, item);

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

        if (_hasContent)
            _needsBlankLine = true;
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

        if (_hasContent)
            _needsBlankLine = true;
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
    {
        if (_sectionExcluded)
            return true;

        var headerArray = headers as string[] ?? headers.ToArray();

        // Apply column projection
        var columnMap = _projectionSectionActive ? null : _options.Projection?.ComputeColumnMap(headerArray);
        if (columnMap != null)
            headerArray = MarkoutProjection.ProjectHeaders(headerArray, columnMap);

        // LINQ-style cascade: batch ITableFormatter (if IList) → IStreamingTableFormatter → materialize + batch ITableFormatter
        if (_formatter is ITableFormatter tf && rows is IList<string[]> list)
        {
            var rowList = ProjectAndTruncateRows(list, columnMap, out int skipped);
            EnsureBlankLineIfNeeded();
            tf.FormatTable(_writer, headerArray, rowList, skipped, _options);
            _needsBlankLine = true;
            _hasContent = true;
            return true;
        }

        if (_formatter is IStreamingTableFormatter stf)
        {
            EnsureBlankLineIfNeeded();
            stf.BeginTable(_writer, headerArray, _options);
            int rowCount = 0;
            int rowsSkipped = 0;
            foreach (var row in rows)
            {
                if (_options.MaxItems is int max2 && rowCount >= max2)
                {
                    rowsSkipped++;
                    continue;
                }
                var projected = columnMap != null ? MarkoutProjection.ProjectRow(row, columnMap) : row;
                stf.WriteRow(_writer, projected);                rowCount++;
            }
            stf.EndTable(_writer, rowsSkipped);
            _needsBlankLine = true;
            _hasContent = true;
            return true;
        }

        if (_formatter is ITableFormatter tf2)
        {
            var rowList = rows as IList<string[]> ?? rows.ToList();
            rowList = ProjectAndTruncateRows(rowList, columnMap, out int skipped);
            EnsureBlankLineIfNeeded();
            tf2.FormatTable(_writer, headerArray, rowList, skipped, _options);
            _needsBlankLine = true;
            _hasContent = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Starts a streaming table with the given headers.
    /// </summary>
    /// <returns><c>true</c> if the formatter supports tables or streaming tables; <c>false</c> otherwise.</returns>
    public bool WriteTableStart(params string[] headers)
    {
        if (_inCode)
            throw new InvalidOperationException("Cannot start a table inside a code region.");

        _inTable = true;
        _tableRowCount = 0;
        _tableRowsSkipped = 0;
        _columnMap = null;
        _streamingDirect = false;

        if (_sectionExcluded)
            return true;

        if (_formatter is not ITableFormatter and not IStreamingTableFormatter)
            return false;

        if (headers.Length == 0)
            throw new ArgumentException("At least one header is required.", nameof(headers));

        _columnMap = _projectionSectionActive ? null : _options.Projection?.ComputeColumnMap(headers);
        _streamingHeaders = _columnMap != null
            ? MarkoutProjection.ProjectHeaders(headers, _columnMap)
            : headers;

        // Cascade: IStreamingTableFormatter (direct) → buffer + ITableFormatter
        if (_formatter is IStreamingTableFormatter stf)
        {
            _streamingDirect = true;
            EnsureBlankLineIfNeeded();
            stf.BeginTable(_writer, _streamingHeaders, _options);
        }
        else
        {
            _streamingRows = [];
        }

        return true;
    }

    /// <summary>
    /// Writes a table row. Must be between WriteTableStart and WriteTableEnd.
    /// </summary>
    public void WriteTableRow(params string[] values)
    {
        if (!_inTable)
            throw new InvalidOperationException("Cannot write table row without starting a table first.");

        if (_sectionExcluded || _streamingHeaders == null)
            return;

        if (!ShouldWriteTableRow())
            return;

        var projected = _columnMap != null
            ? MarkoutProjection.ProjectRow(values, _columnMap)
            : values;

        if (_streamingDirect && _formatter is IStreamingTableFormatter stf)
        {
            stf.WriteRow(_writer, projected);
        }
        else
        {
            _streamingRows?.Add(projected);
        }
    }

    /// <summary>
    /// Ends the current streaming table.
    /// </summary>
    public void WriteTableEnd()
    {
        _inTable = false;

        if (!_sectionExcluded && _streamingHeaders != null)
        {
            if (_streamingDirect && _formatter is IStreamingTableFormatter stf)
            {
                stf.EndTable(_writer, _tableRowsSkipped);
                _needsBlankLine = true;
                _hasContent = true;
            }
            else if (_formatter is ITableFormatter tf)
            {
                EnsureBlankLineIfNeeded();
                tf.FormatTable(_writer, _streamingHeaders, _streamingRows ?? [], _tableRowsSkipped, _options);
                _needsBlankLine = true;
                _hasContent = true;
            }
        }

        _streamingHeaders = null;
        _streamingRows = null;
        _streamingDirect = false;
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

        if (_hasContent)
            _needsBlankLine = true;
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

        if (_hasContent)
            _needsBlankLine = true;
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

        if (_hasContent)
            _needsBlankLine = true;
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

        if (_hasContent)
            _needsBlankLine = true;
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

        if (_hasContent)
            _needsBlankLine = true;
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

    // ── Infrastructure ──

    /// <summary>
    /// Writes a blank line.
    /// </summary>
    public void WriteBlankLine()
    {
        if (_sectionExcluded)
            return;

        _writer.WriteLine();
        _needsBlankLine = false;
    }

    /// <summary>
    /// Flushes any buffered output to the underlying stream.
    /// </summary>
    public void Flush()
    {
        _writer.Flush();
    }

    /// <summary>
    /// Returns the generated output. Only valid when using the constructor without a TextWriter.
    /// Trims trailing whitespace.
    /// </summary>
    public override string ToString()
    {
        if (_writer is StringWriter sw)
            return sw.ToString().TrimEnd();
        return base.ToString() ?? "";
    }

    // ── Private infrastructure ──

    private void UpdateSectionState(int level, string text)
    {
        if (level == 2)
        {
            _currentSectionName = text;
            _sectionExcluded = !IsSectionIncluded();
            _projectionSectionActive = !_sectionExcluded
                && _options.Projection?.IsSectionIncluded(text) == true;
        }
    }

    private bool IsSectionIncluded()
    {
        if (_currentSectionName == null)
            return true;

        if (_options.Projection?.IsSectionIncluded(_currentSectionName) == true)
            return true;

        if (_options.IncludeSections != null && !_options.IncludeSections.Contains(_currentSectionName))
            return false;
        if (_options.ExcludeSections?.Contains(_currentSectionName) == true)
            return false;
        return true;
    }

    private MarkoutField[] ProjectFields(ReadOnlySpan<MarkoutField> fields)
    {
        var projection = _options.Projection;
        if (projection == null || _projectionSectionActive)
            return fields.ToArray();

        if (projection.IncludeFields != null)
        {
            var result = new List<MarkoutField>(projection.IncludeFields.Count);
            foreach (var name in projection.IncludeFields)
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    if (string.Equals(name, fields[i].Key, projection.Comparison))
                    {
                        result.Add(fields[i]);
                        break;
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

    private void EnsureBlankLineIfNeeded()
    {
        FlushPendingSection();

        if (_needsBlankLine)
        {
            _writer.WriteLine();
            _needsBlankLine = false;
        }
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

        if (_hasContent)
            _writer.WriteLine();

        hf.FormatHeading(_writer, level, text, context);
        _writer.WriteLine();
        _hasContent = true;
        _needsBlankLine = true;
    }

    private bool ShouldWriteTableRow()
    {
        if (_options.MaxItems is int max && _tableRowCount >= max)
        {
            _tableRowsSkipped++;
            return false;
        }
        _tableRowCount++;
        return true;
    }

    /// <summary>
    /// Cascade fallback: renders fields as a 2-column Field/Value table.
    /// Tries ITableFormatter → IStreamingTableFormatter → returns false.
    /// </summary>
    private bool RenderFieldsAsTable(MarkoutField[] fields)
    {
        var headers = new[] { "Field", "Value" };
        var rows = new List<string[]>(fields.Length);
        foreach (var field in fields)
            rows.Add([field.Key, field.Value]);

        if (_formatter is ITableFormatter tf)
        {
            EnsureBlankLineIfNeeded();
            tf.FormatTable(_writer, headers, rows, 0, _options);
            _needsBlankLine = true;
            _hasContent = true;
            return true;
        }

        if (_formatter is IStreamingTableFormatter stf)
        {
            EnsureBlankLineIfNeeded();
            stf.BeginTable(_writer, headers, _options);
            foreach (var row in rows)
                stf.WriteRow(_writer, row);
            stf.EndTable(_writer, 0);
            _needsBlankLine = true;
            _hasContent = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Projects and truncates rows for batch table rendering.
    /// </summary>
    private IList<string[]> ProjectAndTruncateRows(IList<string[]> rowList, int[]? columnMap, out int skipped)
    {
        if (columnMap != null)
        {
            var projected = new List<string[]>(rowList.Count);
            foreach (var row in rowList)
                projected.Add(MarkoutProjection.ProjectRow(row, columnMap));
            rowList = projected;
        }

        if (_options.MaxItems is int max && rowList.Count > max)
        {
            skipped = rowList.Count - max;
            return rowList.Take(max).ToList();
        }

        skipped = 0;
        return rowList;
    }

    internal static string EscapeTableCell(string value)
    {
        if (value.Contains('|') || value.Contains('\n') || value.Contains('\r'))
        {
            return value
                .Replace("|", "\\|")
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Replace("\r", " ");
        }
        return value;
    }

    // ── Static factories ──

    /// <summary>
    /// Creates a generic orchestrator that writes to the specified TextWriter.
    /// The generic type enables JIT devirtualization of capability checks.
    /// </summary>
    public static MarkoutOrchestrator<TFormatter> Create<TFormatter>(
        TextWriter writer, TFormatter formatter, MarkoutWriterOptions? options = null)
        where TFormatter : IMarkoutFormatter
        => new(writer, formatter, options);

    /// <summary>
    /// Creates a generic orchestrator that builds output in memory. Use ToString() to get the result.
    /// </summary>
    public static MarkoutOrchestrator<TFormatter> Create<TFormatter>(
        TFormatter formatter, MarkoutWriterOptions? options = null)
        where TFormatter : IMarkoutFormatter
        => new(formatter, options);
}

/// <summary>
/// Generic orchestrator subclass that preserves the concrete formatter type for
/// JIT devirtualization of <c>_formatter is IHeadingFormatter</c> checks.
/// </summary>
/// <typeparam name="TFormatter">The concrete formatter type.</typeparam>
public class MarkoutOrchestrator<TFormatter> : MarkoutOrchestrator where TFormatter : IMarkoutFormatter
{
    /// <summary>
    /// Creates an orchestrator that writes to the specified TextWriter.
    /// </summary>
    public MarkoutOrchestrator(TextWriter writer, TFormatter formatter, MarkoutWriterOptions? options = null)
        : base(writer, formatter, options)
    {
    }

    /// <summary>
    /// Creates an orchestrator that builds output in memory. Use ToString() to get the result.
    /// </summary>
    public MarkoutOrchestrator(TFormatter formatter, MarkoutWriterOptions? options = null)
        : base(formatter, options)
    {
    }
}
