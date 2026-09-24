using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace Markout;

/// <summary>Shared format-neutral lowerings for a <see cref="MappedTextDiff"/>.</summary>
public static class MappedTextDiffLowering
{
    /// <summary>
    /// Projects a diff into context, change, omission, and annotation records.
    /// </summary>
    /// <param name="diff">The validated mapped diff.</param>
    /// <param name="contextLines">
    /// The number of unchanged lines retained on each side of a change, or
    /// <c>null</c> to retain every unchanged line.
    /// </param>
    public static ImmutableArray<TextDiffDisplayLine> ToDisplayLines(
        MappedTextDiff diff,
        int? contextLines = 3)
    {
        ArgumentNullException.ThrowIfNull(diff);
        ValidateContextLines(contextLines);
        if (diff.IsEmpty)
            return [];

        var hunks = SelectHunks(diff, contextLines);
        var records = ImmutableArray.CreateBuilder<TextDiffDisplayLine>();
        var beforeCursor = 0;
        var afterCursor = 0;

        foreach (var hunk in hunks)
        {
            AddOmission(
                records,
                new TextDiffRange(beforeCursor, hunk.Before.Start - beforeCursor),
                new TextDiffRange(afterCursor, hunk.After.Start - afterCursor));
            records.AddRange(hunk.Lines);
            beforeCursor = hunk.Before.End;
            afterCursor = hunk.After.End;
        }

        AddOmission(
            records,
            new TextDiffRange(beforeCursor, diff.Before.Lines.Length - beforeCursor),
            new TextDiffRange(afterCursor, diff.After.Lines.Length - afterCursor));
        return records.ToImmutable();
    }

    /// <summary>
    /// Groups changes into hunks. Changes join one hunk when their unchanged gap fits within the
    /// context on both sides and their labels are equal; changes with different labels always
    /// start a new hunk. When such a forced split leaves a gap that both hunks' context would
    /// reach, the gap's lines are divided between the two hunks so no line appears in both.
    /// </summary>
    internal static ImmutableArray<TextDiffHunk> SelectHunks(
        MappedTextDiff diff,
        int? contextLines)
    {
        ArgumentNullException.ThrowIfNull(diff);
        ValidateContextLines(contextLines);
        if (diff.IsEmpty)
            return [];

        var groups = new List<(int First, int Last, bool Mixed)>();
        var firstAddress = 0;
        while (firstAddress < diff.Changes.Length)
        {
            var lastAddress = firstAddress;
            while (lastAddress + 1 < diff.Changes.Length)
            {
                var current = diff.Changes[lastAddress];
                var next = diff.Changes[lastAddress + 1];
                if (!Equals(current.Label, next.Label))
                    break;
                var gap = next.Before.Start - current.Before.End;
                if (contextLines is not null && (long)gap > (long)contextLines.Value * 2)
                    break;
                lastAddress++;
            }

            groups.Add((firstAddress, lastAddress, false));
            firstAddress = lastAddress + 1;
        }

        MergeGroupsHoldingUnterminatedFinalLines(diff, groups);

        // Context each hunk takes on each side. Natural hunk boundaries keep full context; a
        // label split divides the gap so no line appears in two hunks.
        var trailing = new int[groups.Count];
        var leading = new int[groups.Count];
        leading[0] = Clamp(diff.Changes[groups[0].First].Before.Start, contextLines);
        trailing[^1] = Clamp(
            diff.Before.Lines.Length - diff.Changes[groups[^1].Last].Before.End,
            contextLines);
        for (var index = 0; index + 1 < groups.Count; index++)
        {
            var gap = diff.Changes[groups[index + 1].First].Before.Start
                - diff.Changes[groups[index].Last].Before.End;
            if (contextLines is not null && (long)gap > (long)contextLines.Value * 2)
            {
                trailing[index] = contextLines.Value;
                leading[index + 1] = contextLines.Value;
            }
            else
            {
                trailing[index] = Clamp((gap + 1) / 2, contextLines);
                leading[index + 1] = Clamp(gap - trailing[index], contextLines);
            }
        }

        // GNU patch reads a hunk whose leading and trailing context differ as anchored to the
        // start or end of the file. Unequal context is kept only on the side that really touches
        // the sequence start or end; otherwise both sides take the smaller amount, and any line
        // that drops out is reported as an exact omission.
        for (var index = 0; index < groups.Count; index++)
        {
            var startsSequence = diff.Changes[groups[index].First].Before.Start - leading[index] == 0;
            var endsSequence = diff.Changes[groups[index].Last].Before.End + trailing[index]
                == diff.Before.Lines.Length;
            if (leading[index] < trailing[index] && !startsSequence)
                trailing[index] = leading[index];
            else if (trailing[index] < leading[index] && !endsSequence)
                leading[index] = trailing[index];
        }

        var hunks = ImmutableArray.CreateBuilder<TextDiffHunk>(groups.Count);
        for (var index = 0; index < groups.Count; index++)
        {
            var first = diff.Changes[groups[index].First];
            var last = diff.Changes[groups[index].Last];
            var before = new TextDiffRange(
                first.Before.Start - leading[index],
                last.Before.End + trailing[index] - (first.Before.Start - leading[index]));
            var after = new TextDiffRange(
                first.After.Start - leading[index],
                last.After.End + trailing[index] - (first.After.Start - leading[index]));

            hunks.Add(new TextDiffHunk(
                before,
                after,
                BuildHunkLines(diff, groups[index].First, groups[index].Last, before, after),
                groups[index].Mixed ? null : first.Label));
        }

        return hunks.MoveToImmutable();
    }

