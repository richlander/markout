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

    /// <summary>Prefix glyph marking a <c>[MarkoutChild]</c> row as nested under the previous row. Default <c>↳</c>.</summary>
    public string Child { get; init; } = "\u21b3";

    /// <summary>
    /// Prefix glyph marking a <see cref="TreeNodeState.Revisit"/> node — one whose subtree is
    /// elided because it already appeared earlier in the lowering. Default <c>↩</c>.
    /// </summary>
    public string Revisit { get; init; } = "\u21a9";

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

    /// <summary>The glyph for a node state, or an empty string for <see cref="TreeNodeState.Normal"/>.</summary>
    internal string ForNodeState(TreeNodeState state) => state switch
    {
        TreeNodeState.Revisit => Revisit,
        _ => ""
    };

    /// <summary>
    /// The stable word a sink without glyph support uses for a node state. Parenthesised for the
    /// same reason the goal words are: it sits beside caller text and has to stay distinguishable
    /// from it.
    /// </summary>
    internal static string WordForNodeState(TreeNodeState state) => state switch
    {
        TreeNodeState.Revisit => "(revisit)",
        _ => ""
    };

    /// <summary>
    /// The prefix a tree sink writes before a node, including its trailing space, or an empty
    /// string when the node needs no marker.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="TreeNode.Badge"/> this is not gated by
    /// <see cref="MarkoutWriterOptions.IncludeBadges"/>. A state is information about the shape of
    /// the tree, not decoration: suppressing it would make an elided subtree indistinguishable
    /// from a node that genuinely has no children.
    /// </remarks>
    internal static string NodeStatePrefix(TreeNodeState state, MarkoutWriterOptions options, bool glyphs)
    {
        if (state == TreeNodeState.Normal)
            return "";
        var marker = glyphs ? options.Glyphs.ForNodeState(state) : WordForNodeState(state);
        return marker.Length == 0 ? "" : marker + " ";
    }
}
