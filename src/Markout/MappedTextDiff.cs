using System.Collections.Immutable;

namespace Markout;

/// <summary>
/// A validated mapping between two immutable ordered logical-line sequences.
/// </summary>
/// <remarks>
/// Markout validates and presents the caller-issued mapping. It does not compare
/// the text, infer correspondence, or repair malformed ranges.
/// </remarks>
public sealed class MappedTextDiff
{
    /// <summary>The Before sequence.</summary>
    public TextDiffSequence Before { get; }

    /// <summary>The After sequence.</summary>
    public TextDiffSequence After { get; }

    /// <summary>The canonical ordered change population.</summary>
    public ImmutableArray<TextDiffChange> Changes { get; }

    /// <summary>Whether the canonical change population is empty.</summary>
    public bool IsEmpty => Changes.IsEmpty;

    /// <summary>Creates and validates a mapped text diff.</summary>
    public MappedTextDiff(
        TextDiffSequence before,
        TextDiffSequence after,
        IEnumerable<TextDiffChange> changes)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        ArgumentNullException.ThrowIfNull(changes);

        Before = before;
        After = after;
        Changes = [.. changes];

        ValidateChanges();
        ValidateAbsentFinalLine(Before, After, TextDiffSide.Before);
        ValidateAbsentFinalLine(After, Before, TextDiffSide.After);
    }

    private void ValidateChanges()
    {
        var beforeCursor = 0;
        var afterCursor = 0;

        for (var address = 0; address < Changes.Length; address++)
        {
            var change = Changes[address];
            if (change is null)
                throw new ArgumentException($"Change at index {address} is null.", nameof(Changes));

            ValidateRange(change.Before, Before.Lines.Length, address, TextDiffSide.Before);
            ValidateRange(change.After, After.Lines.Length, address, TextDiffSide.After);

            if (change.Before.Start < beforeCursor || change.After.Start < afterCursor)
            {
                throw new ArgumentException(
                    $"Change at index {address} is not monotonic and non-overlapping on both sides.",
                    nameof(Changes));
            }

            var beforeGap = change.Before.Start - beforeCursor;
            var afterGap = change.After.Start - afterCursor;
            if (beforeGap != afterGap)
            {
                throw new ArgumentException(
                    $"The unchanged gap before change {address} has different Before and After counts.",
                    nameof(Changes));
            }

            ValidateInnerMappings(change, address);
            ValidateAnnotations(change, address);
            beforeCursor = change.Before.End;
            afterCursor = change.After.End;
        }

        if (Before.Lines.Length - beforeCursor != After.Lines.Length - afterCursor)
        {
            throw new ArgumentException(
                "The trailing unchanged gap has different Before and After counts.",
                nameof(Changes));
        }
    }

    private static void ValidateRange(
        TextDiffRange range,
        int lineCount,
        int address,
        TextDiffSide side)
    {
        if (range.End > lineCount)
        {
            throw new ArgumentException(
                $"{side} range for change {address} extends beyond its sequence.",
                nameof(Changes));
        }
    }

    private void ValidateInnerMappings(TextDiffChange change, int address)
    {
        if (change.InnerMappings.IsEmpty)
            return;
        if (change.Form != TextDiffChangeForm.Replacement)
        {
            throw new ArgumentException(
                $"Only replacement change {address} may contain inner mappings.",
                nameof(Changes));
        }

        TextDiffSpan? previousBefore = null;
        TextDiffSpan? previousAfter = null;
        for (var i = 0; i < change.InnerMappings.Length; i++)
        {
            var mapping = change.InnerMappings[i];
            ValidateSpan(
                mapping.Before,
                Before,
                change.Before,
                address,
                i,
                TextDiffSide.Before,
                "inner mapping");
            ValidateSpan(
                mapping.After,
                After,
                change.After,
                address,
                i,
                TextDiffSide.After,
                "inner mapping");

            if (previousBefore is { } before && !EndsBeforeOrAt(before, mapping.Before))
            {
                throw new ArgumentException(
                    $"Before span for inner mapping {i} in change {address} is not monotonic.",
                    nameof(Changes));
            }
            if (previousAfter is { } after && !EndsBeforeOrAt(after, mapping.After))
            {
                throw new ArgumentException(
                    $"After span for inner mapping {i} in change {address} is not monotonic.",
                    nameof(Changes));
            }

            previousBefore = mapping.Before;
            previousAfter = mapping.After;
        }
    }

    private static void ValidateSpan(
        TextDiffSpan span,
        TextDiffSequence sequence,
        TextDiffRange changeRange,
        int address,
        int targetIndex,
        TextDiffSide side,
        string targetKind)
    {
        if (!changeRange.Contains(span.Line))
        {
            throw new ArgumentException(
                $"{side} span for {targetKind} {targetIndex} in change {address} is outside the change.",
                nameof(Changes));
        }

        var line = sequence.Lines[span.Line];
        if (span.End > line.Length)
        {
            throw new ArgumentException(
                $"{side} span for {targetKind} {targetIndex} in change {address} extends beyond its line.",
                nameof(Changes));
        }
        if (!TextDiffValidation.IsUtf16Boundary(line, span.Start)
            || !TextDiffValidation.IsUtf16Boundary(line, span.End))
        {
            throw new ArgumentException(
                $"{side} span for {targetKind} {targetIndex} in change {address} splits a surrogate pair.",
                nameof(Changes));
        }
    }

    private void ValidateAnnotations(TextDiffChange change, int address)
    {
        for (var i = 0; i < change.Annotations.Length; i++)
        {
            var annotation = change.Annotations[i];
            switch (annotation.TargetKind)
            {
                case TextDiffAnnotationTargetKind.Change:
                    break;

                case TextDiffAnnotationTargetKind.Line:
                {
                    var side = annotation.Side!.Value;
                    var line = annotation.Line!.Value;
                    if (!RangeFor(change, side).Contains(line))
                    {
                        throw new ArgumentException(
                            $"Line annotation {i} in change {address} is outside the change.",
                            nameof(Changes));
                    }
                    break;
                }

                case TextDiffAnnotationTargetKind.Span:
                {
                    var side = annotation.Side!.Value;
                    var span = annotation.Span!.Value;
                    ValidateSpan(
                        span,
                        SequenceFor(side),
                        RangeFor(change, side),
                        address,
                        i,
                        side,
                        "annotation");
                    break;
                }

                default:
                    throw new ArgumentException(
                        $"Annotation {i} in change {address} has an invalid target kind.",
                        nameof(Changes));
            }
        }
    }

    private void ValidateAbsentFinalLine(
        TextDiffSequence sequence,
        TextDiffSequence other,
        TextDiffSide side)
    {
        if (sequence.FinalLineTerminator != TextDiffLineTerminator.Absent)
            return;

        var finalLine = sequence.Lines.Length - 1;
        if (Changes.Any(change => RangeFor(change, side).Contains(finalLine)))
            return;

        if (!TryMapUnchangedLine(side, finalLine, out var otherLine)
            || otherLine != other.Lines.Length - 1
            || other.FinalLineTerminator != TextDiffLineTerminator.Absent)
        {
            throw new ArgumentException(
                $"The {side} unterminated final line must be changed or correspond to an unterminated final line.",
                nameof(Changes));
        }
    }

    private bool TryMapUnchangedLine(TextDiffSide side, int line, out int otherLine)
    {
        var beforeCursor = 0;
        var afterCursor = 0;

        foreach (var change in Changes)
        {
            var beforeGap = new TextDiffRange(beforeCursor, change.Before.Start - beforeCursor);
            var afterGap = new TextDiffRange(afterCursor, change.After.Start - afterCursor);
            if (side == TextDiffSide.Before && beforeGap.Contains(line))
            {
                otherLine = afterGap.Start + (line - beforeGap.Start);
                return true;
            }
            if (side == TextDiffSide.After && afterGap.Contains(line))
            {
                otherLine = beforeGap.Start + (line - afterGap.Start);
                return true;
            }

            beforeCursor = change.Before.End;
            afterCursor = change.After.End;
        }

        var trailingBefore = new TextDiffRange(beforeCursor, Before.Lines.Length - beforeCursor);
        var trailingAfter = new TextDiffRange(afterCursor, After.Lines.Length - afterCursor);
        if (side == TextDiffSide.Before && trailingBefore.Contains(line))
        {
            otherLine = trailingAfter.Start + (line - trailingBefore.Start);
            return true;
        }
        if (side == TextDiffSide.After && trailingAfter.Contains(line))
        {
            otherLine = trailingBefore.Start + (line - trailingAfter.Start);
            return true;
        }

        otherLine = -1;
        return false;
    }

    private TextDiffSequence SequenceFor(TextDiffSide side)
        => side == TextDiffSide.Before ? Before : After;

    private static TextDiffRange RangeFor(TextDiffChange change, TextDiffSide side)
        => side == TextDiffSide.Before ? change.Before : change.After;

    private static bool EndsBeforeOrAt(TextDiffSpan earlier, TextDiffSpan later)
        => earlier.Line < later.Line
            || (earlier.Line == later.Line && earlier.End <= later.Start);
}