    /// <summary>
    /// A unified hunk that marks an unterminated final line must be the last hunk, or GNU patch
    /// cannot apply it. When a label split would leave such a line in an earlier hunk, the groups
    /// from there to the end merge into one hunk. A merged hunk that holds different labels is
    /// written without a header label, so a header never claims more than every change it holds.
    /// </summary>
    private static void MergeGroupsHoldingUnterminatedFinalLines(
        MappedTextDiff diff,
        List<(int First, int Last, bool Mixed)> groups)
    {
        var earliest = groups.Count;
        for (var index = 0; index < groups.Count - 1; index++)
        {
            for (var address = groups[index].First; address <= groups[index].Last; address++)
            {
                var change = diff.Changes[address];
                var holdsBefore = diff.Before.FinalLineTerminator == TextDiffLineTerminator.Absent
                    && !change.Before.IsEmpty
                    && change.Before.End == diff.Before.Lines.Length;
                var holdsAfter = diff.After.FinalLineTerminator == TextDiffLineTerminator.Absent
                    && !change.After.IsEmpty
                    && change.After.End == diff.After.Lines.Length;
                if (holdsBefore || holdsAfter)
                    earliest = Math.Min(earliest, index);
            }
        }

        if (earliest >= groups.Count - 1)
            return;

        var first = groups[earliest].First;
        var last = groups[^1].Last;
        var mixed = false;
        for (var address = first + 1; address <= last; address++)
            mixed |= !Equals(diff.Changes[address].Label, diff.Changes[first].Label);

        groups.RemoveRange(earliest, groups.Count - earliest);
        groups.Add((first, last, mixed));
    }

    private static int Clamp(int available, int? contextLines)
        => contextLines is null ? available : Math.Min(contextLines.Value, available);

    private static ImmutableArray<TextDiffDisplayLine> BuildHunkLines(
        MappedTextDiff diff,
        int firstAddress,
        int lastAddress,
        TextDiffRange beforeRange,
        TextDiffRange afterRange)
    {
        var lines = ImmutableArray.CreateBuilder<TextDiffDisplayLine>();
        var beforeCursor = beforeRange.Start;
        var afterCursor = afterRange.Start;

        for (var address = firstAddress; address <= lastAddress; address++)
        {
            var change = diff.Changes[address];
            AddContext(lines, diff, beforeCursor, afterCursor, change.Before.Start - beforeCursor);

            for (var line = change.Before.Start; line < change.Before.End; line++)
            {
                lines.Add(new TextDiffDisplayLine(
                    TextDiffDisplayLineKind.Removal,
                    TextDiffSide.Before,
                    address,
                    change.Form,
                    beforeLine: line,
                    beforeText: diff.Before.Lines[line]));
            }

            for (var line = change.After.Start; line < change.After.End; line++)
            {
                lines.Add(new TextDiffDisplayLine(
                    TextDiffDisplayLineKind.Addition,
                    TextDiffSide.After,
                    address,
                    change.Form,
                    afterLine: line,
                    afterText: diff.After.Lines[line]));
            }

            foreach (var annotation in change.Annotations)
                lines.Add(CreateAnnotationLine(address, change.Form, annotation));

            beforeCursor = change.Before.End;
            afterCursor = change.After.End;
        }

        AddContext(lines, diff, beforeCursor, afterCursor, beforeRange.End - beforeCursor);
        return lines.ToImmutable();
    }

    private static TextDiffDisplayLine CreateAnnotationLine(
        int address,
        TextDiffChangeForm form,
        TextDiffAnnotation annotation)
    {
        var side = annotation.Side ?? TextDiffSide.Both;
        int? beforeLine = null;
        int? afterLine = null;
        if (side == TextDiffSide.Before)
            beforeLine = annotation.Line ?? annotation.Span?.Line;
        else if (side == TextDiffSide.After)
            afterLine = annotation.Line ?? annotation.Span?.Line;

        return new TextDiffDisplayLine(
            TextDiffDisplayLineKind.Annotation,
            side,
            address,
            form,
            beforeLine,
            afterLine,
            annotation: annotation);
    }

