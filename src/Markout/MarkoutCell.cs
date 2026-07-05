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
        return value > 0 ? "+" + text : text;
    }

    /// <summary>Renders a scalar comparison value; numeric types drop trailing <c>.0</c>.</summary>
    public static string Scalar(object? value) => value switch
    {
        null => string.Empty,
        double d => Number(d),
        float f => Number(f),
        long l => Number(l),
        int i => Number(i),
        short or byte or sbyte or ushort or uint or ulong or decimal => Number(Convert.ToDouble(value, CultureInfo.InvariantCulture)),
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

    /// <summary>Combines a decomposition <paramref name="side"/> with a sub-field name as <c>{side}_{sub}</c>.</summary>
    public static string SideKey(string? side, string sub)
        => side is null ? sub : side + "_" + sub;

    /// <summary>Combines a segment <paramref name="label"/> with a decomposition side as <c>{label}_{side}</c>.</summary>
    public static string LabelKey(string label, string? side)
        => side is null ? label : label + "_" + side;
}
