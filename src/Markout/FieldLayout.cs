namespace Markout;

/// <summary>
/// Specifies how scalar fields are rendered in the output.
/// </summary>
public enum FieldLayout
{
    /// <summary>
    /// Fields as a two-column pipe table (default).
    /// Example: | Property | Value |
    ///          | -------- | ----- |
    ///          | Birthplace | London |
    ///          | Born | 1980 |
    /// </summary>
    Table,

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
    Numbered,

    /// <summary>
    /// Fields as plain lines, one per line with no markers.
    /// Uses markdown hard line breaks (trailing double space).
    /// Example: Birthplace: London
    ///          Born: 1980
    /// </summary>
    Plain
}
