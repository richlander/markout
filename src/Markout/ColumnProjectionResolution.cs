namespace Markout;

/// <summary>
/// Result of resolving requested columns against a concrete table shape.
/// </summary>
public sealed class ColumnProjectionResolution
{
    private static readonly int[] EmptyMap = [];
    private static readonly string[] EmptyNames = [];

    private ColumnProjectionResolution(
        ColumnProjectionResolutionKind kind,
        int[] columnMap,
        string[] requestedColumns,
        string[] unmatchedColumns)
    {
        Kind = kind;
        ColumnMap = columnMap;
        RequestedColumns = requestedColumns;
        UnmatchedColumns = unmatchedColumns;
    }

    /// <summary>
    /// The resolution outcome.
    /// </summary>
    public ColumnProjectionResolutionKind Kind { get; }

    /// <summary>
    /// Maps projected column position to original column position when <see cref="Kind"/>
    /// is <see cref="ColumnProjectionResolutionKind.Matched"/>. Empty otherwise.
    /// </summary>
    public IReadOnlyList<int> ColumnMap { get; }

    /// <summary>
    /// The requested columns that were evaluated. Empty when no include projection was configured.
    /// </summary>
    public IReadOnlyList<string> RequestedColumns { get; }

    /// <summary>
    /// Requested columns that did not match the table shape.
    /// </summary>
    public IReadOnlyList<string> UnmatchedColumns { get; }

    /// <summary>
    /// A result representing an unconfigured projection.
    /// </summary>
    public static ColumnProjectionResolution NoProjection()
        => new(ColumnProjectionResolutionKind.NoProjection, EmptyMap, EmptyNames, EmptyNames);

    /// <summary>
    /// A result representing a projection with at least one matched column.
    /// </summary>
    public static ColumnProjectionResolution Matched(
        IReadOnlyList<int> columnMap,
        IReadOnlyList<string>? requestedColumns = null,
        IReadOnlyList<string>? unmatchedColumns = null)
        => new(
            ColumnProjectionResolutionKind.Matched,
            [.. columnMap],
            requestedColumns is null ? EmptyNames : [.. requestedColumns],
            unmatchedColumns is null ? EmptyNames : [.. unmatchedColumns]);

    /// <summary>
    /// A result representing a projection where no requested columns matched.
    /// </summary>
    public static ColumnProjectionResolution NoMatches(IReadOnlyList<string> requestedColumns)
        => new(ColumnProjectionResolutionKind.NoMatches, EmptyMap, [.. requestedColumns], [.. requestedColumns]);
}
