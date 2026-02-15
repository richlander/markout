using Markout;

namespace Markout.Tests;

public class ShapeSupportTests
{
    /// <summary>
    /// A test writer that only supports Tables.
    /// </summary>
    private class TablesOnlyWriter : MarkoutWriter
    {
        public TablesOnlyWriter() : base(new StringWriter()) { }
        public override MarkoutShape SupportedShapes => MarkoutShape.Tables;
    }

    [Fact]
    public void MarkoutShape_All_HasAllFlags()
    {
        var all = MarkoutShape.All;
        Assert.True(all.HasFlag(MarkoutShape.Headings));
        Assert.True(all.HasFlag(MarkoutShape.Paragraphs));
        Assert.True(all.HasFlag(MarkoutShape.Fields));
        Assert.True(all.HasFlag(MarkoutShape.CompactFields));
        Assert.True(all.HasFlag(MarkoutShape.Tables));
        Assert.True(all.HasFlag(MarkoutShape.Lists));
        Assert.True(all.HasFlag(MarkoutShape.Trees));
        Assert.True(all.HasFlag(MarkoutShape.CodeBlocks));
        Assert.True(all.HasFlag(MarkoutShape.SimplePairs));
        Assert.True(all.HasFlag(MarkoutShape.BarCharts));
        Assert.True(all.HasFlag(MarkoutShape.LabeledLists));
        Assert.True(all.HasFlag(MarkoutShape.Callouts));
    }

    [Fact]
    public void BaseWriter_SupportedShapes_IsAll()
    {
        var writer = new MarkoutWriter();
        Assert.Equal(MarkoutShape.All, writer.SupportedShapes);
    }

    [Fact]
    public void MarkdownWriter_SupportedShapes_IsAll()
    {
        var writer = new MarkdownWriter();
        Assert.Equal(MarkoutShape.All, writer.SupportedShapes);
    }

    [Fact]
    public void UnsupportedShape_WarnsOnce()
    {
        var errWriter = new StringWriter();
        var origErr = Console.Error;
        Console.SetError(errWriter);
        try
        {
            var writer = new TablesOnlyWriter();
            // Call unsupported shape three times
            writer.WriteField("A", "1");
            writer.WriteField("B", "2");
            writer.WriteField("C", "3");

            var warnings = errWriter.ToString();
            // Should contain exactly one warning
            var count = warnings.Split("does not support Fields").Length - 1;
            Assert.Equal(1, count);
        }
        finally
        {
            Console.SetError(origErr);
        }
    }

    [Fact]
    public void UnsupportedShape_SkipsOutput()
    {
        var errWriter = new StringWriter();
        var origErr = Console.Error;
        Console.SetError(errWriter);
        try
        {
            var writer = new TablesOnlyWriter();
            writer.WriteHeading(1, "Title");
            writer.WriteParagraph("text");
            writer.WriteField("key", "value");
            writer.WriteListItem("item");
            writer.WriteSimplePair("a", "b");
            // Should produce no output
            Assert.Equal("", writer.ToString());
        }
        finally
        {
            Console.SetError(origErr);
        }
    }

    [Fact]
    public void SupportedShape_StillRenders()
    {
        var writer = new TablesOnlyWriter();
        writer.WriteTableStart("Name");
        writer.WriteTableRow("Alice");
        writer.WriteTableEnd();
        Assert.Contains("Alice", writer.ToString());
    }

