using System.Text.Json;
using Markout;

namespace Markout.Tests;

public class MultiSourceTableTests
{
    // Roles are model names; each metric cell is a nested baseline -> current Change<Shape>.
    private static MultiSourceRow[] MatrixRows() =>
    [
        new("output tok",
            new Source("opus", new Change<Share>(new Share(5056, 21067), new Share(3129, 13037))),
            new Source("gpt5", new Change<Share>(new Share(6100, 21800), new Share(3500, 14000)))),
        new("tool calls",
            new Source("opus", new Change<Segments>(
                new Segments(new Segment("web", 21), new Segment("bash", 171)),
                new Segments(new Segment("web", 0), new Segment("bash", 75)))),
            new Source("gpt5", new Change<Segments>(
                new Segments(new Segment("web", 30), new Segment("bash", 150)),
                new Segments(new Segment("web", 5), new Segment("bash", 80))))),
        new("verdict",
            new Source("opus", new Verdict(GateStatus.Good, "BETTER")),
            new Source("gpt5", new Verdict(GateStatus.Good, "BETTER"))),
    ];

    // ── Dense Markdown pivot: roles become columns ──

    [Fact]
    public void Markdown_PivotsRolesIntoColumns()
    {
        var writer = new MarkoutWriter(new MarkdownFormatter());
        writer.WriteMultiSourceTable("Metric", MatrixRows());
        var output = writer.ToString();

        Assert.Contains("| Metric | opus | gpt5 |", output);
        Assert.Contains("| output tok | 5056 (24%) \u2192 3129 (24%) | 6100 (28%) \u2192 3500 (25%) |", output);
        Assert.Contains("| tool calls | 21/171 \u2192 0/75 | 30/150 \u2192 5/80 |", output);
        Assert.Contains("| verdict | BETTER | BETTER |", output);
    }

    [Fact]
    public void Markdown_AbsentRoleRendersDash()
    {
        MultiSourceRow[] rows =
        [
            new("full", new Source("opus", new Change<long>(1, 2)), new Source("gpt5", new Change<long>(3, 4))),
            new("opus only", new Source("opus", new Change<long>(5, 6))),
        ];

        var writer = new MarkoutWriter(new MarkdownFormatter());
        writer.WriteMultiSourceTable("Metric", rows);
        var output = writer.ToString();

        Assert.Contains("| opus only | 5 \u2192 6 | - |", output);
    }

    // ── Caller-controlled column order (insertion order, not sorted, not first-row-only) ──

    [Fact]
    public void ColumnOrder_IsCallerInsertionOrder_NotSorted()
    {
        MultiSourceRow[] rows =
        [
            new("m1", new Source("zebra", new Change<long>(1, 2))),
            new("m2", new Source("zebra", new Change<long>(3, 4)), new Source("alpha", new Change<long>(5, 6))),
        ];

        var writer = new MarkoutWriter(new MarkdownFormatter());
        writer.WriteMultiSourceTable("Metric", rows);
        var output = writer.ToString();

        // "zebra" first (first appearance), "alpha" appended when it first appears in row 2.
        Assert.Contains("| Metric | zebra | alpha |", output);
        // m1 has no "alpha" -> dash.
        Assert.Contains("| m1 | 1 \u2192 2 | - |", output);
    }

    // ── n-column scalar series: goal glyph on label + pairwise polarity glyphs (issue #153) ──

    private static MultiSourceRow[] WeeklySeries() =>
    [
        new MultiSourceRow("Alloc (bytes)",
            new Source("W1", 100.0), new Source("W2", 110.0),
            new Source("W3", 105.0), new Source("W4", 90.0)) { Goal = Goal.Lower },
        new MultiSourceRow("Throughput",
            new Source("W1", 50.0), new Source("W2", 55.0),
            new Source("W3", 53.0), new Source("W4", 60.0)) { Goal = Goal.Higher },
    ];

    [Fact]
    public void Markdown_ScalarSeries_GoalGlyphOnLabel_AndPairwisePolarity()
    {
        var writer = new MarkoutWriter(new MarkdownFormatter());
        writer.WriteMultiSourceTable("Benchmark", WeeklySeries());
        var output = writer.ToString();

        // Goal glyph on the label; first column has no predecessor; cols 2+ carry pairwise polarity.
        // Alloc lower-is-better: 100->110 up=bad, 110->105 down=good, 105->90 down=good.
        Assert.Contains("| Alloc (bytes) \u2193 | 100 | 110 \u2717 | 105 \u2713 | 90 \u2713 |", output);
        // Throughput higher-is-better: 50->55 up=good, 55->53 down=bad, 53->60 up=good.
        Assert.Contains("| Throughput \u2191 | 50 | 55 \u2713 | 53 \u2717 | 60 \u2713 |", output);
    }

