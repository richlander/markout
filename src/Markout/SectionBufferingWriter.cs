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
/// </summary>
internal sealed class SectionBufferingWriter : TextWriter
{
    private readonly TextWriter _target;
    private readonly List<(string Name, StringWriter Buffer)> _sections = [];
    private readonly StringWriter _preamble;
    private StringWriter _current;

    public SectionBufferingWriter(TextWriter target)
    {
        _target = target;
        _preamble = NewBuffer(target);
        _current = _preamble;
        CoreNewLine = target.NewLine.ToCharArray();
    }

    private static StringWriter NewBuffer(TextWriter target)
    {
        var sw = new StringWriter();
        sw.NewLine = target.NewLine;
        return sw;
    }

    /// <inheritdoc/>
    public override Encoding Encoding => _target.Encoding;

    /// <summary>
    /// Starts a new section buffer. Content written before the first call stays in the
    /// preamble, which is always emitted first — a document title is not a section and
    /// must not be reordered behind one.
    /// </summary>
    public void BeginSection(string name)
    {
        var buffer = NewBuffer(_target);
        _sections.Add((name, buffer));
        _current = buffer;
    }

    /// <inheritdoc/>
    public override void Write(char value) => _current.Write(value);

    /// <inheritdoc/>
    public override void Write(string? value) => _current.Write(value);

    /// <inheritdoc/>
    public override void Write(char[] buffer, int index, int count) => _current.Write(buffer, index, count);

    /// <inheritdoc/>
    public override void Write(ReadOnlySpan<char> buffer) => _current.Write(buffer);

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
        _target.Write(_preamble.GetStringBuilder().ToString());
        _preamble.GetStringBuilder().Clear();

        var rank = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (order != null)
        {
            for (var i = 0; i < order.Count; i++)
                rank.TryAdd(order[i], i);
        }

        var ordered = _sections
            .Select((section, ordinal) => (section.Name, section.Buffer, ordinal))
            .OrderBy(s => rank.TryGetValue(s.Name, out var r) ? r : int.MaxValue)
            .ThenBy(s => s.ordinal);

        foreach (var section in ordered)
            _target.Write(section.Buffer.GetStringBuilder().ToString());

        _sections.Clear();
        _current = _preamble;
    }
}
