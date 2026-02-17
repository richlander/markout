using Markout.Templates;

namespace Markout.Templates.Tests;

public class TemplateParserTests
{
    [Fact]
    public void Parse_EmptyString_ReturnsEmpty()
    {
        var nodes = TemplateParser.Parse("");
        Assert.Empty(nodes);
    }

    [Fact]
    public void Parse_HeadingLevels()
    {
        var nodes = TemplateParser.Parse("# H1\n## H2\n### H3");
        Assert.Equal(3, nodes.Count);
        Assert.Equal(new HeadingNode(1, "H1"), nodes[0]);
        Assert.Equal(new HeadingNode(2, "H2"), nodes[1]);
        Assert.Equal(new HeadingNode(3, "H3"), nodes[2]);
    }

    [Fact]
    public void Parse_HeadingWithInlinePlaceholder()
    {
        var nodes = TemplateParser.Parse("# Report for {{date}}");
        var heading = Assert.IsType<HeadingNode>(Assert.Single(nodes));
        Assert.Equal("Report for {{date}}", heading.Text);
    }

    [Fact]
    public void Parse_BlockPlaceholder()
    {
        var nodes = TemplateParser.Parse("{{my-table}}");
        var placeholder = Assert.IsType<PlaceholderNode>(Assert.Single(nodes));
        Assert.Equal("my-table", placeholder.Key);
    }

    [Fact]
    public void Parse_BlockPlaceholderWithWhitespace()
    {
        var nodes = TemplateParser.Parse("  {{ my-key }}  ");
        var placeholder = Assert.IsType<PlaceholderNode>(Assert.Single(nodes));
        Assert.Equal("my-key", placeholder.Key);
    }

    [Fact]
    public void Parse_Paragraph()
    {
        var nodes = TemplateParser.Parse("This is a paragraph.\nWith two lines.");
        var para = Assert.IsType<ParagraphNode>(Assert.Single(nodes));
        Assert.Equal("This is a paragraph.\nWith two lines.", para.Text);
    }

    [Fact]
    public void Parse_ParagraphsSplitByBlankLine()
    {
        var nodes = TemplateParser.Parse("First paragraph.\n\nSecond paragraph.");
        Assert.Equal(3, nodes.Count);
        Assert.IsType<ParagraphNode>(nodes[0]);
        Assert.IsType<BlankLineNode>(nodes[1]);
        Assert.IsType<ParagraphNode>(nodes[2]);
    }

    [Fact]
    public void Parse_ConditionalSection()
    {
        var text = "{{#if commits}}\n## Commits\n{{/if}}";
        var nodes = TemplateParser.Parse(text);
        Assert.Equal(3, nodes.Count);
        Assert.Equal(new ConditionalStartNode("commits"), nodes[0]);
        Assert.Equal(new HeadingNode(2, "Commits"), nodes[1]);
        Assert.IsType<ConditionalEndNode>(nodes[2]);
    }

    [Fact]
    public void Parse_FullTemplate()
    {
        var text = """
            # Report for {{date}}

            Some intro text.

            {{main-table}}

            ## Details

            More details here.

            {{#if extras}}
            ## Extras

            {{extras-table}}
            {{/if}}
            """;

        var nodes = TemplateParser.Parse(text);

        // Verify key node types are present in order
        Assert.IsType<HeadingNode>(nodes[0]);
        Assert.Contains(nodes, n => n is PlaceholderNode { Key: "main-table" });
        Assert.Contains(nodes, n => n is HeadingNode { Level: 2, Text: "Details" });
        Assert.Contains(nodes, n => n is ConditionalStartNode { Key: "extras" });
        Assert.Contains(nodes, n => n is PlaceholderNode { Key: "extras-table" });
        Assert.Contains(nodes, n => n is ConditionalEndNode);
    }

    [Fact]
    public void ResolveInlinePlaceholders_ReplacesKnownKeys()
    {
        var result = TemplateParser.ResolveInlinePlaceholders(
            "Hello {{name}}, welcome to {{place}}!",
            key => key switch
            {
                "name" => "Alice",
                "place" => "Wonderland",
                _ => null
            });

        Assert.Equal("Hello Alice, welcome to Wonderland!", result);
    }

    [Fact]
    public void ResolveInlinePlaceholders_PreservesUnknownKeys()
    {
        var result = TemplateParser.ResolveInlinePlaceholders(
            "Hello {{name}}!",
            _ => null);

        Assert.Equal("Hello {{name}}!", result);
    }

    [Fact]
    public void ResolveInlinePlaceholders_NoPlaceholders()
    {
        var result = TemplateParser.ResolveInlinePlaceholders(
            "No placeholders here.",
            _ => "replaced");

        Assert.Equal("No placeholders here.", result);
    }

    [Fact]
    public void Parse_HashInParagraphNotHeading()
    {
        // '#' without a space after is not a heading
        var nodes = TemplateParser.Parse("#notaheading");
        var para = Assert.IsType<ParagraphNode>(Assert.Single(nodes));
        Assert.Equal("#notaheading", para.Text);
    }

    [Fact]
    public void Parse_PipeTable()
    {
        var text = "| Name | Value |\n| ---- | ----- |\n| A    | 1     |";
        var nodes = TemplateParser.Parse(text);
        var table = Assert.IsType<TableNode>(Assert.Single(nodes));
        Assert.Equal(["Name", "Value"], table.Headers);
        Assert.Single(table.Rows);
        Assert.Equal("A", table.Rows[0][0]);
    }

    [Fact]
    public void Parse_TableBetweenContent()
    {
        var text = "# Title\n\n| A | B |\n| - | - |\n| 1 | 2 |\n\nAfter.";
        var nodes = TemplateParser.Parse(text);

        Assert.IsType<HeadingNode>(nodes[0]);
        Assert.Contains(nodes, n => n is TableNode);
        Assert.Contains(nodes, n => n is ParagraphNode { Text: "After." });
    }
}
