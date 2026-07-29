namespace MarkdownTable.Query;

/// <summary>
/// Token types for the query language.
/// </summary>
public enum TokenKind
{
    // Literals and identifiers

    /// <summary>The <c>.</c> path separator.</summary>
    Dot,

    /// <summary>A bare word, such as a section or column name.</summary>
    Identifier,

    /// <summary>A double-quoted string literal.</summary>
    QuotedString,

    /// <summary>A numeric literal, integer or decimal.</summary>
    Number,

    // Brackets and delimiters

    /// <summary>The <c>[</c> opening bracket.</summary>
    OpenBracket,

    /// <summary>The <c>]</c> closing bracket.</summary>
    CloseBracket,

    /// <summary>The <c>,</c> separator.</summary>
    Comma,

    /// <summary>The <c>:</c> slice separator.</summary>
    Colon,

    /// <summary>The <c>|</c> pipeline separator.</summary>
    Pipe,

    // Comparison operators

    /// <summary>The <c>==</c> equality operator.</summary>
    Equal,

    /// <summary>The <c>!=</c> inequality operator.</summary>
    NotEqual,

    /// <summary>The <c>&gt;</c> operator.</summary>
    GreaterThan,

    /// <summary>The <c>&lt;</c> operator.</summary>
    LessThan,

    /// <summary>The <c>&gt;=</c> operator.</summary>
    GreaterOrEqual,

    /// <summary>The <c>&lt;=</c> operator.</summary>
    LessOrEqual,

    // Keywords

    /// <summary>The <c>select</c> keyword.</summary>
    Select,

    /// <summary>The <c>where</c> keyword.</summary>
    Where,

    /// <summary>The <c>orderby</c> keyword.</summary>
    OrderBy,

    /// <summary>The <c>take</c> keyword.</summary>
    Take,

    /// <summary>The <c>skip</c> keyword.</summary>
    Skip,

    /// <summary>The <c>first</c> keyword.</summary>
    First,

    /// <summary>The <c>last</c> keyword.</summary>
    Last,

    /// <summary>The <c>count</c> keyword.</summary>
    Count,

    /// <summary>The <c>distinct</c> keyword.</summary>
    Distinct,

    /// <summary>The <c>asc</c> keyword, for ascending sort order.</summary>
    Asc,

    /// <summary>The <c>desc</c> keyword, for descending sort order.</summary>
    Desc,

    /// <summary>The <c>and</c> keyword.</summary>
    And,

    /// <summary>The <c>or</c> keyword.</summary>
    Or,

    // End

    /// <summary>End of input.</summary>
    End
}

/// <summary>
/// A token produced by the query tokenizer.
/// </summary>
public readonly record struct Token(TokenKind Kind, string Value, int Position);

