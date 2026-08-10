using System.Globalization;

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
    /// Resolves the column projection against display headers.
    /// </summary>
    public ColumnProjectionResolution ResolveColumns(ReadOnlySpan<string> headers)
        => ResolveColumns(headers, default);

    /// <summary>
    /// Resolves the column projection against display headers and stable header names.
    /// Matches display headers, stable header names, and snake_case stable names.
    /// </summary>
    public ColumnProjectionResolution ResolveColumns(ReadOnlySpan<string> headers, ReadOnlySpan<string> headerNames)
        => _includeColumns is { } include
            ? ResolveColumns(headers, headerNames, SnapshotSelection(include))
            : ResolveColumns(headers, headerNames, null);

    /// <summary>
    /// Reads an allow list into an array, exactly once.
    /// </summary>
    /// <remarks>
    /// IncludeColumns is an interface the caller implements, so its Count and the items it yields
    /// are separate questions asked of a type that need not answer them consistently, or answer
    /// either the same way twice. Every consumer of a selection -- the matcher that decides which
    /// columns it selects, the writer that records whether it ever matched, and the message that
    /// names it if it did not -- has to be looking at the same names, or they can disagree about
    /// which request was even made. Reading it once, here, is what makes them agree; enumeration
    /// is the definitive read, because it is the one that yields the names.
    /// </remarks>
    internal static string[] SnapshotSelection(IReadOnlyList<string> requested)
        => SnapshotNames(requested, nameof(IncludeColumns));

    internal static string[] SnapshotNames(IEnumerable<string> requested, string paramName)
    {
        List<string> snapshot = [];
        var index = 0;
        foreach (var name in requested)
        {
            if (name is null)
                throw new ArgumentException(
                    $"{paramName} contains a null entry at index {index}.",
                    paramName);
            snapshot.Add(name);
            index++;
        }

        return [.. snapshot];
    }

    internal ColumnProjectionResolution ResolveColumns(
        ReadOnlySpan<string> headers,
        ReadOnlySpan<string> headerNames,
        string[]? includeColumns)
    {
        if (includeColumns != null)
        {
            // Include: output columns in the order specified by IncludeColumns
            var map = new List<int>(includeColumns.Length);
            var claimed = new HashSet<int>();
            var unmatched = new List<string>();
            foreach (var col in includeColumns)
            {
                bool matched = false;
                bool isGlob = col.Contains('*') || col.Contains('?');
                for (int i = 0; i < headers.Length; i++)
                {
                    if (MatchesColumn(col, headers, headerNames, i))
                    {
                        // A column answers to several names -- its display header, its stable name,
                        // and that name's snake_case form -- so an allow list naming two aliases of
                        // one column, or a glob overlapping an explicit name, would otherwise
                        // project that column twice. The duplicate is emitted downstream of
                        // MarkoutTable's construction-time key validation, so it reaches structured
                        // output as a repeated JSONL key from which a consumer recovers one value.
                        // The name still counts as matched -- it named a real column, so it is no
                        // typo -- but the column is emitted once, at its first requested position.
                        matched = true;
                        if (claimed.Add(i))
                            map.Add(i);
                        if (!isGlob) break;
                    }
                }

                if (!matched)
                    unmatched.Add(col);
            }

            if (map.Count == 0)
                return ColumnProjectionResolution.NoMatches(includeColumns);

            return ColumnProjectionResolution.Matched(map, includeColumns, unmatched);
        }

        if (_excludeColumns != null)
        {
            ValidateNames(_excludeColumns, nameof(ExcludeColumns));
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
            return map.Count < headers.Length
                ? ColumnProjectionResolution.Matched(map)
                : ColumnProjectionResolution.NoProjection();
        }

        return ColumnProjectionResolution.NoProjection();
    }

    /// <summary>
    /// Attempts to resolve the column projection against display headers.
    /// </summary>
    public bool TryResolveColumns(ReadOnlySpan<string> headers, out ColumnProjectionResolution resolution)
        => TryResolveColumns(headers, default, out resolution);

    /// <summary>
    /// Attempts to resolve the column projection against display headers and stable header names.
    /// </summary>
    public bool TryResolveColumns(ReadOnlySpan<string> headers, ReadOnlySpan<string> headerNames, out ColumnProjectionResolution resolution)
    {
        resolution = ResolveColumns(headers, headerNames);
        return resolution.Kind != ColumnProjectionResolutionKind.NoMatches;
    }

    private bool MatchesColumn(string pattern, ReadOnlySpan<string> headers, ReadOnlySpan<string> headerNames, int index)
    {
        if (MatchesName(pattern, headers[index]))
            return true;

        // Resolve the stable name the way TableWriter.FormatHeaders does: an explicit name that is
        // null or empty falls back to the display header. Returning early instead would refuse to
        // match a column by the very key structured output emits for it.
        var explicitName = index < headerNames.Length ? headerNames[index] : null;
        var stableName = string.IsNullOrEmpty(explicitName) ? headers[index] : explicitName;
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

        return GlobMatch(pattern, name, _comparison);
    }

    private static bool GlobMatch(string pattern, string text, StringComparison comparison)
    {
        var ordinal = comparison is StringComparison.Ordinal or StringComparison.OrdinalIgnoreCase;
        CompareInfo? compareInfo = null;
        var compareOptions = CompareOptions.None;
        if (!ordinal)
        {
            (compareInfo, compareOptions) = comparison switch
            {
                StringComparison.CurrentCulture =>
                    (CultureInfo.CurrentCulture.CompareInfo, CompareOptions.None),
                StringComparison.CurrentCultureIgnoreCase =>
                    (CultureInfo.CurrentCulture.CompareInfo, CompareOptions.IgnoreCase),
                StringComparison.InvariantCulture =>
                    (CultureInfo.InvariantCulture.CompareInfo, CompareOptions.None),
                StringComparison.InvariantCultureIgnoreCase =>
                    (CultureInfo.InvariantCulture.CompareInfo, CompareOptions.IgnoreCase),
                _ => throw new ArgumentException("The string comparison type is not supported.", nameof(comparison))
            };
        }

        var reachable = new bool[text.Length + 1];
        var next = new bool[text.Length + 1];
        reachable[0] = true;

        for (var patternIndex = 0; patternIndex < pattern.Length;)
        {
            Array.Clear(next);

            if (pattern[patternIndex] == '*')
            {
                while (patternIndex + 1 < pattern.Length && pattern[patternIndex + 1] == '*')
                    patternIndex++;

                var canReach = false;
                for (var textIndex = 0; textIndex <= text.Length; textIndex++)
                {
                    canReach |= reachable[textIndex];
                    next[textIndex] = canReach;
                }
                patternIndex++;
            }
            else if (pattern[patternIndex] == '?')
            {
                for (var textIndex = 0; textIndex < text.Length; textIndex++)
                    next[textIndex + 1] = reachable[textIndex];
                patternIndex++;
            }
            else
            {
                var literalEnd = patternIndex;
                while (literalEnd < pattern.Length && pattern[literalEnd] is not '*' and not '?')
                    literalEnd++;
                var literal = pattern.AsSpan(patternIndex, literalEnd - patternIndex);

                for (var textIndex = 0; textIndex <= text.Length; textIndex++)
                {
                    if (!reachable[textIndex])
                        continue;

                    if (ordinal)
                    {
                        var nextText = textIndex + literal.Length;
                        if (nextText <= text.Length &&
                            literal.Equals(text.AsSpan(textIndex, literal.Length), comparison))
                        {
                            next[nextText] = true;
                        }
                        continue;
                    }

                    if (compareInfo!.IsPrefix(
                        text.AsSpan(textIndex),
                        literal,
                        compareOptions,
                        out var matchLength))
                    {
                        var matchEnd = textIndex + matchLength;
                        while (!next[matchEnd])
                        {
                            next[matchEnd] = true;
                            if (matchEnd == text.Length)
                                break;

                            var ignorableLength = 1;
                            if (compareInfo.Compare(
                                text.AsSpan(matchEnd, ignorableLength),
                                ReadOnlySpan<char>.Empty,
                                compareOptions) != 0)
                            {
                                if (!char.IsHighSurrogate(text[matchEnd]) ||
                                    matchEnd + 1 >= text.Length ||
                                    !char.IsLowSurrogate(text[matchEnd + 1]) ||
                                    compareInfo.Compare(
                                        text.AsSpan(matchEnd, 2),
                                        ReadOnlySpan<char>.Empty,
                                        compareOptions) != 0)
                                {
                                    break;
                                }
                                ignorableLength = 2;
                            }

                            matchEnd += ignorableLength;
                        }
                    }
                }
                patternIndex = literalEnd;
            }

            (reachable, next) = (next, reachable);
            if (!reachable.Contains(true))
                return false;
        }

        return reachable[text.Length];
    }

    private static void ValidateNames(IEnumerable<string>? names, string paramName)
    {
        if (names is null)
            return;

        var index = 0;
        foreach (var name in names)
        {
            if (name is null)
                throw new ArgumentException(
                    $"{paramName} contains a null entry at index {index}.",
                    paramName);
            index++;
        }
    }
}
