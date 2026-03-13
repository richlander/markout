namespace MarkdownTable.Formatting;

/// <summary>
/// Calculates optimal column widths for markdown pipe tables using
/// statistical analysis with percentile-based outlier handling.
/// </summary>
/// <remarks>
/// Implements the A/B/C/D algorithm from smooth-markdown-table:
/// <list type="bullet">
///   <item>A: Minimum constraint — header width + cell padding</item>
///   <item>B: Percentile width — Pth percentile of effective content lengths</item>
///   <item>C: Tolerance-adjusted — longest content within B × tolerance</item>
///   <item>D: Final target — max(A, B, C), capped at MaxColumnWidth</item>
/// </list>
/// Accumulated position tracking ensures earlier column overflows
/// propagate correctly to later columns.
/// </remarks>
public static class ColumnWidthCalculator
{
    private const int CellPadding = 2; // leading space + trailing space

    /// <summary>
    /// Calculates target column widths for the given table data.
    /// </summary>
    /// <param name="headers">Header row values.</param>
    /// <param name="rows">Data row values.</param>
    /// <param name="options">Formatting options.</param>
    /// <returns>Target width for each column (content width, excluding padding and pipes).</returns>
    public static int[] Calculate(string[] headers, IReadOnlyList<string[]> rows, TableFormatterOptions? options = null)
    {
        options ??= new TableFormatterOptions();

        if (options.Mode == CalculationMode.FullWidth)
            return CalculateFullWidth(headers, rows, options.MaxColumnWidth);

        if (options.AutoTune)
            return CalculateAutoTuned(headers, rows, options);

        return CalculateWithParameters(headers, rows, options.Percentile, options.Tolerance,
            options.ShadowThreshold, options.MaxColumnWidth);
    }

    /// <summary>
    /// Full-width calculation: each column gets the maximum content width across all rows.
    /// </summary>
    private static int[] CalculateFullWidth(string[] headers, IReadOnlyList<string[]> rows, int maxColumnWidth)
    {
        int columnCount = headers.Length;
        var targetWidths = new int[columnCount];

        for (int col = 0; col < columnCount; col++)
        {
            int maxWidth = headers[col].Length;
            for (int row = 0; row < rows.Count; row++)
            {
                if (col < rows[row].Length)
                    maxWidth = Math.Max(maxWidth, rows[row][col].Length);
            }
            targetWidths[col] = Math.Min(maxWidth, maxColumnWidth);
        }

        return targetWidths;
    }

    private static int[] CalculateWithParameters(string[] headers, IReadOnlyList<string[]> rows,
        double percentile, double tolerance, int shadowThreshold, int maxColumnWidth)
    {
        int columnCount = headers.Length;
        var targetWidths = new int[columnCount];

        // Build full table (header + data) for analysis
        int totalRows = 1 + rows.Count;
        var allWidths = new int[totalRows][];
        allWidths[0] = new int[columnCount];
        for (int col = 0; col < columnCount; col++)
            allWidths[0][col] = headers[col].Length;

        for (int row = 0; row < rows.Count; row++)
        {
            allWidths[row + 1] = new int[columnCount];
            for (int col = 0; col < columnCount; col++)
                allWidths[row + 1][col] = col < rows[row].Length ? rows[row][col].Length : 0;
        }

        // Track accumulated positions for overflow propagation
        var defaultEndPositions = new int[columnCount];
        var rowEndPositions = new int[totalRows][];
        for (int r = 0; r < totalRows; r++)
            rowEndPositions[r] = new int[columnCount];

        for (int col = 0; col < columnCount; col++)
        {
            bool lastColumn = col == columnCount - 1;

            // A: Minimum constraint — header must fit
            int headerWidth = allWidths[0][col];
            int A = headerWidth + CellPadding;
            int shadowValue = Math.Max(A, shadowThreshold);

            // Default start position for this column
            int defaultStart = col == 0 ? 0 : defaultEndPositions[col - 1] + 1; // +1 for pipe

            // Calculate effective lengths per row
            var effectiveLengths = new List<int>(totalRows);
            for (int row = 0; row < totalRows; row++)
            {
                int contentWidth = allWidths[row][col];
                int cellWidth = contentWidth + CellPadding;

                int rowStart = col == 0 ? 0 : rowEndPositions[row][col - 1] + 1;
                int rowEnd = rowStart + cellWidth;
                rowEndPositions[row][col] = rowEnd;

                // Accumulated position tracking:
                // Non-final columns: use later position (overflow pushes right)
                // Final column: use earlier position (enables trailing-edge alignment)
                int effectiveStart = !lastColumn
                    ? Math.Max(rowStart, defaultStart)
                    : Math.Min(rowStart, defaultStart);
                effectiveLengths.Add(rowEnd - effectiveStart);
            }

            effectiveLengths.Sort();

            // B: Statistical percentile width with shadow threshold
            int lastLength = effectiveLengths[^1];
            int B = lastLength <= shadowValue ? lastLength : 0;
            int C = 0;

            if (B == 0 && effectiveLengths.Count > 0)
            {
                int percentileIndex = Math.Min((int)(effectiveLengths.Count * percentile), effectiveLengths.Count - 1);
                B = effectiveLengths[percentileIndex];

                // C: Longest content within tolerance of B
                int toleranceLimit = (int)(B * tolerance);
                C = effectiveLengths.LastOrDefault(l => l <= toleranceLimit);
            }

            // D: Final target
            int D = Math.Max(Math.Max(A, B), C);
            D = Math.Min(D, maxColumnWidth);

            int defaultEnd = defaultStart + D;
            defaultEndPositions[col] = defaultEnd;

            // Content width = total width minus padding
            targetWidths[col] = D - CellPadding;

            // Update row end positions to reflect the planned width
            for (int row = 0; row < totalRows; row++)
            {
                int rowStart = col == 0 ? 0 : rowEndPositions[row][col - 1] + 1;
                int contentEnd = rowStart + allWidths[row][col] + CellPadding;
                // Use whichever is larger: content or planned width
                rowEndPositions[row][col] = Math.Max(contentEnd, defaultEnd);
            }
        }

        return targetWidths;
    }

