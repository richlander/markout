using System.Text.Json;
using Markout;
using Markout.Formatting;

namespace Markout.Tests;

// ── End-to-end model exercising composite cells through the source generator ──

[MarkoutSerializable]
public class QualityCard
{
    [MarkoutPropertyName("tasks correct")]
    public Change<Fraction> TasksCorrect { get; set; }

    [MarkoutPropertyName("tool calls: web / bash / other")]
    public Change<Segments> ToolCalls { get; set; }

    [MarkoutPropertyName("output tok (% of IET)")]
    public Change<Share> OutputTok { get; set; }

    [MarkoutPropertyName("tool-turn secs (% of turn time)"), MarkoutUnit("s")]
    public Change<Share> ToolTurnSecs { get; set; }

    [MarkoutPropertyName("tool-turn IET (% of turn IET)")]
    public Change<Percent> ToolTurnIet { get; set; }

    [MarkoutPropertyName("Session IET"), MarkoutDelta(Delta.Percent)]
    public Change<long> SessionIet { get; set; }

    public string? Verdict { get; set; }
}

[MarkoutContext(typeof(QualityCard))]
public partial class QualityCardContext : MarkoutSerializerContext
{
}

// Nullable + skip guards on the composite path (regression: generated code must compile
// under TreatWarningsAsErrors and must skip null composites instead of emitting blank rows).
[MarkoutSerializable]
public class NullableCompositeCard
{
    [MarkoutPropertyName("maybe"), MarkoutDelta(Delta.Percent)]
    public Change<long>? Maybe { get; set; }

    public string? Note { get; set; }
}

[MarkoutContext(typeof(NullableCompositeCard))]
public partial class NullableCompositeContext : MarkoutSerializerContext
{
}

// Link formatting must survive the composite-card path (regression: EmitScalarCompositeRow
// previously dropped [MarkoutLink]).
[MarkoutSerializable]
public class LinkedCompositeCard
{
    [MarkoutPropertyName("score")]
    public Change<long> Score { get; set; }

    [MarkoutLink]
    public string? Url { get; set; }
}

[MarkoutContext(typeof(LinkedCompositeCard))]
public partial class LinkedCompositeContext : MarkoutSerializerContext
{
}

// A reference-type IMarkoutCell implementation (built-in shapes are value types) to verify
// skip-null guarding and null-safe skip-default on the composite path.
public sealed class TextCell(string text) : IMarkoutCell
{
    private readonly string _text = text;

    public void FormatInline(TextWriter writer, in MarkoutCellFormat format)
        => writer.Write(_text);

    public void Decompose(ICollection<MarkoutField> fields, string? side, in MarkoutCellFormat format)
        => fields.Add(new MarkoutField(side is null ? "text" : side + "_text", _text));
}

[MarkoutSerializable]
public class RefCellCard
{
    [MarkoutPropertyName("score")]
    public Change<long> Score { get; set; }

    [MarkoutSkipNull]
    public TextCell? Note { get; set; }
}

[MarkoutContext(typeof(RefCellCard))]
public partial class RefCellContext : MarkoutSerializerContext
{
}

public class CompositeCellTests
{
    private static string Inline(IMarkoutCell cell, MarkoutCellFormat format = default)
    {
        var sw = new StringWriter();
        cell.FormatInline(sw, format);
        return sw.ToString();
    }

    private static List<MarkoutField> Decompose(IMarkoutCell cell, MarkoutCellFormat format = default)
    {
        var fields = new List<MarkoutField>();
        cell.Decompose(fields, null, format);
        return fields;
    }

    // ── Fraction ──

    [Fact]
    public void Fraction_RendersAndDecomposes()
    {
        var cell = new Fraction(24, 24);
        Assert.Equal("24/24", Inline(cell));

        var fields = Decompose(cell);
        Assert.Equal([new("count", "24"), new("total", "24")], fields);
    }

    // ── Share ──

    [Fact]
    public void Share_RendersPercentAndDecomposes()
    {
        var cell = new Share(5056, 21067); // 24.0%
        Assert.Equal("5056 (24%)", Inline(cell));

        var fields = Decompose(cell);
        Assert.Equal([new("value", "5056"), new("pct", "24")], fields);
    }

