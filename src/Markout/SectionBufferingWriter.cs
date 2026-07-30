using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Markout;

/// <summary>
/// A <see cref="TextWriter"/> that captures output into per-section buffers so the
/// writer can emit sections in a requested order rather than in call order.
///
/// <para>
/// This sits behind the writer seam deliberately. Reordering rendered text means
/// re-deriving where a section starts from the output — scanning for <c>## </c> in
/// Markdown — which only works for one format and re-implements knowledge the writer
/// already had. Here the section boundary is the boundary the writer itself declared,
/// so ordering applies to every format, including TSV and JSONL, whose output carries
/// no heading to scan for.
/// </para>
///
/// <para>
/// Buffering sections is not enough on its own, because the blank line between two
/// sections is not part of either one. Captured with the section that follows it, it
/// travels when that section moves — putting a leading blank line at the top of the
/// document and leaving no separation where the seam ended up. So the separator is
/// dropped at capture (<see cref="AtSectionBoundary"/>) and recomputed between
/// emitted sections, which is where a separator means anything.
/// </para>
///
/// <para>
/// Recomputed, not replayed. Recording which seams carried a separator and re-emitting
/// that same vector positionally is wrong whenever the sections differ in shape: with
/// <c>TableFormatter</c> a table is set off by a blank line and consecutive fields are
/// not, so moving a table changes which adjacencies need one. The writer decides by
/// asking two questions — did the block before me leave a blank line pending, and am I
/// a heading, which separates from anything at all — and this type records the answers
/// per chunk so the same two questions can be asked again of whatever pair of chunks
/// ends up adjacent.
/// </para>
/// </summary>
internal sealed class SectionBufferingWriter : TextWriter
{
    private readonly TextWriter _target;
    private readonly List<Chunk> _sections = [];
    private Chunk _preamble;
    private Chunk _current;
    private bool _emitted;

    public SectionBufferingWriter(TextWriter target)
    {
        _target = target;
        _preamble = new Chunk(null, target.NewLine);
        _current = _preamble;
    }

    /// <summary>
    /// One captured run of output — the preamble, or one section — together with the
    /// two facts the writer used to decide about separators around it: whether the last
    /// block in it left a blank line pending, and whether it opens with a heading, which
    /// separates itself from any content before it regardless of what that content was.
    /// </summary>
    private sealed class Chunk(string? name, string separatorNewLine)
    {
        public string? Name { get; } = name;

        public StringWriter Buffer { get; } = new();

        public StringBuilder Content => Buffer.GetStringBuilder();

        public bool OpensWithHeading { get; set; }

        public bool EndsNeedingBlankLine { get; set; }

        /// <summary>
        /// The newline in effect where this chunk's leading separator was written, so a
        /// target whose <see cref="TextWriter.NewLine"/> changes mid-document still gets
        /// the line ending it had at that point rather than the one in force at flush.
        /// </summary>
        public string SeparatorNewLine { get; set; } = separatorNewLine;
    }

    /// <inheritdoc/>
    public override Encoding Encoding => _target.Encoding;

    /// <inheritdoc/>
    public override IFormatProvider FormatProvider => _target.FormatProvider;

    /// <summary>
    /// Delegates to the target rather than snapshotting it, so a newline chosen after
    /// this writer was constructed still reaches the output.
    /// </summary>
    [AllowNull]
    public override string NewLine
    {
        get => _target.NewLine;
        set => _target.NewLine = value;
    }

    /// <summary>
    /// Whether the writer is positioned at a seam between two sections: inside a
    /// section that has not been written to yet. A blank line written here separates
    /// this section from the previous one rather than belonging to either.
    /// </summary>
    public bool AtSectionBoundary =>
        !ReferenceEquals(_current, _preamble) && _current.Content.Length == 0;

    /// <summary>
    /// Records that the section now open begins with a heading. A heading separates
    /// itself from any content before it, whatever that content was, so this survives
    /// reordering while <see cref="Chunk.EndsNeedingBlankLine"/> — a fact about the
    /// chunk before the seam — does not.
    /// </summary>
    public void NoteSectionOpensWithHeading()
    {
        if (AtSectionBoundary)
            _current.OpensWithHeading = true;
    }

    /// <summary>
    /// Starts a new section buffer. Content written before the first call stays in the
    /// preamble, which is always emitted first — a document title is not a section and
    /// must not be reordered behind one.
    /// </summary>
    /// <param name="name">The section name to order by.</param>
    /// <param name="endsNeedingBlankLine">
    /// Whether the chunk being closed left a blank line pending. Read from the writer
    /// here because nothing has been written since, so it is still the state the writer
    /// would consult at this seam.
    /// </param>
    public void BeginSection(string name, bool endsNeedingBlankLine)
    {
        ThrowIfEmitted();

        _current.EndsNeedingBlankLine = endsNeedingBlankLine;
        var chunk = new Chunk(name, _target.NewLine);
        _sections.Add(chunk);
        _current = chunk;
    }

    /// <summary>
    /// The buffer to write into, having first noted the newline in force at the start of
    /// a section. The separator before a section is written immediately ahead of its
    /// first content, so that is the newline it would have used — and the one to put
    /// back at flush, which may be reached long after the target's newline moved on.
    /// </summary>
    private StringWriter Prepare()
    {
        ThrowIfEmitted();

        if (AtSectionBoundary)
            _current.SeparatorNewLine = _target.NewLine;

        return _current.Buffer;
    }

    /// <inheritdoc/>
    public override void Write(char value) => Prepare().Write(value);

