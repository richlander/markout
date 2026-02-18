namespace MarkdownTable.Query.Operations;

/// <summary>
/// A table operation that transforms headers and rows into a new result.
/// </summary>
public interface ITableOperation
{
    QueryResult Execute(string[] headers, List<string[]> rows);
}
