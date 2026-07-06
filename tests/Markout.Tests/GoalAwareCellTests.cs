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

public class GoalAwareCellTests
{
    private static List<MarkoutField> Decompose(IMarkoutCell cell, MarkoutCellFormat format)
    {
        var fields = new List<MarkoutField>();
        cell.Decompose(fields, null, format);
        return fields;
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
    public void Change_WithGoal_DerivesDirectionAndPolarity(int before, int after, string goal, string direction, string status)
    {
        var format = new MarkoutCellFormat(Goal: goal == "lower" ? Goal.Lower : Goal.Higher);
        var fields = Decompose(new Change<long>(before, after), format);

        Assert.Equal(direction, fields.Single(f => f.Key == "direction").Value);
        Assert.Equal(status, fields.Single(f => f.Key == "status").Value);
    }

    [Fact]
    public void Change_ContextGoal_EmitsNeitherDirectionNorStatus()
    {
        var fields = Decompose(new Change<long>(45, 46), new MarkoutCellFormat(Goal: Goal.Context));
        Assert.DoesNotContain(fields, f => f.Key is "direction" or "status");
    }

    [Fact]
    public void Change_NoiseTolerance_ClassifiesSubThresholdAsUnchanged()
    {
        // 87.30% -> 87.31% with a 0.001 tolerance is within noise -> unchanged/neutral.
        var format = new MarkoutCellFormat(Goal: Goal.Higher, Noise: 0.001);
        var fields = Decompose(new Change<double>(0.8730, 0.8731), format);

        Assert.Equal("unchanged", fields.Single(f => f.Key == "direction").Value);
        Assert.Equal("neutral", fields.Single(f => f.Key == "status").Value);
    }

    [Fact]
    public void Change_GoalAndDelta_EmitBothDerivedAxesAndDelta()
    {
        var format = new MarkoutCellFormat(Delta.Absolute, Goal: Goal.Lower);
        var fields = Decompose(new Change<long>(10, 3), format);

        Assert.Equal("10", fields[0].Value); // before
        Assert.Equal("3", fields[1].Value);  // after
        Assert.Equal("-7", fields.Single(f => f.Key == "deltaAbs").Value);
        Assert.Equal("decreased", fields.Single(f => f.Key == "direction").Value);
        Assert.Equal("good", fields.Single(f => f.Key == "status").Value);
    }

    // --- MetricChange<T> runtime path ---

    [Fact]
    public void MetricChange_Goal_DerivesStatusInMarkdownColumn()
    {
        var writer = new MarkoutWriter(new MarkdownFormatter());
        writer.WriteMetricChangeTable(new[]
        {
            new MetricChange<int>("Failures", 0, 7) { Goal = Goal.Lower },
            new MetricChange<int>("Fully raised", 40, 55) { Goal = Goal.Higher },
            new MetricChange<int>("Changed bodies", 45, 46), // Context -> no derived status
        });
        var md = writer.ToString();

        Assert.Contains("| Failures | 0 \u2192 7 | - | bad |", md);
        Assert.Contains("| Fully raised | 40 \u2192 55 | - | good |", md);
        Assert.Contains("| Changed bodies | 45 \u2192 46 | - | - |", md);
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
}
