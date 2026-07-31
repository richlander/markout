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
/// Unlike a generated table, a <see cref="MarkoutTable"/> resolves column projection
/// (<see cref="MarkoutProjection.IncludeColumns"/>) <em>tolerantly</em>: a projection that names
/// none of this table's columns leaves the table whole rather than throwing, because the same
/// projection may be aimed at a sibling section whose columns differ. A generated table keeps the
/// strict behavior, so this difference is a property of the shape, not of the writer.
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
    /// <paramref name="headerNames"/> is supplied with a different length than <paramref name="headers"/>.
    /// </exception>
    public MarkoutTable(IReadOnlyList<string> headers, IReadOnlyList<string>? headerNames, IReadOnlyList<string[]> rows)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(rows);
        if (headerNames != null && headerNames.Count != headers.Count)
            throw new ArgumentException("Header names must have the same length as headers.", nameof(headerNames));

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
