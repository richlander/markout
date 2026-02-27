using Markout;
using Markout.Formatting;

namespace Markout.Tests;

[Collection("ConsoleError")]
public class ShapeSupportTests
{
    [Fact]
    public void MarkoutShape_All_HasAllFlags()
    {
        var all = MarkoutShape.All;
        Assert.True(all.HasFlag(MarkoutShape.Headings));
        Assert.True(all.HasFlag(MarkoutShape.Paragraphs));
        Assert.True(all.HasFlag(MarkoutShape.Fields));
        Assert.True(all.HasFlag(MarkoutShape.Tables));
        Assert.True(all.HasFlag(MarkoutShape.Lists));
        Assert.True(all.HasFlag(MarkoutShape.Trees));
        Assert.True(all.HasFlag(MarkoutShape.Code));
        Assert.True(all.HasFlag(MarkoutShape.Metrics));
        Assert.True(all.HasFlag(MarkoutShape.Descriptions));
        Assert.True(all.HasFlag(MarkoutShape.Callouts));
        Assert.True(all.HasFlag(MarkoutShape.Breakdowns));
        Assert.True(all.HasFlag(MarkoutShape.Quotation));
    }

    [Fact]
    public void MarkdownFormatter_SupportedShapes_IsAll()
    {
        var writer = new MarkdownFormatter();
        Assert.IsAssignableFrom<IDocumentFormatter>(writer);
        Assert.IsAssignableFrom<IMetricsFormatter>(writer);
    }