/// <summary>
/// Tokenizes a query string into a sequence of tokens.
/// </summary>
public static class Tokenizer
{
    private static readonly Dictionary<string, TokenKind> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["select"] = TokenKind.Select,
        ["where"] = TokenKind.Where,
        ["orderby"] = TokenKind.OrderBy,
        ["take"] = TokenKind.Take,
        ["skip"] = TokenKind.Skip,
        ["first"] = TokenKind.First,
        ["last"] = TokenKind.Last,
        ["count"] = TokenKind.Count,
        ["distinct"] = TokenKind.Distinct,
        ["asc"] = TokenKind.Asc,
        ["desc"] = TokenKind.Desc,
        ["and"] = TokenKind.And,
        ["or"] = TokenKind.Or,
    };

    /// <summary>
    /// Splits <paramref name="query"/> into tokens, ending with <see cref="TokenKind.End"/>.
    /// </summary>
    /// <param name="query">The query string to tokenize.</param>
    /// <returns>The tokens in source order.</returns>
    /// <exception cref="QueryParseException">The query contains an unrecognized character or an unterminated string.</exception>
    public static List<Token> Tokenize(string query)
    {
        var tokens = new List<Token>();
        int i = 0;

        while (i < query.Length)
        {
            // Skip whitespace
            if (char.IsWhiteSpace(query[i]))
            {
                i++;
                continue;
            }

            var pos = i;
            var ch = query[i];

            switch (ch)
            {
                case '.':
                    tokens.Add(new Token(TokenKind.Dot, ".", pos));
                    i++;
                    break;

                case '[':
                    tokens.Add(new Token(TokenKind.OpenBracket, "[", pos));
                    i++;
                    break;

                case ']':
                    tokens.Add(new Token(TokenKind.CloseBracket, "]", pos));
                    i++;
                    break;

                case ',':
                    tokens.Add(new Token(TokenKind.Comma, ",", pos));
                    i++;
                    break;

                case ':':
                    tokens.Add(new Token(TokenKind.Colon, ":", pos));
                    i++;
                    break;

                case '|':
                    tokens.Add(new Token(TokenKind.Pipe, "|", pos));
                    i++;
                    break;

                case '=':
                    if (i + 1 < query.Length && query[i + 1] == '=')
                    {
                        tokens.Add(new Token(TokenKind.Equal, "==", pos));
                        i += 2;
                    }
                    else
                    {
                        throw new QueryParseException($"Unexpected '=' at position {pos}. Did you mean '=='?", pos);
                    }
                    break;

                case '!':
                    if (i + 1 < query.Length && query[i + 1] == '=')
                    {
                        tokens.Add(new Token(TokenKind.NotEqual, "!=", pos));
                        i += 2;
                    }
                    else
                    {
                        throw new QueryParseException($"Unexpected '!' at position {pos}.", pos);
                    }
                    break;

                case '>':
                    if (i + 1 < query.Length && query[i + 1] == '=')
                    {
                        tokens.Add(new Token(TokenKind.GreaterOrEqual, ">=", pos));
                        i += 2;
                    }
                    else
                    {
                        tokens.Add(new Token(TokenKind.GreaterThan, ">", pos));
                        i++;
                    }
                    break;

                case '<':
                    if (i + 1 < query.Length && query[i + 1] == '=')
                    {
                        tokens.Add(new Token(TokenKind.LessOrEqual, "<=", pos));
                        i += 2;
                    }
                    else
                    {
                        tokens.Add(new Token(TokenKind.LessThan, "<", pos));
                        i++;
                    }
                    break;

                case '"':
                    tokens.Add(ReadQuotedString(query, ref i));
                    break;

                case '-' when i + 1 < query.Length && char.IsDigit(query[i + 1]):
                    tokens.Add(ReadNumber(query, ref i));
                    break;

                default:
                    if (char.IsDigit(ch))
                    {
                        tokens.Add(ReadNumber(query, ref i));
                    }
                    else if (char.IsLetter(ch) || ch == '_')
                    {
                        tokens.Add(ReadIdentifierOrKeyword(query, ref i));
                    }
                    else
                    {
                        throw new QueryParseException($"Unexpected character '{ch}' at position {pos}.", pos);
                    }
                    break;
            }
        }

        tokens.Add(new Token(TokenKind.End, "", query.Length));
        return tokens;
    }

    private static Token ReadQuotedString(string query, ref int i)
    {
        var pos = i;
        i++; // skip opening quote
        var start = i;

        while (i < query.Length && query[i] != '"')
        {
            if (query[i] == '\\' && i + 1 < query.Length)
                i++; // skip escaped char
            i++;
        }

        if (i >= query.Length)
            throw new QueryParseException($"Unterminated string starting at position {pos}.", pos);

        var value = query[start..i];
        i++; // skip closing quote
        return new Token(TokenKind.QuotedString, value, pos);
    }

    private static Token ReadNumber(string query, ref int i)
    {
        var pos = i;
        if (query[i] == '-')
            i++;

        while (i < query.Length && (char.IsDigit(query[i]) || query[i] == '.'))
            i++;

        return new Token(TokenKind.Number, query[pos..i], pos);
    }

    private static Token ReadIdentifierOrKeyword(string query, ref int i)
    {
        var pos = i;
        while (i < query.Length && (char.IsLetterOrDigit(query[i]) || query[i] == '_' || query[i] == '-'))
            i++;

        var word = query[pos..i];

        if (Keywords.TryGetValue(word, out var kind))
            return new Token(kind, word, pos);

        return new Token(TokenKind.Identifier, word, pos);
    }
}

/// <summary>
/// Exception thrown when a query string cannot be parsed.
/// </summary>
public class QueryParseException : Exception
{
    /// <summary>Zero-based character offset in the query where parsing failed.</summary>
    public int Position { get; }

    /// <summary>
    /// Creates the exception.
    /// </summary>
    /// <param name="message">Description of the parse failure.</param>
    /// <param name="position">Zero-based character offset in the query where parsing failed.</param>
    public QueryParseException(string message, int position) : base(message)
    {
        Position = position;
    }
}