    [Fact]
    public void Share_WithUnit_AddsSuffixToDenseValueOnly()
    {
        var cell = new Share(103, 110); // 93.6% -> 94%
        Assert.Equal("103s (94%)", Inline(cell, new MarkoutCellFormat(Delta.None, "s")));

        // Unit does not leak into the decomposed numeric value.
        var fields = Decompose(cell, new MarkoutCellFormat(Delta.None, "s"));
        Assert.Equal("103", fields[0].Value);
    }

    [Fact]
    public void Share_ZeroWhole_RendersPlaceholder()
    {
        var cell = new Share(5, 0);
        Assert.Equal("5 (\u2014)", Inline(cell));
        Assert.Equal("\u2014", Decompose(cell)[1].Value);
    }

    // ── Percent ──

    [Fact]
    public void Percent_RendersAndDecomposes()
    {
        var cell = new Percent(93, 100);
        Assert.Equal("93%", Inline(cell));
        Assert.Equal([new("pct", "93")], Decompose(cell));
    }

    [Fact]
    public void Percent_ZeroWhole_RendersPlaceholder()
    {
        var cell = new Percent(1, 0);
        Assert.Equal("\u2014", Inline(cell));
        Assert.Equal("\u2014", Decompose(cell)[0].Value);
    }

    // ── Segments ──

    [Fact]
    public void Segments_RendersSlashJoinedAndDecomposesByLabel()
    {
        var cell = new Segments(new Segment("web", 21), new Segment("bash", 171), new Segment("other", 236));
        Assert.Equal("21/171/236", Inline(cell));

        var fields = Decompose(cell);
        Assert.Equal([new("web", "21"), new("bash", "171"), new("other", "236")], fields);
    }

    // ── Change (scalar) ──

    [Fact]
    public void Change_Scalar_WithPercentDelta()
    {
        var cell = new Change<long>(98555, 61190); // -37.9% -> -38%
        Assert.Equal("98555 \u2192 61190 (-38%)", Inline(cell, new MarkoutCellFormat(Delta.Percent)));

        var fields = Decompose(cell, new MarkoutCellFormat(Delta.Percent));
        Assert.Equal([new("before", "98555"), new("after", "61190"), new("deltaPct", "-38")], fields);
    }

    [Fact]
    public void Change_Scalar_WithAbsoluteDelta_UsesSignedDifference()
    {
        var cell = new Change<long>(10, 25);
        Assert.Equal("10 \u2192 25 (+15)", Inline(cell, new MarkoutCellFormat(Delta.Absolute)));
        Assert.Equal("15", Decompose(cell, new MarkoutCellFormat(Delta.Absolute))[2].Value);
    }

    [Fact]
    public void Change_Scalar_NoDelta_OmitsSuffix()
    {
        var cell = new Change<long>(10, 25);
        Assert.Equal("10 \u2192 25", Inline(cell));
        Assert.Equal(2, Decompose(cell).Count);
    }

    [Fact]
    public void Change_Scalar_ZeroBefore_RendersDeltaPlaceholder()
    {
        var cell = new Change<long>(0, 5);
        Assert.Equal("0 \u2192 5 (\u2014)", Inline(cell, new MarkoutCellFormat(Delta.Percent)));
        Assert.Equal("\u2014", Decompose(cell, new MarkoutCellFormat(Delta.Percent))[2].Value);
    }

    // ── Change (nested composites) ──

    [Fact]
    public void Change_NestedFraction_RendersAndDecomposesPerSide()
    {
        var cell = new Change<Fraction>(new Fraction(24, 24), new Fraction(20, 24));
        Assert.Equal("24/24 \u2192 20/24", Inline(cell));

        var fields = Decompose(cell);
        Assert.Equal(
            [new("before_count", "24"), new("before_total", "24"), new("after_count", "20"), new("after_total", "24")],
            fields);
    }

    [Fact]
    public void Change_NestedSegments_RendersAndDecomposesByLabelAndSide()
    {
        var cell = new Change<Segments>(
            new Segments(new Segment("web", 21), new Segment("bash", 171), new Segment("other", 236)),
            new Segments(new Segment("web", 0), new Segment("bash", 75), new Segment("other", 183)));
        Assert.Equal("21/171/236 \u2192 0/75/183", Inline(cell));

        var fields = Decompose(cell);
        Assert.Equal(
            [
                new("web_before", "21"), new("bash_before", "171"), new("other_before", "236"),
                new("web_after", "0"), new("bash_after", "75"), new("other_after", "183")
            ],
            fields);
    }

