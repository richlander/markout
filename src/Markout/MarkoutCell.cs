using System.Globalization;

namespace Markout;

/// <summary>
/// Entry points and shared helpers for composite cells (<see cref="IMarkoutCell"/>).
/// </summary>
public static class MarkoutCell
{
    /// <summary>
    /// Renders a composite cell to its dense, human-readable string.
    /// </summary>
    /// <param name="cell">The cell to render (may be <c>null</c>).</param>
    /// <param name="delta">The derived-change mode for a numeric <see cref="Change{V}"/>.</param>
    /// <param name="unit">An optional unit suffix for a <see cref="Share"/> value.</param>
    public static string ToInlineString(IMarkoutCell? cell, Delta delta = Delta.None, string? unit = null)
    {
        if (cell is null)
            return string.Empty;
        var sw = new StringWriter();
        cell.FormatInline(sw, new MarkoutCellFormat(delta, unit));
        return sw.ToString();
    }

    /// <summary>
    /// Renders a cell's dense inline form using a full <see cref="MarkoutCellFormat"/> (carrying
    /// <see cref="MarkoutCellFormat.Goal"/>/<see cref="MarkoutCellFormat.Noise"/> in addition to delta/unit),
    /// so goal-aware status words render in generated table columns as well as composite-card rows.
    /// </summary>
    public static string ToInlineString(IMarkoutCell? cell, in MarkoutCellFormat format)
    {
        if (cell is null)
            return string.Empty;
        var sw = new StringWriter();
        cell.FormatInline(sw, format);
        return sw.ToString();
    }

    /// <summary>
    /// As <see cref="ToInlineString(IMarkoutCell?, in MarkoutCellFormat)"/>, but augments the format
    /// with the <paramref name="writer"/>'s active glyph policy so a goal-annotated <see cref="Change{V}"/>
    /// rendered as an element-table column emits a polarity glyph on rich sinks (matching composite-card
    /// rows). Non-glyph and decomposing sinks are unaffected.
    /// </summary>
    public static string ToInlineString(IMarkoutCell? cell, in MarkoutCellFormat format, MarkoutWriter writer)
    {
        if (cell is null)
            return string.Empty;
        var sw = new StringWriter();
        cell.FormatInline(sw, writer.ApplyGlyphs(format));
        return sw.ToString();
    }
}

/// <summary>
/// Shared text helpers for composite-cell rendering. Numbers use invariant culture;
/// derived percentages round to the nearest whole percent; zero denominators render
/// the placeholder rather than <c>NaN</c>/<c>Inf</c>.
/// </summary>
internal static class CellText
{
    /// <summary>Placeholder for a derivation with a zero denominator (em dash).</summary>
    public const string Placeholder = "\u2014";

    /// <summary>Separator between the before/after halves of a <see cref="Change{V}"/>.</summary>
    public const string Arrow = " \u2192 ";

