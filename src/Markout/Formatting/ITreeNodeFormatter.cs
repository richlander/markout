namespace Markout.Formatting;

internal interface ITreeNodeFormatter
{
    void FormatTreeNode(
        TextWriter writer,
        string text,
        TreeNodeState state,
        string? badge,
        string prefix,
        bool isLastSibling,
        MarkoutWriterOptions options);
}
