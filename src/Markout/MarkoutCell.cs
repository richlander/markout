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
        => SignedNumber(value, null);

    /// <summary>
    /// Formats a signed number for an absolute delta suffix, applying an optional .NET numeric format
    /// string (e.g. <c>"N0"</c> for thousands grouping) to the <em>magnitude</em>; <c>null</c> keeps the
    /// default trailing-<c>.0</c>-trimmed formatting. Markout owns the sign — it prepends <c>+</c> for a
    /// gain and <c>-</c> for a loss — so the format never sees a signed value and cannot collide with
    /// (or double) the delta sign. The format therefore governs the magnitude only and should not carry
    /// its own sign sections or sign literals.
    /// </summary>
    public static string SignedNumber(double value, string? format)
    {
        var magnitude = format is null ? Number(Math.Abs(value)) : FormatNumber(Math.Abs(value), format);
        if (magnitude == Placeholder)
            return Placeholder;   // non-finite: bare placeholder, never "+—"
        return SignPrefix(value, signed: true) + magnitude;
    }

    /// <summary>
    /// The leading sign Markout prepends to a delta magnitude: <c>-</c> for a decrease and, when
    /// <paramref name="signed"/> is set, <c>+</c> for an increase (otherwise nothing). Markout is the
    /// sole authority for the delta sign; the numeric format applies to the magnitude alone.
    /// </summary>
    private static string SignPrefix(double value, bool signed)
        => value < 0 ? "-" : (signed && value > 0 ? "+" : string.Empty);

    /// <inheritdoc cref="SignPrefix(double, bool)"/>
    private static string SignPrefix(decimal value, bool signed)
        => value < 0 ? "-" : (signed && value > 0 ? "+" : string.Empty);

    /// <summary>
    /// Formats a finite numeric value with a .NET numeric format string (invariant culture); a
    /// non-finite value renders the placeholder rather than <c>NaN</c>/<c>Inf</c>.
    /// </summary>
    private static string FormatNumber(double value, string format)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return Placeholder;
        return value.ToString(format, CultureInfo.InvariantCulture);
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

    /// <summary>
    /// Renders a scalar comparison value applying an optional .NET numeric format string (e.g.
    /// <c>"N0"</c>); <c>null</c> defers to <see cref="Scalar(object?)"/>. A non-finite
    /// <see cref="double"/>/<see cref="float"/> renders the placeholder; every numeric type formats
    /// with invariant culture to preserve precision and stay locale-stable.
    /// </summary>
    public static string Scalar(object? value, string? format)
    {
        if (format is null)
            return Scalar(value);
        return value switch
        {
            null => string.Empty,
            double d => double.IsNaN(d) || double.IsInfinity(d) ? Placeholder : d.ToString(format, CultureInfo.InvariantCulture),
            float f => float.IsNaN(f) || float.IsInfinity(f) ? Placeholder : f.ToString(format, CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(format, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

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
        => AbsoluteDelta(before, after, signed, null);

    /// <summary>
    /// As <see cref="AbsoluteDelta(object?, object?, bool)"/>, but applies an optional .NET numeric
    /// format string (e.g. <c>"N0"</c> for thousands grouping) to the delta; <c>null</c> keeps the
    /// default formatting. The exact integral/decimal path formats the <see cref="decimal"/> delta
    /// directly so grouping applies without routing large values through <see cref="double"/>.
    /// </summary>
    public static string AbsoluteDelta(object? before, object? after, bool signed, string? format)
    {
        switch (before, after)
        {
            case (long b, long a):
                return SignDecimal((decimal)a - (decimal)b, signed, format);
            case (ulong b, ulong a):
                return SignDecimal((decimal)a - (decimal)b, signed, format);
            case (decimal b, decimal a):
                try { return SignDecimal(a - b, signed, format); }
                catch (OverflowException) { break; } // fall through to the double path
        }

        if (TryScalarDouble(before, out var bd) && TryScalarDouble(after, out var ad))
        {
            var d = ad - bd;
            var magnitude = format is null ? Number(Math.Abs(d)) : FormatNumber(Math.Abs(d), format);
            return magnitude == Placeholder ? Placeholder : SignPrefix(d, signed) + magnitude;
        }
        return Placeholder;
    }

    private static string SignDecimal(decimal delta, bool signed)
        => SignDecimal(delta, signed, null);

    private static string SignDecimal(decimal delta, bool signed, string? format)
    {
        var magnitude = format is null
            ? Math.Abs(delta).ToString(CultureInfo.InvariantCulture)
            : Math.Abs(delta).ToString(format, CultureInfo.InvariantCulture);
        return SignPrefix(delta, signed) + magnitude;
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
