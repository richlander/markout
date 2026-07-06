using System.Text.Json;
using Markout;

namespace Markout.Tests;

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class GeneratedIlDiffCard
{
    [MarkoutIgnore] public string Title { get; set; } = "IL Diff";

    [MarkoutSection(Name = "Baseline comparison")]
    public List<MetricChange<int>> Metrics { get; set; } = new();
}

[MarkoutContext(typeof(GeneratedIlDiffCard))]
public partial class GeneratedIlDiffCardContext : MarkoutSerializerContext
{
}

public class GeneratedMetricChangeTests
{
    private static GeneratedIlDiffCard Card() => new()
    {
        Metrics =
        [
            new("Failures", 0, 7, 0, "allowed failures", GateStatus.Bad, "regression"),
            new("Changed bodies", 45, 46, Status: GateStatus.Warning, StatusLabel: "drift"),
        ],
    };

    [Fact]
    public void Generated_MetricChangeSection_RendersFixedColumnTable()
    {
        var md = MarkoutSerializer.Serialize(Card(), GeneratedIlDiffCardContext.Default);

        Assert.Contains("## Baseline comparison", md);
        Assert.Contains("| Metric | Change | Target | Status |", md);
        Assert.Contains("| Failures | 0 \u2192 7 | allowed failures: 0 | regression |", md);
        Assert.Contains("| Changed bodies | 45 \u2192 46 | - | drift |", md);
    }

    [Fact]
    public void Generated_MetricChangeSection_Jsonl_DecomposesToFlatFields()
    {
        var sw = new StringWriter();
        MarkoutSerializer.Serialize(Card(), sw, new TableFormatter(), GeneratedIlDiffCardContext.Default,
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, OmitEmptyJsonFields = true });

        var failures = sw.ToString().ReplaceLineEndings("\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => JsonDocument.Parse(l).RootElement)
            .First(e => e.TryGetProperty("metric", out var m) && m.GetString() == "Failures");

        Assert.Equal("0", failures.GetProperty("before").GetString());
        Assert.Equal("7", failures.GetProperty("after").GetString());
        Assert.Equal("0", failures.GetProperty("target").GetString());
        Assert.Equal("allowed failures", failures.GetProperty("target_label").GetString());
        Assert.Equal("regression", failures.GetProperty("status").GetString());
    }
}

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class GeneratedArrayCard
{
    [MarkoutIgnore] public string Title => "Arr";

    [MarkoutSection(Name = "Metrics")]
    public MetricChange<int>[] Metrics { get; set; } = [];
}

[MarkoutContext(typeof(GeneratedArrayCard))]
public partial class GeneratedArrayCardContext : MarkoutSerializerContext
{
}

public class GeneratedMetricChangeArrayTests
{
    [Fact]
    public void Generated_MetricChangeArray_RendersCardNotComplexArray()
    {
        var card = new GeneratedArrayCard
        {
            Metrics = [new("Failures", 0, 7, 0, "allowed failures", GateStatus.Bad, "regression")],
        };
        var md = MarkoutSerializer.Serialize(card, GeneratedArrayCardContext.Default);

        Assert.Contains("| Metric | Change | Target | Status |", md);
        Assert.Contains("| Failures | 0 \u2192 7 | allowed failures: 0 | regression |", md);
    }
}

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class GeneratedSectionRowsCard
{
    [MarkoutIgnore] public string Title => "IL Diff";

    [MarkoutSection(Name = "Baseline metric changes", IncludeSectionInStructuredRows = true)]
    public List<MetricChange<int>> Baseline { get; set; } = new();

    [MarkoutSection(Name = "Plain metrics")]
    public List<MetricChange<int>> Plain { get; set; } = new();
}

[MarkoutContext(typeof(GeneratedSectionRowsCard))]
public partial class GeneratedSectionRowsCardContext : MarkoutSerializerContext
{
}

public class GeneratedSectionRowsTests
{
    [Fact]
    public void Generated_IncludeSectionInStructuredRows_AddsSectionOnlyToFlaggedSection()
    {
        var card = new GeneratedSectionRowsCard
        {
            Baseline = [new("Failures", 0, 7, 0, "max failures", GateStatus.Bad, "regression")],
            Plain = [new("Changed bodies", 45, 46, Status: GateStatus.Warning, StatusLabel: "drift")],
        };

        // Markdown is unaffected — the section is the heading, no section column in the table.
        var md = MarkoutSerializer.Serialize(card, GeneratedSectionRowsCardContext.Default);
        Assert.Contains("## Baseline metric changes", md);
        Assert.DoesNotContain("| section |", md);

        var sw = new StringWriter();
        MarkoutSerializer.Serialize(card, sw, new TableFormatter(), GeneratedSectionRowsCardContext.Default,
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, JsonTypedValues = true });
        var records = sw.ToString().ReplaceLineEndings("\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => JsonDocument.Parse(l).RootElement)
            .ToList();

        var failures = records.First(e => e.GetProperty("metric").GetString() == "Failures");
        Assert.Equal("Baseline metric changes", failures.GetProperty("section").GetString());

        // The un-flagged section's rows carry no section discriminator.
        var changed = records.First(e => e.GetProperty("metric").GetString() == "Changed bodies");
        Assert.False(changed.TryGetProperty("section", out _));
    }
}
