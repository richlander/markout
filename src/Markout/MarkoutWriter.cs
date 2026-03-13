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

        _writer = writer;
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

        if (_formatter is not ITableFormatter and not IStreamingTableFormatter)
            return false;

        var headerArray = headers as string[] ?? headers.ToArray();

        // Apply column projection
        var columnMap = _options.Projection?.ComputeColumnMap(headerArray);
        if (columnMap != null)
            headerArray = MarkoutProjection.ProjectHeaders(headerArray, columnMap);

        // Materialize and project rows
        var rowList = rows as IList<string[]> ?? rows.ToList();
        if (columnMap != null)
        {
            var projected = new List<string[]>(rowList.Count);
            foreach (var row in rowList)
                projected.Add(MarkoutProjection.ProjectRow(row, columnMap));
            rowList = projected;
        }

        EnsureBlankLineIfNeeded();
        CreateTableWriter().WriteTable(headerArray, rowList);
        _needsBlankLine = true;
        _hasContent = true;
        return true;
    }

    /// <summary>
    /// Starts a streaming table with the given headers.
    /// </summary>
    /// <returns><c>true</c> if the formatter supports tables or streaming tables; <c>false</c> otherwise.</returns>
    public bool WriteTableStart(params ReadOnlySpan<string> headers)
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

        _columnMap = _options.Projection?.ComputeColumnMap(headers);

        EnsureBlankLineIfNeeded();
        _tableWriter = CreateTableWriter();
        if (_columnMap != null)
            _tableWriter.WriteTableStart(MarkoutProjection.ProjectHeaders(headers, _columnMap));
        else
            _tableWriter.WriteTableStart(headers);
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

        if (_columnMap != null)
            _tableWriter.WriteTableRow(MarkoutProjection.ProjectRow(values, _columnMap));
        else
            _tableWriter.WriteTableRow(values);
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

    private TableWriter CreateTableWriter()
    {
        if (_formatter is ITableFormatter tf)
            return new TableWriter(_writer, tf, _options);
        if (_formatter is IStreamingTableFormatter stf)
            return new TableWriter(_writer, stf, _options);
        throw new InvalidOperationException("Formatter does not support tables.");
    }

    /// <summary>
    /// Cascade fallback: renders fields as a 2-column Field/Value table.
    /// </summary>
    private bool RenderFieldsAsTable(MarkoutField[] fields)
    {
        if (_formatter is not ITableFormatter and not IStreamingTableFormatter)
            return false;

        var headers = new[] { "Field", "Value" };
        var rows = new List<string[]>(fields.Length);
        foreach (var field in fields)
            rows.Add([field.Key, field.Value]);

        EnsureBlankLineIfNeeded();
        CreateTableWriter().WriteTable(headers, rows);
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
