using Markout.Formatting;

namespace Markout;

/// <summary>
/// Pushes one tree to a formatter without retaining a
/// <see cref="TreeNode"/> hierarchy.
/// </summary>
public sealed class StreamingTreeWriter
{
    private readonly TextWriter _writer;
    private readonly IStreamingTreeFormatter _formatter;
    private readonly MarkoutWriterOptions _options;
    private readonly Action _beginOutput;
    private bool _started;
    private bool _completed;

    internal StreamingTreeWriter(
        TextWriter writer,
        IStreamingTreeFormatter formatter,
        MarkoutWriterOptions options,
        Action beginOutput)
    {
        _writer = writer;
        _formatter = formatter;
        _options = options;
        _beginOutput = beginOutput;
    }

    internal bool HasContent { get; private set; }

    /// <summary>
    /// Writes one node and, synchronously, its optional children.
    /// </summary>
    /// <param name="text">The node label.</param>
    /// <param name="isLastSibling">Whether this is the last node in its sibling population.</param>
    /// <param name="writeChildren">
    /// An optional callback that writes this node's children before the method
    /// returns.
    /// </param>
    /// <param name="state">The structural state of the node.</param>
    /// <param name="badge">An optional badge rendered before the label.</param>
    public void WriteNode(
        string text,
        bool isLastSibling,
        Action<StreamingTreeWriter>? writeChildren = null,
        TreeNodeState state = TreeNodeState.Normal,
        string? badge = null) =>
        WriteNodeCore(
            text,
            state,
            badge,
            isLastSibling,
            writeChildren);

    internal void Complete()
    {
        if (_completed)
            return;

        _completed = true;
        if (_started)
            _formatter.EndTree(_writer);
    }

    private void WriteNodeCore(
        string text,
        TreeNodeState state,
        string? badge,
        bool isLastSibling,
        Action<StreamingTreeWriter>? writeChildren)
    {
        if (_completed)
        {
            throw new InvalidOperationException(
                "The streaming tree operation has completed.");
        }

        ArgumentNullException.ThrowIfNull(text);
        EnsureStarted();
        _formatter.WriteNode(
            _writer,
            text,
            state,
            badge,
            isLastSibling);
        HasContent = true;

        if (writeChildren is null)
            return;

        _formatter.BeginChildren();
        try
        {
            writeChildren(this);
        }
        finally
        {
            _formatter.EndChildren();
        }
    }

    private void EnsureStarted()
    {
        if (_started)
            return;

        _beginOutput();
        _formatter.BeginTree(
            _writer,
            _options);
        _started = true;
    }
}
