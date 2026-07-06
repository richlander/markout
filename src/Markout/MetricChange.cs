namespace Markout;

/// <summary>
/// A gated scalar metric change: a <c>Before → After</c> value with an optional target/threshold
/// and a caller-supplied status. The ergonomic specialization of a multi-source row for the common
/// baseline-delta case; a collection renders as a <c>Metric | Change | Target | Status</c> table
/// and decomposes to flat typed fields (<c>before</c>, <c>after</c>, optional <c>target</c>/
/// <c>target_label</c>, <c>status</c>).
/// </summary>
/// <remarks>
/// Restricted to <c>struct</c> scalar values; the source generator further limits <typeparamref name="T"/>
/// to the renderable numeric scalars (composite shapes like <see cref="Segments"/> stay ordinary rows).
/// <see cref="Status"/> is caller-supplied — Markout never derives regression/drift from the values.
/// </remarks>
/// <param name="Name">The metric name (leading identity column).</param>
/// <param name="Before">The value before.</param>
/// <param name="After">The value after.</param>
/// <param name="Target">An optional target/threshold; <c>null</c> for an ungated metric.</param>
/// <param name="TargetLabel">An optional domain label for the target (e.g. <c>"allowed failures"</c>).</param>
/// <param name="Status">The caller-supplied outcome polarity.</param>
/// <param name="StatusLabel">An optional caller display word for the status (e.g. <c>"regression"</c>).</param>
public readonly record struct MetricChange<T>(
    string Name,
    T Before,
    T After,
    T? Target = null,
    string? TargetLabel = null,
    GateStatus Status = GateStatus.Unknown,
    string? StatusLabel = null)
    where T : struct
{
    /// <summary>
    /// The optimization goal. When not <see cref="Goal.Context"/> and <see cref="Status"/> is
    /// caller-unset, Markout derives a structural <c>direction</c> and a polarity <c>status</c> from
    /// <see cref="Before"/> → <see cref="After"/>. Set via object initializer to preserve the shipped
    /// constructor, e.g. <c>new MetricChange&lt;int&gt;("Failures", b, a) { Goal = Goal.Lower }</c>.
    /// </summary>
    public Goal Goal { get; init; } = Goal.Context;

    /// <summary>The tolerance (inclusive) under which a change is <see cref="Direction.Unchanged"/>; default exact.</summary>
    public double Noise { get; init; }
}
