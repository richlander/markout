using MarkdownTable.Query.Operations;

namespace MarkdownTable.Query;

/// <summary>
/// Parses a tokenized query into a sequence of table operations and
/// optional document-level navigation (section selection, array access).
/// </summary>
public class QueryParser
{
    private readonly List<Token> _tokens;
    private int _pos;

    private QueryParser(List<Token> tokens)
    {
        _tokens = tokens;
        _pos = 0;
    }

    /// <summary>
    /// Parses a query string into a <see cref="ParsedQuery"/>.
    /// </summary>
    public static ParsedQuery Parse(string query)
    {
        var tokens = Tokenizer.Tokenize(query);
        var parser = new QueryParser(tokens);
        return parser.ParseQuery();
    }

    private Token Peek() => _tokens[_pos];
    private Token Advance() => _tokens[_pos++];

    private Token Expect(TokenKind kind)
    {
        var token = Peek();
        if (token.Kind != kind)
            throw new QueryParseException($"Expected {kind} but got {token.Kind} ('{token.Value}') at position {token.Position}.", token.Position);
        return Advance();
    }

    private ParsedQuery ParseQuery()
    {
        var result = new ParsedQuery();

        // Check for leading dot-navigation: .SectionName or .[index] or .[].Column
        if (Peek().Kind == TokenKind.Dot)
        {
            ParseDotNavigation(result);
        }

        // Parse pipeline of operations separated by |
        while (Peek().Kind != TokenKind.End)
        {
            if (Peek().Kind == TokenKind.Pipe)
            {
                Advance(); // consume |

                // After pipe, could be another dot-navigation or keyword operation
                if (Peek().Kind == TokenKind.Dot)
                {
                    ParseDotNavigation(result);
                    continue;
                }
            }

            if (Peek().Kind == TokenKind.End)
                break;

            var op = ParseOperation();
            if (op is not null)
                result.Operations.Add(op);
        }

        return result;
    }

    private void ParseDotNavigation(ParsedQuery result)
    {
        Advance(); // consume .

        var token = Peek();

        if (token.Kind == TokenKind.OpenBracket)
        {
            // .[...] — array access
            ParseArrayAccess(result);
        }
        else if (token.Kind == TokenKind.Identifier || token.Kind == TokenKind.QuotedString)
        {
            // .Name or ."Quoted Name" — could be section name or column reference
            var name = Advance().Value;

            // Check if followed by array access: .Section[...]
            if (Peek().Kind == TokenKind.OpenBracket)
            {
                // This is a section reference with array access
                result.SectionName ??= name;
                ParseArrayAccess(result);
            }
            else if (Peek().Kind == TokenKind.Pipe || Peek().Kind == TokenKind.End)
            {
                // .Section — select the table from this section
                result.SectionName ??= name;
            }
            else if (Peek().Kind == TokenKind.Comma)
            {
                // .Col1, .Col2 — multi-column select
                var columns = new List<string> { name };
                while (Peek().Kind == TokenKind.Comma)
                {
                    Advance(); // consume ,
                    Expect(TokenKind.Dot);
                    var col = Peek();
                    if (col.Kind == TokenKind.Identifier || col.Kind == TokenKind.QuotedString)
                        columns.Add(Advance().Value);
                    else
                        throw new QueryParseException($"Expected column name after '.' at position {col.Position}.", col.Position);
                }
                result.Operations.Add(new SelectOperation(columns.ToArray()));
            }
            else
            {
                // Bare .Column — could be column extract from current table context
                result.Operations.Add(new ColumnExtractOperation(name));
            }
        }
    }

    private void ParseArrayAccess(ParsedQuery result)
    {
        Advance(); // consume [

        var token = Peek();

        if (token.Kind == TokenKind.CloseBracket)
        {
            // .[] — iterate all rows
            Advance(); // consume ]

            // Check for .Column after
            if (Peek().Kind == TokenKind.Dot)
            {
                Advance(); // consume .
                var col = Peek();
                if (col.Kind == TokenKind.Identifier || col.Kind == TokenKind.QuotedString)
                {
                    result.Operations.Add(new ColumnExtractOperation(Advance().Value));
                }
            }
        }
        else if (token.Kind == TokenKind.Number)
        {
            var index = int.Parse(Advance().Value);

            if (Peek().Kind == TokenKind.Colon)
            {
                // .[start:end] — slice
                Advance(); // consume :
                int? end = null;
                if (Peek().Kind == TokenKind.Number)
                    end = int.Parse(Advance().Value);
                Expect(TokenKind.CloseBracket);
                result.Operations.Add(new SliceOperation(index, end));
            }
            else
            {
                // .[N] — single row index
                Expect(TokenKind.CloseBracket);

                // Check for .Column after
                if (Peek().Kind == TokenKind.Dot)
                {
                    Advance(); // consume .
                    var col = Peek();
                    if (col.Kind == TokenKind.Identifier || col.Kind == TokenKind.QuotedString)
                    {
                        result.Operations.Add(new CellExtractOperation(index, Advance().Value));
                    }
                    else
                    {
                        result.Operations.Add(new IndexOperation(index));
                    }
                }
                else
                {
                    result.Operations.Add(new IndexOperation(index));
                }
            }
        }
        else if (token.Kind == TokenKind.Colon)
        {
            // .[:end] — slice from start
            Advance(); // consume :
            int? end = null;
            if (Peek().Kind == TokenKind.Number)
                end = int.Parse(Advance().Value);
            Expect(TokenKind.CloseBracket);
            result.Operations.Add(new SliceOperation(null, end));
        }
        else
        {
            throw new QueryParseException($"Expected number, ':', or ']' after '[' at position {token.Position}.", token.Position);
        }
    }

