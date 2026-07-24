namespace Markout;

/// <summary>
/// Marks a <see cref="bool"/> property on a table row type as the row's <em>child</em> flag: when it
/// is <c>true</c>, the row is a semantic child of the preceding row. Rich document sinks (Markdown,
/// ANSI, Unicode — any formatter implementing <see cref="Formatting.IGlyphFormatter"/>) prefix the
/// child row's first column with the configurable child glyph (default <c>↳</c>); the flag itself is
/// never rendered as its own column.
/// </summary>
/// <remarks>
/// This expresses a data <em>relationship</em>, not presentation: the nesting is described by the
/// model and the visual is a configurable glyph (see <see cref="MarkoutGlyphs.Child"/>). Only one
/// level of nesting is supported. The flag is omitted from decomposing sinks (TSV/JSONL) and plain
/// text in this version.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MarkoutChildAttribute : Attribute
{
}
