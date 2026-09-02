using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Markout;

internal enum SeamEvent : byte
{
    /// <summary>
    /// A block that separates itself from anything before it — a heading, a quotation,
    /// a rule. It requires a blank line whenever content precedes it, and then takes it.
    /// </summary>
    SelfSeparating,

    /// <summary>
    /// A block that takes the blank line before it only if one is already pending.
    /// </summary>
    Ordinary,

    /// <summary>
    /// A blank line the caller wrote. It is unconditional, so it is in the chunk's
    /// content rather than computed here — but it satisfies a pending blank line, and
    /// where in the seam it does that matters.
    /// </summary>
    ExplicitBlank,
}

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
/// A <see cref="SeamEvent.SelfSeparating"/> block — a heading, a quotation, a rule —
/// requires a blank line whenever content precedes it, and then takes it.
/// </description></item>
/// <item><description>
/// An <see cref="SeamEvent.Ordinary"/> block takes one only if one is already pending.
/// </description></item>
/// <item><description>
/// An <see cref="SeamEvent.ExplicitBlank"/> is the caller's own blank line. It is
/// unconditional, so it lives in the chunk's content rather than being computed here,
/// and all it does at emit is satisfy whatever was pending at that point in the seam.
/// </description></item>
/// </list>
///
/// <para>
/// Summarising that into per-chunk booleans is what the first five rounds of review
/// did, and each summary lost a case: a section's opening is a sequence rather than a
/// block, a chunk can be content while emitting nothing, a chunk can emit nothing and
/// still take or leave a pending blank line, and a seam can need two separators rather
/// than one. Recording the decisions and running them again costs a small list per
/// section and stops the question being how many booleans it takes.
/// </para>
/// </summary>
internal sealed class SectionBufferingWriter : TextWriter
{
    private readonly TextWriter _target;
    private readonly List<Chunk> _sections = [];
    private Chunk _preamble;
    private Chunk _current;
    private bool _emitted;
    private int _emittedTrailingWhitespacePreservationLength;

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
        /// What the writer did at this chunk's seam, in order, before any content of
        /// its own settled the question. Not a summary of it — the writer's blank-line
        /// logic is a small state machine over two facts, and five rounds of review
        /// found five separate ways that summarising it loses a case. So the decisions
        /// are recorded and run again at emit, against whatever this chunk turns out
        /// to follow.
        /// </summary>
        public List<SeamEvent> Seam { get; } = [];

        /// <summary>
        /// Whether the chunk ever emitted a character of its own, closing the seam.
        /// Until it does, every blank-line decision in it is still the seam's, and
        /// <see cref="EndsNeedingBlankLine"/> describes the order it was written in
        /// rather than the order it is emitted in — <c>_needsBlankLine</c> is set
        /// against <c>_hasContent</c>, which is a fact about everything before it.
        /// </summary>
        public bool SeamClosed { get; set; }

        /// <summary>
        /// Whether this chunk holds anything a following block would have to separate
        /// itself from. Not the same as being non-empty: a chunk can hold nothing but
        /// blank lines the caller wrote, and a self-separating block does not separate
        /// itself from those.
        /// </summary>
        public bool ContainsContent { get; set; }

        /// <summary>
        /// The position through which document-end trimming must preserve this chunk.
        /// Later whitespace in the same chunk remains ordinary trim candidates.
        /// </summary>
        public int TrailingWhitespacePreservationLength { get; set; }

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
    /// section that has no content yet. A separator written here belongs to neither
    /// section, so it is dropped and recomputed at emit.
    ///
    /// <para>
    /// Content is the test, not characters, and it answers both ends of the seam on
    /// its own. A block that emits no characters and is content anyway — an empty
    /// table in JSONL — closes the seam, because the block after it is not the
    /// section's opener. Blank lines the caller wrote do not close it, because they
    /// are not content and the block after them still is the opener. That leaves
    /// characters that are not content, which is a block writing output the writer
    /// does not count; a streaming table left open was one, and it is fixed where it
    /// was wrong rather than compensated for here.
    /// </para>
    /// </summary>
    public bool AtSectionBoundary =>
        !ReferenceEquals(_current, _preamble) && !_current.ContainsContent;

    /// <summary>
    /// Records one of the writer's blank-line decisions, if it is being made at a seam
    /// where the answer depends on what this section ends up following. Once the
    /// section has content of its own, the answers are its own and are written into
    /// its buffer as usual.
    /// </summary>
    public void NoteSeam(SeamEvent seamEvent)
    {
        if (AtSectionBoundary)
            _current.Seam.Add(seamEvent);
    }

    /// <summary>
    /// Records that the section now open holds content — something a block after it
    /// would separate itself from, as opposed to blank lines, which it would not.
    /// </summary>
    public void NoteContent(bool preservesTrailingWhitespaceAtEnd)
    {
        _current.ContainsContent = true;
        if (preservesTrailingWhitespaceAtEnd)
        {
            _current.TrailingWhitespacePreservationLength =
                _current.Content.Length;
        }
    }

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