    [Fact]
    public void Change_NestedShare_CarriesUnitToBothHalves()
    {
        var cell = new Change<Share>(new Share(103, 110), new Share(61, 68)); // 94%, 90%
        Assert.Equal("103s (94%) \u2192 61s (90%)", Inline(cell, new MarkoutCellFormat(Delta.None, "s")));
    }

    // ── Writer: dense (document) vs decomposed (structured) ──

    private static MarkoutCompositeRow[] SampleRows() =>
    [
        new("Session IET", new Change<long>(98555, 61190), new MarkoutCellFormat(Delta.Percent)),
        new("tool calls", new Change<Segments>(
            new Segments(new Segment("web", 21), new Segment("bash", 171), new Segment("other", 236)),
            new Segments(new Segment("web", 0), new Segment("bash", 75), new Segment("other", 183)))),
        MarkoutCompositeRow.Scalar("Verdict", "BETTER"),
    ];

    [Fact]
    public void WriteCompositeTable_Markdown_RendersDenseFieldValueTable()
    {
        var writer = new MarkoutWriter(new MarkdownFormatter());
        writer.WriteCompositeTable(SampleRows());
        var output = writer.ToString();

        Assert.Contains("| Field | Value |", output);
        Assert.Contains("| Session IET | 98555 \u2192 61190 (-38%) |", output);
        Assert.Contains("| tool calls | 21/171/236 \u2192 0/75/183 |", output);
        Assert.Contains("| Verdict | BETTER |", output);
    }

