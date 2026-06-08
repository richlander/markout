namespace Markout;

/// <summary>
/// Defines a projection that trims markout output to specific sections, columns, and fields.
/// Projection is subtractive — it narrows from full output to what is requested.
/// </summary>
/// <remarks>
/// <para>Three granularities compose naturally: section narrows to a block,
/// column/field narrows within that block.</para>
/// <para>Include lists are ordered — column and field order follows the list order.
/// Exclude sets are unordered.</para>
/// <para>For each granularity, set either Include or Exclude, not both.</para>
/// </remarks>
public class MarkoutProjection
{
    private IReadOnlyList<string>? _includeColumns;
    private HashSet<string>? _excludeColumns;
    private IReadOnlyList<string>? _includeFields;
    private HashSet<string>? _excludeFields;
    private StringComparison _comparison = StringComparison.OrdinalIgnoreCase;

    // ── Factory Methods ──

    /// <summary>
    /// Creates a projection that includes only the specified table columns.
    /// </summary>
    public static MarkoutProjection WithColumns(params ReadOnlySpan<string> columns)
        => new() { IncludeColumns = columns.ToArray() };

    /// <summary>
    /// Creates a projection that excludes the specified table columns.
    /// </summary>
    public static MarkoutProjection WithoutColumns(params ReadOnlySpan<string> columns)
        => new() { ExcludeColumns = [..columns] };

    /// <summary>
    /// Creates a projection that includes only the specified fields.
    /// </summary>
    public static MarkoutProjection WithFields(params ReadOnlySpan<string> fields)
        => new() { IncludeFields = fields.ToArray() };

    /// <summary>
    /// Creates a projection that excludes the specified fields.
    /// </summary>
    public static MarkoutProjection WithoutFields(params ReadOnlySpan<string> fields)
        => new() { ExcludeFields = [..fields] };

    /// <summary>
    /// If set, only table columns whose header text matches are rendered, in the specified order.
    /// </summary>
    public IReadOnlyList<string>? IncludeColumns
    {
        get => _includeColumns;
        set
        {
            if (value != null && _excludeColumns != null)
                throw new InvalidOperationException("Cannot set IncludeColumns when ExcludeColumns is set.");
            _includeColumns = value;
        }
    }

    /// <summary>
    /// If set, table columns whose header text matches are excluded from output.
    /// </summary>
    public HashSet<string>? ExcludeColumns
    {
        get => _excludeColumns;
        set
        {
            if (value != null && _includeColumns != null)
                throw new InvalidOperationException("Cannot set ExcludeColumns when IncludeColumns is set.");
            _excludeColumns = value;
        }
    }

    /// <summary>
    /// If set, only scalar fields whose key matches are rendered, in the specified order.
    /// </summary>
    public IReadOnlyList<string>? IncludeFields
    {
        get => _includeFields;
        set
        {
            if (value != null && _excludeFields != null)
                throw new InvalidOperationException("Cannot set IncludeFields when ExcludeFields is set.");
            _includeFields = value;
        }
    }

    /// <summary>
    /// If set, scalar fields whose key matches are excluded from output.
    /// </summary>
    public HashSet<string>? ExcludeFields
    {
        get => _excludeFields;
        set
        {
            if (value != null && _includeFields != null)
                throw new InvalidOperationException("Cannot set ExcludeFields when IncludeFields is set.");
            _excludeFields = value;
        }
    }

    /// <summary>
    /// String comparison used for matching column headers and field keys.
    /// Default is <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </summary>
    public StringComparison Comparison
    {
        get => _comparison;
        set => _comparison = value;
    }

