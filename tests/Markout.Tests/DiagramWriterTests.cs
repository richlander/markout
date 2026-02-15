using Markout;

namespace Markout.Tests;

public class DiagramWriterTests
{
    [Fact]
    public void SupportedShapes_ReturnsHeadingsTreesAndMetrics()
    {
        var writer = new DiagramWriter(TextWriter.Null);
        Assert.Equal(MarkoutShape.Headings | MarkoutShape.Trees | MarkoutShape.Metrics, writer.SupportedShapes);
    }

    [Fact]
    public void WriteTree_Renders()
    {
        var sw = new StringWriter();
        var writer = new DiagramWriter(sw);
        writer.WriteTree([
            new TreeNode("Root", [
                new TreeNode("Child A"),
                new TreeNode("Child B")
            ])
        ]);
        var output = sw.ToString();
        Assert.Contains("Root", output);
        Assert.Contains("Child A", output);
        Assert.Contains("Child B", output);
        Assert.Contains("├─", output);
        Assert.Contains("└─", output);
    }

    [Fact]
    public void WriteHeading_Renders()
    {
        var sw = new StringWriter();
        var writer = new DiagramWriter(sw);
        writer.WriteHeading(1, "My Diagram");
        Assert.Contains("My Diagram", sw.ToString());
    }

    [Fact]
    public void WriteTable_Skipped_WithWarning()
    {
        var errWriter = new StringWriter();
        var origErr = Console.Error;
        Console.SetError(errWriter);
        try
        {
            var sw = new StringWriter();
            var writer = new DiagramWriter(sw);
            writer.WriteTableStart("Col1");
            writer.WriteTableRow("val1");
            writer.WriteTableEnd();
            Assert.Equal("", sw.ToString());
            Assert.Contains("does not support Tables", errWriter.ToString());
        }
        finally
        {
            Console.SetError(origErr);
        }
    }

    [Fact]
    public void WriteField_Skipped_WithWarning()
    {
        var errWriter = new StringWriter();
        var origErr = Console.Error;
        Console.SetError(errWriter);
        try
        {
            var sw = new StringWriter();
            var writer = new DiagramWriter(sw);
            writer.WriteField("Key", "Value");
            Assert.Equal("", sw.ToString());
            Assert.Contains("does not support Fields", errWriter.ToString());
        }
        finally
        {
            Console.SetError(origErr);
        }
    }

    [Fact]
    public void WriteMetrics_Renders()
    {
        var sw = new StringWriter();
        var writer = new DiagramWriter(sw);
        writer.WriteMetrics([
            new Metric("Toronto", 8),
            new Metric("Vancouver", 3),
        ]);
        var output = sw.ToString();
        Assert.Contains("Toronto", output);
        Assert.Contains("Vancouver", output);
        Assert.Contains("█", output);
        Assert.Contains("8", output);
        Assert.Contains("3", output);
    }

    [Fact]
    public void WriteMetrics_ScalesProportionally()
    {
        var sw = new StringWriter();
        var writer = new DiagramWriter(sw);
        writer.WriteMetrics([
            new Metric("Big", 10),
            new Metric("Small", 1),
        ], maxBarWidth: 20);
        var output = sw.ToString();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        // Big should have way more block chars than Small
        var bigBlocks = lines[0].Count(c => c == '█');
        var smallBlocks = lines[1].Count(c => c == '█');
        Assert.True(bigBlocks > smallBlocks * 5, $"Expected big ({bigBlocks}) >> small ({smallBlocks})");
    }
}
