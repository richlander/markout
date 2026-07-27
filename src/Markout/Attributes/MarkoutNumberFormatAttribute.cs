namespace Markout;

/// <summary>
/// Declares a .NET numeric format string applied to the numbers Markout renders for a numeric
/// <see cref="Change{V}"/> cell in dense Markdown: the scalar <c>before</c>/<c>after</c> operands and
/// the derived signed delta (the <see cref="MarkoutDeltaAttribute"/> absolute suffix and the
/// <see cref="MarkoutDeltaNounAttribute"/> / <see cref="IDeltaCountable"/> delta count). It keeps a
/// grouped cell and its folded delta consistent — <c>165 → 1,168 (+1,003)</c> instead of
/// <c>165 → 1168 (+1,003)</c>. Composite operands keep their own shape formatting; percentage and
/// multiple deltas are unaffected. Markdown-only: structured (TSV/JSONL) output stays raw and
/// ungrouped so it remains machine-parseable.
/// </summary>
/// <remarks>
/// Markout owns the delta sign — it prepends <c>+</c> for a gain and <c>-</c> for a loss — so the
/// format applies to the delta <em>magnitude</em> only and must not include its own sign sections
/// (e.g. <c>"+0;-0;0"</c>) or sign literals, which would double the sign. The format string must be
/// one that the widened delta type (<see cref="double"/> for most integral operands,
/// <see cref="decimal"/> for <see cref="long"/>/<see cref="ulong"/>/<see cref="decimal"/>) can
/// render — the standard numeric formats (<c>"N0"</c>, <c>"F1"</c>, <c>"G"</c>, <c>"P"</c>,
/// <c>"E"</c>, <c>"C"</c>) and custom numeric patterns (<c>"#,0"</c>). Integer-only specifiers
/// (<c>"D"</c>, <c>"X"</c>, <c>"B"</c>) are not supported because the derived delta is not an integral
/// type, and passing one throws <see cref="System.FormatException"/> at render time.
/// </remarks>
/// <param name="format">A standard or custom .NET numeric format string (e.g. <c>"N0"</c>, <c>"N2"</c>).</param>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MarkoutNumberFormatAttribute(string format) : Attribute
{
    /// <summary>The numeric format string applied to the change cell's numbers.</summary>
    public string Format { get; } = format;
}