    private static void AddContext(
        ImmutableArray<TextDiffDisplayLine>.Builder lines,
        MappedTextDiff diff,
        int beforeStart,
        int afterStart,
        int count)
    {
        for (var offset = 0; offset < count; offset++)
        {
            var beforeLine = beforeStart + offset;
            var afterLine = afterStart + offset;
            lines.Add(new TextDiffDisplayLine(
                TextDiffDisplayLineKind.Context,
                TextDiffSide.Both,
                beforeLine: beforeLine,
                afterLine: afterLine,
                beforeText: diff.Before.Lines[beforeLine],
                afterText: diff.After.Lines[afterLine]));
        }
    }

    private static void AddOmission(
        ImmutableArray<TextDiffDisplayLine>.Builder records,
        TextDiffRange before,
        TextDiffRange after)
    {
        if (before.IsEmpty)
            return;

        records.Add(new TextDiffDisplayLine(
            TextDiffDisplayLineKind.Omission,
            TextDiffSide.Both,
            beforeRange: before,
            afterRange: after));
    }

    private static void ValidateContextLines(int? contextLines)
    {
        if (contextLines < 0)
            throw new ArgumentOutOfRangeException(nameof(contextLines));
    }
}

internal sealed class TextDiffHunk
{
    public TextDiffRange Before { get; }
    public TextDiffRange After { get; }
    public ImmutableArray<TextDiffDisplayLine> Lines { get; }
    public TextDiffChangeLabel? Label { get; }

    public TextDiffHunk(
        TextDiffRange before,
        TextDiffRange after,
        ImmutableArray<TextDiffDisplayLine> lines,
        TextDiffChangeLabel? label = null)
    {
        Before = before;
        After = after;
        Lines = lines;
        Label = label;
    }
}

internal static class TextDiffEscaping
{
    public static string Unified(string value)
        => Escape(value, escapeBackslash: false, escapeTab: false);

    public static string Human(string value)
        => Escape(value, escapeBackslash: false, escapeTab: true);

    public static string Structured(string value)
        => Escape(value, escapeBackslash: true, escapeTab: true);

    public static string MarkdownInline(string value)
        => Human(value)
            .Replace("\\", "\\\\")
            .Replace("*", "\\*")
            .Replace("_", "\\_")
            .Replace("~", "\\~")
            .Replace("[", "\\[")
            .Replace("]", "\\]");

    public static int LongestBacktickRun(IEnumerable<string> values)
    {
        var longest = 0;
        foreach (var value in values)
        {
            var current = 0;
            foreach (var c in value)
            {
                if (c == '`')
                {
                    current++;
                    longest = Math.Max(longest, current);
                }
                else
                {
                    current = 0;
                }
            }
        }
        return longest;
    }

    private static string Escape(
        string value,
        bool escapeBackslash,
        bool escapeTab)
    {
        StringBuilder? builder = null;
        for (var i = 0; i < value.Length;)
        {
            var c = value[i];
            var width = char.IsHighSurrogate(c) ? 2 : 1;
            var replacement = Replacement(value, i, width, escapeBackslash, escapeTab);
            if (replacement is null)
            {
                if (builder is not null)
                    builder.Append(value, i, width);
                i += width;
                continue;
            }

            builder ??= new StringBuilder(value.Length + 8).Append(value, 0, i);
            builder.Append(replacement);
            i += width;
        }
        return builder?.ToString() ?? value;
    }

    private static string? Replacement(
        string value,
        int index,
        int width,
        bool escapeBackslash,
        bool escapeTab)
    {
        if (width == 2)
        {
            var rune = new Rune(value[index], value[index + 1]);
            return IsEscapedCategory(Rune.GetUnicodeCategory(rune))
                ? $"\\U{rune.Value:X8}"
                : null;
        }

        var c = value[index];
        if (escapeBackslash && c == '\\')
            return "\\\\";
        if (c == '\t')
            return escapeTab ? "\\t" : null;
        if (c == '\r')
            return "\\r";
        if (c == '\n')
            return "\\n";

        return IsEscapedCategory(char.GetUnicodeCategory(c))
            ? $"\\u{(int)c:X4}"
            : null;
    }

    private static bool IsEscapedCategory(UnicodeCategory category)
        => category is UnicodeCategory.Control
            or UnicodeCategory.Format
            or UnicodeCategory.LineSeparator
            or UnicodeCategory.ParagraphSeparator;
}
