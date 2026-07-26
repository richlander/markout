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
    public void ResolveInlineConditionals_NoBraces_ReturnsSameInstance()
    {
        var input = "plain text with no tokens";
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

    [Fact]
    public void ResolveInlineConditionals_KeylessNoSpace_Throws()
    {
        Assert.Throws<FormatException>(() =>
            TemplateParser.ResolveInlineConditionals("A{{#if}}B{{/if}}", _ => true));
    }

    [Fact]
    public void Parse_KeylessBlockIf_Throws()
    {
        // A standalone {{#if}} (no key, no space) must not silently render as literal text.
        Assert.Throws<FormatException>(() =>
            TemplateParser.Parse("{{#if}}\nbody\n{{/if}}"));
    }

    [Fact]
    public void Parse_InlineErrorInsideFalsyBlock_ThrowsRegardlessOfBinding()
    {
        // The malformed inline {{#if inner}} lives inside a block section; parse-time validation is
        // data-independent, so the template is rejected even though the block would be skipped when
        // 'outer' is falsy. Previously this escaped both validators until 'outer' happened to be truthy.
        var template = "{{#if outer}}\nsome {{#if inner}} text no close\n{{/if}}";
        Assert.Throws<FormatException>(() => TemplateParser.Parse(template));
    }

    [Fact]
    public void Parse_LiteralConditionalTokenInProse_Throws()
    {
        // Structural directive tokens are reserved: a bare {{/if}} in prose is treated as an
        // authoring error (fail fast), not rendered literally. There is no escape mechanism.
        Assert.Throws<FormatException>(() =>
            TemplateParser.Parse("Use {{/if}} to close a block."));
    }

    [Fact]
    public void ResolveInlineConditionals_SpacePaddedDirectives_AreRecognized()
    {
        // The parsers trim inner whitespace, so the guard must too: "{{ #if x }}" / "{{ /if }}"
        // are real directives regardless of padding and drive the same drop as the unpadded form.
        var result = TemplateParser.ResolveInlineConditionals(
            "A {{ #if show }}B{{ /if }} C", _ => false);
        Assert.Equal("A  C", result);
    }

    [Fact]
    public void Parse_SpacePaddedStrayEndInProse_Throws()
    {
        // A space-padded stray {{/if }} must fail fast just like the exact spelling.
        Assert.Throws<FormatException>(() =>
            TemplateParser.Parse("Use {{/if }} here"));
    }

    [Fact]
    public void Parse_NestedOpenerHidingDirective_Throws()
    {
        // A malformed "{{oops {{#if flag}}" must not swallow the nested structural directive; the
        // unclosed {{#if}} is surfaced instead of silently accepted.
        Assert.Throws<FormatException>(() =>
            TemplateParser.Parse("before {{oops {{#if flag}} after"));
    }

    [Fact]
    public void Render_PlaceholderFollowedByStrayBrace_RendersLiterally()
    {
        // "{{name}}}" must resolve the {{name}} placeholder and keep the trailing brace, not be
        // misclassified as a block placeholder with key "name}".
        var result = MarkoutTemplate.Parse("{{name}}}")
            .Bind("name", "Alice")
            .Render()
            .TrimEnd();
        Assert.Equal("Alice}", result);
    }

    [Fact]
    public void ResolveInlinePlaceholders_NestedOpener_ResolvesInnerPlaceholder()
    {
        // A malformed "{{oops {{name}}" must not swallow the valid nested placeholder; the
        // placeholder pass mirrors the conditional pass and resumes at the nested opener.
        var result = TemplateParser.ResolveInlinePlaceholders(
            "before {{oops {{name}} after",
            key => key == "name" ? "Alice" : null);
        Assert.Equal("before {{oops Alice after", result);
    }

    [Fact]
    public void Render_PlaceholderWithNestedOpener_ResolvesInner()
    {
        var result = MarkoutTemplate.Parse("before {{oops {{name}} after")
            .Bind("name", "Alice")
            .Render()
            .TrimEnd();
        Assert.Equal("before {{oops Alice after", result);
    }

    [Fact]
    public void ResolveInlinePlaceholders_OverlappingOpener_ResolvesInner()
    {
        // "{{{x}}" overlaps: the first brace is literal, then {{x}} resolves. The nested-opener
        // search must start at outerOpen+1 to see the overlapping "{{".
        var result = TemplateParser.ResolveInlinePlaceholders(
            "{{{x}}", key => key == "x" ? "X" : null);
        Assert.Equal("{X", result);
    }

    [Fact]
    public void ResolveInlineConditionals_OverlappingOpenerBeforeDirective_Recognized()
    {
        // "{{{#if flag}}shown{{/if}}" — the leading brace is literal and the {{#if}} is a real
        // directive, so a truthy flag keeps "shown" and it does not throw a stray-close error.
        var result = TemplateParser.ResolveInlineConditionals(
            "{{{#if flag}}shown{{/if}}", _ => true);
        Assert.Equal("{shown", result);
    }

    [Fact]
    public void ResolveInlineConditionals_ManyAdjacentOpeners_IsLinear()
    {
        // Guards against O(n^2) rescanning: many adjacent "{{" followed by one close must resolve
        // quickly. Under quadratic behavior this input takes seconds; linear it is milliseconds.
        const int n = 200_000;
        var input = new string('{', 2 * n) + "x}}";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = TemplateParser.ResolveInlinePlaceholders(input, _ => null);
        sw.Stop();
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(2),
            $"Placeholder scan took {sw.Elapsed.TotalSeconds:F2}s — expected linear time.");
        Assert.EndsWith("{{x}}", result);
    }

    [Fact]
    public void ResolveInlineConditionals_BraceInInlineDirectiveKey_Throws()
    {
        // "{{#if x} }}" carries a stray '}' inside the key ("x}") — a malformed reserved directive
        // that must be rejected, not silently evaluated against a phantom "x}" binding.
        Assert.Throws<FormatException>(() =>
            TemplateParser.ResolveInlineConditionals("A{{#if x} }}B{{/if}}C", _ => true));
    }

    [Fact]
    public void Parse_BraceInBlockDirectiveKey_Throws()
    {
        // Block directive "{{#if x}}}" parses a stray '}' into the key ("x}") — reject it.
        Assert.Throws<FormatException>(() =>
            MarkoutTemplate.Parse("{{#if x}}}\nbody\n{{/if}}"));
    }

    [Fact]
    public void ResolveInlineConditionals_UnterminatedDirective_Throws()
    {
        // A reserved directive with no closing "}}" is malformed, not literal prose.
        Assert.Throws<FormatException>(() =>
            TemplateParser.ResolveInlineConditionals("{{#if show\nbody", _ => true));
        Assert.Throws<FormatException>(() =>
            TemplateParser.ResolveInlineConditionals("text {{/if", _ => true));
    }

    [Fact]
    public void ResolveInlineConditionals_UnterminatedPlaceholder_StaysLiteral()
    {
        // A non-directive unterminated "{{" must still degrade gracefully to literal text.
        var result = TemplateParser.ResolveInlineConditionals("keep {{name", _ => true);
        Assert.Equal("keep {{name", result);
    }
}
