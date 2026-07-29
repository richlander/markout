namespace MarkdownTable.Query.Operations;

/// <summary>
/// Takes the first N rows.
/// </summary>
public class TakeOperation : ITableOperation
{
    private readonly int _count;

    /// <summary>Creates the operation.</summary>
    /// <param name="count">Number of leading rows to keep.</param>
    public TakeOperation(int count)
    {
        _count = count;
    }

    /// <inheritdoc/>
    public QueryResult Execute(string[] headers, List<string[]> rows)
    {
        var taken = rows.Take(_count).ToList();
        return QueryResult.Table(headers, taken);
    }
}

/// <summary>
/// Skips the first N rows.
/// </summary>
public class SkipOperation : ITableOperation
{
    private readonly int _count;

    /// <summary>Creates the operation.</summary>
    /// <param name="count">Number of leading rows to discard.</param>
    public SkipOperation(int count)
    {
        _count = count;
    }

    /// <inheritdoc/>
    public QueryResult Execute(string[] headers, List<string[]> rows)
    {
        var skipped = rows.Skip(_count).ToList();
        return QueryResult.Table(headers, skipped);
    }
}

/// <summary>
/// Returns the first row.
/// </summary>
public class FirstOperation : ITableOperation
{
    /// <inheritdoc/>
    public QueryResult Execute(string[] headers, List<string[]> rows)
    {
        if (rows.Count == 0)
            return QueryResult.Table(headers, []);

        return QueryResult.Table(headers, [rows[0]]);
    }
}

/// <summary>
/// Returns the last row.
/// </summary>
public class LastOperation : ITableOperation
{
    /// <inheritdoc/>
    public QueryResult Execute(string[] headers, List<string[]> rows)
    {
        if (rows.Count == 0)
            return QueryResult.Table(headers, []);

        return QueryResult.Table(headers, [rows[^1]]);
    }
}

/// <summary>
/// Returns the row count as a scalar.
/// </summary>
public class CountOperation : ITableOperation
{
    /// <inheritdoc/>
    public QueryResult Execute(string[] headers, List<string[]> rows)
    {
        return QueryResult.Scalar(rows.Count.ToString());
    }
}

/// <summary>
/// Removes duplicate rows.
/// </summary>
public class DistinctOperation : ITableOperation
{
    /// <inheritdoc/>
    public QueryResult Execute(string[] headers, List<string[]> rows)
    {
        var seen = new HashSet<string>();
        var distinct = new List<string[]>();

        foreach (var row in rows)
        {
            var key = string.Join("\0", row);
            if (seen.Add(key))
                distinct.Add(row);
        }

        return QueryResult.Table(headers, distinct);
    }
}

/// <summary>
/// Indexes into the table by row index (supports negative indices).
/// </summary>
public class IndexOperation : ITableOperation
{
    private readonly int _index;

    /// <summary>Creates the operation.</summary>
    /// <param name="index">Row index to select. Negative values count back from the last row.</param>
    public IndexOperation(int index)
    {
        _index = index;
    }

    /// <inheritdoc/>
    public QueryResult Execute(string[] headers, List<string[]> rows)
    {
        var idx = _index < 0 ? rows.Count + _index : _index;
        if (idx < 0 || idx >= rows.Count)
            throw new QueryExecutionException($"Row index {_index} is out of range (table has {rows.Count} rows).");

        return QueryResult.Table(headers, [rows[idx]]);
    }
}

/// <summary>
/// Slices a range of rows: .[start:end]
/// </summary>
public class SliceOperation : ITableOperation
{
    private readonly int? _start;
    private readonly int? _end;

    /// <summary>Creates the operation.</summary>
    /// <param name="start">Inclusive start row index, or <see langword="null"/> for the first row. Negative values count back from the last row.</param>
    /// <param name="end">Exclusive end row index, or <see langword="null"/> for past the last row. Negative values count back from the last row.</param>
    public SliceOperation(int? start, int? end)
    {
        _start = start;
        _end = end;
    }

    /// <inheritdoc/>
    public QueryResult Execute(string[] headers, List<string[]> rows)
    {
        var start = _start ?? 0;
        var end = _end ?? rows.Count;

        if (start < 0) start = Math.Max(0, rows.Count + start);
        if (end < 0) end = Math.Max(0, rows.Count + end);

        start = Math.Max(0, Math.Min(start, rows.Count));
        end = Math.Max(start, Math.Min(end, rows.Count));

        return QueryResult.Table(headers, rows.GetRange(start, end - start));
    }
}

/// <summary>
/// Extracts a single column from all rows as a single-column table.
/// </summary>
public class ColumnExtractOperation : ITableOperation
{
    private readonly string _column;

    /// <summary>Creates the operation.</summary>
    /// <param name="column">Name of the column to extract. Matched case-insensitively.</param>
    public ColumnExtractOperation(string column)
    {
        _column = column;
    }

    /// <inheritdoc/>
    public QueryResult Execute(string[] headers, List<string[]> rows)
    {
        var colIdx = Array.FindIndex(headers, h => string.Equals(h, _column, StringComparison.OrdinalIgnoreCase));
        if (colIdx < 0)
            throw new QueryExecutionException($"Column '{_column}' not found. Available columns: {string.Join(", ", headers)}");

        var newRows = new List<string[]>(rows.Count);
        foreach (var row in rows)
        {
            newRows.Add([colIdx < row.Length ? row[colIdx] : ""]);
        }

        return QueryResult.Table([_column], newRows);
    }
}

/// <summary>
/// Extracts a scalar value from a specific row and column.
/// </summary>
public class CellExtractOperation : ITableOperation
{
    private readonly int _rowIndex;
    private readonly string _column;

    /// <summary>Creates the operation.</summary>
    /// <param name="rowIndex">Row index to read. Negative values count back from the last row.</param>
    /// <param name="column">Name of the column to read. Matched case-insensitively.</param>
    public CellExtractOperation(int rowIndex, string column)
    {
        _rowIndex = rowIndex;
        _column = column;
    }

    /// <inheritdoc/>
    public QueryResult Execute(string[] headers, List<string[]> rows)
    {
        var rowIdx = _rowIndex < 0 ? rows.Count + _rowIndex : _rowIndex;
        if (rowIdx < 0 || rowIdx >= rows.Count)
            throw new QueryExecutionException($"Row index {_rowIndex} is out of range (table has {rows.Count} rows).");

        var colIdx = Array.FindIndex(headers, h => string.Equals(h, _column, StringComparison.OrdinalIgnoreCase));
        if (colIdx < 0)
            throw new QueryExecutionException($"Column '{_column}' not found. Available columns: {string.Join(", ", headers)}");

        var row = rows[rowIdx];
        var value = colIdx < row.Length ? row[colIdx] : "";
        return QueryResult.Scalar(value);
    }
}
