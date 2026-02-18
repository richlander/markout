using MarkdownTable.Query;
using MarkdownTable.Query.Operations;

namespace MarkdownTable.Tests;

public class TokenizerTests
{
    [Fact]
    public void Tokenize_SimpleKeyword_Count()
    {
        var tokens = Tokenizer.Tokenize("count");

        Assert.Equal(2, tokens.Count); // Count + End
        Assert.Equal(TokenKind.Count, tokens[0].Kind);
        Assert.Equal("count", tokens[0].Value);
        Assert.Equal(0, tokens[0].Position);
        Assert.Equal(TokenKind.End, tokens[1].Kind);
    }

    [Fact]
    public void Tokenize_DotIdentifier()
    {
        var tokens = Tokenizer.Tokenize(".Name");

        Assert.Equal(3, tokens.Count);
        Assert.Equal(TokenKind.Dot, tokens[0].Kind);
        Assert.Equal(TokenKind.Identifier, tokens[1].Kind);
        Assert.Equal("Name", tokens[1].Value);
    }

    [Fact]
    public void Tokenize_DotQuotedString()
    {
        var tokens = Tokenizer.Tokenize(".\"Column Name\"");

        Assert.Equal(3, tokens.Count);
        Assert.Equal(TokenKind.Dot, tokens[0].Kind);
        Assert.Equal(TokenKind.QuotedString, tokens[1].Kind);
        Assert.Equal("Column Name", tokens[1].Value);
    }

    [Fact]
    public void Tokenize_StringLiteral()
    {
        var tokens = Tokenizer.Tokenize("\"hello\"");

        Assert.Equal(2, tokens.Count);
        Assert.Equal(TokenKind.QuotedString, tokens[0].Kind);
        Assert.Equal("hello", tokens[0].Value);
    }

    [Theory]
    [InlineData("==", TokenKind.Equal)]
    [InlineData("!=", TokenKind.NotEqual)]
    [InlineData(">", TokenKind.GreaterThan)]
    [InlineData("<", TokenKind.LessThan)]
    [InlineData(">=", TokenKind.GreaterOrEqual)]
    [InlineData("<=", TokenKind.LessOrEqual)]
    public void Tokenize_Operators(string input, TokenKind expected)
    {
        var tokens = Tokenizer.Tokenize(input);

        Assert.Equal(expected, tokens[0].Kind);
        Assert.Equal(input, tokens[0].Value);
    }

