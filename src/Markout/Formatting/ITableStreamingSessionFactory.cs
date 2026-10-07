namespace Markout.Formatting;

// A formatter can offer a per-table streaming session only for modes whose
// output does not need to inspect later rows before writing the first one.
internal interface ITableStreamingSessionFactory
{
    IStreamingTableFormatter? CreateStreamingSession(MarkoutWriterOptions options);
}
