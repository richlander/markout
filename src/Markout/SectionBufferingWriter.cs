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
/// dropped at capture (<see cref="AtSectionBoundary"/>) and re-inserted between
/// emitted sections, which is where a separator means anything.
/// </para>
/// </summary>
internal sealed class SectionBufferingWriter : TextWriter
{
    private readonly TextWriter _target;
    private readonly List<(string Name, StringWriter Buffer)> _sections = [];
    private readonly List<bool> _seams = [];
    private readonly StringWriter _preamble = new();
    private StringWriter _current;
    private bool _emitted;

    public SectionBufferingWriter(TextWriter target)
    {
        _target = target;
        _current = _preamble;
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
        !ReferenceEquals(_current, _preamble) && _current.GetStringBuilder().Length == 0;

    /// <summary>
    /// Records that a separator was written at the current seam and drops it. Whether a
    /// seam carries one is a property of the seam, not of either section: it depends on
    /// how the section before it ended and how the one after it starts. Reordering
    /// permutes the sections and leaves the seams where they were, so the separator
    /// stays with the position rather than travelling with a section.
    /// </summary>
    public void DropSeparatorAtBoundary() => _seams[^1] = true;

    /// <summary>
    /// Starts a new section buffer. Content written before the first call stays in the
    /// preamble, which is always emitted first — a document title is not a section and
    /// must not be reordered behind one.
    /// </summary>
    public void BeginSection(string name)
    {
        ThrowIfEmitted();

        var buffer = new StringWriter();
        _sections.Add((name, buffer));
        _seams.Add(false);
        _current = buffer;
    }

    /// <inheritdoc/>
    public override void Write(char value)
    {
        ThrowIfEmitted();
        _current.Write(value);
    }

    /// <inheritdoc/>
    public override void Write(string? value)
    {
        ThrowIfEmitted();
        _current.Write(value);
    }

    /// <inheritdoc/>
    public override void Write(char[] buffer, int index, int count)
    {
        ThrowIfEmitted();
        _current.Write(buffer, index, count);
    }

    /// <inheritdoc/>
    public override void Write(ReadOnlySpan<char> buffer)
    {
        ThrowIfEmitted();
        _current.Write(buffer);
    }

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
    /// Emits the preamble followed by every buffered section, ordered by
    /// <paramref name="order"/>. Sections named there come first in that order; the rest
    /// follow in the order they were written. Matching is case-insensitive.
    ///
    /// <para>
    /// Emptying the buffers as it goes makes this idempotent, so a second call — from
    /// <c>Flush</c> after <c>ToString</c>, say — does not duplicate the document.
    /// </para>
    /// </summary>
    public void EmitOrdered(IReadOnlyList<string>? order)
    {
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
            .Select((section, ordinal) => (section.Name, section.Buffer, ordinal))
            .OrderBy(s => rank.TryGetValue(s.Name, out var r) ? r : int.MaxValue)
            .ThenBy(s => s.ordinal);

        var seams = SeamsInWriteOrder();
        var seamIndex = 0;
        var wroteSomething = false;

        Emit(_preamble, seams, ref seamIndex, ref wroteSomething);
        foreach (var section in ordered)
            Emit(section.Buffer, seams, ref seamIndex, ref wroteSomething);

        if (wroteSomething)
            _emitted = true;

        _sections.Clear();
        _seams.Clear();
        _current = _preamble;
    }

    /// <summary>
    /// The separators the document actually had, in the order its seams occurred. A
    /// section that wrote nothing sits at no seam, so it contributes none — and the
    /// count then matches the number of seams the reordered document will have.
    /// </summary>
    private List<bool> SeamsInWriteOrder()
    {
        var seams = new List<bool>();
        var seenContent = _preamble.GetStringBuilder().Length > 0;

        for (var i = 0; i < _sections.Count; i++)
        {
            var hasContent = _sections[i].Buffer.GetStringBuilder().Length > 0;
            if (hasContent && seenContent)
                seams.Add(_seams[i]);
            seenContent |= hasContent;
        }

        return seams;
    }

    private void Emit(StringWriter buffer, List<bool> seams, ref int seamIndex, ref bool wroteSomething)
    {
        var content = buffer.GetStringBuilder();
        if (content.Length == 0)
            return;

        if (wroteSomething)
        {
            if (seamIndex < seams.Count && seams[seamIndex])
                _target.Write(_target.NewLine);
            seamIndex++;
        }

        _target.Write(content);
        content.Clear();
        wroteSomething = true;
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
