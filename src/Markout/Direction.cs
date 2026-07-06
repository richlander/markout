namespace Markout;

/// <summary>
/// The <b>structural, goal-neutral</b> category of a numeric <c>Before → After</c> movement. It states
/// only <em>how</em> a value moved, never whether that is good or bad — the good/bad polarity is the
/// separate <see cref="GateStatus"/> axis, derived by applying a <see cref="Goal"/> to this direction.
/// Structured output carries both a <c>direction</c> (this) and a <c>status</c> (polarity) field.
/// </summary>
public enum Direction
{
    /// <summary>No net movement (within the declared noise tolerance).</summary>
    Unchanged,

    /// <summary>The value rose (both sides non-zero).</summary>
    Increased,

    /// <summary>The value fell (both sides non-zero).</summary>
    Decreased,

    /// <summary>The value appeared: <c>0 → N</c>. Rendered as "New" by some cards.</summary>
    Introduced,

    /// <summary>The value cleared: <c>N → 0</c>.</summary>
    Resolved
}

/// <summary>Shared slug text for <see cref="Direction"/> values (the stable structured token).</summary>
internal static class DirectionText
{
    public static string Slug(Direction direction) => direction switch
    {
        Direction.Increased => "increased",
        Direction.Decreased => "decreased",
        Direction.Introduced => "introduced",
        Direction.Resolved => "resolved",
        _ => "unchanged"
    };
}

/// <summary>
/// Derives a structural <see cref="Direction"/> from a numeric change and the goal-applied
/// <see cref="GateStatus"/> polarity. Keeps the two axes separate so a <see cref="Goal.Context"/>
/// metric can report movement with a <see cref="GateStatus.Neutral"/> polarity, and a zero-crossing
/// can flip polarity by goal without changing its structural category.
/// </summary>
internal static class GoalDerivation
{
    /// <summary>
    /// Classifies the movement from <paramref name="before"/> to <paramref name="after"/>. A movement
    /// whose magnitude is within <paramref name="noise"/> (inclusive) is <see cref="Direction.Unchanged"/>.
    /// </summary>
    public static Direction Classify(double before, double after, double noise = 0)
    {
        var delta = after - before;
        if (Math.Abs(delta) <= noise)
            return Direction.Unchanged;
        if (before == 0)
            return Direction.Introduced;
        if (after == 0)
            return Direction.Resolved;
        return delta > 0 ? Direction.Increased : Direction.Decreased;
    }

    /// <summary>
    /// Applies <paramref name="goal"/> to a structural <paramref name="direction"/> to get the good/bad
    /// polarity. <see cref="Goal.Context"/> and <see cref="Direction.Unchanged"/> are always
    /// <see cref="GateStatus.Neutral"/>.
    /// </summary>
    public static GateStatus Polarity(Direction direction, Goal goal)
    {
        if (goal == Goal.Context || direction == Direction.Unchanged)
            return GateStatus.Neutral;

        var rose = direction is Direction.Increased or Direction.Introduced;
        return goal == Goal.Higher
            ? (rose ? GateStatus.Good : GateStatus.Bad)
            : (rose ? GateStatus.Bad : GateStatus.Good);
    }

    /// <summary>
    /// Convenience: classify a change and return both axes. Returns <c>false</c> when either side is not
    /// a numeric scalar (goal derivation does not apply), leaving both outputs at their neutral defaults.
    /// </summary>
    public static bool TryDerive(object? before, object? after, Goal goal, double noise, out Direction direction, out GateStatus status)
    {
        direction = Direction.Unchanged;
        status = GateStatus.Neutral;
        if (!CellText.TryScalarDouble(before, out var b) || !CellText.TryScalarDouble(after, out var a))
            return false;
        direction = Classify(b, a, noise);
        status = Polarity(direction, goal);
        return true;
    }
}