    /// <inheritdoc/>
    public override void Write(string? value) => Prepare().Write(value);

    /// <inheritdoc/>
    public override void Write(char[] buffer, int index, int count) => Prepare().Write(buffer, index, count);

    /// <inheritdoc/>
    public override void Write(ReadOnlySpan<char> buffer) => Prepare().Write(buffer);

    /// <summary>
    /// Every newline this writer emits is read from the target when it is written, not
    /// snapshotted at construction. <see cref="TextWriter.CoreNewLine"/> is a field the
    /// base <c>WriteLine</c> overloads read directly, so delegating the property is not
    /// enough on its own — each overload that ends a line has to be routed too.
    /// </summary>
    public override void WriteLine() => Write(_target.NewLine);

    /// <inheritdoc/>
    public override void WriteLine(string? value)
    {
        Write(value);
        WriteLine();
    }

    /// <inheritdoc/>
    public override void WriteLine(char value)
    {
        Write(value);
        WriteLine();
    }

    /// <inheritdoc/>
    public override void WriteLine(char[]? buffer)
    {
        Write(buffer);
        WriteLine();
    }

    /// <inheritdoc/>
    public override void WriteLine(char[] buffer, int index, int count)
    {
        Write(buffer, index, count);
        WriteLine();
    }

    /// <inheritdoc/>
    public override void WriteLine(ReadOnlySpan<char> buffer)
    {
        Write(buffer);
        WriteLine();
    }

    /// <inheritdoc/>
    public override void WriteLine(StringBuilder? value)
    {
        Write(value);
        WriteLine();
    }

    /// <summary>
    /// The two asynchronous line endings the base class writes from
    /// <see cref="TextWriter.CoreNewLine"/> without going through any overload routed
    /// above. The rest reach <see cref="WriteLine()"/> on their own; these do not, and
    /// would put this writer's own newline into a buffer the target's newline chose the
    /// rest of.
    /// </summary>
    public override Task WriteLineAsync()
    {
        WriteLine();
        return Task.CompletedTask;
    }

    /// <inheritdoc cref="WriteLineAsync()"/>
    public override Task WriteLineAsync(StringBuilder? value, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        WriteLine(value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Emits the preamble followed by every buffered section, ordered by
    /// <paramref name="order"/>. Sections named there come first in that order; the rest
    /// follow in the order they were written. Matching is case-insensitive.
    ///
    /// <para>
    /// Separators are recomputed here rather than replayed, because which adjacencies
    /// need one is a property of the pair that ends up adjacent. Empty chunks — an
    /// excluded section, or one opened but never written to — sit at no adjacency at
    /// all and are skipped, so they neither add nor suppress a separator.
    /// </para>
    ///
    /// <para>
    /// A call that emits nothing changes nothing: the buffers, the open section and the
    /// writable state all survive it. Otherwise flushing a document whose only section
    /// has yet to render — one whose heading a projection has deferred, a headless
    /// section, an unfinished streaming table — would discard the boundary and dump
    /// everything after it into the preamble, unorderable and silently so.
    /// </para>
    /// </summary>
    /// <param name="order">The requested section order, or <c>null</c>.</param>
    /// <param name="endsNeedingBlankLine">
    /// Whether the last chunk left a blank line pending, for the same reason
    /// <see cref="BeginSection"/> takes it.
    /// </param>
    public void EmitOrdered(IReadOnlyList<string>? order, bool endsNeedingBlankLine)
    {
        if (_emitted)
            return;

        _current.EndsNeedingBlankLine = endsNeedingBlankLine;

        var rank = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (order != null)
        {
            for (var i = 0; i < order.Count; i++)
            {
                // A null name cannot match a section, and letting it reach the
                // dictionary would turn a harmless typo into a crash at Flush.
                if (order[i] != null)
                    rank.TryAdd(order[i], i);
            }
        }

        var ordered = _sections
            .Select((chunk, ordinal) => (chunk, ordinal))
            .OrderBy(s => s.chunk.Name is { } name && rank.TryGetValue(name, out var r) ? r : int.MaxValue)
            .ThenBy(s => s.ordinal)
            .Select(s => s.chunk);

        Chunk? previous = null;
        foreach (var chunk in Prepend(_preamble, ordered))
        {
            if (chunk.Content.Length == 0)
                continue;

            if (previous != null && (chunk.OpensWithHeading || previous.EndsNeedingBlankLine))
                _target.Write(chunk.SeparatorNewLine);

            _target.Write(chunk.Content);
            previous = chunk;
        }

        if (previous == null)
            return;

        _emitted = true;

        // Drop the buffers rather than clearing them: StringBuilder.Clear allocates a
        // fresh backing array the size of the one it discards, which on a large
        // document is another document-sized allocation for nothing.
        _sections.Clear();
        _preamble = new Chunk(null, _target.NewLine);
        _current = _preamble;
    }

    private static IEnumerable<Chunk> Prepend(Chunk first, IEnumerable<Chunk> rest)
    {
        yield return first;
        foreach (var chunk in rest)
            yield return chunk;
    }

    private void ThrowIfEmitted()
    {
        if (_emitted)
        {
            throw new InvalidOperationException(
                "The document was already emitted. Ordering sections requires buffering the " +
                "whole document, so Flush() and ToString() complete it: a section written " +
                "afterwards could no longer be moved ahead of one already written out. " +
                "Finish the document before flushing, or clear MarkoutWriterOptions.SectionOrder.");
        }
    }
}
