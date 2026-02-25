namespace Markout;

/// <summary>
/// Specifies how scalar fields are rendered in the output.
/// </summary>
public enum FieldLayout
{
    /// <summary>
    /// Fields on a single line, separated by pipes.
    /// Example: Birthplace: London | Born: 1980 | Citizenship: Canadian
    /// </summary>
    Line,

    /// <summary>
    /// Fields as a bullet list.
    /// Example: - Birthplace: London
    ///          - Born: 1980
    /// </summary>
    List,

    /// <summary>
    /// Each field on its own line with trailing double-space for markdown line breaks.
    /// Example: Birthplace: London
    ///          Born: 1980
    /// </summary>
    BareList
}
