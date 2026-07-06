using System.Text.Json;
using Markout;

namespace Markout.Tests;

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class GeneratedMultiSourceCard
{
    [MarkoutIgnore] public string Title { get; set; } = "Quality";

    [MarkoutSection(Name = "Baseline comparison")]
    [MarkoutLabelHeader("Metric")]
    public List<MultiSourceRow> Rows { get; set; } = new();
}

[MarkoutContext(typeof(GeneratedMultiSourceCard))]
public partial class GeneratedMultiSourceCardContext : MarkoutSerializerContext
{
}

public class GeneratedMultiSourceTests
{
    private static GeneratedMultiSourceCard Card() => new()
    {
        Rows =
        [
            new("output tok",
                new Source("opus", new Change<Share>(new Share(5056, 21067), new Share(3129, 13037))),
                new Source("gpt5", new Change<Share>(new Share(6100, 21800), new Share(3500, 14000)))),
            new("verdict",
                new Source("opus", new Verdict(GateStatus.Good, "BETTER")),
                new Source("gpt5", new Verdict(GateStatus.Good, "BETTER"))),
        ],
    };

    [Fact]
    public void Generated_MultiSource_PivotsRolesIntoColumns_WithLabelHeader()
    {
        var md = MarkoutSerializer.Serialize(Card(), GeneratedMultiSourceCardContext.Default);

        Assert.Contains("## Baseline comparison", md);
        Assert.Contains("| Metric | opus | gpt5 |", md);
        Assert.Contains("| output tok | 5056 (24%) \u2192 3129 (24%) | 6100 (28%) \u2192 3500 (25%) |", md);
        Assert.Contains("| verdict | BETTER | BETTER |", md);
    }

    [Fact]
    public void Generated_MultiSource_Jsonl_DecomposesRolePrefixedKeys()
    {
        var sw = new StringWriter();
        MarkoutSerializer.Serialize(Card(), sw, new TableFormatter(), GeneratedMultiSourceCardContext.Default,
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, OmitEmptyJsonFields = true });

        var tok = sw.ToString().ReplaceLineEndings("\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => JsonDocument.Parse(l).RootElement)
            .First(e => e.TryGetProperty("metric", out var m) && m.GetString() == "output tok");

        Assert.Equal("5056", tok.GetProperty("opus_before_value").GetString());
        Assert.Equal("24", tok.GetProperty("opus_before_pct").GetString());
        Assert.Equal("3500", tok.GetProperty("gpt5_after_value").GetString());
    }
}
