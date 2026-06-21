namespace Markout;

/// <summary>
/// Controls how field rows are ordered inside a field-style section.
/// </summary>
public enum MarkoutFieldOrder
{
    /// <summary>Preserve the model or collection input order.</summary>
    Input,

    /// <summary>Sort fields by display key using ordinal-insensitive comparison.</summary>
    Alphabetical
}
