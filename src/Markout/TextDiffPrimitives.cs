using System.Collections.Immutable;

namespace Markout;

/// <summary>Whether the producer knows how the final logical line is terminated.</summary>
public enum TextDiffLineTerminator
{
    /// <summary>The producer does not assert final-line termination.</summary>
    Unknown,

    /// <summary>The final logical line ends with a line terminator.</summary>
    Present,

    /// <summary>The final logical line does not end with a line terminator.</summary>
    Absent
}

/// <summary>The side of a mapped text diff.</summary>
public enum TextDiffSide
{
    /// <summary>The Before sequence.</summary>
    Before,

    /// <summary>The After sequence.</summary>
    After,

    /// <summary>Both corresponding sequences.</summary>
    Both
}

/// <summary>The form derived from the two mapped line-range counts.</summary>
public enum TextDiffChangeForm
{
    /// <summary>An empty Before range maps to a non-empty After range.</summary>
    Addition,

    /// <summary>A non-empty Before range maps to an empty After range.</summary>
    Removal,

    /// <summary>Two non-empty ranges are related as a replacement.</summary>
    Replacement
}

/// <summary>The target form of a static diff annotation.</summary>
public enum TextDiffAnnotationTargetKind
{
    /// <summary>The complete mapped change.</summary>
    Change,

    /// <summary>One side-local logical line.</summary>
    Line,

    /// <summary>One side-local span within a logical line.</summary>
    Span
}

/// <summary>The semantic kind of one projected display record.</summary>
public enum TextDiffDisplayLineKind
{
    /// <summary>One corresponding unchanged line from both sequences.</summary>
    Context,

    /// <summary>One line from a change's Before range.</summary>
    Removal,

    /// <summary>One line from a change's After range.</summary>
    Addition,

    /// <summary>An exact pair of omitted unchanged ranges.</summary>
    Omission,

    /// <summary>A caller-issued annotation.</summary>
    Annotation
}

/// <summary>A zero-based half-open range within one logical line sequence.</summary>
public readonly record struct TextDiffRange
{
    /// <summary>The zero-based first line.</summary>
    public int Start { get; }

    /// <summary>The number of lines.</summary>
    public int Count { get; }

    /// <summary>The exclusive zero-based end.</summary>
    public int End => checked(Start + Count);

    /// <summary>Whether the range is empty.</summary>
    public bool IsEmpty => Count == 0;

    /// <summary>Creates a half-open line range.</summary>
    public TextDiffRange(int start, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        _ = checked(start + count);
        Start = start;
        Count = count;
    }

    /// <summary>Returns whether the range contains the zero-based line.</summary>
    public bool Contains(int line) => line >= Start && line < End;
}

/// <summary>
/// A zero-based half-open UTF-16 span within one side-local logical line.
/// </summary>
public readonly record struct TextDiffSpan
{
    /// <summary>The zero-based sequence line.</summary>
    public int Line { get; }

    /// <summary>The zero-based UTF-16 code-unit offset.</summary>
    public int Start { get; }

    /// <summary>The number of UTF-16 code units.</summary>
    public int Count { get; }

    /// <summary>The exclusive UTF-16 code-unit end.</summary>
    public int End => checked(Start + Count);

    /// <summary>Whether the span is empty.</summary>
    public bool IsEmpty => Count == 0;

    /// <summary>Creates a side-local text span.</summary>
    public TextDiffSpan(int line, int start, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(line);
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        _ = checked(start + count);
        Line = line;
        Start = start;
        Count = count;
    }
}

/// <summary>A caller-issued mapping between one Before span and one After span.</summary>
public sealed class TextDiffInnerMapping
{
    /// <summary>The Before span.</summary>
    public TextDiffSpan Before { get; }

    /// <summary>The After span.</summary>
    public TextDiffSpan After { get; }

    /// <summary>Creates an inner text mapping.</summary>
    public TextDiffInnerMapping(TextDiffSpan before, TextDiffSpan after)
    {
        if (before.IsEmpty && after.IsEmpty)
            throw new ArgumentException("An inner mapping cannot contain two empty spans.");

        Before = before;
        After = after;
    }
}

/// <summary>A caller-issued static annotation attached to a mapped change.</summary>
public sealed class TextDiffAnnotation
{
    /// <summary>The annotation text.</summary>
    public string Text { get; }

    /// <summary>The annotation severity.</summary>
    public CalloutSeverity Severity { get; }

    /// <summary>The target form.</summary>
    public TextDiffAnnotationTargetKind TargetKind { get; }

