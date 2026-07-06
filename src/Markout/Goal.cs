namespace Markout;

/// <summary>
/// The optimization goal for a numeric metric: which direction of movement is "good". A goal lets
/// Markout derive a structural <see cref="Markout.Direction"/> and a polarity <see cref="GateStatus"/>
/// from a <c>Before → After</c> change, replacing hand-coded ceiling/floor/drift helpers in callers.
/// </summary>
/// <remarks>
/// v1 ships the three unambiguous goals below. A numeric-target goal (reach/approach a specific value)
/// is deferred pending its own design (exact vs distance vs ceiling/floor).
/// </remarks>
public enum Goal
{
    /// <summary>Informational: the metric has no good/bad polarity; movement is drift.</summary>
    Context,

    /// <summary>Higher is better (e.g. <c>Fully raised</c>): an increase is <see cref="GateStatus.Good"/>.</summary>
    Higher,

    /// <summary>Lower is better (e.g. <c>Pass bugs</c>): a decrease is <see cref="GateStatus.Good"/>.</summary>
    Lower
}
