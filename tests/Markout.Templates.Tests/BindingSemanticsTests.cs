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

    // --- Fix C: truthiness must not consume a one-shot enumerable ---

    [Fact]
    public void ObjectBinding_OneShotEnumerable_IsNotConsumedByTruthinessProbe()
    {
        int enumerations = 0;
        IEnumerable<int> OneShot()
        {
            enumerations++;
            yield return 1;
        }

        var result = MarkoutTemplate.Parse("{{#if seq}}\nshown\n{{/if}}")
            .BindObject("seq", OneShot())
            .Render();

        // A non-collection enumerable is truthy when non-null, without enumerating it.
        Assert.Contains("shown", result);
        Assert.Equal(0, enumerations);
    }

    // --- Fix D: additional numeric types participate in zero-is-falsy ---

    [Fact]
    public void ObjectBinding_HalfZero_IsFalsy()
    {
        var result = MarkoutTemplate.Parse("{{#if n}}\nshown\n{{/if}}")
            .BindObject("n", (Half)0)
            .Render();
        Assert.DoesNotContain("shown", result);
    }

    [Fact]
    public void ObjectBinding_NIntZero_IsFalsy()
    {
        var result = MarkoutTemplate.Parse("{{#if n}}\nshown\n{{/if}}")
            .BindObject("n", (nint)0)
            .Render();
        Assert.DoesNotContain("shown", result);
    }

    [Fact]
    public void ObjectBinding_BigIntegerZero_IsFalsy()
    {
        var result = MarkoutTemplate.Parse("{{#if n}}\nshown\n{{/if}}")
            .BindObject("n", System.Numerics.BigInteger.Zero)
            .Render();
        Assert.DoesNotContain("shown", result);
    }

    [Fact]
    public void ObjectBinding_BigIntegerNonZero_IsTruthy()
    {
        var result = MarkoutTemplate.Parse("{{#if n}}\nshown\n{{/if}}")
            .BindObject("n", new System.Numerics.BigInteger(7))
            .Render();
        Assert.Contains("shown", result);
    }

    // --- Fix F: generic-only collections and the AOT-safe truthiness contract ---

    [Fact]
    public void ObjectBinding_EmptyGenericOnlyCollection_IsTruthy_AotLimitation()
    {
        // HashSet<T> does not implement the non-generic ICollection, and counting it would need
        // reflection (breaks AOT) or enumeration (consumes one-shot sequences). So BindObject treats
        // a generic-only collection as truthy-when-non-null. Documented, intentional contract.
        var result = MarkoutTemplate.Parse("{{#if items}}\nshown\n{{/if}}")
            .BindObject("items", new HashSet<int>())
            .Render();
        Assert.Contains("shown", result);
    }

    [Fact]
    public void ListBinding_MaterializesHashSet_EmptyIsFalsy()
    {
        // The recommended path for emptiness-driven truthiness on any sequence: Bind materializes it.
        var result = MarkoutTemplate.Parse("{{#if items}}\nshown\n{{/if}}")
            .Bind("items", new HashSet<string>())
            .Render();
        Assert.DoesNotContain("shown", result);
    }

    // --- Fix G: Int128/UInt128 participate in zero-is-falsy ---

    [Fact]
    public void ObjectBinding_Int128Zero_IsFalsy()
    {
        var result = MarkoutTemplate.Parse("{{#if n}}\nshown\n{{/if}}")
            .BindObject("n", Int128.Zero)
            .Render();
        Assert.DoesNotContain("shown", result);
    }

    [Fact]
    public void ObjectBinding_UInt128Zero_IsFalsy()
    {
        var result = MarkoutTemplate.Parse("{{#if n}}\nshown\n{{/if}}")
            .BindObject("n", UInt128.Zero)
            .Render();
        Assert.DoesNotContain("shown", result);
    }

    [Fact]
    public void ObjectBinding_NFloatZero_IsFalsy()
    {
        var result = MarkoutTemplate.Parse("{{#if n}}\nshown\n{{/if}}")
            .BindObject("n", new System.Runtime.InteropServices.NFloat(0))
            .Render();
        Assert.DoesNotContain("shown", result);
    }

    [Fact]
    public void ObjectBinding_ComplexZero_IsFalsy()
    {
        var result = MarkoutTemplate.Parse("{{#if n}}\nshown\n{{/if}}")
            .BindObject("n", System.Numerics.Complex.Zero)
            .Render();
        Assert.DoesNotContain("shown", result);
    }

    [Fact]
    public void ObjectBinding_ComplexNonZero_IsTruthy()
    {
        var result = MarkoutTemplate.Parse("{{#if n}}\nshown\n{{/if}}")
            .BindObject("n", new System.Numerics.Complex(0, 1))
            .Render();
        Assert.Contains("shown", result);
    }
}
