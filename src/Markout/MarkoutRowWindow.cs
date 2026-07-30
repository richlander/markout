namespace Markout;

/// <summary>
/// A per-table data-row window: which data rows a table emits, preserving its
/// headings and header row. A window is either <em>relative</em> — the first
/// (<see cref="Head"/>) or last (<see cref="Tail"/>) N rows, where which rows
/// those are depends on how many the table has — or <em>absolute</em>
/// (<see cref="Range"/>), naming row numbers to keep regardless of the table's
/// size.
///
/// <para>
/// The two are not interchangeable, which is why this type is constructed
/// through named factories rather than a positional constructor: a bare pair of
/// numbers cannot say whether it means "two rows" or "row two".
/// </para>
///
/// <para>
/// A window is <em>selection</em>, not summarization, which is what separates it
/// from <see cref="MarkoutWriterOptions.MaxItems"/>. A windowed table emits no
/// ellipsis row and reports no skipped count, so its output stays
/// machine-consumable and its row count is exactly what a caller counting the
/// same window would compute.
/// </para>
///
/// <para>
/// <see cref="Resolve"/> is the single place these semantics are interpreted.
/// Every emission path resolves through it rather than branching on the window's
/// shape, so a change to what a window means cannot land in one table mode and
/// miss another.
/// </para>
/// </summary>
public readonly record struct MarkoutRowWindow
{
    private readonly int _count;
    private readonly int _start;
    private readonly int? _end;

    private MarkoutRowWindow(MarkoutRowWindowKind kind, int count, int start, int? end)
    {
        Kind = kind;
        _count = count;
        _start = start;
        _end = end;
    }

    /// <summary>Whether this window counts rows or names them.</summary>
    public MarkoutRowWindowKind Kind { get; }

    /// <summary>
    /// True when this window keeps every row, so a renderer can skip windowing
    /// entirely. Only a relative window can be unlimited (via a negative count);
    /// an absolute range always names a bounded start, even when open-ended.
    /// </summary>
    public bool IsUnlimited => Kind != MarkoutRowWindowKind.Range && _count < 0;

    /// <summary>
    /// Keep the first <paramref name="count"/> data rows. A negative count means
    /// "no limit", which lets a caller hold a window unconditionally rather than
    /// forking between a window and none.
    /// </summary>
    /// <param name="count">How many leading rows to keep; negative for no limit.</param>
    public static MarkoutRowWindow Head(int count) => new(MarkoutRowWindowKind.Head, count, 0, null);

    /// <summary>Keep the last <paramref name="count"/> data rows.</summary>
    /// <param name="count">How many trailing rows to keep; negative for no limit.</param>
    public static MarkoutRowWindow Tail(int count) => new(MarkoutRowWindowKind.Tail, count, 0, null);

    /// <summary>
    /// Keep the rows numbered <paramref name="start"/> through
    /// <paramref name="end"/> inclusive, or through the last row when
    /// <paramref name="end"/> is null. Row numbers are 1-based and are the
    /// numbers a reader counts in the rendered table.
    /// </summary>
    /// <param name="start">1-based number of the first row to keep.</param>
    /// <param name="end">1-based number of the last row to keep, or null for the last row.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="start"/> is less than 1, or <paramref name="end"/> is less
    /// than <paramref name="start"/>.
    /// </exception>
    public static MarkoutRowWindow Range(int start, int? end)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(start, 1);
        if (end is int e)
            ArgumentOutOfRangeException.ThrowIfLessThan(e, start);
        return new(MarkoutRowWindowKind.Range, 0, start, end);
    }

    /// <summary>
    /// Resolves this window against a table of <paramref name="dataCount"/> data
    /// rows, returning the half-open range of 0-based row positions to keep.
    ///
    /// <para>
    /// The result is always a valid range (<c>0 &lt;= keepStart &lt;= keepEnd
    /// &lt;= dataCount</c>), so a caller can use it without re-clamping. An
    /// absolute range starting past the end of the table resolves to an empty
    /// window rather than an error: the rows it names simply are not there.
    /// </para>
    ///
    /// <para>
    /// For a range the ordering half of that invariant holds by construction
    /// rather than by clamping here: <see cref="Range"/> rejects an end before
    /// its start, so <c>_end &gt;= _start</c>, and <see cref="Math.Min(int,int)"/>
    /// is monotonic.
    /// </para>
    /// </summary>
    /// <param name="dataCount">How many data rows the table has.</param>
    /// <returns>The half-open range of 0-based row positions to keep.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="dataCount"/> is negative.</exception>
    public (int KeepStart, int KeepEnd) Resolve(int dataCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(dataCount);

        switch (Kind)
        {
            case MarkoutRowWindowKind.Head:
                // A negative count is "no limit"; clamping it to dataCount keeps
                // the whole table rather than emptying it.
                return (0, _count < 0 ? dataCount : Math.Min(_count, dataCount));
            case MarkoutRowWindowKind.Tail:
                return (_count < 0 ? 0 : Math.Max(0, dataCount - _count), dataCount);
            default:
                var start = Math.Min(_start - 1, dataCount);
                var end = _end is int e ? Math.Min(e, dataCount) : dataCount;
                return (start, end);
        }
    }

    /// <summary>
    /// Applies this window to a materialized row list, returning the rows a
    /// table would keep. Resolves through <see cref="Resolve"/> against the same
    /// row count the renderer sees, which is what keeps a caller's own row count
    /// equal to the windowed table it describes.
    /// </summary>
    /// <typeparam name="T">The row type.</typeparam>
    /// <param name="rows">The rows to window.</param>
    /// <returns>The rows within the window, in their original order.</returns>
    public IReadOnlyList<T> Apply<T>(IReadOnlyList<T> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var (keepStart, keepEnd) = Resolve(rows.Count);
        if (keepStart == 0 && keepEnd == rows.Count)
            return rows;

        var kept = new List<T>(keepEnd - keepStart);
        for (var i = keepStart; i < keepEnd; i++)
            kept.Add(rows[i]);
        return kept;
    }

    /// <summary>
    /// Applies an optional window, treating "no window" and "unlimited" alike as
    /// keeping every row. Callers hold a window as a nullable, so putting the
    /// null handling here keeps it from being re-derived per caller.
    /// </summary>
    /// <typeparam name="T">The row type.</typeparam>
    /// <param name="window">The window to apply, or null to keep every row.</param>
    /// <param name="rows">The rows to window.</param>
    /// <returns>The rows within the window, in their original order.</returns>
    public static IReadOnlyList<T> Apply<T>(MarkoutRowWindow? window, IReadOnlyList<T> rows) =>
        window is { IsUnlimited: false } w ? w.Apply(rows) : rows;
}

/// <summary>Whether a <see cref="MarkoutRowWindow"/> counts rows or names them.</summary>
public enum MarkoutRowWindowKind
{
    /// <summary>Keep a count of rows from the start of the table.</summary>
    Head,

    /// <summary>Keep a count of rows from the end of the table.</summary>
    Tail,

    /// <summary>Keep rows by their 1-based row numbers.</summary>
    Range
}
