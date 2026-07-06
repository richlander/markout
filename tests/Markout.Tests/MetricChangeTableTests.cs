using System.Text.Json;
using Markout;

namespace Markout.Tests;

public class MetricChangeTableTests
{
    private static MetricChange<int>[] Rows() =>
    [
        new("Failures", 0, 7, 0, "allowed failures", GateStatus.Bad, "regression"),
        new("Changed bodies", 45, 46, Status: GateStatus.Warning, StatusLabel: "drift"),
        new("Compared bodies", 78, 78), // ungated, no status
    ];

    [Fact]
    public void Markdown_RendersMetricChangeTargetStatusColumns()
    {
        var writer = new MarkoutWriter(new MarkdownFormatter());
        writer.WriteMetricChangeTable(Rows());
        var output = writer.ToString();

        // Dense default: Status column dropped; caller StatusLabel inlined into the Change cell.
        Assert.Contains("| Metric | Change | Target |", output);
        Assert.DoesNotContain("| Metric | Change | Target | Status |", output);
        Assert.Contains("| Failures | 0 \u2192 7 (regression) | allowed failures: 0 |", output);
        Assert.Contains("| Changed bodies | 45 \u2192 46 (drift) | - |", output);
        Assert.Contains("| Compared bodies | 78 \u2192 78 | - |", output);
    }

    [Fact]
    public void Markdown_InlineGoalStatusDisabled_KeepsLegacyStatusColumn()
    {
        var writer = new MarkoutWriter(new MarkdownFormatter(),
            new MarkoutWriterOptions { InlineGoalStatus = false });
        writer.WriteMetricChangeTable(Rows());
        var output = writer.ToString();

        Assert.Contains("| Metric | Change | Target | Status |", output);
        Assert.Contains("| Failures | 0 \u2192 7 | allowed failures: 0 | regression |", output);
        Assert.Contains("| Changed bodies | 45 \u2192 46 | - | drift |", output);
        Assert.Contains("| Compared bodies | 78 \u2192 78 | - | - |", output);
    }

    [Fact]
    public void Jsonl_EmitsFlatTypedFields_OmittingAbsentTargetAndStatus()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter(),
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, OmitEmptyJsonFields = true });
        writer.WriteMetricChangeTable(Rows());
        var lines = sw.ToString().ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var failures = JsonDocument.Parse(lines[0]).RootElement;
        Assert.Equal("Failures", failures.GetProperty("metric").GetString());
        Assert.Equal("0", failures.GetProperty("before").GetString());
        Assert.Equal("7", failures.GetProperty("after").GetString());
        Assert.Equal("0", failures.GetProperty("target").GetString());
        Assert.Equal("allowed failures", failures.GetProperty("target_label").GetString());
        Assert.Equal("regression", failures.GetProperty("status").GetString());

        // Ungated row: target / target_label omitted; status present.
        var changed = JsonDocument.Parse(lines[1]).RootElement;
        Assert.Equal("45", changed.GetProperty("before").GetString());
        Assert.False(changed.TryGetProperty("target", out _));
        Assert.False(changed.TryGetProperty("target_label", out _));
        Assert.Equal("drift", changed.GetProperty("status").GetString());

        // No target, no status.
        var compared = JsonDocument.Parse(lines[2]).RootElement;
        Assert.Equal("78", compared.GetProperty("before").GetString());
        Assert.False(compared.TryGetProperty("target", out _));
        Assert.False(compared.TryGetProperty("status", out _));
    }

    [Fact]
    public void Jsonl_TypedValues_EmitsNumericFields()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter(),
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, JsonTypedValues = true });
        writer.WriteMetricChangeTable(Rows());
        var line0 = sw.ToString().ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries)[0];
        var failures = JsonDocument.Parse(line0).RootElement;

        Assert.Equal(JsonValueKind.Number, failures.GetProperty("before").ValueKind);
        Assert.Equal(7, failures.GetProperty("after").GetInt32());
        // Identity + status stay strings.
        Assert.Equal(JsonValueKind.String, failures.GetProperty("metric").ValueKind);
        Assert.Equal("regression", failures.GetProperty("status").GetString());
    }

    [Fact]
    public void Jsonl_StructuredSection_PrependsSectionColumn()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new TableFormatter(),
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, JsonTypedValues = true });
        writer.WriteMetricChangeTable(Rows(), structuredSection: "Baseline metric changes");
        var line0 = sw.ToString().ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries)[0];

        // Leading, stable section column; identity columns stay strings; typed numbers still work.
        Assert.StartsWith("{\"section\":\"Baseline metric changes\",\"metric\":\"Failures\"", line0);
        var rec = JsonDocument.Parse(line0).RootElement;
        Assert.Equal(JsonValueKind.String, rec.GetProperty("section").ValueKind);
        Assert.Equal(JsonValueKind.String, rec.GetProperty("metric").ValueKind);
        Assert.Equal(7, rec.GetProperty("after").GetInt32());
    }
}
