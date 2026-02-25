using Markout.Templates;

namespace Markout.Templates.Tests;

public class MarkoutTemplateTests
{
    [Fact]
    public void Render_SimpleHeadingWithInlineBinding()
    {
        var template = MarkoutTemplate.Parse("# Report for {{date}}");
        template.Bind("date", "February 2026");

        var result = template.Render();

        Assert.Equal("# Report for February 2026", result.TrimEnd());
    }

    [Fact]
    public void Render_ParagraphWithInlineBinding()
    {
        var template = MarkoutTemplate.Parse("Hello {{name}}, welcome!");
        template.Bind("name", "Alice");

        var result = template.Render();

        Assert.Equal("Hello Alice, welcome!", result.TrimEnd());
    }

    [Fact]
    public void Render_BlockPlaceholderWithStringBinding()
    {
        var text = "# Title\n\n{{content}}";
        var template = MarkoutTemplate.Parse(text);
        template.Bind("content", "Some paragraph content.");

        var result = template.Render();

        Assert.Contains("# Title", result);
        Assert.Contains("Some paragraph content.", result);
    }

    [Fact]
    public void Render_BlockPlaceholderWithFormattableBinding()
    {
        var text = "# Title\n\n{{content}}";
        var template = MarkoutTemplate.Parse(text);
        template.Bind("content", new TestFormattable());

        var result = template.Render();

        Assert.Contains("# Title", result);
        Assert.Contains("Key: Value", result);
    }

    [Fact]
    public void Render_ConditionalSection_Included()
    {
        var text = "# Report\n\n{{#if extras}}\n## Extras\n\nExtra content.\n{{/if}}";
        var template = MarkoutTemplate.Parse(text);
        template.Bind("extras", "truthy-value");

        var result = template.Render();

        Assert.Contains("## Extras", result);
        Assert.Contains("Extra content.", result);
    }

    [Fact]
    public void Render_ConditionalSection_Excluded()
    {
        var text = "# Report\n\n{{#if extras}}\n## Extras\n\nExtra content.\n{{/if}}";
        var template = MarkoutTemplate.Parse(text);
        // Don't bind "extras" — section should be excluded

        var result = template.Render();

        Assert.Contains("# Report", result);
        Assert.DoesNotContain("Extras", result);
        Assert.DoesNotContain("Extra content.", result);
    }

    [Fact]
    public void Render_ConditionalSection_NullExcluded()
    {
        var text = "# Report\n\n{{#if extras}}\n## Extras\n{{/if}}";
        var template = MarkoutTemplate.Parse(text);
        template.Bind("extras", (string?)null);

        var result = template.Render();

        Assert.DoesNotContain("Extras", result);
    }

    [Fact]
    public void Render_NestedConditionals()
    {
        var text = "{{#if outer}}\nOuter\n{{#if inner}}\nInner\n{{/if}}\n{{/if}}";
        var template = MarkoutTemplate.Parse(text);
        template.Bind("outer", "yes");
        // Don't bind inner

        var result = template.Render();

        Assert.Contains("Outer", result);
        Assert.DoesNotContain("Inner", result);
    }

    [Fact]
    public void Render_NestedConditionals_OuterFalse_SkipsAll()
    {
        var text = "{{#if outer}}\nOuter\n{{#if inner}}\nInner\n{{/if}}\n{{/if}}";
        var template = MarkoutTemplate.Parse(text);
        template.Bind("inner", "yes");
        // Don't bind outer

        var result = template.Render();

        Assert.DoesNotContain("Outer", result);
        Assert.DoesNotContain("Inner", result);
    }

    [Fact]
    public void Render_UnboundPlaceholder_Throws()
    {
        var template = MarkoutTemplate.Parse("{{unknown}}");

        Assert.Throws<InvalidOperationException>(() => template.Render());
    }

    [Fact]
    public void Render_PlainTextWriter()
    {
        var template = MarkoutTemplate.Parse("# Title\n\nSome text.");
        var writer = new MarkoutWriter();
        template.Render(writer);
        var result = writer.ToString();

        // Plain text writer uses different heading format
        Assert.Contains("Title", result);
        Assert.Contains("Some text.", result);
    }

    [Fact]
    public void Render_TextWriterOverload()
    {
        var template = MarkoutTemplate.Parse("# Title");
        using var sw = new StringWriter();
        template.Render(sw);
        var result = sw.ToString();

        Assert.Contains("# Title", result);
    }

