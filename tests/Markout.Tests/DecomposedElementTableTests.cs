using System.Linq;
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

// Reconstruction target: string columns matching the original card's headers, so re-serializing the
// reconstructed dense cells yields the same Markdown document (same title/section/headers/widths).
public class ReconstructedRow
{
    public string Name { get; set; } = "";
    public string Score { get; set; } = "";
    public string Tasks { get; set; } = "";
    public string Bugs { get; set; } = "";
}

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class ReconstructedCard
{
    [MarkoutIgnore] public string Title => "Rows";

    [MarkoutSection(Name = "Rows")]
    public List<ReconstructedRow> Rows { get; set; } = new();
}

[MarkoutContext(typeof(ReconstructedCard))]
public partial class ReconstructedCardContext : MarkoutSerializerContext
{
}

// Pathological naming: a scalar column whose name equals a composite column's decomposed subkey
// ("Score" -> "Score_before"). Guards that both survive as distinct columns (no silent overwrite).
public class CollisionRow
{
    public string Name { get; set; } = "";

    [MarkoutDelta(Delta.Percent)]
    public Change<long> Score { get; set; }

    public long Score_before { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class CollisionCard
{
    [MarkoutIgnore] public string Title => "Rows";

    [MarkoutSection(Name = "Rows")]
    public List<CollisionRow> Rows { get; set; } = new();
}

[MarkoutContext(typeof(CollisionCard))]
public partial class CollisionCardContext : MarkoutSerializerContext
{
}

// Same collision, but the composite column is nullable so it contributes no fields in rows where it is
// null. Guards that the scalar column stays in one output column across rows (no cross-row source mixing).
public class NullableCollisionRow
{
    public string Name { get; set; } = "";

    [MarkoutDelta(Delta.Percent)]
    public Change<long>? Score { get; set; }

    public long Score_before { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class NullableCollisionCard
{
    [MarkoutIgnore] public string Title => "Rows";

    [MarkoutSection(Name = "Rows")]
    public List<NullableCollisionRow> Rows { get; set; } = new();
}

[MarkoutContext(typeof(NullableCollisionCard))]
public partial class NullableCollisionCardContext : MarkoutSerializerContext
{
}

// Element table combining [MarkoutIgnoreColumnWhen] (dynamic-ignore path) with composite columns:
// composite columns must still decompose for structured formatters while dynamic hidden columns drop out.
public class DynIgnoreRow
{
    public string Name { get; set; } = "";

    [MarkoutDelta(Delta.Percent)]
    public Change<long> Score { get; set; }

    [MarkoutGoal(Goal.Lower)]
    public Change<int> Bugs { get; set; }

    public string Note { get; set; } = "";
}

[MarkoutSerializable(TitleProperty = nameof(Title), AutoFields = false)]
public class DynIgnoreCard
{
    [MarkoutIgnore] public string Title => "Rows";

    [MarkoutSection(Name = "Rows")]
    [MarkoutIgnoreColumnWhen(nameof(NoteIsUniform), "Note")]
    public List<DynIgnoreRow> Rows { get; set; } = new();

    public static bool NoteIsUniform(List<DynIgnoreRow> rows)
        => rows.Select(r => r.Note).Distinct().Count() <= 1;
}

[MarkoutContext(typeof(DynIgnoreCard))]
public partial class DynIgnoreCardContext : MarkoutSerializerContext
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

    [Fact]
    public void CompositeSubkeyCollidingWithScalarColumn_KeepsBothColumns()
    {
        // The composite "Score" decomposes to "score_before"; a sibling scalar column is also named
        // "Score_before". Both must survive: the later one is disambiguated to "score_before_2" rather
        // than silently overwriting the first (which would violate the reconstructable-JSON contract).
        var card = new CollisionCard
        {
            Rows = [new CollisionRow { Name = "a", Score = new(100, 50), Score_before = 999 }],
        };

        var sw = new StringWriter();
        MarkoutSerializer.Serialize(card, sw, new TableFormatter(), CollisionCardContext.Default,
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, JsonTypedValues = true });
        var row = JsonDocument.Parse(sw.ToString().Trim()).RootElement;

        Assert.Equal(100, row.GetProperty("score_before").GetInt64());
        Assert.Equal(999, row.GetProperty("score_before_2").GetInt64());
    }

    [Fact]
    public void NullableCompositeAbsentInSomeRows_ScalarColumnStaysAligned()
    {
        // Row 1's composite is null (contributes no fields); row 2's is present. The scalar "Score_before"
        // must stay in a single output column across BOTH rows — never landing in the composite's column
        // just because the composite was absent in row 1. Column identity is per source column, not per-row
        // occurrence, so the two sources never mix.
        var card = new NullableCollisionCard
        {
            Rows =
            [
                new NullableCollisionRow { Name = "a", Score = null, Score_before = 999 },
                new NullableCollisionRow { Name = "b", Score = new Change<long>(100, 50), Score_before = 888 },
            ],
        };

        var sw = new StringWriter();
        MarkoutSerializer.Serialize(card, sw, new TableFormatter(), NullableCollisionCardContext.Default,
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, JsonTypedValues = true });
        var lines = sw.ToString().ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var r0 = JsonDocument.Parse(lines[0]).RootElement;
        var r1 = JsonDocument.Parse(lines[1]).RootElement;

        // The scalar column is stable across rows (established first, so it owns "score_before").
        Assert.Equal(999, r0.GetProperty("score_before").GetInt64());
        Assert.Equal(888, r1.GetProperty("score_before").GetInt64());
        // The composite's before value lives in its own separate column, present only when the composite is.
        Assert.Equal(100, r1.GetProperty("score_before_2").GetInt64());
        // Row 0 has no composite, so its own separate column carries no numeric value.
        bool row0HasCompositeValue = r0.TryGetProperty("score_before_2", out var v)
            && v.ValueKind is JsonValueKind.Number;
        Assert.False(row0HasCompositeValue, "row 0 must not carry a composite before value");
    }

    [Fact]
    public void DynamicIgnoreTable_DecomposesCompositesAndDropsHiddenColumn()
    {
        // Uniform Note -> the "Note" column is dynamically ignored. Composite columns must still decompose
        // (Score/Bugs -> typed sub-fields) in structured output, and Markdown stays dense with Note dropped.
        var card = new DynIgnoreCard
        {
            Rows =
            [
                new DynIgnoreRow { Name = "a", Score = new(100, 50), Bugs = new(7, 0), Note = "x" },
                new DynIgnoreRow { Name = "b", Score = new(40, 60), Bugs = new(2, 5), Note = "x" },
            ],
        };

        var md = MarkoutSerializer.Serialize(card, DynIgnoreCardContext.Default);
        Assert.Contains("| Name | Score | Bugs |", md);          // Note dropped (uniform)
        Assert.Contains("| a | 100 \u2192 50 (-50%) | 7 \u2192 0 (good) |", md);

        var sw = new StringWriter();
        MarkoutSerializer.Serialize(card, sw, new TableFormatter(), DynIgnoreCardContext.Default,
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, JsonTypedValues = true });
        var r0 = JsonDocument.Parse(sw.ToString().ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries)[0]).RootElement;