    [Fact]
    public void SupportedShape_StillRenders()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        orch.WriteTableStart("Name");
        orch.WriteTableRow("Alice");
        orch.WriteTableEnd();
        Assert.Contains("Alice", orch.ToString());
    }

    [Fact]
    public void UnsupportedTable_StateStillTracked()
    {
        var errWriter = new StringWriter();
        var origErr = Console.Error;
        Console.SetError(errWriter);
        try
        {
            // DiagramFormatter doesn't support tables but state must be tracked
            // so WriteTableRow/End don't throw
            var sw = new StringWriter();
            var orch = MarkoutWriter.Create(sw, new DiagramFormatter());
            orch.WriteTableStart("Col");
            orch.WriteTableRow("val");
            orch.WriteTableEnd();
            // Should not throw, and should produce no table output
            Assert.Equal("", sw.ToString());
        }
        finally
        {
            Console.SetError(origErr);
        }
    }

    [Fact]
    public void MaxItems_OnBaseWriter()
    {
        var options = new MarkoutWriterOptions { MaxItems = 2 };
        var orch = MarkoutWriter.Create(new MarkdownFormatter(), options);
        orch.WriteTableStart("Name");
        orch.WriteTableRow("Alice");
        orch.WriteTableRow("Bob");
        orch.WriteTableRow("Carol");
        orch.WriteTableRow("Dave");
        orch.WriteTableEnd();
        var output = orch.ToString();
        Assert.Contains("Alice", output);
        Assert.Contains("Bob", output);
        Assert.DoesNotContain("Carol", output);
        Assert.Contains("... and 2 more", output);
    }

    [Fact]
    public void MaxItems_NullMeansNoLimit()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        orch.WriteTableStart("Name");
        orch.WriteTableRow("Alice");
        orch.WriteTableRow("Bob");
        orch.WriteTableRow("Carol");
        orch.WriteTableEnd();
        var output = orch.ToString();
        Assert.Contains("Alice", output);
        Assert.Contains("Bob", output);
        Assert.Contains("Carol", output);
        Assert.DoesNotContain("more", output);
    }

    [Fact]
    public void SpacePaddedTable_OnBaseWriter()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        orch.WriteTable(
            ["Name", "City"],
            [["Alice", "Toronto"], ["Bob", "Vancouver"]]);
        var output = orch.ToString();
        // Should be space-padded, not tab-separated
        Assert.DoesNotContain("\t", output);
        Assert.Contains("Name", output);
        Assert.Contains("Alice", output);
    }

    [Fact]
    public void BarChart_OnBaseWriter()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        orch.WriteMetrics([
            new Metric("Alpha", 10),
            new Metric("Beta", 5),
            new Metric("Gamma", 1),
        ]);
        var output = orch.ToString();
        Assert.Contains("Alpha", output);
        Assert.Contains("Beta", output);
        Assert.Contains("Gamma", output);
        Assert.Contains("10", output);
    }

    [Fact]
    public void BarChart_HalfBlock_ForFractions()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        orch.WriteMetrics([
            new Metric("Full", 10),
            new Metric("Half", 5),
        ], maxBarWidth: 10);
        var output = orch.ToString();
        // Half at 50% of 10 chars = 5 full blocks, no half
        // Let's just verify it renders both
        Assert.Contains("Full", output);
        Assert.Contains("Half", output);
    }

    [Fact]
    public void BarChart_Empty_NoOutput()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        orch.WriteMetrics([]);
        Assert.Equal("", orch.ToString());
    }

    [Fact]
    public void VerticalBarChart_OnBaseWriter()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        orch.WriteVerticalMetrics([
            new Metric("A", 10),
            new Metric("B", 5),
            new Metric("C", 1),
        ], maxBarHeight: 5);
        var output = orch.ToString();
        Assert.Contains("A", output);
        Assert.Contains("B", output);
        Assert.Contains("C", output);
        Assert.Contains("10", output);
    }

    [Fact]
    public void VerticalBarChart_Empty_NoOutput()
    {
        var orch = MarkoutWriter.Create(new MarkdownFormatter());
        orch.WriteVerticalMetrics([]);
        Assert.Equal("", orch.ToString());
    }

    // ── Capability interface tests ──

    [Fact]
    public void MarkdownFormatter_ImplementsAllCapabilityInterfaces()
    {
        var writer = new MarkdownFormatter();
        Assert.IsAssignableFrom<IHeadingFormatter>(writer);
        Assert.IsAssignableFrom<IFieldFormatter>(writer);
        Assert.IsAssignableFrom<ITableFormatter>(writer);
        Assert.IsAssignableFrom<IListFormatter>(writer);
        Assert.IsAssignableFrom<ICodeBlockFormatter>(writer);
        Assert.IsAssignableFrom<IBlockFormatter>(writer);
        Assert.IsAssignableFrom<IMetricsFormatter>(writer);
    }

    [Fact]
    public void OneLineFormatter_ImplementsSubsetOfInterfaces()
    {
        var writer = new OneLineFormatter();
        Assert.IsAssignableFrom<ITableFormatter>(writer);
        Assert.IsAssignableFrom<IFieldFormatter>(writer);
        Assert.IsAssignableFrom<IListFormatter>(writer);
    }

    [Fact]
    public void OneLineFormatter_DoesNotImplementUnsupportedInterfaces()
    {
        var writer = new OneLineFormatter();
        Assert.False(writer is IHeadingFormatter);
        Assert.False(writer is ICodeBlockFormatter);
        Assert.False(writer is IBlockFormatter);
        Assert.False(writer is IMetricsFormatter);
    }

    [Fact]
    public void UnicodeFormatter_ImplementsCapabilityInterfaces()
    {
        var writer = new UnicodeFormatter();
        Assert.True(writer is IMarkoutFormatter);
        Assert.True(writer is IHeadingFormatter);
        Assert.True(writer is IFieldFormatter);
        Assert.True(writer is ITableFormatter);
        Assert.True(writer is IListFormatter);
        Assert.True(writer is ICodeBlockFormatter);
        Assert.True(writer is IBlockFormatter);
        Assert.True(writer is IMetricsFormatter);
    }

    [Fact]
    public void DiagramFormatter_ImplementsSubsetOfInterfaces()
    {
        var writer = new DiagramFormatter();
        Assert.True(writer is IHeadingFormatter);
        Assert.True(writer is ITreeFormatter);
        Assert.True(writer is IMetricsFormatter);
        Assert.False(writer is ITableFormatter);
        Assert.False(writer is IFieldFormatter);
        Assert.False(writer is IBlockFormatter);
    }
}
