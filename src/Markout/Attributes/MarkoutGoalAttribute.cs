namespace Markout;

/// <summary>
/// Declares the optimization <see cref="Markout.Goal"/> for a numeric <see cref="Change{V}"/> property.
/// When the goal is not <see cref="Markout.Goal.Context"/>, structured output gains a derived
/// <c>direction</c> (structural) and <c>status</c> (goal-applied polarity) field. An optional
/// <paramref name="noise"/> tolerance treats sub-threshold movement as <see cref="Direction.Unchanged"/>.
/// Extends the same property-format mechanism as <see cref="MarkoutDeltaAttribute"/> and
/// <see cref="MarkoutUnitAttribute"/>.
/// </summary>
/// <param name="goal">The optimization goal (which direction of movement is good).</param>
/// <param name="noise">The tolerance (inclusive) under which a change is <see cref="Direction.Unchanged"/>.</param>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MarkoutGoalAttribute(Goal goal, double noise = 0) : Attribute
{
    /// <summary>The optimization goal.</summary>
    public Goal Goal { get; } = goal;

    /// <summary>The noise tolerance under which a change counts as unchanged.</summary>
    public double Noise { get; } = noise;
}
