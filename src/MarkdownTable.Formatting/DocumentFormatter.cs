using System.Text;

namespace MarkdownTable.Formatting;

/// <summary>
/// Scans markdown documents, reformats tables in-place, and passes
/// non-table lines through unchanged.
/// </summary>
public static class DocumentFormatter
{
    /// <summary>
    /// Reformats all tables in a markdown document using the given options.
    /// Non-table content is passed through unchanged.
    /// </summary>
    public static string ReformatDocument(string document, TableFormatterOptions? options = null)
    {
        using var reader = new StringReader(document);
        var result = new StringBuilder();
        var tableLines = new List<string>();
        string? line;

        while ((line = reader.ReadLine()) is not null)
        {
            if (TableParser.IsPipeTableLine(line))
            {
                tableLines.Add(line);
            }
            else
            {
                if (tableLines.Count > 0)
                {
                    FlushTable(result, tableLines, options);
                    tableLines.Clear();
                }
                result.AppendLine(line);
            }
        }

        if (tableLines.Count > 0)
            FlushTable(result, tableLines, options);

        return result.ToString();
    }

    private static void FlushTable(StringBuilder result, List<string> tableLines, TableFormatterOptions? options)
    {
        if (TableParser.TryParse(tableLines, out var headers, out var rows))
        {
            result.Append(TableFormatter.Format(headers, rows, options));
        }
        else
        {
            foreach (var line in tableLines)
                result.AppendLine(line);
        }
    }
}
