using Markout;
using Markout.Formatting;

namespace Markout.Tests;

[Collection("ConsoleError")]
public class DiagramWriterTests
{
    [Fact]
    public void DiagramWriter_ImplementsExpectedInterfaces()
    {
        var writer = new DiagramWriter();
        Assert.IsAssignableFrom<IMarkoutFormatter>(writer);
        Assert.IsAssignableFrom<IHeadingFormatter>(writer);
        Assert.IsAssignableFrom<ITreeFormatter>(writer);
        Assert.IsAssignableFrom<IMetricsFormatter>(writer);
    }

    [Fact]
    public void WriteTree_Renders()
    {
        var orch = MarkoutWriter.Create(new DiagramWriter());
        orch.WriteTree(
            new TreeNode("Root", null,
                new TreeNode("Child A"),
                new TreeNode("Child B")));
        var output = orch.ToString();
        Assert.Contains("Root", output);
        Assert.Contains("Child A", output);
        Assert.Contains("Child B", output);
        Assert.Contains("├─", output);
        Assert.Contains("└─", output);
    }

    [Fact]
    public void WriteHeading_Renders()
    {
        var orch = MarkoutWriter.Create(new DiagramWriter());
        orch.WriteHeading(1, "My Diagram");
        Assert.Contains("My Diagram", orch.ToString());
    }

    [Fact]
    public void WriteTable_ReturnsFalse()
    {
        var orch = MarkoutWriter.Create(new DiagramWriter());
        var result = orch.WriteTable(["Col1"], [["val1"]]);
        Assert.False(result);
        Assert.Equal("", orch.ToString());
    }

    [Fact]
    public void WriteFields_ReturnsFalse()
    {
        var orch = MarkoutWriter.Create(new DiagramWriter());
        var result = orch.WriteFields(new MarkoutField("Key", "Value"));
        Assert.False(result);
        Assert.Equal("", orch.ToString());
    }

    [Fact]
    public void WriteMetrics_Renders()
    {
        var orch = MarkoutWriter.Create(new DiagramWriter());
        orch.WriteMetrics([
            new Metric("Toronto", 8),
            new Metric("Vancouver", 3),
        ]);
        var output = orch.ToString();
        Assert.Contains("Toronto", output);
        Assert.Contains("Vancouver", output);
        Assert.Contains("█", output);
        Assert.Contains("8", output);
        Assert.Contains("3", output);
    }

    [Fact]
    public void WriteMetrics_ScalesProportionally()
    {
        var orch = MarkoutWriter.Create(new DiagramWriter());
        orch.WriteMetrics([
            new Metric("Big", 10),
            new Metric("Small", 1),
        ], maxBarWidth: 20);
        var output = orch.ToString();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var bigBlocks = lines[0].Count(c => c == '█');
        var smallBlocks = lines[1].Count(c => c == '█');
        Assert.True(bigBlocks > smallBlocks * 5, $"Expected big ({bigBlocks}) >> small ({smallBlocks})");
    }
}
