using Markout.Formatting;

namespace Markout;

/// <summary>
/// Pushes one tree to a formatter without retaining a
/// <see cref="TreeNode"/> hierarchy.
/// </summary>
public sealed class StreamingTreeWriter
{
    private readonly Func<TextWriter> _getWriter;
    private readonly IStreamingTreeFormatter _formatter;
    private readonly MarkoutWriterOptions _options;
    private readonly Action _beginOutput;
    private TextWriter? _writer;
    private bool _started;
    private bool _completed;

    internal StreamingTreeWriter(
        Func<TextWriter> getWriter,
        IStreamingTreeFormatter formatter,
        MarkoutWriterOptions options,
        Action beginOutput)
    {
        _getWriter = getWriter;
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

    /// <summary>
    /// Writes one node and invokes a state-carrying child callback without
    /// requiring a capturing delegate.
    /// </summary>
    /// <typeparam name="TState">The caller state passed to the child callback.</typeparam>
    /// <param name="text">The node label.</param>
    /// <param name="isLastSibling">Whether this is the last node in its sibling population.</param>
    /// <param name="state">Caller state passed to <paramref name="writeChildren"/>.</param>
    /// <param name="writeChildren">
    /// A synchronous callback that writes this node's children before the
    /// method returns.
    /// </param>
    /// <param name="nodeState">The structural state of the node.</param>
    /// <param name="badge">An optional badge rendered before the label.</param>
    public void WriteNode<TState>(
        string text,
        bool isLastSibling,
        TState state,
        Action<StreamingTreeWriter, TState> writeChildren,
        TreeNodeState nodeState = TreeNodeState.Normal,
        string? badge = null)
    {
        ArgumentNullException.ThrowIfNull(writeChildren);
        WriteNodeHeader(
            text,
            nodeState,
            badge,
            isLastSibling);
        _formatter.BeginChildren();
        try
        {
            writeChildren(this, state);
        }
        finally
        {
            _formatter.EndChildren();
        }
    }

    internal void Complete()
    {
        if (_completed)
            return;

        _completed = true;
        if (_started)
            _formatter.EndTree(_writer!);
    }

    private void WriteNodeCore(
        string text,
        TreeNodeState state,
        string? badge,
        bool isLastSibling,
        Action<StreamingTreeWriter>? writeChildren)
    {
        WriteNodeHeader(
            text,
            state,
            badge,
            isLastSibling);

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

    private void WriteNodeHeader(
        string text,
        TreeNodeState state,
        string? badge,
        bool isLastSibling)
    {
        if (_completed)
        {
            throw new InvalidOperationException(
                "The streaming tree operation has completed.");
        }

        ArgumentNullException.ThrowIfNull(text);
        EnsureStarted();
        _formatter.WriteNode(
            _writer!,
            text,
            state,
            badge,
            isLastSibling);
        HasContent = true;
    }

    private void EnsureStarted()
    {
        if (_started)
            return;

        _beginOutput();
        _writer = _getWriter();
        _formatter.BeginTree(
            _writer,
            _options);
        _started = true;
    }
}
