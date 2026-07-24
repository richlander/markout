using System.Text.Json;
using Markout;

namespace Markout.Tests;

// Attribute-path model: [MarkoutGoal] on numeric Change<> cells drives derived direction/status.
[MarkoutSerializable(TitleProperty = nameof(Title))]
public class GoalAttrCard
{
    [MarkoutIgnore] public string Title => "Goal card";

    [MarkoutPropertyName("Failures"), MarkoutGoal(Goal.Lower)]
    public Change<int> Failures { get; set; }

    [MarkoutPropertyName("Fully raised"), MarkoutGoal(Goal.Higher)]
    public Change<int> FullyRaised { get; set; }

    [MarkoutPropertyName("Fidelity"), MarkoutGoal(Goal.Higher, 0.001)]
    public Change<double> Fidelity { get; set; }

    [MarkoutPropertyName("Changed bodies")]
    public Change<int> ChangedBodies { get; set; }
}

[MarkoutContext(typeof(GoalAttrCard))]
public partial class GoalAttrCardContext : MarkoutSerializerContext
{
}

// Table-column path: a [MarkoutGoal] Change<T> as a column of a generated element table.
public class GoalTableRow
{
    public string Name { get; set; } = "";

    [MarkoutGoal(Goal.Lower)]
    public Change<int> Failures { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class GoalTableCard
{
    [MarkoutIgnore] public string Title => "Goal table";

    [MarkoutSection(Name = "Rows")]
    public List<GoalTableRow> Rows { get; set; } = new();
}

[MarkoutContext(typeof(GoalTableCard))]
public partial class GoalTableCardContext : MarkoutSerializerContext
{
}

// Attribute path for Delta.Multiple and [MarkoutDeltaNoun].
[MarkoutSerializable(TitleProperty = nameof(Title))]
public class DeltaModesCard
{
    [MarkoutIgnore] public string Title => "Delta modes";

    [MarkoutPropertyName("Residual"), MarkoutDelta(Delta.Multiple)]
    public Change<int> Residual { get; set; }

    [MarkoutPropertyName("Tasks"), MarkoutDeltaNoun("solved")]
    public Change<Fraction> Tasks { get; set; }
}

[MarkoutContext(typeof(DeltaModesCard))]
public partial class DeltaModesCardContext : MarkoutSerializerContext
{
}

public class GoalAwareCellTests
{
    private static List<MarkoutField> Decompose(IMarkoutCell cell, MarkoutCellFormat format)
    {
        var fields = new List<MarkoutField>();
        cell.Decompose(fields, null, format);
        return fields;
    }

    private static string Inline(IMarkoutCell cell, MarkoutCellFormat format)
    {
        var sw = new StringWriter();
        cell.FormatInline(sw, format);
        return sw.ToString();
    }

    // --- Dense inline rendering (Change<V>.FormatInline) ---

    [Fact]
    public void Change_FormatInline_AppendsGoalStatusWord()
    {
        Assert.Equal("10 \u2192 3 (good)", Inline(new Change<long>(10, 3), new MarkoutCellFormat { Goal = Goal.Lower }));
        Assert.Equal("10 \u2192 3 (bad)", Inline(new Change<long>(10, 3), new MarkoutCellFormat { Goal = Goal.Higher }));
        Assert.Equal("5 \u2192 5 (neutral)", Inline(new Change<long>(5, 5), new MarkoutCellFormat { Goal = Goal.Higher }));
    }

    [Fact]
    public void Change_FormatInline_MergesDeltaAndGoalIntoSingleParen()
    {
        Assert.Equal("10 \u2192 3 (-7, good)", Inline(new Change<long>(10, 3), new MarkoutCellFormat(Delta.Absolute) { Goal = Goal.Lower }));
    }

    [Fact]
    public void Change_FormatInline_ContextGoal_LeavesRenderingUnchanged()
    {
        Assert.Equal("10 \u2192 3", Inline(new Change<long>(10, 3), new MarkoutCellFormat { Goal = Goal.Context }));
        Assert.Equal("10 \u2192 3 (-7)", Inline(new Change<long>(10, 3), new MarkoutCellFormat(Delta.Absolute)));
    }

    [Fact]
    public void Change_FormatInline_Composite_AppendsGoalStatusWord()
    {
        // Share magnitude = raw Value: 5056 -> 3129 decreased, Lower -> good.
        var s = Inline(new Change<Share>(new Share(5056, 21067), new Share(3129, 13037)),
            new MarkoutCellFormat { Goal = Goal.Lower });
        Assert.StartsWith("5056", s);
        Assert.EndsWith(" (good)", s);
    }

    [Fact]
    public void MultiSourceRow_CompositeCell_RendersGoalWordInPivotedMarkdown()
    {
        var rows = new[]
        {
            new MultiSourceRow("output tok",
                new Source("opus", new Change<Share>(new Share(5056, 21067), new Share(3129, 13037)),
                    new MarkoutCellFormat { Goal = Goal.Lower })),
        };
        var writer = new MarkoutWriter(new MarkdownFormatter());
        writer.WriteMultiSourceTable("Metric", rows);
        var md = writer.ToString();

        Assert.Contains("(good)", md);
    }

    // --- Direct-cell derivation matrix (structural direction × goal-applied polarity) ---

    [Theory]
    // Increased / Introduced
    [InlineData(0, 7, "lower", "introduced", "bad")]
    [InlineData(0, 7, "higher", "introduced", "good")]
    [InlineData(5, 9, "lower", "increased", "bad")]
    [InlineData(5, 9, "higher", "increased", "good")]
    // Decreased / Resolved
    [InlineData(7, 0, "lower", "resolved", "good")]
    [InlineData(7, 0, "higher", "resolved", "bad")]
    [InlineData(9, 5, "lower", "decreased", "good")]
    [InlineData(9, 5, "higher", "decreased", "bad")]
    // Unchanged is always neutral regardless of goal
    [InlineData(5, 5, "lower", "unchanged", "neutral")]
    [InlineData(5, 5, "higher", "unchanged", "neutral")]
    // Negative-involved crossings fall through to sign-based direction (polarity stays algebraic)
    [InlineData(0, -5, "higher", "decreased", "bad")]
    [InlineData(0, -5, "lower", "decreased", "good")]
    [InlineData(-5, 0, "higher", "increased", "good")]
    [InlineData(-5, 0, "lower", "increased", "bad")]
    [InlineData(-3, -5, "lower", "decreased", "good")]
    [InlineData(-5, -3, "higher", "increased", "good")]
    public void Change_WithGoal_DerivesDirectionAndPolarity(int before, int after, string goal, string direction, string status)
    {
        var format = new MarkoutCellFormat { Goal = goal == "lower" ? Goal.Lower : Goal.Higher };
        var fields = Decompose(new Change<long>(before, after), format);

        Assert.Equal(direction, fields.Single(f => f.Key == "direction").Value);
        Assert.Equal(status, fields.Single(f => f.Key == "status").Value);
    }

    [Fact]
    public void Change_ContextGoal_EmitsNeitherDirectionNorStatus()
    {
        var fields = Decompose(new Change<long>(45, 46), new MarkoutCellFormat { Goal = Goal.Context });
        Assert.DoesNotContain(fields, f => f.Key is "direction" or "status");
    }

    [Fact]
    public void Change_NoiseTolerance_ClassifiesSubThresholdAsUnchanged()
    {
        // 87.30% -> 87.31% with a 0.001 tolerance is within noise -> unchanged/neutral.
        var format = new MarkoutCellFormat { Goal = Goal.Higher, Noise = 0.001 };
        var fields = Decompose(new Change<double>(0.8730, 0.8731), format);

        Assert.Equal("unchanged", fields.Single(f => f.Key == "direction").Value);
        Assert.Equal("neutral", fields.Single(f => f.Key == "status").Value);
    }

    [Fact]
    public void Change_Goal_ExactClassification_BeyondDoublePrecision()
    {
        // 2^53 and 2^53+1 are the same double; the exact decimal path keeps them distinct (#140).
        var up = Decompose(new Change<long>(9007199254740992L, 9007199254740993L),
            new MarkoutCellFormat { Goal = Goal.Higher });
        Assert.Equal("increased", up.Single(f => f.Key == "direction").Value);
        Assert.Equal("good", up.Single(f => f.Key == "status").Value);

        var down = Decompose(new Change<long>(9007199254740993L, 9007199254740992L),
            new MarkoutCellFormat { Goal = Goal.Lower });
        Assert.Equal("decreased", down.Single(f => f.Key == "direction").Value);
        Assert.Equal("good", down.Single(f => f.Key == "status").Value);
    }

    [Fact]
    public void Change_Goal_ExactClassification_Decimal()
    {
        // decimal.MaxValue and MaxValue-1 collapse to the same double; exact decimal keeps them apart.
        var fields = Decompose(new Change<decimal>(decimal.MaxValue, decimal.MaxValue - 1m),
            new MarkoutCellFormat { Goal = Goal.Lower });
        Assert.Equal("decreased", fields.Single(f => f.Key == "direction").Value);
        Assert.Equal("good", fields.Single(f => f.Key == "status").Value);
    }

    [Fact]
    public void Change_Goal_ExactClassification_HonorsNoiseBand()
    {
        // Exact delta of 1 exceeds a 0.5 tolerance -> increased (not collapsed to unchanged).
        var fields = Decompose(new Change<long>(9007199254740992L, 9007199254740993L),
            new MarkoutCellFormat { Goal = Goal.Higher, Noise = 0.5 });
        Assert.Equal("increased", fields.Single(f => f.Key == "direction").Value);
        Assert.Equal("good", fields.Single(f => f.Key == "status").Value);
    }

    [Fact]
    public void Change_Goal_ExactNoiseBand_ComparedInDecimalDomain()
    {
        // Exact delta 2^53+1 exceeds a 2^53 tolerance; the boundary must not round back to double.
        var fields = Decompose(new Change<long>(0L, 9007199254740993L),
            new MarkoutCellFormat { Goal = Goal.Lower, Noise = 9007199254740992d });
        Assert.Equal("introduced", fields.Single(f => f.Key == "direction").Value);
        Assert.Equal("bad", fields.Single(f => f.Key == "status").Value);
    }

    [Fact]
    public void Change_Goal_ExactNoiseBand_InclusiveAtLargeIntegerBoundary()
    {
        // Exact delta exactly equal to a large integer tolerance is within the inclusive band.
        var fields = Decompose(new Change<long>(0L, 9007199254740991L),
            new MarkoutCellFormat { Goal = Goal.Higher, Noise = 9007199254740991d });
        Assert.Equal("unchanged", fields.Single(f => f.Key == "direction").Value);
        Assert.Equal("neutral", fields.Single(f => f.Key == "status").Value);
    }

    [Fact]
    public void Change_Goal_ExactNoiseBand_InclusiveAboveE18()
    {
        // Integer tolerance above 1e18 (still within long range) stays exact and inclusive.
        var fields = Decompose(new Change<long>(0L, 1000000000000000128L),
            new MarkoutCellFormat { Goal = Goal.Higher, Noise = 1000000000000000128d });
        Assert.Equal("unchanged", fields.Single(f => f.Key == "direction").Value);
        Assert.Equal("neutral", fields.Single(f => f.Key == "status").Value);
    }

    [Fact]
    public void Change_Goal_ExactNoiseBand_InclusiveAboveLongRange()
    {
        // Integer ulong tolerance above long.MaxValue stays exact and inclusive (type-agnostic path).
        var fields = Decompose(new Change<ulong>(0UL, 10000000000000002048UL),
            new MarkoutCellFormat { Goal = Goal.Higher, Noise = 10000000000000002048d });
        Assert.Equal("unchanged", fields.Single(f => f.Key == "direction").Value);
        Assert.Equal("neutral", fields.Single(f => f.Key == "status").Value);
    }

    [Fact]
    public void Change_Goal_SmallValues_UnchangedBehaviorPreserved()
    {
        // The exact path must match the double path for ordinary values.
        var fields = Decompose(new Change<long>(5, 5), new MarkoutCellFormat { Goal = Goal.Higher });
        Assert.Equal("unchanged", fields.Single(f => f.Key == "direction").Value);
        Assert.Equal("neutral", fields.Single(f => f.Key == "status").Value);
    }

    [Fact]
    public void Change_Goal_ExactNoiseBand_LargeFractionalTolerance()
    {
        // A large fractional tolerance is reconstructed exactly; the exact delta exceeds it -> introduced.
        var fields = Decompose(new Change<decimal>(0m, 4503599627370498m),
            new MarkoutCellFormat { Goal = Goal.Lower, Noise = 4503599627370495.5d });
        Assert.Equal("introduced", fields.Single(f => f.Key == "direction").Value);
        Assert.Equal("bad", fields.Single(f => f.Key == "status").Value);
    }

    [Fact]
    public void Change_Goal_ExactNoiseBand_SmallNonRoundTolerance()
    {
        // A small tolerance that is a double just below 2 must not round up to 2: delta 2 exceeds it.
        var fields = Decompose(new Change<long>(0L, 2L),
            new MarkoutCellFormat { Goal = Goal.Higher, Noise = 1.9999999999999998d });
        Assert.Equal("introduced", fields.Single(f => f.Key == "direction").Value);
        Assert.Equal("good", fields.Single(f => f.Key == "status").Value);
    }

    [Fact]
    public void Change_Goal_ExactNoiseBand_FractionalDecimalDelta()
    {
        // A fractional decimal delta just above a large integer tolerance must not round into the band.
        var fields = Decompose(new Change<decimal>(0m, 9007199254740991.1m),
            new MarkoutCellFormat { Goal = Goal.Lower, Noise = 9007199254740991d });
        Assert.Equal("introduced", fields.Single(f => f.Key == "direction").Value);
        Assert.Equal("bad", fields.Single(f => f.Key == "status").Value);
    }

    [Fact]
    public void Change_GoalAndDelta_EmitBothDerivedAxesAndDelta()
    {
        var format = new MarkoutCellFormat(Delta.Absolute) { Goal = Goal.Lower };
        var fields = Decompose(new Change<long>(10, 3), format);

        Assert.Equal("10", fields[0].Value); // before
        Assert.Equal("3", fields[1].Value);  // after
        Assert.Equal("-7", fields.Single(f => f.Key == "deltaAbs").Value);
        Assert.Equal("decreased", fields.Single(f => f.Key == "direction").Value);
        Assert.Equal("good", fields.Single(f => f.Key == "status").Value);
    }

    // --- MetricChange<T> runtime path ---

    [Fact]
    public void MetricChange_Goal_RendersDenseMarkdown_GoalAndPolarityGlyphs()
    {
        var writer = new MarkoutWriter(new MarkdownFormatter());
        writer.WriteMetricChangeTable(new[]
        {
            new MetricChange<int>("Failures", 0, 7) { Goal = Goal.Lower },
            new MetricChange<int>("Fully raised", 40, 55) { Goal = Goal.Higher },
            new MetricChange<int>("Changed bodies", 45, 46), // Context -> no marker, no status
        });
        var md = writer.ToString();

        // Goal glyph on the label; derived polarity glyph inline in the Change cell; no Status column.
        Assert.Contains("| Metric | Change | Target |", md);
        Assert.Contains("| Failures \u2193 | 0 \u2192 7 \u2717 | - |", md);
        Assert.Contains("| Fully raised \u2191 | 40 \u2192 55 \u2713 | - |", md);
        Assert.Contains("| Changed bodies | 45 \u2192 46 | - |", md);
    }

    [Fact]
    public void MetricChange_Goal_ConfigurableGlyphs_Override()
    {
        var options = new MarkoutWriterOptions
        {
            Glyphs = new MarkoutGlyphs { GoalLower = "v", GoalHigher = "^", StatusGood = "OK", StatusBad = "X" },
        };
        var writer = new MarkoutWriter(new MarkdownFormatter(), options);
        writer.WriteMetricChangeTable(new[]
        {
            new MetricChange<int>("Failures", 0, 7) { Goal = Goal.Lower },
            new MetricChange<int>("Fully raised", 40, 55) { Goal = Goal.Higher },
        });
        var md = writer.ToString();

        Assert.Contains("| Failures v | 0 \u2192 7 X | - |", md);
        Assert.Contains("| Fully raised ^ | 40 \u2192 55 OK | - |", md);
    }

    [Fact]
    public void MetricChange_Goal_NonGlyphFormatter_KeepsAsciiMarkerAndWord()
    {
        // PlainTextFormatter renders tables but is not IGlyphFormatter (and does not decompose),
        // so it keeps the ASCII (-)/(+) label marker and the status word.
        var writer = new MarkoutWriter(new PlainTextFormatter());
        writer.WriteMetricChangeTable(new[]
        {
            new MetricChange<int>("Failures", 0, 7) { Goal = Goal.Lower },
            new MetricChange<int>("Fully raised", 40, 55) { Goal = Goal.Higher },
        });
        var text = writer.ToString();

        Assert.Contains("Failures (-)", text);
        Assert.Contains("(bad)", text);
        Assert.Contains("Fully raised (+)", text);
        Assert.Contains("(good)", text);
        Assert.DoesNotContain("\u2193", text);
        Assert.DoesNotContain("\u2717", text);
    }

    [Fact]
    public void MetricChange_CustomStatusLabel_StaysWordEvenWithGlyphs()
    {
        var writer = new MarkoutWriter(new MarkdownFormatter());
        writer.WriteMetricChangeTable(new[]
        {
            new MetricChange<int>("Failures", 0, 7) { Goal = Goal.Lower, StatusLabel = "regression" },
        });
        var md = writer.ToString();

        // A caller-supplied custom label is not an enum polarity, so it stays a parenthesized word.
        Assert.Contains("| Failures \u2193 | 0 \u2192 7 (regression) | - |", md);
    }

    [Fact]
    public void MetricChange_Goal_InlineDisabled_DerivedStatusInLegacyColumn()
    {
        var writer = new MarkoutWriter(new MarkdownFormatter(),
            new MarkoutWriterOptions { InlineGoalStatus = false });
        writer.WriteMetricChangeTable(new[]
        {
            new MetricChange<int>("Failures", 0, 7) { Goal = Goal.Lower },
            new MetricChange<int>("Fully raised", 40, 55) { Goal = Goal.Higher },
        });
        var md = writer.ToString();

        Assert.Contains("| Failures | 0 \u2192 7 | - | bad |", md);
        Assert.Contains("| Fully raised | 40 \u2192 55 | - | good |", md);
    }

    [Fact]
    public void MetricChange_Goal_EmitsDirectionAndStatusInJsonl()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter(),
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, OmitEmptyJsonFields = true });
        writer.WriteMetricChangeTable(new[]
        {
            new MetricChange<int>("Failures", 0, 7) { Goal = Goal.Lower },
            new MetricChange<int>("Changed bodies", 45, 46), // Context: no direction/status
        });
        var lines = sw.ToString().ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var failures = JsonDocument.Parse(lines[0]).RootElement;
        Assert.Equal("introduced", failures.GetProperty("direction").GetString());
        Assert.Equal("bad", failures.GetProperty("status").GetString());

