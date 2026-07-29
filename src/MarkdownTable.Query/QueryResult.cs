using MarkdownTable.Formatting;

namespace MarkdownTable.Query;

/// <summary>
/// The result of executing a query — either a table or a scalar value.
/// </summary>
public abstract class QueryResult
{
    /// <summary>Creates a <see cref="TableResult"/>.</summary>
    /// <param name="headers">The result's header cells.</param>
    /// <param name="rows">The result's data rows.</param>
    /// <returns>A table result.</returns>
    public static QueryResult Table(string[] headers, List<string[]> rows) => new TableResult(headers, rows);

    /// <summary>Creates a <see cref="ScalarResult"/>.</summary>
    /// <param name="value">The scalar value.</param>
    /// <returns>A scalar result.</returns>
    public static QueryResult Scalar(string value) => new ScalarResult(value);
}

/// <summary>
/// A table result containing headers and rows.
/// </summary>
public sealed class TableResult : QueryResult
{
    /// <summary>The result's header cells, left to right.</summary>
    public string[] Headers { get; }

    /// <summary>The result's data rows, each holding one cell per header.</summary>
    public List<string[]> Rows { get; }

    /// <summary>Creates a table result.</summary>
    /// <param name="headers">The result's header cells.</param>
    /// <param name="rows">The result's data rows.</param>
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
    /// <summary>The scalar value.</summary>
    public string Value { get; }

    /// <summary>Creates a scalar result.</summary>
    /// <param name="value">The scalar value.</param>
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
    /// <summary>The fields, keyed by field name.</summary>
    public Dictionary<string, FieldValue> Fields { get; }

    /// <summary>Creates a fields result.</summary>
    /// <param name="fields">The fields, keyed by field name.</param>
    public FieldsResult(Dictionary<string, FieldValue> fields)
    {
        Fields = fields;
    }
}