    [Fact]
    public void Render_FluentBindingApi()
    {
        var result = MarkoutTemplate.Parse("# {{title}}\n\n{{body}}")
            .Bind("title", "Hello")
            .Bind("body", "World.")
            .Render();

        Assert.Contains("# Hello", result);
        Assert.Contains("World.", result);
    }

    [Fact]
    public void Render_FullDocumentTemplate()
    {
        var text = """
            # .NET CVEs for {{date}}

            The following vulnerabilities were disclosed.

            {{vuln-table}}

            ## Products

            {{product-table}}

            {{#if commits}}
            ## Commits

            Commit details follow.

            {{commit-table}}
            {{/if}}
            """;

        var template = MarkoutTemplate.Parse(text);
        template.Bind("date", "February 2026");
        template.Bind("vuln-table", new TestTableFormattable("CVE-2026-001", "Critical"));
        template.Bind("product-table", new TestTableFormattable(".NET 9", "Patched"));
        template.Bind("commits", "has-commits");
        template.Bind("commit-table", new TestTableFormattable("abc123", "Fix"));

        var result = template.Render();

        Assert.Contains("# .NET CVEs for February 2026", result);
        Assert.Contains("The following vulnerabilities were disclosed.", result);
        Assert.Contains("CVE-2026-001", result);
        Assert.Contains(".NET 9", result);
        Assert.Contains("## Commits", result);
        Assert.Contains("abc123", result);
    }

    [Fact]
    public void Render_FullDocumentTemplate_WithoutCommits()
    {
        var text = """
            # .NET CVEs for {{date}}

            The following vulnerabilities were disclosed.

            {{vuln-table}}

            {{#if commits}}
            ## Commits

            {{commit-table}}
            {{/if}}
            """;

        var template = MarkoutTemplate.Parse(text);
        template.Bind("date", "February 2026");
        template.Bind("vuln-table", new TestTableFormattable("CVE-2026-001", "Critical"));
        // Don't bind commits — conditional section excluded

        var result = template.Render();

        Assert.Contains("CVE-2026-001", result);
        Assert.DoesNotContain("Commits", result);
    }

    [Fact]
    public void Render_ObjectBinding()
    {
        var template = MarkoutTemplate.Parse("{{#if data}}\nHas data.\n{{/if}}");
        template.BindObject("data", new object());

        var result = template.Render();
        Assert.Contains("Has data.", result);
    }

    [Fact]
    public void Render_ObjectBinding_Null()
    {
        var template = MarkoutTemplate.Parse("{{#if data}}\nHas data.\n{{/if}}");
        template.BindObject("data", null);

        var result = template.Render();
        Assert.DoesNotContain("Has data.", result);
    }

    [Fact]
    public void Render_InlineTable_RenderedThroughWriter()
    {
        var text = """
            # Report

            | Name | Value |
            | ---- | ----- |
            | Alpha | 1 |
            | Beta  | 2 |
            """;

        var template = MarkoutTemplate.Parse(text);
        var result = template.Render();

        // Table should be rendered through the writer's table shape
        Assert.Contains("# Report", result);
        Assert.Contains("Alpha", result);
        Assert.Contains("Beta", result);
        Assert.Contains("|", result);
    }

    [Fact]
    public void Render_InlineTable_PlainText()
    {
        var text = "| Name | Value |\n| ---- | ----- |\n| A | 1 |";
        var template = MarkoutTemplate.Parse(text);
        var writer = new MarkoutWriter();
        template.Render(writer);
        var result = writer.ToString();

        // Plain text writer renders tables without pipes
        Assert.Contains("Name", result);
        Assert.Contains("A", result);
    }

    // Test helpers

    private class TestFormattable : IMarkoutFormattable
    {
        public void WriteTo(MarkoutWriter writer)
        {
            writer.WriteFields([new("Key", "Value")]);
        }

        public string? ToMarkoutString() => "Key=Value";
    }

    private class TestTableFormattable(string col1, string col2) : IMarkoutFormattable
    {
        public void WriteTo(MarkoutWriter writer)
        {
            writer.WriteTableStart("Name", "Status");
            writer.WriteTableRow(col1, col2);
            writer.WriteTableEnd();
        }

        public string? ToMarkoutString() => $"{col1}: {col2}";
    }
}
