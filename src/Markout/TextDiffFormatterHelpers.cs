using System.Collections.Immutable;

namespace Markout;

internal static class TextDiffFormatterHelpers
{
    internal static List<string> UnifiedLines(
        MappedTextDiff diff,
        int? contextLines)
    {
        var lines = new List<string>
        {
            $"--- {TextDiffEscaping.Human(diff.Before.Label ?? "Before")}",
            $"+++ {TextDiffEscaping.Human(diff.After.Label ?? "After")}"
        };

        foreach (var hunk in MappedTextDiffLowering.SelectHunks(diff, contextLines))
        {
            lines.Add($"@@ -{FormatRange(hunk.Before)} +{FormatRange(hunk.After)} @@");
            foreach (var line in hunk.Lines)
            {
                switch (line.Kind)
                {
                    case TextDiffDisplayLineKind.Context:
                        lines.Add(" " + TextDiffEscaping.Unified(line.BeforeText!));
                        if (IsSharedUnterminatedFinalLine(diff, line))
                            lines.Add(@"\ No newline at end of file");
                        break;

                    case TextDiffDisplayLineKind.Removal:
                        lines.Add("-" + TextDiffEscaping.Unified(line.BeforeText!));
                        if (IsUnterminatedFinalLine(diff.Before, line.BeforeLine))
                            lines.Add(@"\ No newline at end of file");
                        break;

                    case TextDiffDisplayLineKind.Addition:
                        lines.Add("+" + TextDiffEscaping.Unified(line.AfterText!));
                        if (IsUnterminatedFinalLine(diff.After, line.AfterLine))
                            lines.Add(@"\ No newline at end of file");
                        break;
                }
            }
        }

        return lines;
    }

    internal static StructuredTextDiffTable StructuredTable(
        MappedTextDiff diff,
        int? contextLines)
    {
        var rows = new List<string[]>();
        AddSequenceRow(rows, TextDiffSide.Before, diff.Before);
        AddSequenceRow(rows, TextDiffSide.After, diff.After);

        foreach (var line in MappedTextDiffLowering.ToDisplayLines(diff, contextLines))
            rows.Add(ToStructuredRow(diff, line));

        for (var address = 0; address < diff.Changes.Length; address++)
        {
            var change = diff.Changes[address];
            foreach (var mapping in change.InnerMappings)
            {
                var row = EmptyRow();
                row[RecordKind] = "inner_mapping";
                row[ChangeAddress] = address.ToString();
                row[ChangeForm] = Form(change.Form);
                row[Side] = "both";
                row[BeforeLine] = mapping.Before.Line.ToString();
                row[AfterLine] = mapping.After.Line.ToString();
                row[BeforeOffset] = mapping.Before.Start.ToString();
                row[BeforeLength] = mapping.Before.Count.ToString();
                row[AfterOffset] = mapping.After.Start.ToString();
                row[AfterLength] = mapping.After.Count.ToString();
                rows.Add(row);
            }
        }

        return new StructuredTextDiffTable(Headers, rows);
    }

    internal static string AnnotationTarget(
        TextDiffAnnotation annotation,
        int? changeAddress = null)
        => annotation.TargetKind switch
        {
            TextDiffAnnotationTargetKind.Change =>
                changeAddress is { } address ? $"change {address + 1}" : "change",
            TextDiffAnnotationTargetKind.Line => $"{SideName(annotation.Side!.Value)} line {annotation.Line!.Value + 1}",
            TextDiffAnnotationTargetKind.Span =>
                SpanTarget(annotation.Side!.Value, annotation.Span!.Value),
            _ => "annotation"
        };

    private static string SpanTarget(TextDiffSide side, TextDiffSpan span)
        => span.IsEmpty
            ? $"{SideName(side)} line {span.Line + 1}, insertion point at column {span.Start + 1}"
            : $"{SideName(side)} line {span.Line + 1}, columns {span.Start + 1}-{span.End}";

    internal static string FormatMarkedLine(
        MappedTextDiff diff,
        TextDiffDisplayLine line,
        string open,
        string close)
    {
        var raw = line.Side == TextDiffSide.Before ? line.BeforeText! : line.AfterText!;
        if (line.ChangeAddress is not { } address)
            return TextDiffEscaping.Human(raw);

        var spans = diff.Changes[address].InnerMappings
            .Select(mapping => line.Side == TextDiffSide.Before ? mapping.Before : mapping.After)
            .Where(span => span.Line == (line.BeforeLine ?? line.AfterLine) && !span.IsEmpty)
            .ToArray();
        if (spans.Length == 0)
            return TextDiffEscaping.Human(raw);

        var writer = new StringWriter();
        var cursor = 0;
        foreach (var span in spans)
        {
            writer.Write(TextDiffEscaping.Human(raw[cursor..span.Start]));
            writer.Write(open);
            writer.Write(TextDiffEscaping.Human(raw[span.Start..span.End]));
            writer.Write(close);
            cursor = span.End;
        }
        writer.Write(TextDiffEscaping.Human(raw[cursor..]));
        return writer.ToString();
    }

    internal static string SideName(TextDiffSide side)
        => side switch
        {
            TextDiffSide.Before => "Before",
            TextDiffSide.After => "After",
            _ => "Both"
        };

