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
/// not, so moving a table changes which adjacencies need one. So this type records per
/// chunk the facts the writer decides from, and asks the writer's own questions again
/// of whatever pair of chunks ends up adjacent:
/// </para>
///
/// <list type="bullet">
/// <item><description>
/// Does this chunk open with a block that separates itself from anything before it —
/// a heading, a quotation, a rule (<see cref="Chunk.OpensSelfSeparating"/>)? Natively
/// that block writes its blank line whenever content precedes it, so at emit it needs
/// one whenever content precedes it here.
/// </description></item>
/// <item><description>
/// Did the chunk before this one leave a blank line pending
/// (<see cref="Chunk.EndsNeedingBlankLine"/>)? That is a fact about the other chunk,
/// which is exactly why it cannot travel with this one.
/// </description></item>
/// <item><description>
/// Did the caller open this chunk with blank lines of its own
/// (<see cref="Chunk.OpensWithExplicitBlankLine"/>)? Those satisfy a pending blank
/// line, so computing another on the same grounds doubles it — but they do not close
/// the seam (<see cref="Chunk.OpeningLength"/>), because the block after them is
/// still the chunk's first and still separates itself.
/// </description></item>
/// <item><description>
/// Does this chunk hold content at all (<see cref="Chunk.ContainsContent"/>)? A chunk
/// holding nothing but blank lines, or whose only block the formatter does not
/// support, is not something a following block separates itself from — so "a chunk
/// came before" is not the question, "content came before" is.
/// </description></item>
/// </list>
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

        /// <summary>
        /// Whether this chunk opens with something that separates itself from any
        /// content before it — a heading, a quotation, a rule — rather than relying on
        /// the block before it to have left a blank line pending.
        /// </summary>
        public bool OpensSelfSeparating { get; set; }

        /// <summary>
        /// Whether this chunk opens with a blank line the caller wrote itself. That
        /// blank line satisfies whatever the block before the seam left pending, so a
        /// computed separator on the same grounds would double it.
        /// </summary>
        public bool OpensWithExplicitBlankLine { get; set; }

        /// <summary>
        /// How much of <see cref="Content"/> is the chunk's opening: blank lines the
        /// caller wrote at the seam, before any block. They are content — they are in
        /// the output wherever this chunk lands — but they are not what the section
        /// starts with, so a block that follows them is still opening the section and
        /// its own separator is still the seam's to compute.
        /// </summary>
        public int OpeningLength { get; set; }

        /// <summary>
        /// Whether this chunk holds anything a following block would have to separate
        /// itself from. Not the same as being non-empty: a chunk can hold nothing but
        /// blank lines the caller wrote, and a self-separating block does not separate
        /// itself from those.
        /// </summary>
        public bool ContainsContent { get; set; }

        /// <summary>
        /// Whether <see cref="SeparatorNewLine"/> has been observed yet. The first
        /// observation wins: it is taken where the separator would have been written,
        /// which is before the chunk's first content, and a later write may see a
        /// newline the target has since changed.
        /// </summary>
        public bool SeparatorNewLineCaptured { get; set; }

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
    /// section that has written nothing but its opening blank lines. A separator
    /// written here belongs to neither section, so it is dropped and recomputed at
    /// emit. Explicit blank lines do not close the seam, because the block after them
    /// is still the section's first and its separator still depends on what the
    /// section ends up following.
    /// </summary>
    public bool AtSectionBoundary =>
        !ReferenceEquals(_current, _preamble) && _current.Content.Length == _current.OpeningLength;

    /// <summary>
    /// Records that the section now open begins with something that separates itself
    /// from any content before it, whatever that content was. This survives reordering,
    /// while <see cref="Chunk.EndsNeedingBlankLine"/> — a fact about the chunk before
    /// the seam — does not.
    /// </summary>
    public void NoteSelfSeparatingOpen()
    {
        if (AtSectionBoundary)
            _current.OpensSelfSeparating = true;
    }

    /// <summary>
    /// Records that the section now open holds content — something a block after it
    /// would separate itself from, as opposed to blank lines, which it would not.
    /// </summary>
    public void NoteContent() => _current.ContainsContent = true;

    /// <summary>
    /// Writes a blank line the caller asked for at a section seam, and records that the
    /// section opens with one. The blank line stays as content — it is the caller's, and
    /// it belongs to the section wherever the section lands — but it does not close the
    /// seam, so a self-separating block after it still has its separator computed rather
    /// than captured.
    /// </summary>
    /// <returns>
    /// Whether the writer was at a seam and wrote the line. When it was not, the caller
    /// writes the blank line the ordinary way.
    /// </returns>
    public bool TryWriteSectionOpeningBlankLine()
    {
        if (!AtSectionBoundary)
            return false;

        WriteLine();
        _current.OpeningLength = _current.Content.Length;
        _current.OpensWithExplicitBlankLine = true;
        return true;
    }

    /// <summary>
    /// Observes the newline in force where this section's leading separator belongs.
    /// First observation wins, so the point the writer suppressed a separator at beats
    /// the point the first content arrived at, which may be later and may see a newline
    /// the target has changed in between.
    ///
    /// <para>
    /// This is exact only while the target's newline is stable, which is the boundary
    /// of what reordering can promise. A separator sits between two chunks that were
    /// never adjacent, so "the newline in force there" names a moment the document
    /// never had: the newline the section itself was written under and the newline the
    /// section it now follows ended under can differ, and neither is the answer in
    /// every case. The section's own is used, and a caller or formatter that changes
    /// <see cref="TextWriter.NewLine"/> mid-document gets that rather than a guarantee.
    /// </para>
    /// </summary>
    public void NoteSeparatorNewLine()
    {
        if (!AtSectionBoundary || _current.SeparatorNewLineCaptured)
            return;

        _current.SeparatorNewLine = _target.NewLine;
        _current.SeparatorNewLineCaptured = true;
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
        NoteSeparatorNewLine();
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
        var precededByContent = false;
        foreach (var chunk in Prepend(_preamble, ordered))
        {
            if (chunk.Content.Length == 0)
                continue;

            // A self-separating opening needs its blank line whenever content precedes
            // it — natively it is written against _hasContent, which the section's own
            // opening blank lines do not set and an unsupported block does not either,
            // so "some chunk came before" is the wrong question. An ordinary opening
            // needs one only because the chunk before left one pending, and a blank
            // line the caller wrote at this seam has already satisfied that.
            var separate = (chunk.OpensSelfSeparating && precededByContent)
                || (!chunk.OpensWithExplicitBlankLine && previous?.EndsNeedingBlankLine == true);

            if (previous != null && separate)
                _target.Write(chunk.SeparatorNewLine);

            _target.Write(chunk.Content);
            previous = chunk;
            precededByContent |= chunk.ContainsContent;
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
