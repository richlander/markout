namespace Markout;

/// <summary>
/// The polarity of a gate/verdict outcome. This is the neutral, domain-independent category a
/// renderer uses to pick a badge/color; the exact domain word (<c>regression</c>, <c>BETTER</c>,
/// <c>drift</c>, …) is carried by <see cref="Verdict.Label"/>.
/// </summary>
/// <remarks>
/// The member set is provisional pending design sign-off (issue #128): it may become a curated
/// domain vocabulary or stay a small polarity scale plus caller label.
/// </remarks>
public enum GateStatus
{
    /// <summary>No/unknown outcome.</summary>
    Unknown,

    /// <summary>A positive outcome (improvement / pass).</summary>
    Good,

    /// <summary>A neutral outcome (unchanged).</summary>
    Neutral,

    /// <summary>A cautionary outcome (drift / warning).</summary>
    Warning,

    /// <summary>A negative outcome (regression / violation / failure).</summary>
    Bad
}

/// <summary>Shared slug text for <see cref="GateStatus"/> polarity values.</summary>
internal static class GateStatusText
{
    public static string Slug(GateStatus status) => status switch
    {
        GateStatus.Good => "good",
        GateStatus.Neutral => "neutral",
        GateStatus.Warning => "warning",
        GateStatus.Bad => "bad",
        _ => "unknown"
    };
}
