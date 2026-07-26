using Markout.Templates;

namespace Markout.Templates.Tests;

public class InlineConditionalTests
{
    // --- TemplateParser.ResolveInlineConditionals ---

    [Fact]
    public void ResolveInlineConditionals_KeepsTruthyContent()
    {
        var result = TemplateParser.ResolveInlineConditionals(
            "Ref: {{#if has}}{{ref}}{{/if}}", _ => true);
        Assert.Equal("Ref: {{ref}}", result);
    }

    [Fact]
    public void ResolveInlineConditionals_DropsFalsyContent()
    {
        var result = TemplateParser.ResolveInlineConditionals(
            "Ref: {{#if has}}{{ref}}{{/if}}done", _ => false);
        Assert.Equal("Ref: done", result);
    }

    [Fact]
    public void ResolveInlineConditionals_PreservesPlaceholdersForLaterPass()
    {
        // Non-conditional placeholders must survive verbatim for ResolveInlinePlaceholders.
        var result = TemplateParser.ResolveInlineConditionals("{{a}} and {{b}}", _ => true);
        Assert.Equal("{{a}} and {{b}}", result);
    }

    [Fact]
    public void ResolveInlineConditionals_Nested_InnerFalsy()
    {
        var result = TemplateParser.ResolveInlineConditionals(
            "A{{#if outer}}B{{#if inner}}C{{/if}}D{{/if}}E",
            key => key == "outer");
        Assert.Equal("ABDE", result);
    }

    [Fact]
    public void ResolveInlineConditionals_Nested_OuterFalsy_DropsAll()
    {
        var result = TemplateParser.ResolveInlineConditionals(
            "A{{#if outer}}B{{#if inner}}C{{/if}}D{{/if}}E",
            key => key == "inner");
        Assert.Equal("AE", result);
    }

    [Fact]
    public void ResolveInlineConditionals_SpansNewlines()
    {
        var result = TemplateParser.ResolveInlineConditionals(
            "one\n{{#if drop}}two\n{{/if}}three", _ => false);
        Assert.Equal("one\nthree", result);
    }

    [Fact]
    public void ResolveInlineConditionals_NoConditionals_ReturnsInput()
    {
        var input = "plain {{placeholder}} text";
        Assert.Same(input, TemplateParser.ResolveInlineConditionals(input, _ => true));
    }

    // --- Parser hardening: inline directives must not be misparsed as block nodes ---

    [Fact]
    public void Parse_InlineConditionalLine_IsParagraphNotBlockDirective()
    {
        // Regression: a line carrying an inline {{#if}}…{{/if}} must not be swallowed as a
        // block ConditionalStartNode (which previously produced a garbage key and skipped to EOF).
        var nodes = TemplateParser.Parse("Ref: {{#if has}}{{ref}}{{/if}}");
        var para = Assert.IsType<ParagraphNode>(Assert.Single(nodes));
        Assert.Equal("Ref: {{#if has}}{{ref}}{{/if}}", para.Text);
    }

    [Fact]
    public void Parse_LineWithMultiplePlaceholders_IsParagraphNotBlockPlaceholder()
    {
        var nodes = TemplateParser.Parse("{{a}} {{b}}");
        Assert.IsType<ParagraphNode>(Assert.Single(nodes));
    }

    [Fact]
    public void Parse_StandaloneDirectivesStillWork()
    {
        var nodes = TemplateParser.Parse("{{#if x}}\ncontent\n{{/if}}");
        Assert.Equal(new ConditionalStartNode("x"), nodes[0]);
        Assert.IsType<ConditionalEndNode>(nodes[^1]);
    }

    // --- End-to-end render: tight optional lines ---

    [Fact]
    public void Render_InlineConditional_OmittedLineLeavesNoBlankGap()
    {
        var text = "Corpus: {{corpus}}\n{{#if ref}}Baseline ref: {{ref}}{{/if}}\nCoverage: {{cov}}";
        var result = MarkoutTemplate.Parse(text)
            .Bind("corpus", "8,000 methods")
            .Bind("cov", "validity 2")
            .Render()
            .TrimEnd();

        Assert.Equal("Corpus: 8,000 methods\nCoverage: validity 2", result);
        Assert.DoesNotContain("Baseline ref", result);
    }

    [Fact]
    public void Render_InlineConditional_PresentLineStaysTight()
    {
        var text = "Corpus: {{corpus}}\n{{#if ref}}Baseline ref: {{ref}}{{/if}}\nCoverage: {{cov}}";
        var result = MarkoutTemplate.Parse(text)
            .Bind("corpus", "8,000 methods")
            .Bind("ref", "`abc123`")
            .Bind("cov", "validity 2")
            .Render()
            .TrimEnd();

        Assert.Equal("Corpus: 8,000 methods\nBaseline ref: `abc123`\nCoverage: validity 2", result);
    }

    [Fact]
    public void Render_InlineConditional_DrivenByFalseBool_OmitsLine()
    {
        var text = "A: {{a}}\n{{#if risky}}Risk: {{msg}}{{/if}}\nB: {{b}}";
        var result = MarkoutTemplate.Parse(text)
            .Bind("a", "1").Bind("b", "2")
            .Bind("risky", false)
            .Bind("msg", "warning")
            .Render()
            .TrimEnd();

        Assert.Equal("A: 1\nB: 2", result);
    }

    // --- Fix A: bound multi-line values keep their own blank-line separators ---

    [Fact]
    public void Render_BoundMultilineValue_KeepsBlankLineSeparators()
    {
        // The conditional-emptied-line drop must not swallow blank lines that live *inside*
        // a bound value (placeholders are expanded after the drop).
        var text = "{{#if show}}Body: {{body}}{{/if}}";
        var result = MarkoutTemplate.Parse(text)
            .Bind("show", true)
            .Bind("body", "alpha\n\nbeta")
            .Render()
            .TrimEnd();

        Assert.Contains("alpha", result);
        Assert.Contains("beta", result);
        // The blank line between the two words survives the paragraph render.
        Assert.Contains("alpha\n\nbeta", result);
    }

    // --- Fix B: unbalanced conditionals throw eagerly ---

    [Fact]
    public void Parse_BlockConditional_MissingEnd_Throws()
    {
        Assert.Throws<FormatException>(() =>
            TemplateParser.Parse("{{#if x}}\ncontent"));
    }

    [Fact]
    public void Parse_BlockConditional_StrayEnd_Throws()
    {
        Assert.Throws<FormatException>(() =>
            TemplateParser.Parse("content\n{{/if}}"));
    }

    [Fact]
    public void ResolveInlineConditionals_MissingEnd_Throws()
    {
        Assert.Throws<FormatException>(() =>
            TemplateParser.ResolveInlineConditionals("A {{#if k}}B", _ => true));
    }

    [Fact]
    public void ResolveInlineConditionals_StrayEnd_Throws()
    {
        Assert.Throws<FormatException>(() =>
            TemplateParser.ResolveInlineConditionals("A{{/if}}B", _ => true));
    }

    [Fact]
    public void ResolveInlineConditionals_EmptyKey_Throws()
    {
        Assert.Throws<FormatException>(() =>
            TemplateParser.ResolveInlineConditionals("A{{#if }}B{{/if}}", _ => true));
    }

    [Fact]
    public void ResolveInlineConditionals_MissingEnd_ThrowsEvenWhenTruthy()
    {
        // Data-independent: an unclosed truthy #if is still an authoring error.
        Assert.Throws<FormatException>(() =>
            TemplateParser.ResolveInlineConditionals("A{{#if k}}B", _ => false));
    }
}