    private static string[] ToStructuredRow(
        MappedTextDiff diff,
        TextDiffDisplayLine line)
    {
        var row = EmptyRow();
        row[RecordKind] = line.Kind switch
        {
            TextDiffDisplayLineKind.Context => "context",
            TextDiffDisplayLineKind.Removal => "removal",
            TextDiffDisplayLineKind.Addition => "addition",
            TextDiffDisplayLineKind.Omission => "omission",
            TextDiffDisplayLineKind.Annotation => "annotation",
            _ => ""
        };
        row[Side] = line.Side switch
        {
            TextDiffSide.Before => "before",
            TextDiffSide.After => "after",
            _ => "both"
        };
        if (line.ChangeAddress is { } address)
        {
            var change = diff.Changes[address];
            row[ChangeAddress] = address.ToString();
            row[ChangeForm] = Form(change.Form);
            SetRange(row, change.Before, change.After);
        }
        if (line.BeforeLine is { } beforeLine)
            row[BeforeLine] = beforeLine.ToString();
        if (line.AfterLine is { } afterLine)
            row[AfterLine] = afterLine.ToString();
        if (line.BeforeRange is { } beforeRange && line.AfterRange is { } afterRange)
        {
            SetRange(row, beforeRange, afterRange);
            row[HiddenCount] = beforeRange.Count.ToString();
        }
        if (line.BeforeText is not null)
            row[BeforeText] = TextDiffEscaping.Structured(line.BeforeText);
        if (line.AfterText is not null)
            row[AfterText] = TextDiffEscaping.Structured(line.AfterText);
        if (line.Annotation is { } annotation)
        {
            row[Annotation] = TextDiffEscaping.Structured(annotation.Text);
            row[Target] = AnnotationTarget(annotation);
            row[Severity] = annotation.Severity.ToString().ToLowerInvariant();
            if (annotation.Span is { } span)
            {
                if (annotation.Side == TextDiffSide.Before)
                {
                    row[BeforeOffset] = span.Start.ToString();
                    row[BeforeLength] = span.Count.ToString();
                }
                else
                {
                    row[AfterOffset] = span.Start.ToString();
                    row[AfterLength] = span.Count.ToString();
                }
            }
        }
        return row;
    }

    private static void AddSequenceRow(
        List<string[]> rows,
        TextDiffSide side,
        TextDiffSequence sequence)
    {
        var row = EmptyRow();
        row[RecordKind] = "sequence";
        row[Side] = side == TextDiffSide.Before ? "before" : "after";
        row[Label] = TextDiffEscaping.Structured(sequence.Label ?? SideName(side));
        row[LineCount] = sequence.Lines.Length.ToString();
        row[Terminator] = sequence.FinalLineTerminator.ToString().ToLowerInvariant();
        rows.Add(row);
    }

    private static void SetRange(
        string[] row,
        TextDiffRange before,
        TextDiffRange after)
    {
        row[BeforeStart] = before.Start.ToString();
        row[BeforeCount] = before.Count.ToString();
        row[AfterStart] = after.Start.ToString();
        row[AfterCount] = after.Count.ToString();
    }

    private static string Form(TextDiffChangeForm form)
        => form.ToString().ToLowerInvariant();

    private static string FormatRange(TextDiffRange range)
    {
        var start = range.Count == 0 ? range.Start : range.Start + 1;
        return range.Count == 1 ? start.ToString() : $"{start},{range.Count}";
    }

    private static bool IsUnterminatedFinalLine(
        TextDiffSequence sequence,
        int? line)
        => sequence.FinalLineTerminator == TextDiffLineTerminator.Absent
            && line == sequence.Lines.Length - 1;

    private static bool IsSharedUnterminatedFinalLine(
        MappedTextDiff diff,
        TextDiffDisplayLine line)
        => diff.Before.FinalLineTerminator == TextDiffLineTerminator.Absent
            && diff.After.FinalLineTerminator == TextDiffLineTerminator.Absent
            && line.BeforeLine == diff.Before.Lines.Length - 1
            && line.AfterLine == diff.After.Lines.Length - 1;

    private static string[] EmptyRow() => new string[Headers.Length];

    private const int RecordKind = 0;
    private const int ChangeAddress = 1;
    private const int ChangeForm = 2;
    private const int Side = 3;
    private const int BeforeLine = 4;
    private const int AfterLine = 5;
    private const int BeforeStart = 6;
    private const int BeforeCount = 7;
    private const int AfterStart = 8;
    private const int AfterCount = 9;
    private const int HiddenCount = 10;
    private const int BeforeOffset = 11;
    private const int BeforeLength = 12;
    private const int AfterOffset = 13;
    private const int AfterLength = 14;
    private const int BeforeText = 15;
    private const int AfterText = 16;
    private const int Annotation = 17;
    private const int Target = 18;
    private const int Severity = 19;
    private const int Label = 20;
    private const int LineCount = 21;
    private const int Terminator = 22;

    private static readonly ImmutableArray<string> Headers =
    [
        "record_kind",
        "change_address",
        "change_form",
        "side",
        "before_line",
        "after_line",
        "before_start",
        "before_count",
        "after_start",
        "after_count",
        "hidden_count",
        "before_offset",
        "before_length",
        "after_offset",
        "after_length",
        "before_text",
        "after_text",
        "annotation",
        "target",
        "severity",
        "label",
        "line_count",
        "terminator"
    ];

    internal static readonly IReadOnlySet<int> JsonStringColumnIndices = new HashSet<int>
    {
        RecordKind,
        ChangeForm,
        Side,
        BeforeText,
        AfterText,
        Annotation,
        Target,
        Severity,
        Label,
        Terminator
    };
}

internal readonly record struct StructuredTextDiffTable(
    ImmutableArray<string> Headers,
    List<string[]> Rows);
