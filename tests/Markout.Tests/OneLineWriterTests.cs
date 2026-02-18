using Markout;

namespace Markout.Tests;

public class OneLineWriterTests
{
    [Fact]
    public void SupportedShapes_ReturnsTablesAndLists()
    {
        var writer = new OneLineWriter(TextWriter.Null);
        Assert.Equal(MarkoutShape.Tables | MarkoutShape.Lists, writer.SupportedShapes);
    }

    [Fact]
    public void WriteTable_SpacePaddedColumns()
    {
        var sw = new StringWriter();
        var writer = new OneLineWriter(sw);
        writer.WriteTable(
            ["Name", "Age"],
            [["Alice", "30"], ["Bob", "7"]]);
        var output = sw.ToString();
        Assert.Contains("NAME", output);
        Assert.Contains("AGE", output);
        Assert.Contains("Alice", output);
        // Space-padded, not tab-separated
        Assert.DoesNotContain("\t", output);
    }

    [Fact]
    public void WriteTable_NoHeader()
    {
        var sw = new StringWriter();
        var writer = new OneLineWriter(sw, showHeader: false);
        writer.WriteTable(
            ["Name", "Age"],
            [["Alice", "30"]]);
        var output = sw.ToString();
        Assert.DoesNotContain("NAME", output);
        Assert.Contains("Alice", output);
    }

    [Fact]
    public void WriteTable_WithMaxItems()
    {
        var sw = new StringWriter();
        var options = new MarkoutWriterOptions { MaxItems = 1 };
        var writer = new OneLineWriter(sw, options);
        writer.WriteTable(
            ["Name"],
            [["Alice"], ["Bob"], ["Carol"]]);
        var output = sw.ToString();
        Assert.Contains("Alice", output);
        Assert.DoesNotContain("Bob", output);
        Assert.Contains("... and 2 more", output);
    }

    [Fact]
    public void StreamingTable_WithMaxItems()
    {
        var sw = new StringWriter();
        var options = new MarkoutWriterOptions { MaxItems = 1 };
        var writer = new OneLineWriter(sw, options);
        writer.WriteTableStart("Name");
        writer.WriteTableRow("Alice");
        writer.WriteTableRow("Bob");
        writer.WriteTableRow("Carol");
        writer.WriteTableEnd();
        var output = sw.ToString();
        Assert.Contains("Alice", output);
        Assert.DoesNotContain("Bob", output);
        Assert.Contains("... and 2 more", output);
    }

    [Fact]
    public void WriteListItem_RendersPlainText()
    {
        var sw = new StringWriter();
        var writer = new OneLineWriter(sw);
        writer.WriteListItem("hello");
        Assert.Equal("hello\n", sw.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void WriteHeading_Suppressed()
    {
        var sw = new StringWriter();
        var writer = new OneLineWriter(sw);
        writer.WriteHeading(1, "Title");
        Assert.Equal("", sw.ToString());
    }

    [Fact]
    public void WriteField_Suppressed()
    {
        var errWriter = new StringWriter();
        var origErr = Console.Error;
        Console.SetError(errWriter);
        try
        {
            var sw = new StringWriter();
            var writer = new OneLineWriter(sw);
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
    public void WriteParagraph_Suppressed()
    {
        var sw = new StringWriter();
        var writer = new OneLineWriter(sw);
        writer.WriteParagraph("hello");
        // Paragraphs are unsupported — output should be empty
        Assert.Equal("", sw.ToString());
    }
}
