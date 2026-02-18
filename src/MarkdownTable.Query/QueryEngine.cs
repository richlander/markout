using MarkdownTable.Formatting;

namespace MarkdownTable.Query;

/// <summary>
/// Executes queries against markdown documents.
/// </summary>
public static class QueryEngine
{
    /// <summary>
    /// Executes a query against a markdown document string.
    /// </summary>
    public static QueryResult Execute(string markdown, string query)
    {
        var doc = DocumentReader.Read(markdown);
        var parsed = QueryParser.Parse(query);
        return Execute(doc, parsed);
    }

    /// <summary>
    /// Executes a query against a UTF-8 byte buffer using byte-level parsing.
    /// </summary>
    public static QueryResult Execute(ReadOnlySpan<byte> utf8, string query)
    {
        var doc = DocumentReader.Read(utf8);
        var parsed = QueryParser.Parse(query);
        return Execute(doc, parsed);
    }

    /// <summary>
    /// Executes a query against a stream of markdown using buffered I/O.
    /// </summary>
    public static async Task<QueryResult> ExecuteAsync(
        Stream stream, string query, CancellationToken cancellationToken = default)
    {
        var doc = await DocumentReader.ReadAsync(stream, cancellationToken);
        var parsed = QueryParser.Parse(query);
        return Execute(doc, parsed);
    }

    /// <summary>
    /// Executes a parsed query against a parsed document.
    /// </summary>
    public static QueryResult Execute(MarkdownDocument doc, ParsedQuery query)
    {
        // Resolve the target table
        var table = ResolveTable(doc, query.SectionName);

        if (table is null)
        {
            var msg = query.SectionName is not null
                ? $"No table found in section '{query.SectionName}'."
                : "No table found in document.";
            throw new QueryExecutionException(msg);
        }

        // Execute the operation pipeline
        QueryResult result = QueryResult.Table(table.Headers, table.Rows);

        foreach (var op in query.Operations)
        {
            if (result is not TableResult tableResult)
                throw new QueryExecutionException("Cannot apply table operations to a scalar result.");

            result = op.Execute(tableResult.Headers, tableResult.Rows);
        }

        return result;
    }

    /// <summary>
    /// Formats a query result as a string (markdown table or scalar value).
    /// </summary>
    public static string FormatResult(QueryResult result)
    {
        return result switch
        {
            ScalarResult scalar => scalar.Value,
            TableResult table => TableFormatter.Format(table.Headers, table.Rows),
            _ => throw new InvalidOperationException("Unknown result type."),
        };
    }

    private static DocumentTable? ResolveTable(MarkdownDocument doc, string? sectionName)
    {
        if (sectionName is null)
            return doc.DefaultTable;

        // Find section by name (case-insensitive)
        foreach (var section in doc.Sections)
        {
            if (string.Equals(section.Heading, sectionName, StringComparison.OrdinalIgnoreCase))
            {
                if (section.Table is not null)
                    return section.Table;
            }
        }

        // Also try matching as a prefix or partial match
        foreach (var section in doc.Sections)
        {
            if (section.Heading is not null &&
                section.Heading.Contains(sectionName, StringComparison.OrdinalIgnoreCase))
            {
                if (section.Table is not null)
                    return section.Table;
            }
        }

        return null;
    }
}
