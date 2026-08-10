namespace Markout;

internal static class TableHeaderValidator
{
    public static void Validate(IReadOnlyList<string> headers, IReadOnlyList<string>? headerNames)
    {
        for (int i = 0; i < headers.Count; i++)
            ValidateValue(headers[i], nameof(headers), "Header", i, allowEmptyFallback: false);

        if (headerNames is null)
            return;

        for (int i = 0; i < headerNames.Count; i++)
            ValidateValue(headerNames[i], nameof(headerNames), "Header name", i, allowEmptyFallback: true);
    }

    public static void Validate(ReadOnlySpan<string> headers, ReadOnlySpan<string> headerNames)
    {
        for (int i = 0; i < headers.Length; i++)
            ValidateValue(headers[i], "headers", "Header", i, allowEmptyFallback: false);

        for (int i = 0; i < headerNames.Length; i++)
            ValidateValue(headerNames[i], "headerNames", "Header name", i, allowEmptyFallback: true);
    }

    private static void ValidateValue(
        string? value,
        string paramName,
        string description,
        int index,
        bool allowEmptyFallback)
    {
        if (value is null)
        {
            if (allowEmptyFallback)
                return;
            throw new ArgumentException($"{description} {index} is null.", paramName);
        }

        if (allowEmptyFallback && value.Length == 0)
            return;

        if (value.AsSpan().IndexOfAnyInRange('\uD800', '\uDFFF') >= 0 &&
            !string.Equals(value, Utf8RoundTrip(value), StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"{description} {index} contains malformed UTF-16 and cannot round-trip through UTF-8.",
                paramName);
        }
    }

    private static string Utf8RoundTrip(string value)
        => System.Text.Encoding.UTF8.GetString(System.Text.Encoding.UTF8.GetBytes(value));
}