    /// <summary>The target side for line and span annotations.</summary>
    public TextDiffSide? Side { get; }

    /// <summary>The target zero-based line for line annotations.</summary>
    public int? Line { get; }

    /// <summary>The target span for span annotations.</summary>
    public TextDiffSpan? Span { get; }

    private TextDiffAnnotation(
        string text,
        CalloutSeverity severity,
        TextDiffAnnotationTargetKind targetKind,
        TextDiffSide? side,
        int? line,
        TextDiffSpan? span)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);
        TextDiffValidation.ValidateText(text, nameof(text));
        if (!Enum.IsDefined(severity))
            throw new ArgumentOutOfRangeException(nameof(severity));

        Text = text;
        Severity = severity;
        TargetKind = targetKind;
        Side = side;
        Line = line;
        Span = span;
    }

    /// <summary>Creates an annotation targeting the complete change.</summary>
    public static TextDiffAnnotation ForChange(
        string text,
        CalloutSeverity severity = CalloutSeverity.Note)
        => new(text, severity, TextDiffAnnotationTargetKind.Change, null, null, null);

    /// <summary>Creates an annotation targeting one side-local line.</summary>
    public static TextDiffAnnotation ForLine(
        TextDiffSide side,
        int line,
        string text,
        CalloutSeverity severity = CalloutSeverity.Note)
    {
        TextDiffValidation.ValidateSingleSide(side, nameof(side));
        ArgumentOutOfRangeException.ThrowIfNegative(line);
        return new(text, severity, TextDiffAnnotationTargetKind.Line, side, line, null);
    }

    /// <summary>Creates an annotation targeting one side-local span.</summary>
    public static TextDiffAnnotation ForSpan(
        TextDiffSide side,
        TextDiffSpan span,
        string text,
        CalloutSeverity severity = CalloutSeverity.Note)
    {
        TextDiffValidation.ValidateSingleSide(side, nameof(side));
        return new(text, severity, TextDiffAnnotationTargetKind.Span, side, null, span);
    }
}

/// <summary>One immutable logical-line sequence in a mapped text diff.</summary>
public sealed class TextDiffSequence
{
    /// <summary>The optional display label.</summary>
    public string? Label { get; }

    /// <summary>The logical lines in sequence order.</summary>
    public ImmutableArray<string> Lines { get; }

    /// <summary>The producer's final-line-terminator assertion.</summary>
    public TextDiffLineTerminator FinalLineTerminator { get; }

    /// <summary>Whether the sequence contains no logical lines.</summary>
    public bool IsEmpty => Lines.IsEmpty;

    /// <summary>Creates an immutable logical-line sequence.</summary>
    public TextDiffSequence(
        IEnumerable<string> lines,
        string? label = null,
        TextDiffLineTerminator finalLineTerminator = TextDiffLineTerminator.Unknown)
    {
        ArgumentNullException.ThrowIfNull(lines);
        if (!Enum.IsDefined(finalLineTerminator))
            throw new ArgumentOutOfRangeException(nameof(finalLineTerminator));
        if (label is not null)
            TextDiffValidation.ValidateLogicalLine(label, nameof(label), "Sequence label");

        Lines = [.. lines];
        for (var i = 0; i < Lines.Length; i++)
        {
            var line = Lines[i];
            if (line is null)
                throw new ArgumentException($"Line at index {i} is null.", nameof(lines));
            TextDiffValidation.ValidateLogicalLine(line, nameof(lines), $"Line at index {i}");
        }

        if (Lines.IsEmpty && finalLineTerminator != TextDiffLineTerminator.Unknown)
        {
            throw new ArgumentException(
                "An empty sequence cannot assert a final-line terminator.",
                nameof(finalLineTerminator));
        }

        Label = label;
        FinalLineTerminator = finalLineTerminator;
    }
}

/// <summary>One caller-issued mapping between Before and After line ranges.</summary>
public sealed class TextDiffChange
{
    /// <summary>The mapped Before range.</summary>
    public TextDiffRange Before { get; }

    /// <summary>The mapped After range.</summary>
    public TextDiffRange After { get; }

    /// <summary>The caller-issued intraline mappings.</summary>
    public ImmutableArray<TextDiffInnerMapping> InnerMappings { get; }

    /// <summary>The caller-issued annotations.</summary>
    public ImmutableArray<TextDiffAnnotation> Annotations { get; }