    [Fact]
    public void Markdown_ScalarSeries_UnchangedCell_HasNoGlyph()
    {
        MultiSourceRow[] rows =
        [
            new MultiSourceRow("Errors",
                new Source("W1", 5.0), new Source("W2", 5.0), new Source("W3", 2.0)) { Goal = Goal.Lower },
        ];
        var writer = new MarkoutWriter(new MarkdownFormatter());
        writer.WriteMultiSourceTable("Benchmark", rows);
        var output = writer.ToString();

        // 5->5 neutral (no glyph); 5->2 down=good.
        Assert.Contains("| Errors \u2193 | 5 | 5 | 2 \u2713 |", output);
    }

    [Fact]
    public void Markdown_ScalarSeries_ContextGoal_HasNoGlyphs()
    {
        MultiSourceRow[] rows =
        [
            new MultiSourceRow("Count", new Source("W1", 10.0), new Source("W2", 20.0)),
        ];
        var writer = new MarkoutWriter(new MarkdownFormatter());
        writer.WriteMultiSourceTable("Benchmark", rows);
        var output = writer.ToString();

        Assert.Contains("| Count | 10 | 20 |", output);
        Assert.DoesNotContain("\u2713", output);
        Assert.DoesNotContain("\u2191", output);
    }

    [Fact]
    public void Tsv_ScalarSeries_NoGlyphsRegardlessOfGoal()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter(),
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Tsv });
        writer.WriteMultiSourceTable("Benchmark", WeeklySeries());
        var output = sw.ToString();

        Assert.DoesNotContain("\u2713", output);
        Assert.DoesNotContain("\u2717", output);
        Assert.DoesNotContain("\u2193", output);
        Assert.DoesNotContain("\u2191", output);
        Assert.Contains("110", output);
    }

    // ── Decomposed JSONL: one flat record per row, {role}_{side}_{field} keys ──
    [Fact]
    public void Jsonl_DecomposesToRolePrefixedKeys()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter(),
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, OmitEmptyJsonFields = true });
        writer.WriteMultiSourceTable("Metric", MatrixRows());
        var lines = sw.ToString().ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Row 0: Change<Share> -> {role}_{side}_{value|pct}
        var tok = JsonDocument.Parse(lines[0]).RootElement;
        Assert.Equal("output tok", tok.GetProperty("metric").GetString());
        Assert.Equal("5056", tok.GetProperty("opus_before_value").GetString());
        Assert.Equal("24", tok.GetProperty("opus_before_pct").GetString());
        Assert.Equal("3500", tok.GetProperty("gpt5_after_value").GetString());

        // Row 1: Change<Segments> -> side-first labels (post-#129 flip): {role}_{side}_{label}
        var tools = JsonDocument.Parse(lines[1]).RootElement;
        Assert.Equal("21", tools.GetProperty("opus_before_web").GetString());
        Assert.Equal("75", tools.GetProperty("opus_after_bash").GetString());
        Assert.Equal("5", tools.GetProperty("gpt5_after_web").GetString());

        // Row 2: Verdict -> {role} (flat, no _status suffix)
        var verdict = JsonDocument.Parse(lines[2]).RootElement;
        Assert.Equal("BETTER", verdict.GetProperty("opus").GetString());
        Assert.Equal("BETTER", verdict.GetProperty("gpt5").GetString());
    }

    [Fact]
    public void Jsonl_Heterogeneous_OmitsAbsentRoleKeys()
    {
        MultiSourceRow[] rows =
        [
            new("m1", new Source("opus", new Change<long>(1, 2))),
            new("m2", new Source("opus", new Change<long>(3, 4)), new Source("gpt5", new Change<long>(5, 6))),
        ];

        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter(),
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, OmitEmptyJsonFields = true });
        writer.WriteMultiSourceTable("Metric", rows);
        var lines = sw.ToString().ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var m1 = JsonDocument.Parse(lines[0]).RootElement;
        Assert.True(m1.TryGetProperty("opus_before", out _));
        Assert.False(m1.TryGetProperty("gpt5_before", out _)); // absent role omitted

        var m2 = JsonDocument.Parse(lines[1]).RootElement;
        Assert.True(m2.TryGetProperty("gpt5_before", out _));
    }

    // ── TSV keeps a uniform column union ──

    [Fact]
    public void Tsv_KeepsUniformColumnUnion()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter(),
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Tsv });
        writer.WriteMultiSourceTable("Metric", MatrixRows());
        var lines = sw.ToString().ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var headers = lines[0].Split('\t');
        Assert.Equal("metric", headers[0]);
        Assert.Contains("opus_before_value", headers);
        Assert.Contains("gpt5_before_web", headers);
        Assert.Contains("opus", headers);
    }

    // ── Scalar role values (leak-triage shape): baseline/current/budget + verdict ──

    [Fact]
    public void ScalarSources_RenderAndDecomposeByRole()
    {
        MultiSourceRow[] rows =
        [
            new("arraypool-rent-not-returned",
                new Source("baseline", 2),
                new Source("current", 5),
                new Source("budget", 0),
                new Source("verdict", new Verdict(GateStatus.Bad, "REGRESSION"))),
        ];

        var writer = new MarkoutWriter(new MarkdownFormatter());
        writer.WriteMultiSourceTable("Metric", rows);
        Assert.Contains("| Metric | baseline | current | budget | verdict |", writer.ToString());
        Assert.Contains("| arraypool-rent-not-returned | 2 | 5 | 0 | REGRESSION |", writer.ToString());

        var sw = new StringWriter();
        var jsonl = MarkoutWriter.Create(sw, new TableFormatter(),
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, JsonTypedValues = true });
        jsonl.WriteMultiSourceTable("Metric", rows);
        var rec = JsonDocument.Parse(sw.ToString().ReplaceLineEndings("\n").Trim()).RootElement;

        Assert.Equal("arraypool-rent-not-returned", rec.GetProperty("metric").GetString());
        Assert.Equal(2, rec.GetProperty("baseline").GetInt32());   // typed number
        Assert.Equal(5, rec.GetProperty("current").GetInt32());
        Assert.Equal(0, rec.GetProperty("budget").GetInt32());
        Assert.Equal("REGRESSION", rec.GetProperty("verdict").GetString());
    }

    // ── Null source value is unambiguous and renders as an absent cell ──

    [Fact]
    public void NullSourceValue_CompilesAndRendersDash()
    {
        // Regression: the scalar ctor overloads must not make `new Source(role, null)` ambiguous.
        MultiSourceRow[] rows = [new("m", new Source("a", null), new Source("b", 5))];

        var writer = new MarkoutWriter(new MarkdownFormatter());
        writer.WriteMultiSourceTable("Metric", rows);

        Assert.Contains("| m | - | 5 |", writer.ToString());
    }

    // ── Colliding composed keys are disambiguated consistently across rows (no data mixing) ──

    [Fact]
    public void Jsonl_CollidingComposedKeys_AreDisambiguatedConsistentlyAcrossRows()
    {
        // role "a" + segment "b_c" and role "a_b" + segment "c" both compose to "a_b_c".
        // The two rows present the colliding sources in DIFFERENT orders; a given role must map to
        // the same column in every row (no data landing in the wrong column).
        MultiSourceRow[] rows =
        [
            new("row1",
                new Source("a", new Segments(new Segment("b_c", 1))),
                new Source("a_b", new Segments(new Segment("c", 2)))),
            new("row2",
                new Source("a_b", new Segments(new Segment("c", 3))),
                new Source("a", new Segments(new Segment("b_c", 4)))),
        ];

        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter(),
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl });
        writer.WriteMultiSourceTable("Metric", rows);
        var lines = sw.ToString().ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var r1 = JsonDocument.Parse(lines[0]).RootElement;
        var r2 = JsonDocument.Parse(lines[1]).RootElement;

        // Role "a" is always column "a_b_c"; role "a_b" is always column "a_b_c_2".
        Assert.Equal("1", r1.GetProperty("a_b_c").GetString());
        Assert.Equal("2", r1.GetProperty("a_b_c_2").GetString());
        Assert.Equal("4", r2.GetProperty("a_b_c").GetString());
        Assert.Equal("3", r2.GetProperty("a_b_c_2").GetString());
    }

    // ── Structured-section discriminator (issue #131) ──

    [Fact]
    public void Jsonl_StructuredSection_ComposesWithLabelColumn()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter(),
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, JsonTypedValues = true });
        writer.WriteMultiSourceTable("Metric", MatrixRows(), structuredSection: "grounding");
        var line0 = sw.ToString().ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries)[0];

        // section + the multi-source label column ("metric") both lead, both stay strings.
        Assert.StartsWith("{\"section\":\"grounding\",\"metric\":\"output tok\"", line0);
        var rec = JsonDocument.Parse(line0).RootElement;
        Assert.Equal("grounding", rec.GetProperty("section").GetString());
        Assert.Equal(JsonValueKind.String, rec.GetProperty("metric").ValueKind);
    }
}