    private ITableOperation? ParseOperation()
    {
        var token = Peek();

        return token.Kind switch
        {
            TokenKind.Select => ParseSelect(),
            TokenKind.Where => ParseWhere(),
            TokenKind.OrderBy => ParseOrderBy(),
            TokenKind.Take => ParseTake(),
            TokenKind.Skip => ParseSkip(),
            TokenKind.First => ParseFirst(),
            TokenKind.Last => ParseLast(),
            TokenKind.Count => ParseCount(),
            TokenKind.Distinct => ParseDistinct(),
            _ => throw new QueryParseException($"Expected operation keyword but got '{token.Value}' at position {token.Position}.", token.Position),
        };
    }

    private SelectOperation ParseSelect()
    {
        Advance(); // consume 'select'
        var columns = new List<string>();

        // First column
        Expect(TokenKind.Dot);
        columns.Add(ReadColumnName());

        // Additional columns
        while (Peek().Kind == TokenKind.Comma)
        {
            Advance(); // consume ,
            Expect(TokenKind.Dot);
            columns.Add(ReadColumnName());
        }

        return new SelectOperation(columns.ToArray());
    }

    private WhereOperation ParseWhere()
    {
        Advance(); // consume 'where'
        Expect(TokenKind.Dot);
        var column = ReadColumnName();

        var op = Peek();
        if (!IsComparisonOperator(op.Kind))
            throw new QueryParseException($"Expected comparison operator at position {op.Position}.", op.Position);
        Advance();

        var value = ReadValue();

        return new WhereOperation(column, op.Kind, value);
    }

    private OrderByOperation ParseOrderBy()
    {
        Advance(); // consume 'orderby'
        Expect(TokenKind.Dot);
        var column = ReadColumnName();

        var descending = false;
        if (Peek().Kind == TokenKind.Desc)
        {
            Advance();
            descending = true;
        }
        else if (Peek().Kind == TokenKind.Asc)
        {
            Advance();
        }

        return new OrderByOperation(column, descending);
    }

    private TakeOperation ParseTake()
    {
        Advance(); // consume 'take'
        var count = int.Parse(Expect(TokenKind.Number).Value);
        return new TakeOperation(count);
    }

    private SkipOperation ParseSkip()
    {
        Advance(); // consume 'skip'
        var count = int.Parse(Expect(TokenKind.Number).Value);
        return new SkipOperation(count);
    }

    private FirstOperation ParseFirst()
    {
        Advance(); // consume 'first'
        return new FirstOperation();
    }

    private LastOperation ParseLast()
    {
        Advance(); // consume 'last'
        return new LastOperation();
    }

    private CountOperation ParseCount()
    {
        Advance(); // consume 'count'
        return new CountOperation();
    }

    private DistinctOperation ParseDistinct()
    {
        Advance(); // consume 'distinct'
        return new DistinctOperation();
    }

    private string ReadColumnName()
    {
        var token = Peek();
        if (token.Kind == TokenKind.Identifier || token.Kind == TokenKind.QuotedString)
            return Advance().Value;

        // Allow keywords to be used as column names
        if (IsKeyword(token.Kind))
            return Advance().Value;

        throw new QueryParseException($"Expected column name at position {token.Position}.", token.Position);
    }

    private string ReadValue()
    {
        var token = Peek();
        return token.Kind switch
        {
            TokenKind.QuotedString => Advance().Value,
            TokenKind.Number => Advance().Value,
            TokenKind.Identifier => Advance().Value,
            _ => throw new QueryParseException($"Expected value at position {token.Position}.", token.Position),
        };
    }

    private static bool IsComparisonOperator(TokenKind kind) => kind is
        TokenKind.Equal or TokenKind.NotEqual or
        TokenKind.GreaterThan or TokenKind.LessThan or
        TokenKind.GreaterOrEqual or TokenKind.LessOrEqual;

    private static bool IsKeyword(TokenKind kind) => kind is
        TokenKind.Select or TokenKind.Where or TokenKind.OrderBy or
        TokenKind.Take or TokenKind.Skip or TokenKind.First or
        TokenKind.Last or TokenKind.Count or TokenKind.Distinct or
        TokenKind.Asc or TokenKind.Desc;
}

/// <summary>
/// A parsed query consisting of optional section navigation and a pipeline of operations.
/// </summary>
public class ParsedQuery
{
    /// <summary>
    /// The section name to select a table from (e.g., "Methods" from .Methods).
    /// Null means use the default/first table.
    /// </summary>
    public string? SectionName { get; set; }

    /// <summary>
    /// The ordered list of operations to apply to the selected table.
    /// </summary>
    public List<ITableOperation> Operations { get; } = [];
}
