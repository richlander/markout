using Markout;

namespace Markout.Tests;

[Collection("ConsoleError")]
public class OneLineWriterTests
{
    [Fact]
    public void SupportedShapes_ReturnsTablesListsAndFields()
    {
        var writer = new OneLineWriter(TextWriter.Null);
        Assert.Equal(MarkoutShape.Tables | MarkoutShape.Lists | MarkoutShape.Fields, writer.SupportedShapes);
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
    public void WriteFields_BufferedAndRenderedAsTable()
    {
        var sw = new StringWriter();
        var writer = new OneLineWriter(sw);
        writer.WriteFields(
            new MarkoutField("Name", "System.Text.Json"),
            new MarkoutField("Version", "11.0.0"));

        // Fields are buffered until ToString() or next heading
        var output = writer.ToString();

        // Should be rendered as a FIELD/VALUE table
        Assert.Contains("FIELD", output);
        Assert.Contains("VALUE", output);
        Assert.Contains("Name", output);
        Assert.Contains("System.Text.Json", output);
        Assert.Contains("Version", output);
        Assert.Contains("11.0.0", output);
    }

    [Fact]
    public void WriteFieldsInline_RendersInline()
    {
        var sw = new StringWriter();
        var writer = new OneLineWriter(sw);
        writer.WriteFieldsInline(
            new MarkoutField("Name", "System.Text.Json"),
            new MarkoutField("Version", "11.0.0"));

        var output = sw.ToString();

        // Should be rendered inline, values only, pipe-separated
        Assert.Contains("System.Text.Json", output);
        Assert.Contains("|", output);
        Assert.Contains("11.0.0", output);
        // Field names are not included in inline mode
        Assert.DoesNotContain("Name:", output);
    }

    [Fact]
    public void WriteFields_FlushedOnHeading()
    {
        var sw = new StringWriter();
        var writer = new OneLineWriter(sw);
        writer.WriteHeading(2, "Section 1", null);
        writer.WriteFields([new("Key1", "Value1")]);
        writer.WriteHeading(2, "Section 2", null);  // Should flush buffered fields

        var output = sw.ToString();
        Assert.Contains("Key1", output);
        Assert.Contains("Value1", output);
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