    [Fact]
    public void WriteCompositeTable_Tsv_DecomposesIntoUnionColumns()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter(),
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Tsv });
        writer.WriteCompositeTable(SampleRows());
        var lines = sw.ToString().ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var headers = lines[0].Split('\t');
        Assert.Equal("field", headers[0]);
        Assert.Contains("before", headers);
        Assert.Contains("delta_pct", headers);
        Assert.Contains("web_before", headers);

        // Session IET row carries before/after/delta but no segment columns.
        var sessionRow = lines[1].Split('\t');
        Assert.Equal("Session IET", sessionRow[0]);
        Assert.Contains("98555", sessionRow);
        Assert.Contains("-38", sessionRow);
    }

    [Fact]
    public void WriteCompositeTable_Jsonl_EmitsDecomposedRecords()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter(),
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl });
        writer.WriteCompositeTable(SampleRows());
        var lines = sw.ToString().ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var session = JsonDocument.Parse(lines[0]).RootElement;
        Assert.Equal("Session IET", session.GetProperty("field").GetString());
        Assert.Equal("98555", session.GetProperty("before").GetString());
        Assert.Equal("61190", session.GetProperty("after").GetString());
        Assert.Equal("-38", session.GetProperty("delta_pct").GetString());

        var tools = JsonDocument.Parse(lines[1]).RootElement;
        Assert.Equal("21", tools.GetProperty("web_before").GetString());
        Assert.Equal("183", tools.GetProperty("other_after").GetString());
    }

    // ── End-to-end through the source generator ──

    private static QualityCard SampleCard() => new()
    {
        TasksCorrect = new(new Fraction(24, 24), new Fraction(24, 24)),
        ToolCalls = new(
            new Segments(new Segment("web", 21), new Segment("bash", 171), new Segment("other", 236)),
            new Segments(new Segment("web", 0), new Segment("bash", 75), new Segment("other", 183))),
        OutputTok = new(new Share(5056, 21067), new Share(3129, 13037)),
        ToolTurnSecs = new(new Share(103, 110), new Share(61, 68)),
        ToolTurnIet = new(new Percent(93, 100), new Percent(91, 100)),
        SessionIet = new(98555, 61190),
        Verdict = "BETTER",
    };

    [Fact]
    public void Generated_QualityCard_Markdown_IsDenseTable()
    {
        var output = MarkoutSerializer.Serialize(SampleCard(), QualityCardContext.Default);

        Assert.Contains("| tasks correct | 24/24 \u2192 24/24 |", output);
        Assert.Contains("| tool calls: web / bash / other | 21/171/236 \u2192 0/75/183 |", output);
        Assert.Contains("| tool-turn secs (% of turn time) | 103s (94%) \u2192 61s (90%) |", output);
        Assert.Contains("| tool-turn IET (% of turn IET) | 93% \u2192 91% |", output);
        Assert.Contains("| Session IET | 98555 \u2192 61190 (-38%) |", output);
        Assert.Contains("| Verdict | BETTER |", output);
    }

    [Fact]
    public void Generated_QualityCard_Jsonl_Decomposes()
    {
        var sw = new StringWriter();
        MarkoutSerializer.Serialize(SampleCard(), sw, new TableFormatter(), QualityCardContext.Default,
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl });
        var lines = sw.ToString().ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // One record per property/row, in declaration order.
        var tasks = JsonDocument.Parse(lines[0]).RootElement;
        Assert.Equal("tasks correct", tasks.GetProperty("field").GetString());
        Assert.Equal("24", tasks.GetProperty("before_count").GetString());

        var session = JsonDocument.Parse(lines[5]).RootElement;
        Assert.Equal("Session IET", session.GetProperty("field").GetString());
        Assert.Equal("-38", session.GetProperty("delta_pct").GetString());

        var verdict = JsonDocument.Parse(lines[6]).RootElement;
        Assert.Equal("BETTER", verdict.GetProperty("value").GetString());
    }

    [Fact]
    public void Generated_QualityCard_Tsv_HasUnionColumns()
    {
        var sw = new StringWriter();
        MarkoutSerializer.Serialize(SampleCard(), sw, new TableFormatter(), QualityCardContext.Default,
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Tsv });
        var headers = sw.ToString().ReplaceLineEndings("\n").Split('\n')[0].Split('\t');

        Assert.Equal("field", headers[0]);
        Assert.Contains("before_count", headers);
        Assert.Contains("web_before", headers);
        Assert.Contains("delta_pct", headers);
        Assert.Contains("value", headers);
    }

    // ── Regression: review findings ──

    [Fact]
    public void Generated_NullableComposite_Null_IsSkipped()
    {
        var card = new NullableCompositeCard { Maybe = null, Note = "n/a" };
        var output = MarkoutSerializer.Serialize(card, NullableCompositeContext.Default);

        Assert.DoesNotContain("maybe", output);
        Assert.Contains("| Note | n/a |", output);
    }

    [Fact]
    public void Generated_NullableComposite_Present_Renders()
    {
        var card = new NullableCompositeCard { Maybe = new Change<long>(100, 50), Note = "ok" };
        var output = MarkoutSerializer.Serialize(card, NullableCompositeContext.Default);

        Assert.Contains("| maybe | 100 \u2192 50 (-50%) |", output);
    }

    [Fact]
    public void WriteCompositeTable_AppliesFieldProjection()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new MarkdownFormatter(),
            new MarkoutWriterOptions { Projection = MarkoutProjection.WithFields("Verdict") });
        writer.WriteCompositeTable(SampleRows());
        var output = sw.ToString();

        Assert.Contains("| Verdict | BETTER |", output);
        Assert.DoesNotContain("Session IET", output);
        Assert.DoesNotContain("tool calls", output);
    }

    [Fact]
    public void WriteCompositeTable_Jsonl_DisambiguatesCollidingKeys()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter(),
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl });
        // "a b" and "a_b" both normalize to "a_b"; they must not silently merge into one key.
        writer.WriteCompositeTable(
            new MarkoutCompositeRow("segments", new Segments(new Segment("a b", 1), new Segment("a_b", 2))));

        var line = sw.ToString().ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries)[0];
        var root = JsonDocument.Parse(line).RootElement;

        // Leading column + two distinct value columns, both values preserved.
        var props = root.EnumerateObject().ToList();
        Assert.Equal(3, props.Count);
        var values = props.Where(p => p.Name != "field").Select(p => p.Value.GetString()).OrderBy(v => v).ToList();
        Assert.Equal(["1", "2"], values);
    }

    [Fact]
    public void Change_NestedInChange_ThreadsSideWithoutKeyCollision()
    {
        // Change<Change<int>> is an edge case, but its decomposed keys must stay distinct.
        var cell = new Change<Change<int>>(new Change<int>(1, 2), new Change<int>(3, 4));
        var fields = Decompose(cell);

        var keys = fields.Select(f => f.Key).ToList();
        Assert.Equal(keys.Count, keys.Distinct().Count());
        Assert.Contains(new MarkoutField("before_before", "1"), fields);
        Assert.Contains(new MarkoutField("after_after", "4"), fields);
    }

    // ── Regression: second adversarial review (Gemini / GPT-5.5 / MAI) ──

    [Fact]
    public void Change_Scalar_NegativeBase_PercentUsesMagnitude()
    {
        // A rise from a negative base is a gain, not a loss: divide by |before|.
        var cell = new Change<int>(-10, 10);
        Assert.Equal("-10 \u2192 10 (+200%)", Inline(cell, new MarkoutCellFormat(Delta.Percent)));
        Assert.Equal("200", Decompose(cell, new MarkoutCellFormat(Delta.Percent))[2].Value);
    }

    [Fact]
    public void Change_Scalar_LargeLong_PreservesPrecision()
    {
        // Values beyond double's exact integer range must not be rounded.
        var cell = new Change<long>(9007199254740993L, 9007199254740994L);
        var fields = Decompose(cell);
        Assert.Equal("9007199254740993", fields[0].Value);
        Assert.Equal("9007199254740994", fields[1].Value);
        Assert.Equal("9007199254740993 \u2192 9007199254740994", Inline(cell));
    }

    [Fact]
    public void Change_NullableCompositeSide_RendersShapeNotStructDump()
    {
        var cell = new Change<Fraction?>(null, new Fraction(1, 2));
        Assert.Equal(" \u2192 1/2", Inline(cell));

        var fields = Decompose(cell);
        Assert.Equal([new("after_count", "1"), new("after_total", "2")], fields);
    }

    [Fact]
    public void WriteCompositeTable_Jsonl_HandlesEmptyAndNumericKeys()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter(),
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl });
        // "!!!" and "???" both normalize to empty; "2" is a bare digit — all must stay distinct.
        writer.WriteCompositeTable(
            new MarkoutCompositeRow("segments",
                new Segments(new Segment("!!!", 1), new Segment("2", 2), new Segment("???", 3))));

        var line = sw.ToString().ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries)[0];
        var root = JsonDocument.Parse(line).RootElement;

        var props = root.EnumerateObject().ToList();
        Assert.Equal(4, props.Count); // field + 3 distinct value columns
        Assert.Equal(props.Count, props.Select(p => p.Name).Distinct().Count());
        Assert.All(props, p => Assert.False(string.IsNullOrEmpty(p.Name)));
        var values = props.Where(p => p.Name != "field").Select(p => p.Value.GetString()).OrderBy(v => v).ToList();
        Assert.Equal(["1", "2", "3"], values);
    }

    [Fact]
    public void Generated_LinkedComposite_RendersLinkOnScalarRow()
    {
        var card = new LinkedCompositeCard { Score = new Change<long>(10, 20), Url = "https://example.com" };
        var output = MarkoutSerializer.Serialize(card, LinkedCompositeContext.Default);

        Assert.Contains("[https://example.com](https://example.com)", output);
        Assert.Contains("| score | 10 \u2192 20 |", output);
    }

    [Fact]
    public void Change_Scalar_LargeLong_AbsoluteDeltaIsExact()
    {
        // The absolute delta must be computed in long, not via double.
        var cell = new Change<long>(9007199254740993L, 9007199254740994L);
        Assert.Equal("1", Decompose(cell, new MarkoutCellFormat(Delta.Absolute))[2].Value);
        Assert.Equal("9007199254740993 \u2192 9007199254740994 (+1)", Inline(cell, new MarkoutCellFormat(Delta.Absolute)));
    }

    [Fact]
    public void Change_Scalar_ExtremeAbsoluteDelta_DoesNotOverflow()
    {
        // long endpoints must not wrap around, and decimal extremes must not throw.
        var lng = new Change<long>(long.MinValue, long.MaxValue);
        Assert.Equal("18446744073709551615", Decompose(lng, new MarkoutCellFormat(Delta.Absolute))[2].Value);

        var dec = new Change<decimal>(decimal.MinValue, decimal.MaxValue);
        Assert.Null(Record.Exception(() => Decompose(dec, new MarkoutCellFormat(Delta.Absolute))));
    }

    [Fact]
    public void Generated_ReferenceTypeCell_SkipNull_IsSkipped()
    {
        var withNote = new RefCellCard { Score = new Change<long>(1, 2), Note = new TextCell("hi") };
        Assert.Contains("| Note | hi |", MarkoutSerializer.Serialize(withNote, RefCellContext.Default));

        var withoutNote = new RefCellCard { Score = new Change<long>(1, 2), Note = null };
        Assert.DoesNotContain("Note", MarkoutSerializer.Serialize(withoutNote, RefCellContext.Default));
    }

    // ── Structured-output follow-ups (#124 typed values, #125 heterogeneous JSONL) ──

    [Fact]
    public void WriteCompositeTable_Jsonl_IsHeterogeneous_OmitsAbsentKeys()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter(),
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, OmitEmptyJsonFields = true });
        writer.WriteCompositeTable(SampleRows());
        var lines = sw.ToString().ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Each record carries only its own keys — a scalar Change row has no segment columns.
        var session = JsonDocument.Parse(lines[0]).RootElement;
        Assert.True(session.TryGetProperty("before", out _));
        Assert.False(session.TryGetProperty("web_before", out _));

        var tools = JsonDocument.Parse(lines[1]).RootElement;
        Assert.True(tools.TryGetProperty("web_before", out _));
        Assert.False(tools.TryGetProperty("before", out _));

        var verdict = JsonDocument.Parse(lines[2]).RootElement;
        Assert.Equal(new[] { "field", "value" }, verdict.EnumerateObject().Select(p => p.Name).ToArray());
    }

    [Fact]
    public void WriteCompositeTable_Jsonl_TypedValues_EmitsNumbersButKeepsStrings()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter(),
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, JsonTypedValues = true });
        writer.WriteCompositeTable(SampleRows());
        var lines = sw.ToString().ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var session = JsonDocument.Parse(lines[0]).RootElement;
        Assert.Equal(JsonValueKind.Number, session.GetProperty("before").ValueKind);
        Assert.Equal(98555, session.GetProperty("before").GetInt64());
        Assert.Equal(-38, session.GetProperty("delta_pct").GetInt32());

        var verdict = JsonDocument.Parse(lines[2]).RootElement;
        Assert.Equal(JsonValueKind.String, verdict.GetProperty("value").ValueKind);
        Assert.Equal("BETTER", verdict.GetProperty("value").GetString());
    }

    [Fact]
    public void WriteCompositeTable_Tsv_StaysUniform_BlankForAbsentColumns()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter(),
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Tsv });
        writer.WriteCompositeTable(SampleRows());
        var lines = sw.ToString().ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // TSV keeps the uniform union: every row has the same column count, absent cells blank.
        var width = lines[0].Split('\t').Length;
        Assert.All(lines, line => Assert.Equal(width, line.Split('\t').Length));
    }

    [Fact]
    public void WriteCompositeTable_Jsonl_TypedValues_KeepsIdentityColumnAsString()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter(),
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, JsonTypedValues = true });
        // Numeric/boolean row labels must not be type-coerced — the identity key stays a string.
        writer.WriteCompositeTable(
            new MarkoutCompositeRow("2024", new Fraction(3, 4)),
            new MarkoutCompositeRow("true", new Fraction(1, 2)));
        var lines = sw.ToString().ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var row0 = JsonDocument.Parse(lines[0]).RootElement;
        Assert.Equal(JsonValueKind.String, row0.GetProperty("field").ValueKind);
        Assert.Equal("2024", row0.GetProperty("field").GetString());
        Assert.Equal(JsonValueKind.Number, row0.GetProperty("count").ValueKind); // data still typed

        var row1 = JsonDocument.Parse(lines[1]).RootElement;
        Assert.Equal(JsonValueKind.String, row1.GetProperty("field").ValueKind);
        Assert.Equal("true", row1.GetProperty("field").GetString());
    }
}
