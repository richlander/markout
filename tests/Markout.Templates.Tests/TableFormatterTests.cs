using MarkdownTable.Formatting;

namespace Markout.Templates.Tests;

public class TableFormatterTests
{
    [Fact]
    public void Format_SimpleTable()
    {
        var headers = new[] { "Name", "Value" };
        var rows = new List<string[]>
        {
            new[] { "Alpha", "1" },
            new[] { "Beta", "2" },
        };

        var result = TableFormatter.Format(headers, rows);

        Assert.Contains("| Name", result);
        Assert.Contains("| Alpha", result);
        Assert.Contains("---", result);
    }

    [Fact]
    public void Format_UnevenColumnWidths()
    {
        var headers = new[] { "Short", "Much Longer Header" };
        var rows = new List<string[]>
        {
            new[] { "a", "b" },
            new[] { "longer value", "c" },
        };

        var result = TableFormatter.Format(headers, rows);

        // Header should be padded to at least its own length
        Assert.Contains("Much Longer Header", result);
        // The separator should have dashes matching the header width
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.True(lines.Length >= 3);
    }

    [Fact]
    public void Format_EmptyHeaders_ReturnsEmpty()
    {
        var result = TableFormatter.Format(Array.Empty<string>(), new List<string[]>());
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Reformat_ValidTable()
    {
        var input = """
            | Name | Value |
            | --- | --- |
            | Alpha | 1 |
            | Beta | 2 |
            """;

        var result = TableFormatter.Reformat(input);

        Assert.Contains("Alpha", result);
        Assert.Contains("Beta", result);
    }

    [Fact]
    public void Reformat_NotATable_ReturnsOriginal()
    {
        var input = "This is just text.";
        var result = TableFormatter.Reformat(input);
        Assert.Equal(input, result);
    }

    [Fact]
    public void Format_WithAutoTune()
    {
        var headers = new[] { "CVE", "Severity", "Component" };
        var rows = new List<string[]>
        {
            new[] { "CVE-2026-1234", "Critical", "System.Net.Http" },
            new[] { "CVE-2026-1235", "High", "Microsoft.Data.SqlClient" },
            new[] { "CVE-2026-1236", "Medium", "System.Text.Json" },
        };

        var options = new TableFormatterOptions { AutoTune = true };
        var result = TableFormatter.Format(headers, rows, options);

        Assert.Contains("CVE-2026-1234", result);
        Assert.Contains("Microsoft.Data.SqlClient", result);
    }

    [Fact]
    public void Format_OutlierRow_HandledGracefully()
    {
        var headers = new[] { "Name", "Description" };
        var rows = new List<string[]>
        {
            new[] { "Short", "Brief" },
            new[] { "Also short", "Tiny" },
            new[] { "Reasonable", "This is a very long description that far exceeds the typical column width and should be handled as an outlier by the statistical algorithm" },
        };

        var result = TableFormatter.Format(headers, rows);

        // Should produce valid output without crashing
        Assert.Contains("Short", result);
        Assert.Contains("very long description", result);
    }
}

public class TableParserTests
{
    [Fact]
    public void TryParse_ValidTable()
    {
        var text = """
            | Name | Value |
            | ---- | ----- |
            | A    | 1     |
            | B    | 2     |
            """;

        Assert.True(TableParser.TryParse(text, out var headers, out var rows));
        Assert.Equal(new[] { "Name", "Value" }, headers);
        Assert.Equal(2, rows.Count);
        Assert.Equal("A", rows[0][0]);
    }

    [Fact]
    public void TryParse_NoPipes_ReturnsFalse()
    {
        Assert.False(TableParser.TryParse("Not a table", out _, out _));
    }

    [Fact]
    public void TryParse_NoSeparator_ReturnsFalse()
    {
        var text = """
            | Name | Value |
            | A    | 1     |
            """;

        // Second line doesn't have all dashes — it's a data row, not a separator
        Assert.False(TableParser.TryParse(text, out _, out _));
    }

    [Fact]
    public void IsPipeTableLine_Various()
    {
        Assert.True(TableParser.IsPipeTableLine("| Name | Value |"));
        Assert.True(TableParser.IsPipeTableLine("| --- | --- |"));
        Assert.False(TableParser.IsPipeTableLine("# Heading"));
        Assert.False(TableParser.IsPipeTableLine("{{placeholder}}"));
        Assert.False(TableParser.IsPipeTableLine("No pipes here"));
    }
}