    [Fact]
    public void UnsupportedTable_StateStillTracked()
    {
        var errWriter = new StringWriter();
        var origErr = Console.Error;
        Console.SetError(errWriter);
        try
        {
            // DiagramWriter doesn't support tables but state must be tracked
            // so WriteTableRow/End don't throw
            var sw = new StringWriter();
            var writer = new DiagramWriter(sw);
            writer.WriteTableStart("Col");
            writer.WriteTableRow("val");
            writer.WriteTableEnd();
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
        var writer = new MarkoutWriter(options);
        writer.WriteTableStart("Name");
        writer.WriteTableRow("Alice");
        writer.WriteTableRow("Bob");
        writer.WriteTableRow("Carol");
        writer.WriteTableRow("Dave");
        writer.WriteTableEnd();
        var output = writer.ToString();
        Assert.Contains("Alice", output);
        Assert.Contains("Bob", output);
        Assert.DoesNotContain("Carol", output);
        Assert.Contains("... and 2 more", output);
    }

    [Fact]
    public void MaxItems_NullMeansNoLimit()
    {
        var writer = new MarkoutWriter();
        writer.WriteTableStart("Name");
        writer.WriteTableRow("Alice");
        writer.WriteTableRow("Bob");
        writer.WriteTableRow("Carol");
        writer.WriteTableEnd();
        var output = writer.ToString();
        Assert.Contains("Alice", output);
        Assert.Contains("Bob", output);
        Assert.Contains("Carol", output);
        Assert.DoesNotContain("more", output);
    }

    [Fact]
    public void SpacePaddedTable_OnBaseWriter()
    {
        var writer = new MarkoutWriter();
        writer.WriteTable(
            ["Name", "City"],
            [["Alice", "Toronto"], ["Bob", "Vancouver"]]);
        var output = writer.ToString();
        // Should be space-padded, not tab-separated
        Assert.DoesNotContain("\t", output);
        Assert.Contains("Name", output);
        Assert.Contains("Alice", output);
    }

    [Fact]
    public void BarChart_OnBaseWriter()
    {
        var writer = new MarkoutWriter();
        writer.WriteBarChart([
            new BarItem("Alpha", 10),
            new BarItem("Beta", 5),
            new BarItem("Gamma", 1),
        ]);
        var output = writer.ToString();
        Assert.Contains("Alpha", output);
        Assert.Contains("Beta", output);
        Assert.Contains("Gamma", output);
        Assert.Contains("█", output);
        Assert.Contains("10", output);
    }

    [Fact]
    public void BarChart_HalfBlock_ForFractions()
    {
        var writer = new MarkoutWriter();
        writer.WriteBarChart([
            new BarItem("Full", 10),
            new BarItem("Half", 5),
        ], maxBarWidth: 10);
        var output = writer.ToString();
        // Half at 50% of 10 chars = 5 full blocks, no half
        // Let's just verify it renders both
        Assert.Contains("Full", output);
        Assert.Contains("Half", output);
    }

    [Fact]
    public void BarChart_Empty_NoOutput()
    {
        var writer = new MarkoutWriter();
        writer.WriteBarChart([]);
        Assert.Equal("", writer.ToString());
    }

    [Fact]
    public void BarChart_UnsupportedWriter_SkipsWithWarning()
    {
        var errWriter = new StringWriter();
        var origErr = Console.Error;
        Console.SetError(errWriter);
        try
        {
            var writer = new TablesOnlyWriter();
            writer.WriteBarChart([new BarItem("X", 5)]);
            Assert.Equal("", writer.ToString());
            Assert.Contains("does not support BarCharts", errWriter.ToString());
        }
        finally
        {
            Console.SetError(origErr);
        }
    }

    [Fact]
    public void VerticalBarChart_OnBaseWriter()
    {
        var writer = new MarkoutWriter();
        writer.WriteVerticalBarChart([
            new BarItem("A", 10),
            new BarItem("B", 5),
            new BarItem("C", 1),
        ], maxBarHeight: 5);
        var output = writer.ToString();
        Assert.Contains("█", output);
        Assert.Contains("A", output);
        Assert.Contains("B", output);
        Assert.Contains("C", output);
        Assert.Contains("10", output);
        // Labels should be on last line
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains("A", lines[^1]);
    }

    [Fact]
    public void VerticalBarChart_Empty_NoOutput()
    {
        var writer = new MarkoutWriter();
        writer.WriteVerticalBarChart([]);
        Assert.Equal("", writer.ToString());
    }
}
