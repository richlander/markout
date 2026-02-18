using MarkdownTable.Formatting;

namespace MarkdownTable.Query;

/// <summary>
/// The result of executing a query — either a table or a scalar value.
/// </summary>
public abstract class QueryResult
{
    public static QueryResult Table(string[] headers, List<string[]> rows) => new TableResult(headers, rows);
    public static QueryResult Scalar(string value) => new ScalarResult(value);
}

/// <summary>
/// A table result containing headers and rows.
/// </summary>
public sealed class TableResult : QueryResult
{
    public string[] Headers { get; }
    public List<string[]> Rows { get; }

    public TableResult(string[] headers, List<string[]> rows)
    {
        Headers = headers;
        Rows = rows;
    }
}

/// <summary>
/// A scalar result containing a single value (e.g., from count or field access).
/// </summary>
public sealed class ScalarResult : QueryResult
{
    public string Value { get; }

    public ScalarResult(string value)
    {
        Value = value;
    }
}

/// <summary>
/// A result containing key-value fields (e.g., from top-level field access or section fields).
/// </summary>
public sealed class FieldsResult : QueryResult
{
    public Dictionary<string, FieldValue> Fields { get; }

    public FieldsResult(Dictionary<string, FieldValue> fields)
    {
        Fields = fields;
    }
}
