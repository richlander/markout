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
}
