namespace Markout.Formatting;

// A formatter whose streaming tree lowering needs per-tree state can create a
// fresh session so sharing the formatter across writers does not mix trees.
internal interface ITreeStreamingSessionFactory
{
    IStreamingTreeFormatter? CreateStreamingSession(
        MarkoutWriterOptions options);
}
