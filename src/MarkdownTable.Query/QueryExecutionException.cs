namespace MarkdownTable.Query;

/// <summary>
/// Exception thrown when a query fails during execution.
/// </summary>
public class QueryExecutionException : Exception
{
    public QueryExecutionException(string message) : base(message) { }
}
