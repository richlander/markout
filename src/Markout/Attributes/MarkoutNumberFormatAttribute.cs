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
/// <param name="format">A standard or custom .NET numeric format string (e.g. <c>"N0"</c>, <c>"N2"</c>).</param>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MarkoutNumberFormatAttribute(string format) : Attribute
{
    /// <summary>The numeric format string applied to the change cell's numbers.</summary>
    public string Format { get; } = format;
}
