namespace MarkdownTable.Query.Operations;

/// <summary>
/// Sorts rows by a column.
/// </summary>
public class OrderByOperation : ITableOperation
{
    private readonly string _column;
    private readonly bool _descending;

    /// <summary>Creates the operation.</summary>
    /// <param name="column">Name of the column to sort by. Matched case-insensitively.</param>
    /// <param name="descending">Whether to sort in descending order.</param>
    public OrderByOperation(string column, bool descending)
    {
        _column = column;
        _descending = descending;
    }

    /// <inheritdoc/>
    public QueryResult Execute(string[] headers, List<string[]> rows)
    {
        var colIdx = Array.FindIndex(headers, h => string.Equals(h, _column, StringComparison.OrdinalIgnoreCase));
        if (colIdx < 0)
            throw new QueryExecutionException($"Column '{_column}' not found. Available columns: {string.Join(", ", headers)}");

        var sorted = new List<string[]>(rows);
        sorted.Sort((a, b) =>
        {
            var av = colIdx < a.Length ? a[colIdx] : "";
            var bv = colIdx < b.Length ? b[colIdx] : "";

            // Try numeric comparison
            if (double.TryParse(av, out var an) && double.TryParse(bv, out var bn))
            {
                var result = an.CompareTo(bn);
                return _descending ? -result : result;
            }

            var cmp = string.Compare(av, bv, StringComparison.OrdinalIgnoreCase);
            return _descending ? -cmp : cmp;
        });

        return QueryResult.Table(headers, sorted);
    }
}
