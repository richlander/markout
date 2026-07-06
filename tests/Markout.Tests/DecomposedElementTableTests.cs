using System.Text.Json;
using Markout;

namespace Markout.Tests;

public class DecomposedElementRow
{
    public string Name { get; set; } = "";

    [MarkoutDelta(Delta.Percent)]
    public Change<long> Score { get; set; }

    [MarkoutDeltaNoun("solved")]
    public Change<Fraction> Tasks { get; set; }

    [MarkoutGoal(Goal.Lower)]
    public Change<int> Bugs { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class DecomposedElementCard
{
    [MarkoutIgnore] public string Title => "Rows";

    [MarkoutSection(Name = "Rows")]
    public List<DecomposedElementRow> Rows { get; set; } = new();
}

[MarkoutContext(typeof(DecomposedElementCard))]
public partial class DecomposedElementCardContext : MarkoutSerializerContext
{
}

// A scalar-only element table (regression guard: no decompose branch, unchanged output).
public class ScalarElementRow
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class ScalarElementCard
{
    [MarkoutIgnore] public string Title => "Items";

    [MarkoutSection(Name = "Items")]
    public List<ScalarElementRow> Items { get; set; } = new();
}

[MarkoutContext(typeof(ScalarElementCard))]
public partial class ScalarElementCardContext : MarkoutSerializerContext
{
}

public class DecomposedElementTableTests
{
    private static DecomposedElementCard Card() => new()
    {
        Rows =
        [
            new DecomposedElementRow
            {
                Name = "a", Score = new(100, 50),
                Tasks = new(new Fraction(4, 6), new Fraction(6, 6)), Bugs = new(7, 0),
            },
        ],
    };

    [Fact]
    public void Markdown_KeepsDenseCompositeColumns()
    {
        var md = MarkoutSerializer.Serialize(Card(), DecomposedElementCardContext.Default);

        Assert.Contains("| Name | Score | Tasks | Bugs |", md);
        Assert.Contains("| a | 100 \u2192 50 (-50%) | 4/6 \u2192 6/6 (+2 solved) | 7 \u2192 0 (good) |", md);
    }

    [Fact]
    public void Jsonl_DecomposesCompositeColumnsIntoTypedPrefixedFields()
    {
        var sw = new StringWriter();
        MarkoutSerializer.Serialize(Card(), sw, new TableFormatter(), DecomposedElementCardContext.Default,
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, JsonTypedValues = true });
        var row = JsonDocument.Parse(sw.ToString().Trim()).RootElement;

        Assert.Equal("a", row.GetProperty("name").GetString());
        // score ([MarkoutDelta]) -> before/after/delta_pct
        Assert.Equal(100, row.GetProperty("score_before").GetInt32());
        Assert.Equal(50, row.GetProperty("score_after").GetInt32());
        Assert.Equal(-50, row.GetProperty("score_delta_pct").GetInt32());
        // tasks ([MarkoutDeltaNoun]) -> parts + delta_count + delta_noun
        Assert.Equal(4, row.GetProperty("tasks_before_count").GetInt32());
        Assert.Equal(6, row.GetProperty("tasks_after_count").GetInt32());
        Assert.Equal(2, row.GetProperty("tasks_delta_count").GetInt32());
        Assert.Equal("solved", row.GetProperty("tasks_delta_noun").GetString());
        // bugs ([MarkoutGoal]) -> before/after/direction/status
        Assert.Equal(7, row.GetProperty("bugs_before").GetInt32());
        Assert.Equal(0, row.GetProperty("bugs_after").GetInt32());
        Assert.Equal("resolved", row.GetProperty("bugs_direction").GetString());
        Assert.Equal("good", row.GetProperty("bugs_status").GetString());
        // The dense string must NOT appear as a value.
        Assert.False(row.TryGetProperty("score", out _));
    }

    [Fact]
    public void Tsv_DecomposesToUnionColumns()
    {
        var sw = new StringWriter();
        MarkoutSerializer.Serialize(Card(), sw, new TableFormatter(), DecomposedElementCardContext.Default,
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Tsv });
        var lines = sw.ToString().ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var header = lines[0];

        Assert.Contains("score_before", header);
        Assert.Contains("bugs_status", header);
        Assert.Contains("tasks_delta_noun", header);
        Assert.DoesNotContain("\tscore\t", "\t" + header + "\t"); // no dense composite column
    }

    [Fact]
    public void ScalarOnlyTable_Unchanged()
    {
        var card = new ScalarElementCard { Items = [new ScalarElementRow { Name = "x", Count = 3 }] };

        var md = MarkoutSerializer.Serialize(card, ScalarElementCardContext.Default);
        Assert.Contains("| Name | Count |", md);
        Assert.Contains("| x | 3 |", md);

        var sw = new StringWriter();
        MarkoutSerializer.Serialize(card, sw, new TableFormatter(), ScalarElementCardContext.Default,
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, JsonTypedValues = true });
        var row = JsonDocument.Parse(sw.ToString().Trim()).RootElement;
        Assert.Equal("x", row.GetProperty("name").GetString());
        Assert.Equal(3, row.GetProperty("count").GetInt32());
    }
}
