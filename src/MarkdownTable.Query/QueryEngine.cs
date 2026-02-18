using MarkdownTable.Formatting;
using MarkdownTable.Query.Operations;

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
            // No table — check if the name resolves to a field
            if (query.SectionName is not null)
            {
                var fieldResult = ResolveField(doc, query.SectionName, query.Operations);
                if (fieldResult is not null)
                    return fieldResult;
            }

            var msg = query.SectionName is not null
                ? $"No table or field found for '{query.SectionName}'."
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
    /// Formats a query result as a string (markdown table, scalar, or fields).
    /// </summary>
    public static string FormatResult(QueryResult result)
    {
        return result switch
        {
            ScalarResult scalar => scalar.Value,
            TableResult table => TableFormatter.Format(table.Headers, table.Rows),
            FieldsResult fields => FormatFields(fields.Fields),
            _ => throw new InvalidOperationException("Unknown result type."),
        };
    }

    private static string FormatFields(Dictionary<string, FieldValue> fields)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var (key, value) in fields)
        {
            if (value.IsArray)
            {
                sb.AppendLine($"**{key}:**");
                foreach (var item in value.Items)
                    sb.AppendLine($"- {item}");
            }
            else
            {
                sb.AppendLine($"**{key}:** {value.Text}");
            }
        }
        return sb.ToString().TrimEnd();
    }

    private static QueryResult? ResolveField(
        MarkdownDocument doc, string name, List<ITableOperation> operations)
    {
        // Check top-level fields first
        var fields = doc.Fields;
        if (fields.TryGetValue(name, out var fieldValue))
        {
            if (operations.Count == 0)
                return QueryResult.Scalar(fieldValue.Text);

            // Can't apply table operations to a scalar field
            throw new QueryExecutionException(
                $"Cannot apply table operations to field '{name}' (scalar value).");
        }

        // Check section fields (section exists but has no table)
        foreach (var section in doc.Sections)
        {
            if (string.Equals(section.Heading, name, StringComparison.OrdinalIgnoreCase)
                && section.Fields.Count > 0)
            {
                if (operations.Count == 0)
                    return new FieldsResult(section.Fields);

                throw new QueryExecutionException(
                    $"Cannot apply table operations to section '{name}' (contains fields, not a table).");
            }
        }

        return null;
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
