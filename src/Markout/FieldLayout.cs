namespace Markout;

/// <summary>
/// Specifies how scalar fields are rendered in the output.
/// </summary>
public enum FieldLayout
{
    /// <summary>
    /// Each field on its own line (default).
    /// Example: Birthplace: London
    ///          Born: 1980
    /// </summary>
    Vertical,

    /// <summary>
    /// Fields on a single line, separated by pipes.
    /// Example: Birthplace: London | Born: 1980 | Citizenship: Canadian
    /// </summary>
    Inline,

    /// <summary>
    /// Fields as a bulleted list.
    /// Example: - Birthplace: London
    ///          - Born: 1980
    /// </summary>
    Bulleted,

    /// <summary>
    /// Fields as a numbered list.
    /// Example: 1. Birthplace: London
    ///          2. Born: 1980
    /// </summary>
    Numbered
}
