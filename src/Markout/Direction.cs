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
    /// Classifies the movement from <paramref name="before"/> to <paramref name="after"/> (both assumed
    /// finite). A movement whose magnitude is within <paramref name="noise"/> (inclusive) is
    /// <see cref="Direction.Unchanged"/>. The zero-crossing categories are restricted to
    /// <em>non-negative</em> counts — <c>0 → +N</c> is <see cref="Direction.Introduced"/> and
    /// <c>+N → 0</c> is <see cref="Direction.Resolved"/> — so a crossing that involves a negative value
    /// (e.g. <c>0 → -5</c>) falls through to sign-based <see cref="Direction.Increased"/>/<see cref="Direction.Decreased"/>,
    /// keeping <see cref="Polarity"/> algebraically correct.
    /// </summary>
    public static Direction Classify(double before, double after, double noise = 0)
    {
        // A NaN or negative tolerance is meaningless; treat it as exact (0).
        if (!(noise >= 0))
            noise = 0;

        var delta = after - before;
        if (Math.Abs(delta) <= noise)
            return Direction.Unchanged;
        if (before == 0 && after > 0)
            return Direction.Introduced;
        if (after == 0 && before > 0)
            return Direction.Resolved;
        return delta > 0 ? Direction.Increased : Direction.Decreased;
    }

    /// <summary>
    /// Applies <paramref name="goal"/> to a structural <paramref name="direction"/> to get the good/bad
    /// polarity. <see cref="Goal.Context"/> and <see cref="Direction.Unchanged"/> are always
    /// <see cref="GateStatus.Neutral"/>. Relies on <see cref="Classify"/> restricting
    /// <see cref="Direction.Introduced"/> to rises and <see cref="Direction.Resolved"/> to falls.
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
    /// Derives both axes from finite scalar magnitudes. Returns <c>false</c> when either side is not a
    /// finite number (NaN/Infinity), so callers omit <c>direction</c>/<c>status</c> — matching the
    /// <c>—</c> placeholder a non-finite value renders.
    /// </summary>
    public static bool TryDerive(double before, double after, Goal goal, double noise, out Direction direction, out GateStatus status)
    {
        direction = Direction.Unchanged;
        status = GateStatus.Neutral;
        if (!double.IsFinite(before) || !double.IsFinite(after))
            return false;
        direction = Classify(before, after, noise);
        status = Polarity(direction, goal);
        return true;
    }

    /// <summary>
    /// Convenience: derive both axes from boxed scalars. Returns <c>false</c> when either side is not a
    /// (finite) numeric scalar, leaving both outputs at their neutral defaults.
    /// </summary>
    public static bool TryDerive(object? before, object? after, Goal goal, double noise, out Direction direction, out GateStatus status)
    {
        direction = Direction.Unchanged;
        status = GateStatus.Neutral;
        if (!CellText.TryScalarDouble(before, out var b) || !CellText.TryScalarDouble(after, out var a))
            return false;

        // Exact path: classify large long/ulong/decimal from their exact decimal delta so adjacent
        // values beyond double's 2^53 range aren't collapsed to Unchanged. Applies at any tolerance
        // (the noise band is checked against the exact delta). Zero is exactly representable, so the
        // b == 0 / a == 0 zero-crossing checks stay valid in double.
        if (CellText.TryExactDelta(before, after, out var exact))
        {
            var tolerance = noise >= 0 ? noise : 0;   // NaN/negative -> exact, matching Classify
            direction = Math.Abs((double)exact) <= tolerance
                ? Direction.Unchanged
                : ClassifyFromSign(Math.Sign(exact), b == 0, a == 0);
            status = Polarity(direction, goal);
            return true;
        }

        return TryDerive(b, a, goal, noise, out direction, out status);
    }

    /// <summary>
    /// Classifies from a precomputed sign of <c>after − before</c> and the zero-crossing flags, matching
    /// <see cref="Classify"/> but without re-deriving the sign from a lossy <see cref="double"/> delta.
    /// </summary>
    private static Direction ClassifyFromSign(int sign, bool beforeZero, bool afterZero)
    {
        if (sign == 0)
            return Direction.Unchanged;
        if (beforeZero && sign > 0)
            return Direction.Introduced;
        if (afterZero && sign < 0)
            return Direction.Resolved;
        return sign > 0 ? Direction.Increased : Direction.Decreased;
    }
}
