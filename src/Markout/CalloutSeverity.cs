namespace Markout;

/// <summary>
/// Severity level for callout/admonition blocks.
/// </summary>
public enum CalloutSeverity
{
    /// <summary>Informational note.</summary>
    Note,

    /// <summary>Helpful tip.</summary>
    Tip,

    /// <summary>Important information.</summary>
    Important,

    /// <summary>Warning about potential issues.</summary>
    Warning,

    /// <summary>Critical caution about dangerous actions or states.</summary>
    Caution
}