    [Fact]
    public void Tokenize_Number()
    {
        var tokens = Tokenizer.Tokenize("42");

        Assert.Equal(2, tokens.Count);
        Assert.Equal(TokenKind.Number, tokens[0].Kind);
        Assert.Equal("42", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_Pipe()
    {
        var tokens = Tokenizer.Tokenize("|");

        Assert.Equal(2, tokens.Count);
        Assert.Equal(TokenKind.Pipe, tokens[0].Kind);
    }

    [Fact]
    public void Tokenize_ArrayAccess()
    {
        var tokens = Tokenizer.Tokenize(".[0]");

        Assert.Equal(5, tokens.Count);
        Assert.Equal(TokenKind.Dot, tokens[0].Kind);
        Assert.Equal(TokenKind.OpenBracket, tokens[1].Kind);
        Assert.Equal(TokenKind.Number, tokens[2].Kind);
        Assert.Equal("0", tokens[2].Value);
        Assert.Equal(TokenKind.CloseBracket, tokens[3].Kind);
    }

    [Fact]
    public void Tokenize_NegativeIndex()
    {
        var tokens = Tokenizer.Tokenize(".[-1]");

        Assert.Equal(5, tokens.Count);
        Assert.Equal(TokenKind.Dot, tokens[0].Kind);
        Assert.Equal(TokenKind.OpenBracket, tokens[1].Kind);
        Assert.Equal(TokenKind.Number, tokens[2].Kind);
        Assert.Equal("-1", tokens[2].Value);
        Assert.Equal(TokenKind.CloseBracket, tokens[3].Kind);
    }

    [Fact]
    public void Tokenize_Slice()
    {
        var tokens = Tokenizer.Tokenize(".[0:3]");

        Assert.Equal(7, tokens.Count);
        Assert.Equal(TokenKind.Dot, tokens[0].Kind);
        Assert.Equal(TokenKind.OpenBracket, tokens[1].Kind);
        Assert.Equal(TokenKind.Number, tokens[2].Kind);
        Assert.Equal("0", tokens[2].Value);
        Assert.Equal(TokenKind.Colon, tokens[3].Kind);
        Assert.Equal(TokenKind.Number, tokens[4].Kind);
        Assert.Equal("3", tokens[4].Value);
        Assert.Equal(TokenKind.CloseBracket, tokens[5].Kind);
    }

    [Fact]
    public void Tokenize_MultipleTokens_WhereQuery()
    {
        var tokens = Tokenizer.Tokenize("where .Age == \"30\"");

        Assert.Equal(6, tokens.Count);
        Assert.Equal(TokenKind.Where, tokens[0].Kind);
        Assert.Equal(TokenKind.Dot, tokens[1].Kind);
        Assert.Equal(TokenKind.Identifier, tokens[2].Kind);
        Assert.Equal("Age", tokens[2].Value);
        Assert.Equal(TokenKind.Equal, tokens[3].Kind);
        Assert.Equal(TokenKind.QuotedString, tokens[4].Kind);
        Assert.Equal("30", tokens[4].Value);
        Assert.Equal(TokenKind.End, tokens[5].Kind);
    }

    [Fact]
    public void Tokenize_EmptyInput_ReturnsOnlyEndToken()
    {
        var tokens = Tokenizer.Tokenize("");

        Assert.Single(tokens);
        Assert.Equal(TokenKind.End, tokens[0].Kind);
    }

    [Theory]
    [InlineData("select", TokenKind.Select)]
    [InlineData("where", TokenKind.Where)]
    [InlineData("orderby", TokenKind.OrderBy)]
    [InlineData("take", TokenKind.Take)]
    [InlineData("skip", TokenKind.Skip)]
    [InlineData("first", TokenKind.First)]
    [InlineData("last", TokenKind.Last)]
    [InlineData("count", TokenKind.Count)]
    [InlineData("distinct", TokenKind.Distinct)]
    [InlineData("asc", TokenKind.Asc)]
    [InlineData("desc", TokenKind.Desc)]
    [InlineData("and", TokenKind.And)]
    [InlineData("or", TokenKind.Or)]
    public void Tokenize_AllKeywords(string keyword, TokenKind expected)
    {
        var tokens = Tokenizer.Tokenize(keyword);

        Assert.Equal(expected, tokens[0].Kind);
        Assert.Equal(keyword, tokens[0].Value);
    }

    [Fact]
    public void Tokenize_PositionTracking()
    {
        var tokens = Tokenizer.Tokenize("where .X == \"y\"");

        Assert.Equal(0, tokens[0].Position);  // where
        Assert.Equal(6, tokens[1].Position);  // .
        Assert.Equal(7, tokens[2].Position);  // X
        Assert.Equal(9, tokens[3].Position);  // ==
        Assert.Equal(12, tokens[4].Position); // "y"
    }

    [Fact]
    public void Tokenize_Comma()
    {
        var tokens = Tokenizer.Tokenize(",");

        Assert.Equal(TokenKind.Comma, tokens[0].Kind);
    }

    [Fact]
    public void Tokenize_Colon()
    {
        var tokens = Tokenizer.Tokenize(":");

        Assert.Equal(TokenKind.Colon, tokens[0].Kind);
    }

    [Fact]
    public void Tokenize_UnexpectedCharacter_Throws()
    {
        var ex = Assert.Throws<QueryParseException>(() => Tokenizer.Tokenize("@"));
        Assert.Equal(0, ex.Position);
    }

    [Fact]
    public void Tokenize_SingleEquals_Throws()
    {
        Assert.Throws<QueryParseException>(() => Tokenizer.Tokenize("="));
    }

    [Fact]
    public void Tokenize_UnterminatedString_Throws()
    {
        Assert.Throws<QueryParseException>(() => Tokenizer.Tokenize("\"hello"));
    }

    [Fact]
    public void Tokenize_KeywordsAreCaseInsensitive()
    {
        var tokens = Tokenizer.Tokenize("WHERE");

        Assert.Equal(TokenKind.Where, tokens[0].Kind);
    }

    [Fact]
    public void Tokenize_IdentifierWithUnderscore()
    {
        var tokens = Tokenizer.Tokenize("my_column");

        Assert.Equal(TokenKind.Identifier, tokens[0].Kind);
        Assert.Equal("my_column", tokens[0].Value);
    }
}

public class QueryParserTests
{
    [Fact]
    public void Parse_Count_ReturnsCountOperation()
    {
        var result = QueryParser.Parse("count");

        Assert.Null(result.SectionName);
        var op = Assert.Single(result.Operations);
        Assert.IsType<CountOperation>(op);
    }

    [Fact]
    public void Parse_Where_ReturnsWhereOperation()
    {
        var result = QueryParser.Parse("where .X == \"val\"");

        var op = Assert.Single(result.Operations);
        Assert.IsType<WhereOperation>(op);
    }

    [Fact]
    public void Parse_Select_TwoColumns()
    {
        var result = QueryParser.Parse("select .A, .B");

        var op = Assert.Single(result.Operations);
        var selectOp = Assert.IsType<SelectOperation>(op);

        // Verify it selects 2 columns by executing against sample data
        var headers = new[] { "A", "B", "C" };
        var rows = new List<string[]> { new[] { "1", "2", "3" } };
        var queryResult = Assert.IsType<TableResult>(selectOp.Execute(headers, rows));
        Assert.Equal(new[] { "A", "B" }, queryResult.Headers);
    }

    [Fact]
    public void Parse_OrderBy_Descending()
    {
        var result = QueryParser.Parse("orderby .X desc");

        var op = Assert.Single(result.Operations);
        var orderByOp = Assert.IsType<OrderByOperation>(op);

        // Verify descending by executing against sample data
        var headers = new[] { "X" };
        var rows = new List<string[]> { new[] { "1" }, new[] { "3" }, new[] { "2" } };
        var queryResult = Assert.IsType<TableResult>(orderByOp.Execute(headers, rows));
        Assert.Equal("3", queryResult.Rows[0][0]);
        Assert.Equal("2", queryResult.Rows[1][0]);
        Assert.Equal("1", queryResult.Rows[2][0]);
    }

    [Fact]
    public void Parse_OrderBy_Ascending_IsDefault()
    {
        var result = QueryParser.Parse("orderby .X");

        var op = Assert.Single(result.Operations);
        var orderByOp = Assert.IsType<OrderByOperation>(op);

        var headers = new[] { "X" };
        var rows = new List<string[]> { new[] { "3" }, new[] { "1" }, new[] { "2" } };
        var queryResult = Assert.IsType<TableResult>(orderByOp.Execute(headers, rows));
        Assert.Equal("1", queryResult.Rows[0][0]);
    }

    [Fact]
    public void Parse_Take_ReturnsCorrectCount()
    {
        var result = QueryParser.Parse("take 5");

        var op = Assert.Single(result.Operations);
        var takeOp = Assert.IsType<TakeOperation>(op);

        var headers = new[] { "X" };
        var rows = Enumerable.Range(0, 10).Select(i => new[] { i.ToString() }).ToList();
        var queryResult = Assert.IsType<TableResult>(takeOp.Execute(headers, rows));
        Assert.Equal(5, queryResult.Rows.Count);
    }

    [Fact]
    public void Parse_Skip_ReturnsCorrectCount()
    {
        var result = QueryParser.Parse("skip 3");

        var op = Assert.Single(result.Operations);
        var skipOp = Assert.IsType<SkipOperation>(op);

        var headers = new[] { "X" };
        var rows = Enumerable.Range(0, 10).Select(i => new[] { i.ToString() }).ToList();
        var queryResult = Assert.IsType<TableResult>(skipOp.Execute(headers, rows));
        Assert.Equal(7, queryResult.Rows.Count);
        Assert.Equal("3", queryResult.Rows[0][0]);
    }

    [Fact]
    public void Parse_First_ReturnsFirstOperation()
    {
        var result = QueryParser.Parse("first");

        var op = Assert.Single(result.Operations);
        Assert.IsType<FirstOperation>(op);
    }

    [Fact]
    public void Parse_Last_ReturnsLastOperation()
    {
        var result = QueryParser.Parse("last");

        var op = Assert.Single(result.Operations);
        Assert.IsType<LastOperation>(op);
    }

    [Fact]
    public void Parse_Distinct_ReturnsDistinctOperation()
    {
        var result = QueryParser.Parse("distinct");

        var op = Assert.Single(result.Operations);
        Assert.IsType<DistinctOperation>(op);
    }

    [Fact]
    public void Parse_IndexAccess_ReturnsIndexOperation()
    {
        var result = QueryParser.Parse(".[0]");

        var op = Assert.Single(result.Operations);
        var indexOp = Assert.IsType<IndexOperation>(op);

        var headers = new[] { "X" };
        var rows = new List<string[]> { new[] { "first" }, new[] { "second" } };
        var queryResult = Assert.IsType<TableResult>(indexOp.Execute(headers, rows));
        Assert.Single(queryResult.Rows);
        Assert.Equal("first", queryResult.Rows[0][0]);
    }

    [Fact]
    public void Parse_NegativeIndex_ReturnsIndexOperation()
    {
        var result = QueryParser.Parse(".[-1]");

        var op = Assert.Single(result.Operations);
        var indexOp = Assert.IsType<IndexOperation>(op);

        var headers = new[] { "X" };
        var rows = new List<string[]> { new[] { "first" }, new[] { "last" } };
        var queryResult = Assert.IsType<TableResult>(indexOp.Execute(headers, rows));
        Assert.Single(queryResult.Rows);
        Assert.Equal("last", queryResult.Rows[0][0]);
    }

    [Fact]
    public void Parse_Slice_ReturnsSliceOperation()
    {
        var result = QueryParser.Parse(".[0:3]");

        var op = Assert.Single(result.Operations);
        var sliceOp = Assert.IsType<SliceOperation>(op);

        var headers = new[] { "X" };
        var rows = Enumerable.Range(0, 5).Select(i => new[] { i.ToString() }).ToList();
        var queryResult = Assert.IsType<TableResult>(sliceOp.Execute(headers, rows));
        Assert.Equal(3, queryResult.Rows.Count);
        Assert.Equal("0", queryResult.Rows[0][0]);
        Assert.Equal("2", queryResult.Rows[2][0]);
    }

    [Fact]
    public void Parse_ColumnExtract_ReturnsColumnExtractOperation()
    {
        var result = QueryParser.Parse(".[].Col");

        var op = Assert.Single(result.Operations);
        var colOp = Assert.IsType<ColumnExtractOperation>(op);

        var headers = new[] { "Col", "Other" };
        var rows = new List<string[]> { new[] { "a", "b" }, new[] { "c", "d" } };
        var queryResult = Assert.IsType<TableResult>(colOp.Execute(headers, rows));
        Assert.Equal(new[] { "Col" }, queryResult.Headers);
        Assert.Equal("a", queryResult.Rows[0][0]);
        Assert.Equal("c", queryResult.Rows[1][0]);
    }

    [Fact]
    public void Parse_CellExtract_ReturnsCellExtractOperation()
    {
        var result = QueryParser.Parse(".[0].Col");

        var op = Assert.Single(result.Operations);
        var cellOp = Assert.IsType<CellExtractOperation>(op);

        var headers = new[] { "Col", "Other" };
        var rows = new List<string[]> { new[] { "val", "x" } };
        var queryResult = Assert.IsType<ScalarResult>(cellOp.Execute(headers, rows));
        Assert.Equal("val", queryResult.Value);
    }

    [Fact]
    public void Parse_PipedOperations_ReturnsTwoOperations()
    {
        var result = QueryParser.Parse("where .X == \"y\" | count");

        Assert.Equal(2, result.Operations.Count);
        Assert.IsType<WhereOperation>(result.Operations[0]);
        Assert.IsType<CountOperation>(result.Operations[1]);
    }

    [Fact]
    public void Parse_InvalidQuery_ThrowsQueryParseException()
    {
        Assert.Throws<QueryParseException>(() => QueryParser.Parse("badtoken"));
    }

    [Fact]
    public void Parse_SectionName_SetsSectionName()
    {
        var result = QueryParser.Parse(".Methods");

        Assert.Equal("Methods", result.SectionName);
    }

    [Fact]
    public void Parse_SectionWithPipe_SetsSectionAndOperations()
    {
        var result = QueryParser.Parse(".Methods | count");

        Assert.Equal("Methods", result.SectionName);
        var op = Assert.Single(result.Operations);
        Assert.IsType<CountOperation>(op);
    }

    [Fact]
    public void Parse_WhereWithDifferentOperators()
    {
        // All comparison operators should parse successfully
        var operators = new[] { "==", "!=", ">", "<", ">=", "<=" };
        foreach (var op in operators)
        {
            var result = QueryParser.Parse($"where .X {op} \"val\"");
            Assert.IsType<WhereOperation>(Assert.Single(result.Operations));
        }
    }

    [Fact]
    public void Parse_SelectWithQuotedColumnNames()
    {
        var result = QueryParser.Parse("select .\"First Name\", .\"Last Name\"");

        var op = Assert.Single(result.Operations);
        var selectOp = Assert.IsType<SelectOperation>(op);

        var headers = new[] { "First Name", "Last Name", "Age" };
        var rows = new List<string[]> { new[] { "John", "Doe", "30" } };
        var queryResult = Assert.IsType<TableResult>(selectOp.Execute(headers, rows));
        Assert.Equal(new[] { "First Name", "Last Name" }, queryResult.Headers);
    }

    [Fact]
    public void Parse_OrderByWithExplicitAsc()
    {
        var result = QueryParser.Parse("orderby .X asc");

        var op = Assert.Single(result.Operations);
        var orderByOp = Assert.IsType<OrderByOperation>(op);

        var headers = new[] { "X" };
        var rows = new List<string[]> { new[] { "3" }, new[] { "1" }, new[] { "2" } };
        var queryResult = Assert.IsType<TableResult>(orderByOp.Execute(headers, rows));
        Assert.Equal("1", queryResult.Rows[0][0]);
    }

    [Fact]
    public void Parse_EmptyBrackets_ColumnExtract()
    {
        var result = QueryParser.Parse(".[].Name");

        var op = Assert.Single(result.Operations);
        Assert.IsType<ColumnExtractOperation>(op);
    }

    [Fact]
    public void Parse_MultiplePipedOperations()
    {
        var result = QueryParser.Parse("where .Age > \"18\" | orderby .Name | take 10");

        Assert.Equal(3, result.Operations.Count);
        Assert.IsType<WhereOperation>(result.Operations[0]);
        Assert.IsType<OrderByOperation>(result.Operations[1]);
        Assert.IsType<TakeOperation>(result.Operations[2]);
    }

    [Fact]
    public void Parse_EmptyInput_ReturnsEmptyOperations()
    {
        var result = QueryParser.Parse("");

        Assert.Null(result.SectionName);
        Assert.Empty(result.Operations);
    }
}
