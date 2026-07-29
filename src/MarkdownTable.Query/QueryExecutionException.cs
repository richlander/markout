namespace MarkdownTable.Query;

/// <summary>
/// Exception thrown when a query fails during execution.
/// </summary>
public class QueryExecutionException : Exception
{
    /// <summary>Creates the exception.</summary>
    /// <param name="message">Description of the execution failure.</param>
    public QueryExecutionException(string message) : base(message) { }
}
