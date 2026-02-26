namespace Markout;

/// <summary>
/// Represents a key-value field for Markout output.
/// </summary>
/// <param name="Key">The field name.</param>
/// <param name="Value">The field value.</param>
public readonly record struct MarkoutField(string Key, string Value);
