namespace Markout.Formatting;

/// <summary>
/// Marker interface for types that can serve as formatters in a <see cref="MarkoutWriter{TFormatter}"/>.
/// Formatters declare capabilities by implementing individual interfaces
/// (<see cref="IHeadingFormatter"/>, <see cref="IFieldFormatter"/>, etc.).
/// The orchestrator checks capabilities at runtime but the generic constraint
/// ensures only valid formatter types are accepted.
/// </summary>
public interface IMarkoutFormatter { }