    /// <summary>
    /// Hill-climbing auto-tune: iteratively adjusts percentile and tolerance
    /// to achieve perfect trailing-edge alignment.
    /// </summary>
    /// <remarks>
    /// Computes a baseline with default statistical parameters, then hill-climbs
    /// from there. Rejects candidates that expand the table width beyond
    /// the outlier threshold relative to baseline — this prevents outlier rows
    /// from inflating the entire table to full-width.
    /// Falls back to baseline statistical widths when no perfect alignment is found.
    /// </remarks>
    private static int[] CalculateAutoTuned(string[] headers, IReadOnlyList<string[]> rows,
        TableFormatterOptions options)
    {
        const int toleranceBumpsPerPercentile = 2;
        const double toleranceIncrement = 0.2;
        const double percentileIncrement = 0.2;
        const int maxAttempts = 4;
        const double outlierThreshold = 0.50;

        // Compute baseline with default statistical parameters
        var baseline = CalculateWithParameters(headers, rows,
            options.Percentile, options.Tolerance, options.ShadowThreshold, options.MaxColumnWidth);
        int baselineRowLength = CalculateRowLength(headers, baseline);

        double currentPercentile = options.Percentile;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            for (int bump = 0; bump < toleranceBumpsPerPercentile; bump++)
            {
                double currentTolerance = options.Tolerance + (bump * toleranceIncrement);
                var widths = CalculateWithParameters(headers, rows,
                    currentPercentile, currentTolerance, options.ShadowThreshold, options.MaxColumnWidth);

                if (HasPerfectAlignment(headers, rows, widths, outlierThreshold, baselineRowLength))
                    return widths;
            }

            currentPercentile += percentileIncrement;
        }

        // Modal expansion: detect bimodal columns and expand affordable ones
        var expanded = TryModalExpansion(headers, rows, baseline);
        if (expanded is not null)
            return expanded;

