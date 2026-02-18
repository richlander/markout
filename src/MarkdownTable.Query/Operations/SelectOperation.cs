namespace MarkdownTable.Query.Operations;

/// <summary>
/// Projects specific columns from the table.
/// </summary>
public class SelectOperation : ITableOperation
{
    private readonly string[] _columns;

    public SelectOperation(string[] columns)
    {
        _columns = columns;
    }

    public QueryResult Execute(string[] headers, List<string[]> rows)
    {
        var indices = ResolveColumnIndices(headers, _columns);
        var newHeaders = indices.Select(i => headers[i]).ToArray();
        var newRows = new List<string[]>(rows.Count);

        foreach (var row in rows)
        {
            var newRow = new string[indices.Length];
            for (int j = 0; j < indices.Length; j++)
            {
                var idx = indices[j];
                newRow[j] = idx < row.Length ? row[idx] : "";
            }
            newRows.Add(newRow);
        }

        return QueryResult.Table(newHeaders, newRows);
    }

    internal static int[] ResolveColumnIndices(string[] headers, string[] columns)
    {
        var indices = new int[columns.Length];
        for (int i = 0; i < columns.Length; i++)
        {
            var idx = Array.FindIndex(headers, h => string.Equals(h, columns[i], StringComparison.OrdinalIgnoreCase));
            if (idx < 0)
                throw new QueryExecutionException($"Column '{columns[i]}' not found. Available columns: {string.Join(", ", headers)}");
            indices[i] = idx;
        }
        return indices;
    }
}
