namespace Markout;

/// <summary>
/// A <c>before → after</c> change. When <typeparamref name="V"/> is a composite shape
/// (<see cref="Fraction"/>, <see cref="Share"/>, <see cref="Percent"/>, <see cref="Segments"/>)
/// the halves render and decompose recursively. When <typeparamref name="V"/> is numeric,
/// <see cref="MarkoutDeltaAttribute"/> appends a derived change, e.g. <c>98555 → 61190 (−38%)</c>.
/// </summary>
/// <typeparam name="V">The compared value type (numeric scalar or a composite shape).</typeparam>
/// <param name="Before">The value before.</param>
/// <param name="After">The value after.</param>
public readonly record struct Change<V>(V Before, V After) : IMarkoutCell
{
    /// <inheritdoc/>
    public void FormatInline(TextWriter writer, in MarkoutCellFormat format)
    {
        // If either half is a composite shape, render both as shapes (a null half writes nothing)
        // so a nullable composite side never leaks a struct ToString via the scalar path.
        if (Before is IMarkoutCell || After is IMarkoutCell)
        {
            // Buffer the core (halves + delta parenthetical) so a trailing polarity glyph can be
            // composed onto the whole cell; write straight through in word mode.
            var glyphMode = format.Glyphs is not null;
            TextWriter core = glyphMode ? new StringWriter() : writer;

            (Before as IMarkoutCell)?.FormatInline(core, format);
            core.Write(CellText.Arrow);
            (After as IMarkoutCell)?.FormatInline(core, format);

            // Composite cells append a dense delta-noun (from IDeltaCountable) and/or the goal status
            // (from IGoalMagnitude), merged into one parenthetical.
            string? compositeFirst = null;
            if (format.DeltaNoun is not null && Before is IDeltaCountable beforeCount && After is IDeltaCountable afterCount)
            {
                var nounDelta = CellText.SignedNumber(afterCount.DeltaCount - beforeCount.DeltaCount, format.NumberFormat);
                compositeFirst = nounDelta == CellText.Placeholder ? CellText.Placeholder : nounDelta + " " + format.DeltaNoun;
            }
            GateStatus? compositeStatus = null;
            if (format.Goal != Goal.Context && Before is IGoalMagnitude beforeMag && After is IGoalMagnitude afterMag &&
                GoalDerivation.TryDerive(beforeMag.GoalMagnitude, afterMag.GoalMagnitude, format.Goal, format.Noise, out _, out var compositeGate))
                compositeStatus = compositeGate;
            WriteParenGroup(core, compositeFirst, glyphMode ? null : StatusWord(compositeStatus));

            if (glyphMode)
                writer.Write(ComposeStatusGlyph(format, core.ToString()!, compositeStatus));
            return;
        }

        // Buffer the scalar core when a trailing glyph will be composed onto it.
        var scalarGlyphMode = format.Glyphs is not null;
        TextWriter target = scalarGlyphMode ? new StringWriter() : writer;

        target.Write(CellText.Scalar(Before, format.NumberFormat));
        target.Write(CellText.Arrow);
        target.Write(CellText.Scalar(After, format.NumberFormat));

        // Merge an optional derived-change suffix (or a delta-noun) and an optional goal status into a
        // single parenthetical in word mode: "(+40%)", "(bad)", "(+2 solved)", or "(+40%, bad)". In
        // glyph mode the status leaves the parenthetical and trails as a composed glyph: "(+40%) ✗".
        string? deltaPart;
        if (format.DeltaNoun is not null)
            deltaPart = NounText(format.DeltaNoun, format.NumberFormat);
        else
            deltaPart = format.Delta == Delta.None ? null : DeltaSuffix(format.Delta, format.NumberFormat);
        GateStatus? statusValue = null;
        if (format.Goal != Goal.Context &&
            GoalDerivation.TryDerive(Before, After, format.Goal, format.Noise, out _, out var status))
        {
            // Delta.Multiple already renders a goal-aligned direction word ("fewer"/"more"), so an
            // aligned (Good) status word is redundant — suppress it. Keep it when it conflicts (Bad),
            // when there is no rendered multiple phrase (placeholder), or for a delta-noun.
            var multipleImpliesGood = format.DeltaNoun is null
                && format.Delta == Delta.Multiple
                && status == GateStatus.Good
                && deltaPart is not null && deltaPart != CellText.Placeholder;
            if (!multipleImpliesGood)
                statusValue = status;
        }

        WriteParenGroup(target, deltaPart, scalarGlyphMode ? null : StatusWord(statusValue));

        if (scalarGlyphMode)
            writer.Write(ComposeStatusGlyph(format, target.ToString()!, statusValue));
    }

    /// <summary>The status slug word for a derived polarity, or <c>null</c> when there is none.</summary>
    private static string? StatusWord(GateStatus? status)
        => status is { } value ? GateStatusText.Slug(value) : null;

    /// <summary>Composes a trailing polarity glyph onto the buffered cell <paramref name="text"/> via the
    /// format's glyph set + composer (append-with-space by default). No status → text unchanged.</summary>
    private static string ComposeStatusGlyph(in MarkoutCellFormat format, string text, GateStatus? status)
    {
        if (status is not { } value)
            return text;
        var glyph = format.Glyphs!.ForStatus(value);
        var context = new GlyphContext(GlyphSlot.MovementCell, text, glyph, format.Goal, value);
        return format.Compose is { } compose ? compose(context) : context.Combine();
    }

    private string NounText(string noun, string? numberFormat)
    {
        // Reuse the exact (decimal) delta path so large long/decimal changes don't lose precision.
        var delta = CellText.AbsoluteDelta(Before, After, signed: true, numberFormat);
        return delta == CellText.Placeholder ? CellText.Placeholder : delta + " " + noun;
    }

    private static void WriteParenGroup(TextWriter writer, string? first, string? second)
    {
        if (first is null && second is null)
            return;
        writer.Write(" (");
        if (first is not null)
            writer.Write(first);
        if (first is not null && second is not null)
            writer.Write(", ");
        if (second is not null)
            writer.Write(second);
        writer.Write(')');
    }

    /// <inheritdoc/>
    public void Decompose(ICollection<MarkoutField> fields, string? side, in MarkoutCellFormat format)
    {
        if (Before is IMarkoutCell || After is IMarkoutCell)
        {
            (Before as IMarkoutCell)?.Decompose(fields, CellText.SideKey(side, "before"), format);
            (After as IMarkoutCell)?.Decompose(fields, CellText.SideKey(side, "after"), format);

            // Composite shapes derive goal direction/status from their comparable magnitude
            // (a scalar Change<T> does this in the branch below).
            if (format.Goal != Goal.Context && Before is IGoalMagnitude beforeMag && After is IGoalMagnitude afterMag &&
                GoalDerivation.TryDerive(beforeMag.GoalMagnitude, afterMag.GoalMagnitude, format.Goal, format.Noise, out var compositeDir, out var compositeStatus))
            {
                fields.Add(new MarkoutField(CellText.SideKey(side, "direction"), DirectionText.Slug(compositeDir)));
                fields.Add(new MarkoutField(CellText.SideKey(side, "status"), GateStatusText.Slug(compositeStatus)));
            }

            // Caller delta-noun decomposes to a typed count + the noun word (caller metadata that is not
            // derivable downstream), keeping structured output reconstructable.
            if (format.DeltaNoun is not null && Before is IDeltaCountable beforeCount && After is IDeltaCountable afterCount)
            {
                fields.Add(new MarkoutField(CellText.SideKey(side, "deltaCount"), CellText.Number(afterCount.DeltaCount - beforeCount.DeltaCount)));
                fields.Add(new MarkoutField(CellText.SideKey(side, "deltaNoun"), format.DeltaNoun));
            }
            return;
        }

        fields.Add(new MarkoutField(CellText.SideKey(side, "before"), CellText.Scalar(Before)));
        fields.Add(new MarkoutField(CellText.SideKey(side, "after"), CellText.Scalar(After)));

        if (format.Delta == Delta.Percent)
            fields.Add(new MarkoutField(CellText.SideKey(side, "deltaPct"), DeltaValue(Delta.Percent)));
        else if (format.Delta == Delta.Absolute)
            fields.Add(new MarkoutField(CellText.SideKey(side, "deltaAbs"), DeltaValue(Delta.Absolute)));
        else if (format.Delta == Delta.Multiple)
            fields.Add(new MarkoutField(CellText.SideKey(side, "deltaMultiple"), DeltaValue(Delta.Multiple)));

        if (format.Goal != Goal.Context &&
            GoalDerivation.TryDerive(Before, After, format.Goal, format.Noise, out var direction, out var status))
        {
            fields.Add(new MarkoutField(CellText.SideKey(side, "direction"), DirectionText.Slug(direction)));
            fields.Add(new MarkoutField(CellText.SideKey(side, "status"), GateStatusText.Slug(status)));
        }

        if (format.DeltaNoun is not null && CellText.TryScalarDouble(Before, out _) && CellText.TryScalarDouble(After, out _))
        {
            fields.Add(new MarkoutField(CellText.SideKey(side, "deltaCount"), CellText.AbsoluteDelta(Before, After, signed: false)));
            fields.Add(new MarkoutField(CellText.SideKey(side, "deltaNoun"), format.DeltaNoun));
        }
    }

    private string DeltaSuffix(Delta mode, string? numberFormat)
    {
        if (!CellText.TryScalarDouble(Before, out var before) || !CellText.TryScalarDouble(After, out var after))
            return CellText.Placeholder;
        return mode switch
        {
            // Divide by |before| so a rise from a negative base reports as a gain, not a loss.
            Delta.Percent => before == 0 ? CellText.Placeholder : CellText.SignedPercent((after - before) / Math.Abs(before) * 100),
            Delta.Absolute => CellText.AbsoluteDelta(Before, After, signed: true, numberFormat),
            Delta.Multiple => MultipleText(withWord: true),
            _ => CellText.Placeholder
        };
    }

    private string DeltaValue(Delta mode)
    {
        if (!CellText.TryScalarDouble(Before, out var before) || !CellText.TryScalarDouble(After, out var after))
            return CellText.Placeholder;
        return mode switch
        {
            Delta.Percent => before == 0 ? CellText.Placeholder : CellText.PercentNumber((after - before) / Math.Abs(before) * 100),
            Delta.Absolute => CellText.AbsoluteDelta(Before, After, signed: false),
            Delta.Multiple => MultipleText(withWord: false),
            _ => CellText.Placeholder
        };
    }

    /// <summary>
    /// Renders the multiplicative factor <c>max/min</c> of the two magnitudes (rounded to one decimal).
    /// With <paramref name="withWord"/>, appends <c>× more</c>/<c>× fewer</c> from the change direction;
    /// a zero endpoint has no finite multiple and renders the placeholder.
    /// </summary>
    private string MultipleText(bool withWord)
    {
        if (!CellText.TryScalarDouble(Before, out var before) || !CellText.TryScalarDouble(After, out var after))
            return CellText.Placeholder;
        if (!double.IsFinite(before) || !double.IsFinite(after))
            return CellText.Placeholder;
        var magBefore = Math.Abs(before);
        var magAfter = Math.Abs(after);
        if (magBefore == 0 || magAfter == 0)
            return CellText.Placeholder;
        var factor = CellText.Number(Math.Round(Math.Max(magBefore, magAfter) / Math.Min(magBefore, magAfter), 1));
        if (!withWord)
            return factor;
        if (after > before)
            return factor + "\u00d7 more";
        if (after < before)
            return factor + "\u00d7 fewer";
        return factor + "\u00d7";
    }
}
