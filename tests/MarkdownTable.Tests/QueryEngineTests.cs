using MarkdownTable.Query;
using MarkdownTable.Formatting;

namespace MarkdownTable.Tests;

public class QueryEngineTests
{
    private const string TestTable = """
        | Name  | Age | City    |
        | ----- | --- | ------- |
        | Alice | 30  | NYC     |
        | Bob   | 25  | LA      |
        | Carol | 30  | NYC     |
        | Dave  | 35  | Chicago |
        """;

    private const string EmptyTable = """
        | Name | Age | City |
        | ---- | --- | ---- |
        """;

    // --- count ---

    [Fact]
    public void Execute_Count_ReturnsScalarResult4()
    {
        var result = QueryEngine.Execute(TestTable, "count");
        var scalar = Assert.IsType<ScalarResult>(result);
        Assert.Equal("4", scalar.Value);
    }

    // --- where ---

    [Fact]
    public void Execute_WhereEquals_FiltersMatchingRows()
    {
        var result = QueryEngine.Execute(TestTable, """where .Age == "30" """);
        var table = Assert.IsType<TableResult>(result);
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("Alice", table.Rows[0][0]);
        Assert.Equal("Carol", table.Rows[1][0]);
    }

    [Fact]
    public void Execute_WhereGreaterThan_NumericComparison()
    {
        var result = QueryEngine.Execute(TestTable, """where .Age > "26" """);
        var table = Assert.IsType<TableResult>(result);
        Assert.Equal(3, table.Rows.Count);
    }

    [Fact]
    public void Execute_WhereNotEquals_ExcludesMatchingRows()
    {
        var result = QueryEngine.Execute(TestTable, """where .City != "NYC" """);
        var table = Assert.IsType<TableResult>(result);
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("Bob", table.Rows[0][0]);
        Assert.Equal("Dave", table.Rows[1][0]);
    }

    // --- select ---

    [Fact]
    public void Execute_Select_ProjectsColumns()
    {
        var result = QueryEngine.Execute(TestTable, "select .Name, .City");
        var table = Assert.IsType<TableResult>(result);
        Assert.Equal(2, table.Headers.Length);
        Assert.Equal("Name", table.Headers[0]);
        Assert.Equal("City", table.Headers[1]);
        Assert.Equal(4, table.Rows.Count);
        Assert.Equal(2, table.Rows[0].Length);
    }

    // --- orderby ---

    [Fact]
    public void Execute_OrderByAsc_SortsAscending()
    {
        var result = QueryEngine.Execute(TestTable, "orderby .Age");
        var table = Assert.IsType<TableResult>(result);
        Assert.Equal("Bob", table.Rows[0][0]);
        Assert.Equal("Dave", table.Rows[^1][0]);
    }

    [Fact]
    public void Execute_OrderByDesc_SortsDescending()
    {
        var result = QueryEngine.Execute(TestTable, "orderby .Age desc");
        var table = Assert.IsType<TableResult>(result);
        Assert.Equal("Dave", table.Rows[0][0]);
        Assert.Equal("Bob", table.Rows[^1][0]);
    }

    // --- take / skip ---

    [Fact]
    public void Execute_Take2_ReturnsFirst2Rows()
    {
        var result = QueryEngine.Execute(TestTable, "take 2");
        var table = Assert.IsType<TableResult>(result);
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("Alice", table.Rows[0][0]);
        Assert.Equal("Bob", table.Rows[1][0]);
    }

    [Fact]
    public void Execute_Skip2_ReturnsLast2Rows()
    {
        var result = QueryEngine.Execute(TestTable, "skip 2");
        var table = Assert.IsType<TableResult>(result);
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("Carol", table.Rows[0][0]);
        Assert.Equal("Dave", table.Rows[1][0]);
    }

    // --- first / last ---

    [Fact]
    public void Execute_First_ReturnsFirstRow()
    {
        var result = QueryEngine.Execute(TestTable, "first");
        var table = Assert.IsType<TableResult>(result);
        Assert.Single(table.Rows);
        Assert.Equal("Alice", table.Rows[0][0]);
    }

    [Fact]
    public void Execute_Last_ReturnsLastRow()
    {
        var result = QueryEngine.Execute(TestTable, "last");
        var table = Assert.IsType<TableResult>(result);
        Assert.Single(table.Rows);
        Assert.Equal("Dave", table.Rows[0][0]);
    }

    // --- distinct ---

