namespace Markout;

/// <summary>
/// Describes the result of resolving a column projection against a table shape.
/// </summary>
public enum ColumnProjectionResolutionKind
{
    /// <summary>
    /// No column projection was configured.
    /// </summary>
    NoProjection,

    /// <summary>
    /// At least one requested column matched the table shape.
    /// </summary>
    Matched,

    /// <summary>
    /// A column projection was configured, but none of the requested columns matched.
    /// </summary>
    NoMatches
}
