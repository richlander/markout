namespace Markout;

/// <summary>
/// A table whose columns are runtime data rather than a fixed set of attributed properties.
/// </summary>
/// <remarks>
/// <para>
/// The source generator derives a table's columns from an element type's annotated properties, so
/// a fixed model type can only describe a table whose shape is known at compile time. Some data
/// does not have a compile-time shape — the columns themselves are a runtime value (for example a
/// tool projecting rows out of a foreign schema it discovers at run time). Attach a
/// <see cref="MarkoutTable"/> as a model property and the formatter renders it exactly as it would
/// a generated table: it participates in section ordering, inclusion filtering, and row windowing,
/// and it decomposes to TSV/JSONL by the same rules, so a caller never re-earns those features by
/// hand.
/// </para>
/// <para>
/// A column projection that matches none of this table's columns renders nothing rather than
/// throwing, because the same projection may be aimed at a sibling section whose columns differ.
/// Generated tables follow the same rule, so projection behaves identically for both shapes.
/// </para>
/// </remarks>
public sealed class MarkoutTable
{
    /// <summary>Creates a table from display headers and rows.</summary>
    /// <param name="headers">The column display headers.</param>
    /// <param name="rows">The rows; each row must have one cell per header.</param>
    public MarkoutTable(IReadOnlyList<string> headers, IReadOnlyList<string[]> rows)
        : this(headers, null, rows)
    {
    }

    /// <summary>Creates a table from display headers, stable structured column names, and rows.</summary>
    /// <param name="headers">The column display headers (used for Markdown/table headings).</param>
    /// <param name="headerNames">
    /// The stable column names keyed on in structured output (TSV/JSONL). When null, the display
    /// headers are used. Must be the same length as <paramref name="headers"/> when supplied.
    /// </param>
    /// <param name="rows">The rows; each row must have one cell per header.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="headerNames"/> is supplied with a different length than <paramref name="headers"/>;
    /// two columns share a canonical structured key; or a row is null or does not have exactly one
    /// cell per header.
    /// </exception>
    public MarkoutTable(IReadOnlyList<string> headers, IReadOnlyList<string>? headerNames, IReadOnlyList<string[]> rows)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(rows);
        if (headerNames != null && headerNames.Count != headers.Count)
            throw new ArgumentException("Header names must have the same length as headers.", nameof(headerNames));

        // Structured output keys on the canonical (snake_case) column name, so two columns whose
        // names canonicalize alike would emit a JSONL object with duplicate keys — from which a
        // consumer recovers only the last value. Reject at construction rather than emit output
        // that silently loses a column.
        var effectiveNames = headerNames ?? headers;
        if (effectiveNames.Count > 1)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < effectiveNames.Count; i++)
            {
                var key = Formatting.FormatHelper.ToSnakeCase(effectiveNames[i] ?? "");
                if (!seen.Add(key))
                    throw new ArgumentException(
                        $"Two columns share the canonical structured key '{key}'. Structured output would emit duplicate keys and lose a column.",
                        headerNames != null ? nameof(headerNames) : nameof(headers));
            }
        }

        // Every renderer indexes rows positionally against the headers. A short row emits a
        // truncated Markdown row, a long row emits cells under no header, and the formats disagree
        // about which — Markdown and TSV keep the extra cell while JSONL drops it. Fail here, where
        // the caller can see which row is wrong, rather than emit a malformed table.
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row == null)
                throw new ArgumentException($"Row {i} is null.", nameof(rows));
            if (row.Length != headers.Count)
                throw new ArgumentException(
                    $"Row {i} has {row.Length} cell(s) but the table has {headers.Count} column(s). Each row must have one cell per header.",
                    nameof(rows));
        }

        Headers = headers;
        HeaderNames = headerNames;
        Rows = rows;
    }

    /// <summary>The column display headers.</summary>
    public IReadOnlyList<string> Headers { get; }

    /// <summary>The stable structured column names keyed on in TSV/JSONL, or null to use <see cref="Headers"/>.</summary>
    public IReadOnlyList<string>? HeaderNames { get; }

    /// <summary>The rows, each carrying one cell per column.</summary>
    public IReadOnlyList<string[]> Rows { get; }

    /// <summary>
    /// True when the table has no columns, so a section over it renders nothing. A table with
    /// columns but no rows still renders its header row, matching a generated empty table.
    /// </summary>
    public bool IsEmpty => Headers.Count == 0;
}
