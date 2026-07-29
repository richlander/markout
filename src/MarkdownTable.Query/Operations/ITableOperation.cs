namespace MarkdownTable.Query.Operations;

/// <summary>
/// A table operation that transforms headers and rows into a new result.
/// </summary>
public interface ITableOperation
{
    /// <summary>
    /// Applies the operation to the given table.
    /// </summary>
    /// <param name="headers">The input table's header cells.</param>
    /// <param name="rows">The input table's data rows.</param>
    /// <returns>The operation's result, which may be a table or a scalar.</returns>
    QueryResult Execute(string[] headers, List<string[]> rows);
}