        _current.Seam.Add(SeamEvent.ExplicitBlank);
        WriteLine();
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

        _current.SeamClosed = !AtSectionBoundary;
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

        var targetLength = TargetLength();
        if (!WriteOrdered(
                _target,
                order,
                endsNeedingBlankLine,
                out var relativePreservationLength))
            return;

        _emittedTrailingWhitespacePreservationLength =
            relativePreservationLength == 0
                ? 0
                : targetLength + relativePreservationLength;
        _emitted = true;

        // Drop the buffers rather than clearing them: StringBuilder.Clear allocates a
        // fresh backing array the size of the one it discards, which on a large
        // document is another document-sized allocation for nothing.
        _sections.Clear();
        _preamble = new Chunk(null, _target.NewLine);
        _current = _preamble;
    }

    /// <summary>
    /// Renders the currently buffered document without emitting it or making the writer
    /// read-only. Used by <see cref="MarkoutWriter.ToString"/> for side-effect-free previews.
    /// </summary>
    public string RenderOrdered(
        IReadOnlyList<string>? order,
        bool endsNeedingBlankLine,
        out int trailingWhitespacePreservationLength)
    {
        if (_emitted)
        {
            trailingWhitespacePreservationLength =
                _emittedTrailingWhitespacePreservationLength;
            return "";
        }

        var preview = new StringWriter(_target.FormatProvider)
        {
            NewLine = _target.NewLine
        };
        WriteOrdered(
            preview,
            order,
            endsNeedingBlankLine,
            out var relativePreservationLength);
        trailingWhitespacePreservationLength =
            relativePreservationLength == 0
                ? 0
                : TargetLength() + relativePreservationLength;
        return preview.ToString();
    }

    public int EmittedTrailingWhitespacePreservationLength
        => _emittedTrailingWhitespacePreservationLength;

    private bool WriteOrdered(
        TextWriter output,
        IReadOnlyList<string>? order,
        bool currentEndsNeedingBlankLine,
        out int trailingWhitespacePreservationLength)
    {
        trailingWhitespacePreservationLength = 0;
        var outputLength = 0;
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

        var emitted = false;
        var hasContent = false;
        var blankLinePending = false;
        foreach (var chunk in Prepend(_preamble, ordered))
        {
            // Run the writer's seam decisions again, in order, against what this chunk
            // actually follows now. Nothing is skipped: a chunk that emitted no
            // characters can still have taken a pending blank line, or left one.
            foreach (var seamEvent in chunk.Seam)
            {
                if (seamEvent == SeamEvent.ExplicitBlank)
                {
                    // Unconditional, and already in the chunk's content. All it does
                    // here is satisfy whatever was pending at this point in the seam.
                    blankLinePending = false;
                    continue;
                }

                if (seamEvent == SeamEvent.SelfSeparating && hasContent)
                    blankLinePending = true;

                if (blankLinePending)
                {
                    output.Write(chunk.SeparatorNewLine);
                    outputLength += chunk.SeparatorNewLine.Length;
                    blankLinePending = false;
                    emitted = true;
                }
            }

            if (chunk.Content.Length > 0)
            {
                if (chunk.TrailingWhitespacePreservationLength > 0)
                {
                    trailingWhitespacePreservationLength =
                        outputLength + chunk.TrailingWhitespacePreservationLength;
                }

                output.Write(chunk.Content);
                outputLength += chunk.Content.Length;
                emitted = true;
            }

            hasContent |= chunk.ContainsContent;

            // A chunk that did nothing at all passes the pending blank line through:
            // what it inherited in the order it was written in says nothing about the
            // order it is emitted in. A chunk that did anything owns its end state,
            // even if nothing reached the output — every block that reaches the seam
            // consumes what is pending before writing, so what is pending after it is
            // what the block itself left, which no reordering changes. An empty table
            // in JSONL is the case: it emits nothing but still settles the end state,
            // leaving the next record adjacent rather than creating an empty record.
            // Only a closed seam needs its end state supplied out of band. While the
            // seam is open every raise is recorded in it, so the replay already knows.
            var isCurrent = ReferenceEquals(chunk, _current);
            var seamClosed = isCurrent ? !AtSectionBoundary : chunk.SeamClosed;
            if (seamClosed)
            {
                blankLinePending = isCurrent
                    ? currentEndsNeedingBlankLine
                    : chunk.EndsNeedingBlankLine;
            }
        }

        return emitted;
    }

    private int TargetLength()
        => _target is StringWriter sw ? sw.GetStringBuilder().Length : 0;

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
                "whole document, so Flush() and Complete() finalize it: a section written " +
                "afterwards could no longer be moved ahead of one already written out. " +
                "Finish the document before flushing, or clear MarkoutWriterOptions.SectionOrder.");
        }
    }
}
