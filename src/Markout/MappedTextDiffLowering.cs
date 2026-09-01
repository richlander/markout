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

    internal static ImmutableArray<TextDiffHunk> SelectHunks(
        MappedTextDiff diff,
        int? contextLines)
    {
        ArgumentNullException.ThrowIfNull(diff);
        ValidateContextLines(contextLines);
        if (diff.IsEmpty)
            return [];

        var hunks = ImmutableArray.CreateBuilder<TextDiffHunk>();
        var firstAddress = 0;
        while (firstAddress < diff.Changes.Length)
        {
            var lastAddress = firstAddress;
            if (contextLines is null)
            {
                lastAddress = diff.Changes.Length - 1;
            }
            else
            {
                while (lastAddress + 1 < diff.Changes.Length)
                {
                    var current = diff.Changes[lastAddress];
                    var next = diff.Changes[lastAddress + 1];
                    var gap = next.Before.Start - current.Before.End;
                    if ((long)gap > (long)contextLines.Value * 2)
                        break;
                    lastAddress++;
                }
            }

            var first = diff.Changes[firstAddress];
            var last = diff.Changes[lastAddress];
            var previousBeforeEnd = firstAddress == 0 ? 0 : diff.Changes[firstAddress - 1].Before.End;
            var previousAfterEnd = firstAddress == 0 ? 0 : diff.Changes[firstAddress - 1].After.End;
            var nextBeforeStart = lastAddress + 1 == diff.Changes.Length
                ? diff.Before.Lines.Length
                : diff.Changes[lastAddress + 1].Before.Start;
            var nextAfterStart = lastAddress + 1 == diff.Changes.Length
                ? diff.After.Lines.Length
                : diff.Changes[lastAddress + 1].After.Start;

            var leading = contextLines is null
                ? first.Before.Start - previousBeforeEnd
                : Math.Min(contextLines.Value, first.Before.Start - previousBeforeEnd);
            var trailing = contextLines is null
                ? nextBeforeStart - last.Before.End
                : Math.Min(contextLines.Value, nextBeforeStart - last.Before.End);
            var before = new TextDiffRange(
                first.Before.Start - leading,
                last.Before.End + trailing - (first.Before.Start - leading));
            var after = new TextDiffRange(
                first.After.Start - leading,
                last.After.End + trailing - (first.After.Start - leading));

            hunks.Add(new TextDiffHunk(
                before,
                after,
                BuildHunkLines(diff, firstAddress, lastAddress, before, after)));
            firstAddress = lastAddress + 1;
        }

        return hunks.ToImmutable();
    }

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

    public TextDiffHunk(
        TextDiffRange before,
        TextDiffRange after,
        ImmutableArray<TextDiffDisplayLine> lines)
    {
        Before = before;
        After = after;
        Lines = lines;
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
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            var replacement = Replacement(c, escapeBackslash, escapeTab);
            if (replacement is null)
            {
                if (builder is not null)
                    builder.Append(c);
                continue;
            }

            builder ??= new StringBuilder(value.Length + 8).Append(value, 0, i);
            builder.Append(replacement);
        }
        return builder?.ToString() ?? value;
    }

    private static string? Replacement(
        char c,
        bool escapeBackslash,
        bool escapeTab)
    {
        if (escapeBackslash && c == '\\')
            return "\\\\";
        if (c == '\t')
            return escapeTab ? "\\t" : null;
        if (c == '\r')
            return "\\r";
        if (c == '\n')
            return "\\n";

        var category = char.GetUnicodeCategory(c);
        return category is UnicodeCategory.Control
            or UnicodeCategory.Format
            or UnicodeCategory.LineSeparator
            or UnicodeCategory.ParagraphSeparator
            ? $"\\u{(int)c:X4}"
            : null;
    }
}
