namespace Markout;

/// <summary>
/// Declares a caller noun rendered on the signed absolute delta of a numeric <see cref="Change{V}"/>
/// in dense Markdown — the sibling of <see cref="MarkoutUnitAttribute"/> (which suffixes the value).
/// <c>[MarkoutDeltaNoun("solved")]</c> turns <c>4/6 → 6/6</c> into <c>4/6 → 6/6 (+2 solved)</c>. The
/// caller owns the word; Markout renders it on the derived delta. Markdown-only; structured
/// (TSV/JSONL) output is unaffected. For composites, the count comes from <see cref="IDeltaCountable"/>
/// (<see cref="Fraction"/> → count, <see cref="Share"/> → value).
/// </summary>
/// <param name="noun">The noun rendered after the signed delta (e.g. <c>"solved"</c>, <c>"methods"</c>).</param>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MarkoutDeltaNounAttribute(string noun) : Attribute
{
    /// <summary>The noun rendered on the delta.</summary>
    public string Noun { get; } = noun;
}
