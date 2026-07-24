namespace Markout.Formatting;

/// <summary>
/// Opt-in capability marker: a formatter that renders goal/polarity <em>glyphs</em> (from
/// <see cref="MarkoutGlyphs"/>) instead of the <c>direction</c>/<c>status</c> slug words. Implemented
/// by rich document sinks (Markdown, ANSI, Unicode). Plain text and decomposing sinks (TSV/JSONL)
/// deliberately do not implement it, so their output stays word-based and non-Unicode-safe.
/// </summary>
public interface IGlyphFormatter { }
