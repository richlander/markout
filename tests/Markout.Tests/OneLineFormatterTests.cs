using Markout;
using Markout.Formatting;

namespace Markout.Tests;

[Collection("ConsoleError")]
public class OneLineFormatterTests
{
    [Fact]
    public void SupportedShapes_ReturnsTablesListsAndFields()
    {
        var formatter = new OneLineFormatter();
        Assert.IsAssignableFrom<ITableFormatter>(formatter);
        Assert.IsAssignableFrom<IListFormatter>(formatter);
        Assert.IsAssignableFrom<IFieldFormatter>(formatter);
    }

    [Fact]
    public void WriteTable_SpacePaddedColumns()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new OneLineFormatter());
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
        var writer = MarkoutWriter.Create(sw, new OneLineFormatter(showHeader: false));
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
        var writer = MarkoutWriter.Create(sw, new OneLineFormatter(), options);
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
        var writer = MarkoutWriter.Create(sw, new OneLineFormatter(), options);
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
        var writer = MarkoutWriter.Create(sw, new OneLineFormatter());
        writer.WriteListItem("hello");
        Assert.Equal("hello\n", sw.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void WriteHeading_Suppressed()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new OneLineFormatter());
        writer.WriteHeading(1, "Title");
        Assert.Equal("", sw.ToString());
    }

    [Fact]
    public void WriteFields_BufferedAndRenderedAsTable()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new OneLineFormatter());
        writer.WriteFields(
            new MarkoutField("Name", "System.Text.Json"),
            new MarkoutField("Version", "11.0.0"));

        // Fields are rendered inline (values pipe-separated)
        var output = sw.ToString();

        Assert.Contains("System.Text.Json", output);
        Assert.Contains("11.0.0", output);
        Assert.Contains("|", output);
    }

    [Fact]
    public void WriteFieldsInline_RendersInline()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new OneLineFormatter());
        writer.WriteFieldsInline(
            new MarkoutField("Name", "System.Text.Json"),
            new MarkoutField("Version", "11.0.0"));

        var output = sw.ToString();

        // Should be rendered inline, values pipe-separated with field names
        Assert.Contains("System.Text.Json", output);
        Assert.Contains("|", output);
        Assert.Contains("11.0.0", output);
    }

    [Fact]
    public void WriteFields_FlushedOnHeading()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new OneLineFormatter());
        writer.WriteHeading(2, "Section 1", null);
        writer.WriteFields([new("Key1", "Value1")]);
        writer.WriteHeading(2, "Section 2", null);  // Should flush buffered fields

        var output = sw.ToString();
        Assert.Contains("Value1", output);
    }

    [Fact]
    public void WriteParagraph_ReturnsFalse()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new OneLineFormatter());
        var result = writer.WriteParagraph("hello");
        // OneLineFormatter doesn't implement IBlockFormatter
        Assert.False(result);
        Assert.Equal("", sw.ToString());
    }
}
