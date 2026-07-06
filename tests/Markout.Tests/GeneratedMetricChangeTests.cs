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
