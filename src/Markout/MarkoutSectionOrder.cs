namespace Markout;

/// <summary>
/// Controls how sections without an explicit position are ordered.
/// </summary>
public enum MarkoutSectionOrder
{
    /// <summary>Preserve the order in which the document writes sections.</summary>
    Data,

    /// <summary>Sort sections by name using ordinal-insensitive comparison.</summary>
    Alphabetical
}