    /// <summary>Formats a number without a trailing <c>.0</c> for integral values.</summary>
    public static string Number(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return Placeholder;
        if (value == Math.Floor(value) && Math.Abs(value) < 1e15)
            return ((long)value).ToString(CultureInfo.InvariantCulture);
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    /// <summary>Formats a percentage for dense output: rounded whole number plus <c>%</c>.</summary>
    public static string Percent(double pct)
        => PercentNumber(pct) + "%";

    /// <summary>Formats a percentage for decomposed columns: rounded whole number, no <c>%</c>.</summary>
    public static string PercentNumber(double pct)
    {
        if (double.IsNaN(pct) || double.IsInfinity(pct))
            return Placeholder;
        var rounded = Math.Round(pct, MidpointRounding.AwayFromZero);
        return ((long)rounded).ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>Formats a signed percentage for a delta suffix (explicit <c>+</c> for gains).</summary>
    public static string SignedPercent(double pct)
    {
        if (double.IsNaN(pct) || double.IsInfinity(pct))
            return Placeholder;
        var text = PercentNumber(pct) + "%";
        return pct > 0 ? "+" + text : text;
    }

    /// <summary>Formats a signed number for an absolute delta suffix (explicit <c>+</c> for gains).</summary>
    public static string SignedNumber(double value)
    {
        var text = Number(value);
        if (text == Placeholder)
            return Placeholder;   // non-finite: bare placeholder, never "+—"
        return value > 0 ? "+" + text : text;
    }

    /// <summary>Renders a scalar comparison value; numeric types drop trailing <c>.0</c>.</summary>
    public static string Scalar(object? value) => value switch
    {
        null => string.Empty,
        double d => Number(d),
        float f => Number(f),
        // Format integral and decimal types directly to preserve full precision
        // (routing large long/ulong/decimal through double would round them).
        decimal m => m.ToString(CultureInfo.InvariantCulture),
        long or int or short or sbyte or byte or ushort or uint or ulong
            => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
        _ => value.ToString() ?? string.Empty
    };

    /// <summary>Attempts to interpret a scalar comparison value as a double for derivations.</summary>
    public static bool TryScalarDouble(object? value, out double result)
    {
        switch (value)
        {
            case double d: result = d; return true;
            case float f: result = f; return true;
            case long l: result = l; return true;
            case int i: result = i; return true;
            case short or byte or sbyte or ushort or uint or ulong or decimal:
                result = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return true;
            default:
                result = 0;
                return false;
        }
    }

    /// <summary>
    /// Computes a signed absolute delta (<c>After − Before</c>), exact for integral and decimal
    /// types (a large <c>long</c>/<c>decimal</c> difference must not round through <c>double</c>).
    /// Integral types subtract as <c>decimal</c> to avoid <c>long</c> wrap-around; a <c>decimal</c>
    /// overflow falls back to <c>double</c> rather than throwing.
    /// </summary>
    public static string AbsoluteDelta(object? before, object? after, bool signed)
    {
        switch (before, after)
        {
            case (long b, long a):
                return SignDecimal((decimal)a - (decimal)b, signed);
            case (ulong b, ulong a):
                return SignDecimal((decimal)a - (decimal)b, signed);
            case (decimal b, decimal a):
                try { return SignDecimal(a - b, signed); }
                catch (OverflowException) { break; } // fall through to the double path
        }

        if (TryScalarDouble(before, out var bd) && TryScalarDouble(after, out var ad))
        {
            var d = ad - bd;
            return signed ? SignedNumber(d) : Number(d);
        }
        return Placeholder;
    }

    private static string SignDecimal(decimal delta, bool signed)
    {
        var text = delta.ToString(CultureInfo.InvariantCulture);
        return signed && delta > 0 ? "+" + text : text;
    }

    /// <summary>
    /// Computes the exact <c>after − before</c> as a <see cref="decimal"/> for integral/decimal pairs
    /// (<see cref="long"/>/<see cref="ulong"/>/<see cref="decimal"/>) whose magnitudes can exceed
    /// <see cref="double"/>'s exact-integer range (2^53). Returns <c>false</c> for other types (the
    /// caller uses the <c>double</c> path); a <see cref="decimal"/> overflow also returns <c>false</c>.
    /// </summary>
    public static bool TryExactDelta(object? before, object? after, out decimal delta)
    {
        switch (before, after)
        {
            case (long b, long a):
                delta = (decimal)a - b;
                return true;
            case (ulong b, ulong a):
                delta = (decimal)a - b;
                return true;
            case (decimal b, decimal a):
                try { delta = a - b; return true; }
                catch (OverflowException) { break; }
        }

        delta = 0m;
        return false;
    }

    /// <summary>Combines a decomposition <paramref name="side"/> with a sub-field name as <c>{side}_{sub}</c>.</summary>
    public static string SideKey(string? side, string sub)
        => side is null ? sub : side + "_" + sub;
}
