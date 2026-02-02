namespace Markout;

/// <summary>
/// Represents a key-value field for Markout output.
/// </summary>
/// <param name="Key">The field name.</param>
/// <param name="Value">The field value.</param>
public readonly record struct MarkoutField(string Key, string? Value)
{
    /// <summary>
    /// Creates a field with a string value.
    /// </summary>
    public static MarkoutField Create(string key, string? value) => new(key, value);

    /// <summary>
    /// Creates a field with a boolean value (rendered as yes/no).
    /// </summary>
    public static MarkoutField Create(string key, bool value) => new(key, value ? "yes" : "no");

    /// <summary>
    /// Creates a field with an integer value.
    /// </summary>
    public static MarkoutField Create(string key, int value) => new(key, value.ToString());

    /// <summary>
    /// Creates a field with a long value.
    /// </summary>
    public static MarkoutField Create(string key, long value) => new(key, value.ToString());
}
