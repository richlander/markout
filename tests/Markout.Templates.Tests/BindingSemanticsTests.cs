using Markout;
using Markout.Templates;

namespace Markout.Templates.Tests;

public class BindingSemanticsTests
{
    // --- Falsy semantics for conditional sections ---

    [Fact]
    public void StringBinding_EmptyString_IsFalsy()
    {
        var result = MarkoutTemplate.Parse("{{#if k}}\nshown\n{{/if}}")
            .Bind("k", "")
            .Render();
        Assert.DoesNotContain("shown", result);
    }

    [Fact]
    public void StringBinding_NonEmpty_IsTruthy()
    {
        var result = MarkoutTemplate.Parse("{{#if k}}\nshown\n{{/if}}")
            .Bind("k", "x")
            .Render();
        Assert.Contains("shown", result);
    }

    [Fact]
    public void BoolBinding_False_ExcludesSection()
    {
        var result = MarkoutTemplate.Parse("{{#if flag}}\nshown\n{{/if}}")
            .Bind("flag", false)
            .Render();
        Assert.DoesNotContain("shown", result);
    }

    [Fact]
    public void BoolBinding_True_IncludesSection()
    {
        var result = MarkoutTemplate.Parse("{{#if flag}}\nshown\n{{/if}}")
            .Bind("flag", true)
            .Render();
        Assert.Contains("shown", result);
    }

    [Fact]
    public void ObjectBinding_ZeroNumber_IsFalsy()
    {
        var result = MarkoutTemplate.Parse("{{#if n}}\nshown\n{{/if}}")
            .BindObject("n", 0)
            .Render();
        Assert.DoesNotContain("shown", result);
    }

    [Fact]
    public void ObjectBinding_NonZeroNumber_IsTruthy()
    {
        var result = MarkoutTemplate.Parse("{{#if n}}\nshown\n{{/if}}")
            .BindObject("n", 5)
            .Render();
        Assert.Contains("shown", result);
    }

    [Fact]
    public void ObjectBinding_EmptyCollection_IsFalsy()
    {
        var result = MarkoutTemplate.Parse("{{#if items}}\nshown\n{{/if}}")
            .BindObject("items", new List<string>())
            .Render();
        Assert.DoesNotContain("shown", result);
    }

    [Fact]
    public void ObjectBinding_NonEmptyObject_IsTruthy()
    {
        var result = MarkoutTemplate.Parse("{{#if data}}\nshown\n{{/if}}")
            .BindObject("data", new object())
            .Render();
        Assert.Contains("shown", result);
    }

    // --- List binding ---

    [Fact]
    public void ListBinding_RendersBullets()
    {
        var result = MarkoutTemplate.Parse("{{items}}")
            .Bind("items", new[] { "alpha", "beta" })
            .Render();
        Assert.Contains("- alpha", result);
        Assert.Contains("- beta", result);
    }

    [Fact]
    public void ListBinding_Inline_JoinsWithComma()
    {
        var result = MarkoutTemplate.Parse("Tags: {{items}}")
            .Bind("items", new[] { "alpha", "beta" })
            .Render()
            .TrimEnd();
        Assert.Equal("Tags: alpha, beta", result);
    }

    [Fact]
    public void ListBinding_Empty_IsFalsy()
    {
        var result = MarkoutTemplate.Parse("{{#if items}}\nshown\n{{/if}}")
            .Bind("items", Array.Empty<string>())
            .Render();
        Assert.DoesNotContain("shown", result);
    }

    [Fact]
    public void ListBinding_NonEmpty_IsTruthy()
    {
        var result = MarkoutTemplate.Parse("{{#if items}}\nshown\n{{/if}}")
            .Bind("items", new[] { "one" })
            .Render();
        Assert.Contains("shown", result);
    }
}
