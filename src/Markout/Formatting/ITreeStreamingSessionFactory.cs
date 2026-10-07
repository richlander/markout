namespace Markout.Formatting;

/// <summary>
/// Creates isolated formatter state for one streaming tree operation.
/// </summary>
/// <remarks>
/// Stateful streaming Tree formatters should implement this interface rather
/// than storing parent stacks, node ids, or similar state on a reusable
/// formatter instance.
/// </remarks>
public interface ITreeStreamingSessionFactory
{
    /// <summary>
    /// Creates a formatter session for one tree.
    /// </summary>
    /// <param name="options">Writer options for format-specific settings.</param>
    /// <returns>
    /// A fresh streaming formatter, or <c>null</c> when the requested options
    /// are unsupported.
    /// </returns>
    IStreamingTreeFormatter? CreateStreamingSession(
        MarkoutWriterOptions options);
}
