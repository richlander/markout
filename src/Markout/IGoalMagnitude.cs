namespace Markout;

/// <summary>
/// Implemented by composite cell shapes that expose a single comparable magnitude, so a
/// <see cref="Change{V}"/> over that shape can derive a goal <c>direction</c>/<c>status</c> the same
/// way a scalar <see cref="Change{V}"/> does. Shapes without one meaningful magnitude (e.g.
/// <see cref="Segments"/>) do not implement this and get no derived direction. A custom
/// <see cref="IMarkoutCell"/> can opt into goal derivation by implementing it.
/// </summary>
public interface IGoalMagnitude
{
    /// <summary>
    /// The single numeric magnitude used to classify goal direction (compared before vs after). Only
    /// the relative order/sign matters, so any monotonic magnitude (raw value or ratio) is valid.
    /// </summary>
    double GoalMagnitude { get; }
}
