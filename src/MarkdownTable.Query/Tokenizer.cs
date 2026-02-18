namespace MarkdownTable.Query;

/// <summary>
/// Token types for the query language.
/// </summary>
public enum TokenKind
{
    // Literals and identifiers
    Dot,             // .
    Identifier,      // bare word (Name, CPU, etc.)
    QuotedString,    // "..."
    Number,          // 42, 3.14

    // Brackets and delimiters
    OpenBracket,     // [
    CloseBracket,    // ]
    Comma,           // ,
    Colon,           // :
    Pipe,            // |

    // Comparison operators
    Equal,           // ==
    NotEqual,        // !=
    GreaterThan,     // >
    LessThan,        // <
    GreaterOrEqual,  // >=
    LessOrEqual,     // <=

    // Keywords
    Select,
    Where,
    OrderBy,
    Take,
    Skip,
    First,
    Last,
    Count,
    Distinct,
    Asc,
    Desc,
    And,
    Or,

    // End
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
    public int Position { get; }

    public QueryParseException(string message, int position) : base(message)
    {
        Position = position;
    }
}
