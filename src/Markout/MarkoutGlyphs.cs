namespace Markout;

/// <summary>
/// The configurable glyph set Markout uses to render goal and polarity indicators in rich document
/// sinks (Markdown, ANSI, Unicode — any formatter implementing <see cref="Formatting.IGlyphFormatter"/>).
/// A metric label carries a <em>goal</em> glyph (which direction is good: <c>↑</c>/<c>↓</c>), and a
/// derived <see cref="GateStatus"/> renders as a <em>polarity</em> glyph (<c>✓</c>/<c>✗</c>) rather
/// than the <c>good</c>/<c>bad</c> word.
/// </summary>
/// <remarks>
/// Glyphs are a presentation concern: decomposing sinks (TSV/JSONL) and plain text keep the stable
/// <c>direction</c>/<c>status</c> slug words instead, so machine-readable and non-Unicode output stay
/// meaningful. Set <see cref="MarkoutWriterOptions.Glyphs"/> to override any glyph; an empty string
/// suppresses that indicator.
/// </remarks>
public sealed record MarkoutGlyphs
{
    /// <summary>Goal glyph for <see cref="Goal.Higher"/> (higher is better). Default <c>↑</c>.</summary>
    public string GoalHigher { get; init; } = "\u2191";

    /// <summary>Goal glyph for <see cref="Goal.Lower"/> (lower is better). Default <c>↓</c>.</summary>
    public string GoalLower { get; init; } = "\u2193";

    /// <summary>Polarity glyph for <see cref="GateStatus.Good"/>. Default <c>✓</c>.</summary>
    public string StatusGood { get; init; } = "\u2713";

    /// <summary>Polarity glyph for <see cref="GateStatus.Bad"/>. Default <c>✗</c>.</summary>
    public string StatusBad { get; init; } = "\u2717";

    /// <summary>Polarity glyph for <see cref="GateStatus.Warning"/>. Default <c>⚠</c>.</summary>
    public string StatusWarning { get; init; } = "\u26a0";

    /// <summary>Polarity glyph for <see cref="GateStatus.Neutral"/> (unchanged). Default empty (no glyph).</summary>
    public string StatusNeutral { get; init; } = "";

    /// <summary>The default glyph set: <c>↑</c>/<c>↓</c> goals, <c>✓</c>/<c>✗</c> polarity.</summary>
    public static MarkoutGlyphs Default { get; } = new();

    /// <summary>The glyph for a goal, or an empty string for <see cref="Goal.Context"/> (no polarity).</summary>
    internal string ForGoal(Goal goal) => goal switch
    {
        Goal.Higher => GoalHigher,
        Goal.Lower => GoalLower,
        _ => ""
    };

    /// <summary>The glyph for a polarity, or an empty string for <see cref="GateStatus.Unknown"/>.</summary>
    internal string ForStatus(GateStatus status) => status switch
    {
        GateStatus.Good => StatusGood,
        GateStatus.Bad => StatusBad,
        GateStatus.Warning => StatusWarning,
        GateStatus.Neutral => StatusNeutral,
        _ => ""
    };
}
