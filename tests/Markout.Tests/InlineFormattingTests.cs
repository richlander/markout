using System.Text.Json;

namespace Markout.Tests;

public class InlineFormattingTests
{
    [Fact]
    public void MarkdownFormatter_RendersCodeTagsInFieldsTablesAndLists()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new MarkdownFormatter());

        writer.WriteFields(new MarkoutField("Signature", "<code>List&lt;T&gt;</code>"));
        writer.WriteTable(["Name"], [["<code>Serialize:1</code>"]]);
        writer.WriteList(["Use <code>Span&lt;T&gt;</code>"]);

        var output = sw.ToString().ReplaceLineEndings("\n");
        Assert.Contains("Signature: `List<T>`", output);
        Assert.Contains("| `Serialize:1` |", output);
        Assert.Contains("- Use `Span<T>`", output);
    }

    [Fact]
    public void TsvFormatter_StripsCodeTagsAndDecodesXmlText()
    {
        var sw = new StringWriter();
        var options = new MarkoutWriterOptions { TableMode = MarkoutTableMode.Tsv };
        var writer = MarkoutWriter.Create(sw, new TableFormatter(), options);

        writer.WriteTable(
            ["Signature"],
            ["Signature"],
            [["<code>List&lt;T&gt;</code>"]]);

        Assert.Equal("signature\nList<T>\n", sw.ToString().ReplaceLineEndings("\n"));
    }

    [Fact]
    public void JsonlFormatter_StripsCodeTagsAndDecodesXmlText()
    {
        var sw = new StringWriter();
        var options = new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl };
        var writer = MarkoutWriter.Create(sw, new TableFormatter(), options);

        writer.WriteTable(
            ["Signature"],
            ["Signature"],
            [["<code>List&lt;T&gt;</code>"]]);

        Assert.Contains("\"signature\":\"List<T>\"", sw.ToString());

        using var document = JsonDocument.Parse(sw.ToString());
        Assert.Equal("List<T>", document.RootElement.GetProperty("signature").GetString());
    }

    [Fact]
    public void PlainTextFormatter_StripsCodeTagsAndDecodesXmlText()
    {
        var sw = new StringWriter();
        var writer = MarkoutWriter.Create(sw, new PlainTextFormatter());

        writer.WriteFields(new MarkoutField("Signature", "<code>List&lt;T&gt;</code>"));

        Assert.Contains("List<T>", sw.ToString());
        Assert.DoesNotContain("<code>", sw.ToString());
        Assert.DoesNotContain("`List", sw.ToString());
    }
}