    [Fact]
    public void Execute_Distinct_ReturnsUniqueRows()
    {
        var result = QueryEngine.Execute(TestTable, "select .City | distinct");
        var table = Assert.IsType<TableResult>(result);
        Assert.Equal(3, table.Rows.Count);
    }

    // --- jq-style index access ---

    [Fact]
    public void Execute_IndexZero_ReturnsFirstRow()
    {
        var result = QueryEngine.Execute(TestTable, ".[0]");
        var table = Assert.IsType<TableResult>(result);
        Assert.Single(table.Rows);
        Assert.Equal("Alice", table.Rows[0][0]);
    }

    [Fact]
    public void Execute_NegativeIndex_ReturnsLastRow()
    {
        var result = QueryEngine.Execute(TestTable, ".[-1]");
        var table = Assert.IsType<TableResult>(result);
        Assert.Single(table.Rows);
        Assert.Equal("Dave", table.Rows[0][0]);
    }

    [Fact]
    public void Execute_Slice_ReturnsRowRange()
    {
        var result = QueryEngine.Execute(TestTable, ".[0:2]");
        var table = Assert.IsType<TableResult>(result);
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("Alice", table.Rows[0][0]);
        Assert.Equal("Bob", table.Rows[1][0]);
    }

    // --- column extraction ---

    [Fact]
    public void Execute_ExtractColumn_ReturnsAllValues()
    {
        var result = QueryEngine.Execute(TestTable, ".[].Name");
        var table = Assert.IsType<TableResult>(result);
        Assert.Equal(4, table.Rows.Count);
        Assert.Equal("Alice", table.Rows[0][0]);
        Assert.Equal("Bob", table.Rows[1][0]);
        Assert.Equal("Carol", table.Rows[2][0]);
        Assert.Equal("Dave", table.Rows[3][0]);
    }

    [Fact]
    public void Execute_ExtractCell_ReturnsScalar()
    {
        var result = QueryEngine.Execute(TestTable, ".[0].Name");
        var scalar = Assert.IsType<ScalarResult>(result);
        Assert.Equal("Alice", scalar.Value);
    }

    // --- pipes ---

    [Fact]
    public void Execute_PipeWhereCount_ReturnsFilteredCount()
    {
        var result = QueryEngine.Execute(TestTable, """where .City == "NYC" | count""");
        var scalar = Assert.IsType<ScalarResult>(result);
        Assert.Equal("2", scalar.Value);
    }

    [Fact]
    public void Execute_PipeWhereSelect_ReturnsFilteredProjection()
    {
        var result = QueryEngine.Execute(TestTable, """where .City == "NYC" | select .Name""");
        var table = Assert.IsType<TableResult>(result);
        Assert.Single(table.Headers);
        Assert.Equal("Name", table.Headers[0]);
        Assert.Equal(2, table.Rows.Count);
    }

    // --- case-insensitive column matching ---

    [Fact]
    public void Execute_CaseInsensitiveColumn_MatchesColumn()
    {
        var result = QueryEngine.Execute(TestTable, """where .city == "NYC" """);
        var table = Assert.IsType<TableResult>(result);
        Assert.Equal(2, table.Rows.Count);
    }

    // --- FormatResult ---

    [Fact]
    public void FormatResult_TableResult_ReturnsMarkdownTable()
    {
        var result = QueryEngine.Execute(TestTable, "take 2");
        var formatted = QueryEngine.FormatResult(result);
        Assert.Contains("| Name", formatted);
        Assert.Contains("Alice", formatted);
        Assert.Contains("Bob", formatted);
        Assert.Contains("---", formatted);
    }

    [Fact]
    public void FormatResult_ScalarResult_ReturnsPlainString()
    {
        var result = QueryEngine.Execute(TestTable, "count");
        var formatted = QueryEngine.FormatResult(result);
        Assert.Equal("4", formatted);
    }

    // --- error handling ---

    [Fact]
    public void Execute_InvalidQuery_ThrowsException()
    {
        Assert.ThrowsAny<Exception>(() => QueryEngine.Execute(TestTable, "gibberish totallyinvalid"));
    }

    // --- empty table ---

    [Fact]
    public void Execute_EmptyTable_CountReturnsZero()
    {
        var result = QueryEngine.Execute(EmptyTable, "count");
        var scalar = Assert.IsType<ScalarResult>(result);
        Assert.Equal("0", scalar.Value);
    }
}