        var changed = JsonDocument.Parse(lines[1]).RootElement;
        Assert.False(changed.TryGetProperty("direction", out _));
        Assert.False(changed.TryGetProperty("status", out _));
    }

    [Fact]
    public void MetricChange_SameCrossing_FlipsPolarityByGoal()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter(),
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl });
        writer.WriteMetricChangeTable(new[]
        {
            new MetricChange<int>("Pass bugs", 0, 7) { Goal = Goal.Lower },
            new MetricChange<int>("Fully raised", 0, 7) { Goal = Goal.Higher },
        });
        var lines = sw.ToString().ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var bad = JsonDocument.Parse(lines[0]).RootElement;
        var good = JsonDocument.Parse(lines[1]).RootElement;

        // Same structural direction, opposite polarity.
        Assert.Equal("introduced", bad.GetProperty("direction").GetString());
        Assert.Equal("introduced", good.GetProperty("direction").GetString());
        Assert.Equal("bad", bad.GetProperty("status").GetString());
        Assert.Equal("good", good.GetProperty("status").GetString());
    }

    [Fact]
    public void MetricChange_CallerStatus_OverridesDerivedPolarity_ButDirectionStillDerived()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter(),
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl });
        // Goal says decrease->good, but the caller asserts a domain-threshold "warning".
        writer.WriteMetricChangeTable(new[]
        {
            new MetricChange<int>("Failures", 10, 3, Status: GateStatus.Warning, StatusLabel: "near-threshold") { Goal = Goal.Lower },
        });
        var row = JsonDocument.Parse(sw.ToString().Trim()).RootElement;

        Assert.Equal("decreased", row.GetProperty("direction").GetString()); // still derived
        Assert.Equal("near-threshold", row.GetProperty("status").GetString()); // caller override wins
    }

    [Fact]
    public void Change_NonFiniteInput_EmitsNoDirectionOrStatus()
    {
        // NaN/Infinity render as the — placeholder; goal derivation is omitted rather than guessed.
        Assert.DoesNotContain(Decompose(new Change<double>(double.NaN, 5), new MarkoutCellFormat { Goal = Goal.Higher }),
            f => f.Key is "direction" or "status");
        Assert.DoesNotContain(Decompose(new Change<double>(5, double.PositiveInfinity), new MarkoutCellFormat { Goal = Goal.Lower }),
            f => f.Key is "direction" or "status");
    }

    // --- Composite cells derive from a comparable magnitude (runtime Source path) ---

    [Fact]
    public void MultiSourceRow_CompositeCells_DeriveDirectionAndStatusFromMagnitude()
    {
        var rows = new[]
        {
            // Share magnitude = raw Value: 5056 -> 3129 tokens, Lower -> good.
            new MultiSourceRow("output tok",
                new Source("opus", new Change<Share>(new Share(5056, 21067), new Share(3129, 13037)),
                    new MarkoutCellFormat { Goal = Goal.Lower })),
            // Fraction magnitude = Count/Total rate: 20/24 -> 24/24 rate up, Higher -> good.
            new MultiSourceRow("tasks correct",
                new Source("opus", new Change<Fraction>(new Fraction(20, 24), new Fraction(24, 24)),
                    new MarkoutCellFormat { Goal = Goal.Higher })),
            // Percent magnitude = Part/Whole: 80% -> 95%, Higher -> good.
            new MultiSourceRow("read grounding",
                new Source("opus", new Change<Percent>(new Percent(80, 100), new Percent(95, 100)),
                    new MarkoutCellFormat { Goal = Goal.Higher })),
        };

        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter(),
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, OmitEmptyJsonFields = true });
        writer.WriteMultiSourceTable("Metric", rows);
        var lines = sw.ToString().ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var tok = JsonDocument.Parse(lines[0]).RootElement;
        Assert.Equal("decreased", tok.GetProperty("opus_direction").GetString());
        Assert.Equal("good", tok.GetProperty("opus_status").GetString());
        // The composite's own decomposed fields are still present.
        Assert.Equal("5056", tok.GetProperty("opus_before_value").GetString());

        var tasks = JsonDocument.Parse(lines[1]).RootElement;
        Assert.Equal("increased", tasks.GetProperty("opus_direction").GetString());
        Assert.Equal("good", tasks.GetProperty("opus_status").GetString());

        var read = JsonDocument.Parse(lines[2]).RootElement;
        Assert.Equal("increased", read.GetProperty("opus_direction").GetString());
        Assert.Equal("good", read.GetProperty("opus_status").GetString());
    }

    [Fact]
    public void MultiSourceRow_SegmentsCell_ContextGoal_NoDerivedDirection()
    {
        // With NO goal (Context), a Segments breakdown still derives nothing (opt-out).
        var rows = new[]
        {
            new MultiSourceRow("tool calls",
                new Source("opus", new Change<Segments>(
                    new Segments(new Segment("web", 21), new Segment("other", 171)),
                    new Segments(new Segment("web", 10), new Segment("other", 183))))),
        };

        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter(),
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, OmitEmptyJsonFields = true });
        writer.WriteMultiSourceTable("Metric", rows);
        var row = JsonDocument.Parse(sw.ToString().Trim()).RootElement;

        Assert.False(row.TryGetProperty("opus_direction", out _));
        Assert.False(row.TryGetProperty("opus_status", out _));
    }

    // --- Segments aggregate goal magnitude (sum of parts) ---

    [Fact]
    public void Change_Segments_WithGoal_DerivesFromSumTotal_AndKeepsParts()
    {
        // Archaeology-style: 14/7 -> 0/0, sum 21 -> 0, Goal.Lower -> resolved/good.
        var cell = new Change<Segments>(
            new Segments(new Segment("cache", 14), new Segment("nuget_org", 7)),
            new Segments(new Segment("cache", 0), new Segment("nuget_org", 0)));
        var fields = Decompose(cell, new MarkoutCellFormat { Goal = Goal.Lower });

        // Parts still decompose.
        Assert.Equal("14", fields.Single(f => f.Key == "before_cache").Value);
        Assert.Equal("7", fields.Single(f => f.Key == "before_nuget_org").Value);
        Assert.Equal("0", fields.Single(f => f.Key == "after_cache").Value);
        // Total-derived axes.
        Assert.Equal("resolved", fields.Single(f => f.Key == "direction").Value);
        Assert.Equal("good", fields.Single(f => f.Key == "status").Value);
    }

    [Fact]
    public void Change_Segments_ConstantSum_IsUnchanged()
    {
        // Parts shift but the total is constant -> Unchanged/neutral (honest, if uninformative).
        var cell = new Change<Segments>(
            new Segments(new Segment("a", 3), new Segment("b", 7)),   // sum 10
            new Segments(new Segment("a", 6), new Segment("b", 4)));  // sum 10
        var fields = Decompose(cell, new MarkoutCellFormat { Goal = Goal.Lower });

        Assert.Equal("unchanged", fields.Single(f => f.Key == "direction").Value);
        Assert.Equal("neutral", fields.Single(f => f.Key == "status").Value);
    }

    [Fact]
    public void Change_Segments_WithGoal_RendersDenseInlineWord()
    {
        // Free dense rendering via the IGoalMagnitude seam: 14/7 -> 0/0 (good).
        var s = Inline(new Change<Segments>(
            new Segments(new Segment("cache", 14), new Segment("nuget_org", 7)),
            new Segments(new Segment("cache", 0), new Segment("nuget_org", 0))),
            new MarkoutCellFormat { Goal = Goal.Lower });

        Assert.Equal("14/7 \u2192 0/0 (good)", s);
    }

    [Fact]
    public void MultiSourceRow_SegmentsCell_WithGoal_DerivesTotalAxes()
    {
        var rows = new[]
        {
            new MultiSourceRow("failure buckets",
                new Source("baseline",
                    new Change<Segments>(
                        new Segments(new Segment("new_body_missing", 4), new Segment("old_body_missing", 3)),
                        new Segments(new Segment("new_body_missing", 0), new Segment("old_body_missing", 0))),
                    new MarkoutCellFormat { Goal = Goal.Lower })),
        };

        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter(),
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, OmitEmptyJsonFields = true });
        writer.WriteMultiSourceTable("Metric", rows);
        var row = JsonDocument.Parse(sw.ToString().Trim()).RootElement;

        Assert.Equal("4", row.GetProperty("baseline_before_new_body_missing").GetString());
        Assert.Equal("resolved", row.GetProperty("baseline_direction").GetString());
        Assert.Equal("good", row.GetProperty("baseline_status").GetString());
    }

    [Fact]
    public void MultiSourceRow_UndefinedRatioComposite_OmitsDirectionAndStatus()
    {
        // A zero-denominator Percent/Fraction renders — ; its magnitude is undefined (NaN),
        // so no direction/status is derived (rather than a synthetic 0).
        var rows = new[]
        {
            new MultiSourceRow("pct",
                new Source("opus", new Change<Percent>(new Percent(5, 0), new Percent(1, 2)),
                    new MarkoutCellFormat { Goal = Goal.Higher })),
            new MultiSourceRow("frac",
                new Source("opus", new Change<Fraction>(new Fraction(3, 0), new Fraction(2, 4)),
                    new MarkoutCellFormat { Goal = Goal.Higher })),
        };

        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter(),
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, OmitEmptyJsonFields = true });
        writer.WriteMultiSourceTable("Metric", rows);
        var lines = sw.ToString().ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var row = JsonDocument.Parse(line).RootElement;
            Assert.False(row.TryGetProperty("opus_direction", out _));
            Assert.False(row.TryGetProperty("opus_status", out _));
        }
    }

    // --- Delta.Multiple (slice 2) ---

    [Fact]
    public void Change_DeltaMultiple_RendersFactorAndDirectionWord()
    {
        Assert.Equal("15 \u2192 5 (3\u00d7 fewer)", Inline(new Change<long>(15, 5), new MarkoutCellFormat(Delta.Multiple)));
        Assert.Equal("5 \u2192 15 (3\u00d7 more)", Inline(new Change<long>(5, 15), new MarkoutCellFormat(Delta.Multiple)));
        Assert.Equal("15 \u2192 6 (2.5\u00d7 fewer)", Inline(new Change<long>(15, 6), new MarkoutCellFormat(Delta.Multiple)));
    }

    [Fact]
    public void Change_DeltaMultiple_ZeroEndpoint_RendersPlaceholder()
    {
        Assert.Equal("15 \u2192 0 (\u2014)", Inline(new Change<long>(15, 0), new MarkoutCellFormat(Delta.Multiple)));
    }

    [Fact]
    public void Change_DeltaMultiple_NonFinite_RendersPlaceholderWithoutWord()
    {
        var s = Inline(new Change<double>(double.PositiveInfinity, 5), new MarkoutCellFormat(Delta.Multiple));
        Assert.EndsWith("(\u2014)", s);
        Assert.DoesNotContain("\u00d7", s);   // no "—× fewer"
        Assert.DoesNotContain("fewer", s);
    }

    [Fact]
    public void Change_DeltaMultiple_AlignedGoal_SuppressesRedundantStatus()
    {
        // fewer/more already conveys the aligned (good) polarity -> omit "good" (#141).
        Assert.Equal("15 \u2192 5 (3\u00d7 fewer)",
            Inline(new Change<long>(15, 5), new MarkoutCellFormat(Delta.Multiple) { Goal = Goal.Lower }));
        Assert.Equal("5 \u2192 15 (3\u00d7 more)",
            Inline(new Change<long>(5, 15), new MarkoutCellFormat(Delta.Multiple) { Goal = Goal.Higher }));
    }

    [Fact]
    public void Change_DeltaMultiple_ConflictingGoal_KeepsStatus()
    {
        // The word conflicts with the goal -> keep "bad" (it adds information).
        Assert.Equal("5 \u2192 15 (3\u00d7 more, bad)",
            Inline(new Change<long>(5, 15), new MarkoutCellFormat(Delta.Multiple) { Goal = Goal.Lower }));
        Assert.Equal("15 \u2192 5 (3\u00d7 fewer, bad)",
            Inline(new Change<long>(15, 5), new MarkoutCellFormat(Delta.Multiple) { Goal = Goal.Higher }));
    }

    [Fact]
    public void Change_DeltaMultiple_ZeroEndpoint_KeepsStatus_NoWord()
    {
        // No rendered multiple phrase (placeholder) -> keep the status word.
        Assert.Equal("15 \u2192 0 (\u2014, good)",
            Inline(new Change<long>(15, 0), new MarkoutCellFormat(Delta.Multiple) { Goal = Goal.Lower }));
    }

    [Fact]
    public void Change_NonMultipleDelta_KeepsGoalStatus()
    {
        // Suppression is specific to Delta.Multiple; Absolute/Percent still show the status word.
        Assert.Equal("15 \u2192 5 (-10, good)",
            Inline(new Change<long>(15, 5), new MarkoutCellFormat(Delta.Absolute) { Goal = Goal.Lower }));
    }

    [Fact]
    public void Change_DeltaMultiple_DecomposesFactor()
    {
        var fields = Decompose(new Change<long>(15, 5), new MarkoutCellFormat(Delta.Multiple));
        Assert.Equal("3", fields.Single(f => f.Key == "deltaMultiple").Value);
    }

    [Fact]
    public void Change_DeltaMultiple_AlignedGoal_KeepsStructuredStatus()
    {
        // #141 suppresses only the Markdown status word; structured output keeps direction/status.
        var fields = Decompose(new Change<long>(15, 5), new MarkoutCellFormat(Delta.Multiple) { Goal = Goal.Lower });
        Assert.Equal("3", fields.Single(f => f.Key == "deltaMultiple").Value);
        Assert.Equal("decreased", fields.Single(f => f.Key == "direction").Value);
        Assert.Equal("good", fields.Single(f => f.Key == "status").Value);
    }

    // --- Delta-noun (slice 3) ---

    [Fact]
    public void Change_DeltaNoun_Scalar_RendersSignedDeltaWithNoun()
    {
        Assert.Equal("4 \u2192 6 (+2 solved)", Inline(new Change<long>(4, 6), new MarkoutCellFormat { DeltaNoun = "solved" }));
        Assert.Equal("6 \u2192 4 (-2 solved)", Inline(new Change<long>(6, 4), new MarkoutCellFormat { DeltaNoun = "solved" }));
    }

    [Fact]
    public void Change_DeltaNoun_MergesWithGoalStatus()
    {
        Assert.Equal("4 \u2192 6 (+2 solved, good)",
            Inline(new Change<long>(4, 6), new MarkoutCellFormat { DeltaNoun = "solved", Goal = Goal.Higher }));
    }

    [Fact]
    public void Change_DeltaNoun_Fraction_UsesCountDelta()
    {
        // Fraction delta-noun is on the numerator (Count): 4/6 -> 6/6 => +2 solved.
        Assert.Equal("4/6 \u2192 6/6 (+2 solved)",
            Inline(new Change<Fraction>(new Fraction(4, 6), new Fraction(6, 6)), new MarkoutCellFormat { DeltaNoun = "solved" }));
    }

    [Fact]
    public void Change_DeltaNoun_Fraction_WithGoal_MergesRatioStatus()
    {
        // Ratio 0.667 -> 1.0 increased, Higher -> good; noun on Count delta.
        Assert.Equal("4/6 \u2192 6/6 (+2 solved, good)",
            Inline(new Change<Fraction>(new Fraction(4, 6), new Fraction(6, 6)),
                new MarkoutCellFormat { DeltaNoun = "solved", Goal = Goal.Higher }));
    }

    [Fact]
    public void Change_DeltaNoun_Share_UsesValueDelta()
    {
        var s = Inline(new Change<Share>(new Share(10, 20), new Share(7, 20)), new MarkoutCellFormat { DeltaNoun = "tokens" });
        Assert.EndsWith("(-3 tokens)", s);
    }

    [Fact]
    public void Change_DeltaNoun_Scalar_ExactForLargeIntegers()
    {
        // 2^53 and 2^53+1 are indistinguishable as double; the exact delta path keeps them apart.
        var s = Inline(new Change<long>(9007199254740992L, 9007199254740993L), new MarkoutCellFormat { DeltaNoun = "solved" });
        Assert.EndsWith("(+1 solved)", s);
    }

    [Fact]
    public void Change_DeltaNoun_NonFinite_OmitsNoun()
    {
        var s = Inline(new Change<Fraction>(new Fraction(double.NaN, 6), new Fraction(6, 6)),
            new MarkoutCellFormat { DeltaNoun = "solved" });
        Assert.DoesNotContain("solved", s);   // no "— solved"
    }

    [Fact]
    public void Change_DeltaNoun_PositiveInfinity_OmitsNoun()
    {
        // +Infinity delta must render the bare placeholder, never "+— solved".
        var s = Inline(new Change<double>(5, double.PositiveInfinity), new MarkoutCellFormat { DeltaNoun = "solved" });
        Assert.DoesNotContain("solved", s);
        Assert.DoesNotContain("+\u2014", s);
    }

    [Fact]
    public void Change_DeltaNoun_DecomposesToCountAndNoun()
    {
        // Delta-noun is now structured too (reconstructable contract): scalar count delta + the noun.
        var fields = Decompose(new Change<long>(4, 6), new MarkoutCellFormat { DeltaNoun = "solved" });
        Assert.Equal("4", fields.Single(f => f.Key == "before").Value);
        Assert.Equal("6", fields.Single(f => f.Key == "after").Value);
        Assert.Equal("2", fields.Single(f => f.Key == "deltaCount").Value);
        Assert.Equal("solved", fields.Single(f => f.Key == "deltaNoun").Value);
    }

    [Fact]
    public void Change_DeltaNoun_Fraction_DecomposesCountDeltaAndNoun()
    {
        // Composite delta-noun: count delta from IDeltaCountable + the noun, alongside the parts.
        var fields = Decompose(new Change<Fraction>(new Fraction(4, 6), new Fraction(6, 6)),
            new MarkoutCellFormat { DeltaNoun = "solved" });
        Assert.Equal("4", fields.Single(f => f.Key == "before_count").Value);
        Assert.Equal("6", fields.Single(f => f.Key == "after_count").Value);
        Assert.Equal("2", fields.Single(f => f.Key == "deltaCount").Value);
        Assert.Equal("solved", fields.Single(f => f.Key == "deltaNoun").Value);
    }

    // --- Attribute path (source generator) ---

    [Fact]
    public void Generated_MarkoutGoal_EmitsDerivedAxesInJsonl()
    {
        var card = new GoalAttrCard
        {
            Failures = new(0, 7),          // Lower: introduced/bad
            FullyRaised = new(40, 55),     // Higher: increased/good
            Fidelity = new(0.8730, 0.8731),// Higher, noise 0.001: unchanged/neutral
            ChangedBodies = new(45, 46),   // no goal: no direction/status
        };

        var sw = new StringWriter();
        MarkoutSerializer.Serialize(card, sw, new TableFormatter(), GoalAttrCardContext.Default,
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, OmitEmptyJsonFields = true });
        var records = sw.ToString().ReplaceLineEndings("\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => JsonDocument.Parse(l).RootElement)
            .ToList();

        var failures = records.First(e => e.GetProperty("field").GetString() == "Failures");
        Assert.Equal("introduced", failures.GetProperty("direction").GetString());
        Assert.Equal("bad", failures.GetProperty("status").GetString());

        var raised = records.First(e => e.GetProperty("field").GetString() == "Fully raised");
        Assert.Equal("increased", raised.GetProperty("direction").GetString());
        Assert.Equal("good", raised.GetProperty("status").GetString());

        var fidelity = records.First(e => e.GetProperty("field").GetString() == "Fidelity");
        Assert.Equal("unchanged", fidelity.GetProperty("direction").GetString());
        Assert.Equal("neutral", fidelity.GetProperty("status").GetString());

        var changed = records.First(e => e.GetProperty("field").GetString() == "Changed bodies");
        Assert.False(changed.TryGetProperty("direction", out _));
        Assert.False(changed.TryGetProperty("status", out _));
    }

    [Fact]
    public void Generated_MarkoutGoal_TableColumn_RendersInlineWord()
    {
        // A [MarkoutGoal] Change<T> rendered as a generated element-table column must also carry
        // the dense goal word (the table-cell path, not just the composite-card path).
        var card = new GoalTableCard { Rows = [new GoalTableRow { Name = "raise", Failures = new(0, 7) }] };
        var md = MarkoutSerializer.Serialize(card, GoalTableCardContext.Default);

        Assert.Contains("0 \u2192 7 (bad)", md);
    }

    [Fact]
    public void Generated_MarkoutGoal_CardProperty_RendersInlineWord()
    {
        // The composite-card (field-layout) path also renders the dense goal word in Markdown.
        var card = new GoalAttrCard
        {
            Failures = new(0, 7),       // Lower: introduced -> bad
            FullyRaised = new(40, 55),  // Higher: increased -> good
        };
        var md = MarkoutSerializer.Serialize(card, GoalAttrCardContext.Default);

        Assert.Contains("0 \u2192 7 (bad)", md);
        Assert.Contains("40 \u2192 55 (good)", md);
    }

    [Fact]
    public void Generated_DeltaMultipleAndNoun_RenderInMarkdown()
    {
        var card = new DeltaModesCard
        {
            Residual = new(15, 5),
            Tasks = new(new Fraction(4, 6), new Fraction(6, 6)),
        };
        var md = MarkoutSerializer.Serialize(card, DeltaModesCardContext.Default);

        Assert.Contains("15 \u2192 5 (3\u00d7 fewer)", md);
        Assert.Contains("4/6 \u2192 6/6 (+2 solved)", md);
    }
}
