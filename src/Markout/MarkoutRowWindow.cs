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
/// This type is the single owner of what a window means. It offers two ways to
/// ask — <see cref="Resolve"/> against a known row count, and
/// <see cref="KeepsPosition"/>/<see cref="IsPastEnd"/> for a row whose table has
/// not finished arriving — so that a streaming caller never has to re-derive the
/// semantics for itself. The two must agree, and a renderer is expected to prove
/// that rather than assume it.
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
    /// Whether every row this window keeps can be decided from that row's position
    /// alone. True for <see cref="Head"/> and <see cref="Range"/>, whose bounds are
    /// fixed in advance; false for <see cref="Tail"/>, which cannot know which rows
    /// are the last ones until the table ends.
    ///
    /// <para>
    /// A renderer uses this to decide whether it may stream. It is not a hint about
    /// what the window means — <see cref="KeepsPosition"/> answers that.
    /// </para>
    /// </summary>
    public bool IsPositional => Kind != MarkoutRowWindowKind.Tail;

    /// <summary>
    /// The most rows a <see cref="Tail"/> window can ever keep, and therefore the
    /// most a renderer buffering for one needs to retain. Zero for the positional
    /// kinds, which need to retain nothing.
    /// </summary>
    public int RetentionBound => Kind == MarkoutRowWindowKind.Tail ? _count : 0;

    /// <summary>
    /// Keep the first <paramref name="count"/> data rows.
    /// </summary>
    /// <param name="count">How many leading rows to keep.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    public static MarkoutRowWindow Head(int count)
    {
        // A negative count is rejected rather than read as "no limit". "No limit" is
        // already spelled by not setting a window at all, and a count is usually
        // computed -- a subtraction that slips below zero should fail rather than
        // silently widen the table to everything.
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return new(MarkoutRowWindowKind.Head, count, 0, null);
    }

    /// <summary>Keep the last <paramref name="count"/> data rows.</summary>
    /// <param name="count">How many trailing rows to keep.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    public static MarkoutRowWindow Tail(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return new(MarkoutRowWindowKind.Tail, count, 0, null);
    }

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
                return (0, Math.Min(_count, dataCount));
            case MarkoutRowWindowKind.Tail:
                return (Math.Max(0, dataCount - _count), dataCount);
            default:
                var start = Math.Min(_start - 1, dataCount);
                var end = _end is int e ? Math.Min(e, dataCount) : dataCount;
                return (start, end);
        }
    }

    /// <summary>
    /// Whether the data row at 0-based <paramref name="position"/> is inside this
    /// window, for a table whose total row count is not yet known.
    ///
    /// <para>
    /// Only meaningful when <see cref="IsPositional"/> is true; a <see cref="Tail"/>
    /// window has no answer until the table ends, and asking throws rather than
    /// returning a guess.
    /// </para>
    /// </summary>
    /// <param name="position">0-based position of the data row.</param>
    /// <exception cref="InvalidOperationException">This window is not positional.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is negative.</exception>
    public bool KeepsPosition(int position)
    {
        ThrowIfNotPositional();
        ArgumentOutOfRangeException.ThrowIfNegative(position);

        return position >= FirstKeptPosition && position < EndPositionExclusive;
    }

    /// <summary>
    /// Whether no row at or after 0-based <paramref name="position"/> can be kept,
    /// so a caller may stop considering rows entirely.
    /// </summary>
    /// <param name="position">0-based position of the data row.</param>
    /// <exception cref="InvalidOperationException">This window is not positional.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is negative.</exception>
    public bool IsPastEnd(int position)
    {
        ThrowIfNotPositional();
        ArgumentOutOfRangeException.ThrowIfNegative(position);

        return position >= EndPositionExclusive;
    }

    private int FirstKeptPosition => Kind == MarkoutRowWindowKind.Range ? _start - 1 : 0;

    private long EndPositionExclusive => Kind switch
    {
        MarkoutRowWindowKind.Head => _count,
        MarkoutRowWindowKind.Range => _end ?? long.MaxValue,
        _ => long.MaxValue
    };

    private void ThrowIfNotPositional()
    {
        if (!IsPositional)
            throw new InvalidOperationException(
                $"A {Kind} window is defined against the table's total row count and cannot be resolved by position.");
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
    /// Applies an optional window, treating "no window" as keeping every row.
    /// Callers hold a window as a nullable, so putting the null handling here keeps
    /// it from being re-derived per caller.
    /// </summary>
    /// <typeparam name="T">The row type.</typeparam>
    /// <param name="window">The window to apply, or null to keep every row.</param>
    /// <param name="rows">The rows to window.</param>
    /// <returns>The rows within the window, in their original order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rows"/> is null.</exception>
    public static IReadOnlyList<T> Apply<T>(MarkoutRowWindow? window, IReadOnlyList<T> rows)
    {
        // Guard before the fast path, or a null list is returned as a non-null
        // result whenever the window happens to be absent or unlimited.
        ArgumentNullException.ThrowIfNull(rows);

        return window is { } w ? w.Apply(rows) : rows;
    }
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