    /// <summary>
    /// Returns true if the given column header should be included in output.
    /// </summary>
    internal bool IsColumnIncluded(string header)
    {
        if (_includeColumns != null)
        {
            foreach (var col in _includeColumns)
            {
                if (MatchesName(col, header))
                    return true;
            }
            return false;
        }

        if (_excludeColumns != null)
        {
            foreach (var col in _excludeColumns)
            {
                if (MatchesName(col, header))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns true if the given field key should be included in output.
    /// </summary>
    internal bool IsFieldIncluded(string key)
    {
        if (_includeFields != null)
        {
            foreach (var field in _includeFields)
            {
                if (MatchesName(field, key))
                    return true;
            }
            return false;
        }

        if (_excludeFields != null)
        {
            foreach (var field in _excludeFields)
            {
                if (MatchesName(field, key))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Computes a column index map for projecting table columns.
    /// Returns null if no column projection is needed.
    /// Each entry maps projected position → original position.
    /// </summary>
    internal int[]? ComputeColumnMap(ReadOnlySpan<string> headers)
        => ComputeColumnMap(headers, default);

    /// <summary>
    /// Computes a column index map for projecting table columns.
    /// Matches display headers, stable header names, and snake_case stable names.
    /// </summary>
    internal int[]? ComputeColumnMap(ReadOnlySpan<string> headers, ReadOnlySpan<string> headerNames)
    {
        if (_includeColumns != null)
        {
            // Include: output columns in the order specified by IncludeColumns
            var map = new List<int>(_includeColumns.Count);
            foreach (var col in _includeColumns)
            {
                bool isGlob = col.Contains('*') || col.Contains('?');
                for (int i = 0; i < headers.Length; i++)
                {
                    if (MatchesColumn(col, headers, headerNames, i))
                    {
                        map.Add(i);
                        if (!isGlob) break;
                    }
                }
            }
            if (map.Count == 0)
                throw new InvalidOperationException(
                    $"No columns matched projection: {string.Join(", ", _includeColumns)}");

            return map.ToArray();
        }

        if (_excludeColumns != null)
        {
            // Exclude: preserve original order, skip excluded columns
            var map = new List<int>(headers.Length);
            for (int i = 0; i < headers.Length; i++)
            {
                bool excluded = false;
                foreach (var col in _excludeColumns)
                {
                    if (MatchesColumn(col, headers, headerNames, i))
                    {
                        excluded = true;
                        break;
                    }
                }
                if (!excluded)
                    map.Add(i);
            }
            return map.Count < headers.Length ? map.ToArray() : null;
        }

        return null;
    }

    private bool MatchesColumn(string pattern, ReadOnlySpan<string> headers, ReadOnlySpan<string> headerNames, int index)
    {
        if (MatchesName(pattern, headers[index]))
            return true;

        if (index >= headerNames.Length || string.IsNullOrEmpty(headerNames[index]))
            return false;

        var stableName = headerNames[index];
        return MatchesName(pattern, stableName)
            || MatchesName(pattern, Formatting.FormatHelper.ToSnakeCase(stableName));
    }

    /// <summary>
    /// Projects headers through a column map.
    /// </summary>
    internal static string[] ProjectHeaders(ReadOnlySpan<string> headers, int[] columnMap)
    {
        var result = new string[columnMap.Length];
        for (int i = 0; i < columnMap.Length; i++)
            result[i] = headers[columnMap[i]];
        return result;
    }

    /// <summary>
    /// Projects a row through a column map.
    /// </summary>
    internal static string[] ProjectRow(ReadOnlySpan<string> row, int[] columnMap)
    {
        var result = new string[columnMap.Length];
        for (int i = 0; i < columnMap.Length; i++)
        {
            int srcIndex = columnMap[i];
            result[i] = srcIndex < row.Length ? row[srcIndex] : string.Empty;
        }
        return result;
    }

    /// <summary>
    /// Matches a pattern against a name. Supports exact match and glob patterns (* and ?).
    /// </summary>
    internal bool MatchesName(string pattern, string name)
    {
        if (!pattern.Contains('*') && !pattern.Contains('?'))
            return string.Equals(pattern, name, _comparison);

        return GlobMatch(pattern, name, _comparison == StringComparison.OrdinalIgnoreCase);
    }

    private static bool GlobMatch(string pattern, string text, bool ignoreCase)
    {
        int pi = 0, ti = 0, starPi = -1, starTi = -1;
        while (ti < text.Length)
        {
            if (pi < pattern.Length && pattern[pi] == '?')
            {
                pi++; ti++;
            }
            else if (pi < pattern.Length && pattern[pi] == '*')
            {
                starPi = pi++; starTi = ti;
            }
            else if (pi < pattern.Length && CharEquals(pattern[pi], text[ti], ignoreCase))
            {
                pi++; ti++;
            }
            else if (starPi >= 0)
            {
                pi = starPi + 1; ti = ++starTi;
            }
            else
            {
                return false;
            }
        }
        while (pi < pattern.Length && pattern[pi] == '*') pi++;
        return pi == pattern.Length;
    }

    private static bool CharEquals(char a, char b, bool ignoreCase)
        => ignoreCase ? char.ToUpperInvariant(a) == char.ToUpperInvariant(b) : a == b;
}