    /// <summary>The form derived from the mapped range counts.</summary>
    public TextDiffChangeForm Form => Before.IsEmpty
        ? TextDiffChangeForm.Addition
        : After.IsEmpty
            ? TextDiffChangeForm.Removal
            : TextDiffChangeForm.Replacement;

    /// <summary>Creates one mapped change.</summary>
    public TextDiffChange(
        TextDiffRange before,
        TextDiffRange after,
        IEnumerable<TextDiffInnerMapping>? innerMappings = null,
        IEnumerable<TextDiffAnnotation>? annotations = null)
    {
        if (before.IsEmpty && after.IsEmpty)
            throw new ArgumentException("A change cannot contain two empty ranges.");

        Before = before;
        After = after;
        InnerMappings = innerMappings is null ? [] : [.. innerMappings];
        Annotations = annotations is null ? [] : [.. annotations];

        for (var i = 0; i < InnerMappings.Length; i++)
        {
            if (InnerMappings[i] is null)
                throw new ArgumentException($"Inner mapping at index {i} is null.", nameof(innerMappings));
        }

        for (var i = 0; i < Annotations.Length; i++)
        {
            if (Annotations[i] is null)
                throw new ArgumentException($"Annotation at index {i} is null.", nameof(annotations));
        }
    }
}

/// <summary>One context-selected record projected from a mapped text diff.</summary>
public sealed class TextDiffDisplayLine
{
    /// <summary>The record kind.</summary>
    public TextDiffDisplayLineKind Kind { get; }

    /// <summary>The zero-based change address, when derived from a change.</summary>
    public int? ChangeAddress { get; }

    /// <summary>The mapped change form, when derived from a change.</summary>
    public TextDiffChangeForm? ChangeForm { get; }

    /// <summary>The side represented by this record.</summary>
    public TextDiffSide Side { get; }

    /// <summary>The zero-based Before line, when present.</summary>
    public int? BeforeLine { get; }

    /// <summary>The zero-based After line, when present.</summary>
    public int? AfterLine { get; }

    /// <summary>The exact Before range represented by an omission.</summary>
    public TextDiffRange? BeforeRange { get; }

    /// <summary>The exact After range represented by an omission.</summary>
    public TextDiffRange? AfterRange { get; }

    /// <summary>The Before text, when present.</summary>
    public string? BeforeText { get; }

    /// <summary>The After text, when present.</summary>
    public string? AfterText { get; }

    /// <summary>The annotation, for annotation records.</summary>
    public TextDiffAnnotation? Annotation { get; }

    internal TextDiffDisplayLine(
        TextDiffDisplayLineKind kind,
        TextDiffSide side,
        int? changeAddress = null,
        TextDiffChangeForm? changeForm = null,
        int? beforeLine = null,
        int? afterLine = null,
        TextDiffRange? beforeRange = null,
        TextDiffRange? afterRange = null,
        string? beforeText = null,
        string? afterText = null,
        TextDiffAnnotation? annotation = null)
    {
        Kind = kind;
        Side = side;
        ChangeAddress = changeAddress;
        ChangeForm = changeForm;
        BeforeLine = beforeLine;
        AfterLine = afterLine;
        BeforeRange = beforeRange;
        AfterRange = afterRange;
        BeforeText = beforeText;
        AfterText = afterText;
        Annotation = annotation;
    }
}

internal static class TextDiffValidation
{
    public static void ValidateSingleSide(TextDiffSide side, string paramName)
    {
        if (side is not TextDiffSide.Before and not TextDiffSide.After)
            throw new ArgumentOutOfRangeException(paramName);
    }

    public static void ValidateLogicalLine(string value, string paramName, string description)
    {
        ValidateText(value, paramName);
        if (value.AsSpan().IndexOfAny('\r', '\n') >= 0)
            throw new ArgumentException($"{description} contains a carriage return or line feed.", paramName);
    }

    public static void ValidateText(string value, string paramName)
    {
        for (var i = 0; i < value.Length; i++)
        {
            if (char.IsHighSurrogate(value[i]))
            {
                if (i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1]))
                    throw new ArgumentException("Text contains malformed UTF-16.", paramName);
                i++;
            }
            else if (char.IsLowSurrogate(value[i]))
            {
                throw new ArgumentException("Text contains malformed UTF-16.", paramName);
            }
        }
    }

    public static bool IsUtf16Boundary(string value, int offset)
        => offset >= 0
            && offset <= value.Length
            && (offset == 0
                || offset == value.Length
                || !char.IsHighSurrogate(value[offset - 1])
                || !char.IsLowSurrogate(value[offset]));
}
