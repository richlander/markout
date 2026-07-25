namespace Markout;

/// <summary>
/// Identifies where a glyph is being composed, so a <see cref="MarkoutWriterOptions.ComposeGlyph"/>
/// callback can treat a metric <em>label</em> goal indicator differently from a value-cell
/// <em>movement</em>/polarity indicator.
/// </summary>
public enum GlyphSlot
{
    /// <summary>A goal glyph appended to a metric label (which direction is good: <c>↑</c>/<c>↓</c>).</summary>
    GoalLabel,

    /// <summary>A polarity glyph on a value/change cell derived from a <see cref="GateStatus"/> (<c>✓</c>/<c>✗</c>).</summary>
    MovementCell,

    /// <summary>A glyph prefixed to a child row's first column marking it as nested under the previous row (<c>↳</c>).</summary>
    ChildRow,
}

/// <summary>
/// The input to a <see cref="MarkoutWriterOptions.ComposeGlyph"/> callback: the base cell/label
/// <see cref="Text"/> and the resolved <see cref="Glyph"/> (already looked up from
/// <see cref="MarkoutGlyphs"/>, and possibly empty), plus the <see cref="Goal"/> and
/// <see cref="Status"/> context. The callback returns the final string, so it can replace the glyph
/// with a word, integrate it into the text, or fall back to the default via <see cref="Combine"/>.
/// </summary>
/// <param name="Slot">Whether this is a label goal glyph or a value-cell movement glyph.</param>
/// <param name="Text">The base text the glyph decorates (the metric label or the rendered cell value).</param>
/// <param name="Glyph">The resolved glyph from <see cref="MarkoutGlyphs"/>; empty when the indicator has no glyph.</param>
/// <param name="Goal">The goal in effect (<see cref="Goal.Higher"/>/<see cref="Goal.Lower"/> for a label; the cell's goal otherwise).</param>
/// <param name="Status">The derived polarity for a movement cell; <see cref="GateStatus.Unknown"/> for a label glyph.</param>
public readonly record struct GlyphContext(GlyphSlot Slot, string Text, string Glyph, Goal Goal, GateStatus Status)
{
    /// <summary>
    /// The default composition: appends the glyph to the text with a single space, or returns the
    /// text unchanged when the glyph is empty. A custom composer can call this to keep the default
    /// spacing while deciding <em>when</em> to apply it.
    /// </summary>
    public string Combine() => Glyph.Length == 0 ? Text : Text + " " + Glyph;
}