        // Composites decomposed, not dense strings.
        Assert.Equal(100, r0.GetProperty("score_before").GetInt64());
        Assert.Equal(50, r0.GetProperty("score_after").GetInt64());
        Assert.Equal(-50, r0.GetProperty("score_delta_pct").GetInt32());
        Assert.Equal(7, r0.GetProperty("bugs_before").GetInt32());
        Assert.Equal(0, r0.GetProperty("bugs_after").GetInt32());
        Assert.Equal("good", r0.GetProperty("bugs_status").GetString());
        Assert.False(r0.TryGetProperty("score", out _), "composite must not leak a dense 'score' field");
        // Hidden column absent from structured output too.
        Assert.False(r0.TryGetProperty("note", out _), "uniform Note column must be dropped");
    }

    [Fact]
    public void DynamicIgnoreTable_KeepsVaryingColumnAlongsideDecomposedComposites()
    {
        // Note varies -> the column is kept; composites still decompose.
        var card = new DynIgnoreCard
        {
            Rows =
            [
                new DynIgnoreRow { Name = "a", Score = new(100, 50), Bugs = new(7, 0), Note = "x" },
                new DynIgnoreRow { Name = "b", Score = new(40, 60), Bugs = new(2, 5), Note = "y" },
            ],
        };

        var md = MarkoutSerializer.Serialize(card, DynIgnoreCardContext.Default);
        Assert.Contains("| Name | Score | Bugs | Note |", md);

        var sw = new StringWriter();
        MarkoutSerializer.Serialize(card, sw, new TableFormatter(), DynIgnoreCardContext.Default,
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, JsonTypedValues = true });
        var r0 = JsonDocument.Parse(sw.ToString().ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries)[0]).RootElement;

        Assert.Equal(100, r0.GetProperty("score_before").GetInt64());
        Assert.Equal("good", r0.GetProperty("bugs_status").GetString());
        Assert.Equal("x", r0.GetProperty("note").GetString());
    }

    [Fact]
    public void MarkdownCard_IsByteForByteReconstructableFromJson()
    {
        // The reconstructable-JSON contract: rebuild the dense Markdown card from the JSON fields alone
        // (a hard-coded algorithm that re-applies Markout's display formatting), and require it to be
        // byte-for-byte identical to the directly-rendered Markdown. This proves the JSON carries all the
        // data (raw + derived judgement + caller metadata) needed to reconstruct the card.
        var card = Card();
        var markdown = MarkoutSerializer.Serialize(card, DecomposedElementCardContext.Default);

        var sw = new StringWriter();
        MarkoutSerializer.Serialize(card, sw, new TableFormatter(), DecomposedElementCardContext.Default,
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, JsonTypedValues = true });
        var records = sw.ToString().ReplaceLineEndings("\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => JsonDocument.Parse(l).RootElement)
            .ToList();

        const string arrow = " \u2192 ";
        static string SignedPct(int pct) => (pct > 0 ? "+" : "") + pct + "%";
        static string Signed(int n) => (n > 0 ? "+" : "") + n;

        var reconstructed = new ReconstructedCard
        {
            Rows = records.Select(r => new ReconstructedRow
            {
                Name = r.GetProperty("name").GetString()!,
                // score: [MarkoutDelta(Percent)] -> "before → after (±pct%)"
                Score = r.GetProperty("score_before").GetInt64() + arrow + r.GetProperty("score_after").GetInt64()
                    + " (" + SignedPct(r.GetProperty("score_delta_pct").GetInt32()) + ")",
                // tasks: [MarkoutDeltaNoun] -> "bc/bt → ac/at (±count noun)"
                Tasks = r.GetProperty("tasks_before_count").GetInt32() + "/" + r.GetProperty("tasks_before_total").GetInt32()
                    + arrow + r.GetProperty("tasks_after_count").GetInt32() + "/" + r.GetProperty("tasks_after_total").GetInt32()
                    + " (" + Signed(r.GetProperty("tasks_delta_count").GetInt32()) + " " + r.GetProperty("tasks_delta_noun").GetString() + ")",
                // bugs: [MarkoutGoal] -> "before → after (status)"
                Bugs = r.GetProperty("bugs_before").GetInt32() + arrow + r.GetProperty("bugs_after").GetInt32()
                    + " (" + r.GetProperty("bugs_status").GetString() + ")",
            }).ToList(),
        };

        var reconstructedMarkdown = MarkoutSerializer.Serialize(reconstructed, ReconstructedCardContext.Default);

        Assert.Equal(markdown, reconstructedMarkdown);
    }
}
