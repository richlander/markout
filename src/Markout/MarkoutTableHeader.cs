namespace Markout;

/// <summary>
/// Metadata for a table header as it is about to be rendered.
/// </summary>
/// <param name="Name">Stable source name for the column, usually the property name.</param>
/// <param name="DisplayName">Human-facing display name for the column.</param>
/// <param name="Index">Zero-based column index after projection.</param>
public readonly record struct MarkoutTableHeader(string Name, string DisplayName, int Index)
{
    /// <summary>
    /// Canonical machine-facing key for the column.
    /// </summary>
    public string Key => Formatting.FormatHelper.ToSnakeCase(Name);
}
