using System.Text;

namespace Markout.Formatting;

internal sealed class TextTreeStreamingSession(
    ITreeFormatter formatter,
    MarkoutWriterOptions options)
    : IStreamingTreeFormatter
{
    private readonly List<bool> _ancestorLastSibling = [];
    private bool? _currentNodeLastSibling;

    public void BeginTree(
        TextWriter writer,
        MarkoutWriterOptions writerOptions)
    {
    }

    public void WriteNode(
        TextWriter writer,
        string text,
        TreeNodeState state,
        string? badge,
        bool isLastSibling)
    {
        _currentNodeLastSibling = isLastSibling;
        if (formatter is ITreeNodeFormatter nodeFormatter)
        {
            nodeFormatter.FormatTreeNode(
                writer,
                text,
                state,
                badge,
                AncestorPrefix(),
                isLastSibling,
                options);
            return;
        }

        string label = MarkoutGlyphs.NodeStatePrefix(
            state,
            options,
            formatter is IGlyphFormatter);
        if (badge is not null && options.IncludeBadges)
            label += $"{badge} ";
        label += text;

        formatter.FormatTreeNode(
            writer,
            label,
            AncestorPrefix()
                + (isLastSibling
                    ? "└─ "
                    : "├─ "));
    }

    public void BeginChildren()
    {
        if (_currentNodeLastSibling is not bool isLastSibling)
        {
            throw new InvalidOperationException(
                "A parent node must be written before its children begin.");
        }

        _ancestorLastSibling.Add(isLastSibling);
        _currentNodeLastSibling = null;
    }

    public void EndChildren()
    {
        if (_ancestorLastSibling.Count == 0)
        {
            throw new InvalidOperationException(
                "No streaming tree child population is open.");
        }

        _ancestorLastSibling.RemoveAt(
            _ancestorLastSibling.Count - 1);
    }

    public void EndTree(TextWriter writer)
    {
        _ancestorLastSibling.Clear();
        _currentNodeLastSibling = null;
    }

    private string AncestorPrefix()
    {
        var prefix =
            new StringBuilder(
                _ancestorLastSibling.Count * 3);
        for (int i = 0; i < _ancestorLastSibling.Count; i++)
        {
            prefix.Append(
                _ancestorLastSibling[i]
                    ? "   "
                    : "│  ");
        }
        return prefix.ToString();
    }
}