        // Fallback: return baseline statistical widths
        return baseline;
    }

    /// <summary>
    /// Detects bimodal column distributions and expands affordable second modes.
    /// </summary>
    /// <remarks>
    /// After hill-climbing fails to find perfect alignment, this method analyzes
    /// each column for bimodality by looking for gaps in the sorted cell lengths.
    /// If a column has a clear second mode (≥2 values above a gap ≥3) and the
    /// expansion cost is affordable (≤25 chars absolute, ≤3× current width),
    /// the column is widened to accommodate both modes.
    /// </remarks>
    private static int[]? TryModalExpansion(string[] headers, IReadOnlyList<string[]> rows, int[] baseline)
    {
        const int minGapSize = 3;
        const int minClusterSize = 2;
        const int maxAbsoluteExpansion = 25;
        const double maxRelativeFactor = 3.0;

        int columnCount = headers.Length;
        var expanded = new int[columnCount];
        bool anyExpanded = false;

        for (int col = 0; col < columnCount; col++)
        {
            expanded[col] = baseline[col];

            // Collect raw cell lengths for this column
            var lengths = new List<int>(1 + rows.Count) { headers[col].Length };
            for (int r = 0; r < rows.Count; r++)
                lengths.Add(col < rows[r].Length ? rows[r][col].Length : 0);

            lengths.Sort();

            // Find the largest gap where the lower value is at or below the baseline width
            int bestGap = 0;
            int bestGapIndex = -1;
            for (int i = 1; i < lengths.Count; i++)
            {
                int gap = lengths[i] - lengths[i - 1];
                if (gap > bestGap && lengths[i - 1] <= baseline[col])
                {
                    bestGap = gap;
                    bestGapIndex = i;
                }
            }

            if (bestGap < minGapSize || bestGapIndex < 0)
                continue;

            // Count values in the second mode (above the gap)
            int mode2Count = lengths.Count - bestGapIndex;
            if (mode2Count < minClusterSize)
                continue;

            int mode2Max = lengths[^1];
            int expansion = mode2Max - baseline[col];
            if (expansion <= 0)
                continue;

            // Affordability: absolute cap AND relative cap
            if (expansion <= maxAbsoluteExpansion && mode2Max <= maxRelativeFactor * baseline[col])
            {
                expanded[col] = mode2Max;
                anyExpanded = true;
            }
        }

        return anyExpanded ? expanded : null;
    }

    /// <summary>
    /// Checks whether the target widths achieve perfect trailing-edge alignment
    /// without impractical table expansion.
    /// </summary>
    /// <remarks>
    /// Compares the candidate table width against the baseline statistical width.
    /// If expanding beyond the outlier threshold, the alignment is rejected —
    /// this prevents a single outlier row from inflating the entire table.
    /// </remarks>
    private static bool HasPerfectAlignment(string[] headers, IReadOnlyList<string[]> rows,
        int[] targetWidths, double outlierThreshold, int baselineRowLength)
    {
        int headerRowLength = CalculateRowLength(headers, targetWidths);

        // Reject if expansion from baseline exceeds threshold
        if (headerRowLength > baselineRowLength * (1.0 + outlierThreshold))
            return false;

        for (int r = 0; r < rows.Count; r++)
        {
            int rowLength = CalculateRowLength(rows[r], targetWidths);
            if (rowLength != headerRowLength)
                return false;
        }

        return true;
    }

    private static int CalculateRowLength(string[] row, int[] targetWidths)
    {
        int length = 1; // leading pipe
        for (int col = 0; col < targetWidths.Length; col++)
        {
            int contentWidth = col < row.Length ? row[col].Length : 0;
            int cellWidth = Math.Max(contentWidth, targetWidths[col]) + CellPadding;
            length += cellWidth + 1; // +1 for trailing pipe
        }
        return length;
    }

    /// <summary>
    /// Core P/T statistical target calculation on sorted values.
    /// Same algorithm used for column widths — reusable for trailing pipe clustering.
    /// </summary>
    /// <param name="sortedValues">Values sorted ascending.</param>
    /// <param name="minimum">Floor value (e.g., header width or previous cluster target).</param>
    /// <param name="percentile">Percentile for baseline (0.0–1.0).</param>
    /// <param name="tolerance">Multiplier on percentile value to capture nearby values.</param>
    /// <param name="shadowThreshold">Below this, all values are kept as-is.</param>
    /// <returns>Statistical target that covers the main mode of values.</returns>
    public static int ComputeStatisticalTarget(
        List<int> sortedValues, int minimum,
        double percentile, double tolerance, int shadowThreshold)
    {
        if (sortedValues.Count == 0) return minimum;

        int shadowValue = Math.Max(minimum, shadowThreshold);
        int maxValue = sortedValues[^1];

        if (maxValue <= shadowValue)
            return maxValue;

        int pIndex = Math.Min(
            (int)(sortedValues.Count * percentile),
            sortedValues.Count - 1);
        int B = sortedValues[pIndex];
        int toleranceLimit = (int)(B * tolerance);

        int C = 0;
        for (int i = sortedValues.Count - 1; i >= 0; i--)
        {
            if (sortedValues[i] <= toleranceLimit)
            {
                C = sortedValues[i];
                break;
            }
        }

        return Math.Max(minimum, Math.Max(B, C));
    }
}
