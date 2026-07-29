namespace MarkdownTable.Query.Operations;

/// <summary>
/// Filters rows based on a condition.
/// </summary>
public class WhereOperation : ITableOperation
{
    private readonly string _column;
    private readonly TokenKind _op;
    private readonly string _value;

    /// <summary>Creates the operation.</summary>
    /// <param name="column">Name of the column to test. Matched case-insensitively.</param>
    /// <param name="op">The comparison operator, such as <see cref="TokenKind.Equal"/>.</param>
    /// <param name="value">The value to compare each cell against.</param>
    public WhereOperation(string column, TokenKind op, string value)
    {
        _column = column;
        _op = op;
        _value = value;
    }

    /// <inheritdoc/>
    public QueryResult Execute(string[] headers, List<string[]> rows)
    {
        var colIdx = FindColumn(headers, _column);
        var filtered = new List<string[]>();

        foreach (var row in rows)
        {
            var cellValue = colIdx < row.Length ? row[colIdx] : "";
            if (Evaluate(cellValue, _op, _value))
                filtered.Add(row);
        }

        return QueryResult.Table(headers, filtered);
    }

    internal static bool Evaluate(string cellValue, TokenKind op, string compareValue)
    {
        // Try numeric comparison if both sides are numbers
        if (double.TryParse(cellValue, out var cellNum) && double.TryParse(compareValue, out var compNum))
        {
            return op switch
            {
                TokenKind.Equal => cellNum == compNum,
                TokenKind.NotEqual => cellNum != compNum,
                TokenKind.GreaterThan => cellNum > compNum,
                TokenKind.LessThan => cellNum < compNum,
                TokenKind.GreaterOrEqual => cellNum >= compNum,
                TokenKind.LessOrEqual => cellNum <= compNum,
                _ => false,
            };
        }

        // String comparison
        var cmp = string.Compare(cellValue, compareValue, StringComparison.OrdinalIgnoreCase);
        return op switch
        {
            TokenKind.Equal => cmp == 0,
            TokenKind.NotEqual => cmp != 0,
            TokenKind.GreaterThan => cmp > 0,
            TokenKind.LessThan => cmp < 0,
            TokenKind.GreaterOrEqual => cmp >= 0,
            TokenKind.LessOrEqual => cmp <= 0,
            _ => false,
        };
    }

    private static int FindColumn(string[] headers, string name)
    {
        var idx = Array.FindIndex(headers, h => string.Equals(h, name, StringComparison.OrdinalIgnoreCase));
        if (idx < 0)
            throw new QueryExecutionException($"Column '{name}' not found. Available columns: {string.Join(", ", headers)}");
        return idx;
    }
}
