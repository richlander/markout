namespace Markout;

/// <summary>The comparison a <see cref="MarkoutEmphasis"/> threshold applies to a scalar cell value.</summary>
public enum EmphasisComparison
{
    /// <summary>Emphasize when the value is greater than or equal to the cut.</summary>
    AtLeast,

    /// <summary>Emphasize when the value is less than or equal to the cut.</summary>
    AtMost,
}

/// <summary>
/// A declared, per-row rule that decides which scalar cells of a <see cref="MultiSourceRow"/> render
/// <em>emphasized</em> (bold in Markdown) — so "which numbers matter" is a property of the data rather
/// than hand-applied bolding, and stays correct when the numbers change. A threshold clears when the
/// cell value meets the <see cref="Comparison"/> against <see cref="Cut"/>; point the comparison at the
/// <em>bad</em> side to get an alarm (emphasize a cell only when it fails).
/// </summary>
/// <remarks>
/// Emphasis is a Markdown presentation concern: sinks that do not implement
/// <see cref="Formatting.IEmphasisFormatter"/> (plain text, TSV/JSONL) render the value unchanged, so
/// output stays meaningful without styling. It augments the value, never replaces it.
/// </remarks>
public sealed record MarkoutEmphasis
{
    /// <summary>The threshold comparison applied to a cell value.</summary>
    public EmphasisComparison Comparison { get; init; }

    /// <summary>The threshold value the cell is compared against.</summary>
    public double Cut { get; init; }

    /// <summary>Emphasize a cell when its value is <c>&gt;= <paramref name="cut"/></c>.</summary>
    public static MarkoutEmphasis AtLeast(double cut) => new() { Comparison = EmphasisComparison.AtLeast, Cut = cut };

    /// <summary>Emphasize a cell when its value is <c>&lt;= <paramref name="cut"/></c>.</summary>
    public static MarkoutEmphasis AtMost(double cut) => new() { Comparison = EmphasisComparison.AtMost, Cut = cut };

    /// <summary>Whether <paramref name="value"/> satisfies this rule (and should be emphasized).</summary>
    internal bool IsSatisfiedBy(double value) => Comparison switch
    {
        EmphasisComparison.AtLeast => value >= Cut,
        EmphasisComparison.AtMost => value <= Cut,
        _ => false,
    };
}
