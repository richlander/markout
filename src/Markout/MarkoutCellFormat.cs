namespace Markout;

/// <summary>
/// Render-time configuration for a composite cell, sourced from property attributes
/// (<see cref="MarkoutDeltaAttribute"/>, <see cref="MarkoutUnitAttribute"/>, <see cref="MarkoutGoalAttribute"/>).
/// Shapes are data-only; derivation/formatting options travel through this struct.
/// </summary>
/// <param name="Delta">The derived change mode for a numeric <see cref="Change{V}"/>.</param>
/// <param name="Unit">An optional unit suffix (e.g. <c>"s"</c>) for a <see cref="Share"/> value.</param>
public readonly record struct MarkoutCellFormat(Delta Delta = Delta.None, string? Unit = null)
{
    /// <summary>
    /// The optimization goal; when not <see cref="Markout.Goal.Context"/>, a numeric
    /// <see cref="Change{V}"/> derives a structural <c>direction</c> and a polarity <c>status</c>.
    /// Added as an <c>init</c> property (not a constructor parameter) so the shipped two-arg
    /// constructor stays binary-compatible.
    /// </summary>
    public Goal Goal { get; init; }

    /// <summary>
    /// The tolerance (inclusive) under which a change counts as <see cref="Direction.Unchanged"/>;
    /// defaults to <c>0</c> (exact).
    /// </summary>
    public double Noise { get; init; }

    /// <summary>
    /// An optional caller-supplied noun rendered on the signed absolute delta in dense Markdown
    /// (e.g. <c>[MarkoutDeltaNoun("solved")]</c> → <c>4 → 6 (+2 solved)</c>). Markdown-only; structured
    /// output is unaffected. Applies to scalar <see cref="Change{V}"/> and composites implementing
    /// <see cref="IDeltaCountable"/> (<see cref="Fraction"/> → count, <see cref="Share"/> → value).
    /// </summary>
    public string? DeltaNoun { get; init; }

    /// <summary>
    /// The active glyph set when the target sink renders glyphs (a formatter implementing
    /// <see cref="Formatting.IGlyphFormatter"/>); <c>null</c> keeps the <c>good</c>/<c>bad</c> status
    /// <em>word</em>. The writer injects this (with <see cref="Compose"/>) before formatting a
    /// composite cell so a numeric <see cref="Change{V}"/> emits a trailing polarity glyph.
    /// </summary>
    internal MarkoutGlyphs? Glyphs { get; init; }

    /// <summary>
    /// The optional glyph composer (from <see cref="MarkoutWriterOptions.ComposeGlyph"/>) paired with
    /// <see cref="Glyphs"/>; <c>null</c> uses the default append-with-space composition.
    /// </summary>
    internal Func<GlyphContext, string>? Compose { get; init; }
}
