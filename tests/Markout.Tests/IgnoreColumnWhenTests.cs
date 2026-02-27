using System.Collections.Generic;
using System.Linq;
using Markout;

namespace Markout.Tests;

// --- Test types ---

[MarkoutSerializable]
public class FindRow
{
    public string Pattern { get; set; } = "";
    public string Type { get; set; } = "";
    public string Kind { get; set; } = "";
    public string? Similarity { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Title), AutoFields = false)]
public class FindResultView
{
    [MarkoutIgnore] public string Title { get; set; } = "";

    [MarkoutSection(Name = "Results")]
    [MarkoutIgnoreColumnWhen(nameof(PatternIsUniform), "Pattern")]
    [MarkoutIgnoreColumnWhen(nameof(SimilarityIsUniform), "Similarity")]
    public List<FindRow>? Results { get; set; }

    public static bool PatternIsUniform(List<FindRow>? rows)
        => rows?.Select(r => r.Pattern).Distinct().Count() <= 1;

    public static bool SimilarityIsUniform(List<FindRow>? rows)
        => rows?.Select(r => r.Similarity).Distinct().Count() <= 1;
}

[MarkoutSerializable(TitleProperty = nameof(Title), AutoFields = false)]
public class SingleConditionView
{
    [MarkoutIgnore] public string Title { get; set; } = "";

    [MarkoutSection(Name = "Items")]
    [MarkoutIgnoreColumnWhen(nameof(KindIsUniform), "Kind")]
    public List<FindRow>? Items { get; set; }

    public static bool KindIsUniform(List<FindRow>? rows)
        => rows?.Select(r => r.Kind).Distinct().Count() <= 1;
}

[MarkoutSerializable(TitleProperty = nameof(Title), AutoFields = false)]
public class MixedIgnoreView
{
    [MarkoutIgnore] public string Title { get; set; } = "";

    [MarkoutSection(Name = "Items", IgnoreProperty = "Similarity")]
    [MarkoutIgnoreColumnWhen(nameof(PatternIsUniform), "Pattern")]
    public List<FindRow>? Items { get; set; }

    public static bool PatternIsUniform(List<FindRow>? rows)
        => rows?.Select(r => r.Pattern).Distinct().Count() <= 1;
}

// --- GroupBy + IgnoreColumnWhen integration ---

[MarkoutSerializable]
public class GroupedFindRow
{
    public string Category { get; set; } = "";
    public string Pattern { get; set; } = "";
    public string Type { get; set; } = "";
    public string Kind { get; set; } = "";
}

[MarkoutSerializable(TitleProperty = nameof(Title), AutoFields = false)]
public class GroupByWithIgnoreColumnWhenView
{
    [MarkoutIgnore] public string Title { get; set; } = "";

    [MarkoutSection(Name = "Results", GroupBy = nameof(GroupedFindRow.Category))]
    [MarkoutIgnoreColumnWhen(nameof(PatternIsUniform), "Pattern")]
    public List<GroupedFindRow>? Results { get; set; }

    public static bool PatternIsUniform(List<GroupedFindRow>? rows)
        => rows?.Select(r => r.Pattern).Distinct().Count() <= 1;
}

// --- OneLineFormatter + IgnoreColumnWhen integration ---

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class OneLineIgnoreColumnWhenView
{
    [MarkoutIgnore] public string Title { get; set; } = "";
    public int Count { get; set; }

    [MarkoutSection(Name = "Results")]
    [MarkoutIgnoreColumnWhen(nameof(PatternIsUniform), "Pattern")]
    public List<FindRow>? Results { get; set; }

    public static bool PatternIsUniform(List<FindRow>? rows)
        => rows?.Select(r => r.Pattern).Distinct().Count() <= 1;
}

[MarkoutContext(typeof(FindRow))]
[MarkoutContext(typeof(FindResultView))]
[MarkoutContext(typeof(SingleConditionView))]
[MarkoutContext(typeof(MixedIgnoreView))]
[MarkoutContext(typeof(GroupByWithIgnoreColumnWhenView))]
[MarkoutContext(typeof(GroupedFindRow))]
[MarkoutContext(typeof(OneLineIgnoreColumnWhenView))]
public partial class IgnoreColumnWhenTestContext : MarkoutSerializerContext
{
}

// --- Tests ---

[Collection("ConsoleError")]
public class IgnoreColumnWhenTests
{
    [Fact]
    public void UniformPattern_HidesPatternColumn()
    {
        var view = new FindResultView
        {
            Title = "Search",
            Results = new List<FindRow>
            {
                new() { Pattern = "Foo", Type = "Class", Kind = "public", Similarity = "1.00" },
                new() { Pattern = "Foo", Type = "Struct", Kind = "internal", Similarity = "0.85" }
            }
        };

        var mdf = MarkoutSerializer.Serialize(view, IgnoreColumnWhenTestContext.Default);

        Assert.Contains("## Results", mdf);
        // Pattern is uniform ("Foo" for all) → hidden
        Assert.DoesNotContain("| Pattern", mdf);
        // Similarity is NOT uniform → shown
        Assert.Contains("Similarity", mdf);
        Assert.Contains("Class", mdf);
        Assert.Contains("Struct", mdf);
    }

    [Fact]
    public void NonUniformPattern_ShowsAllColumns()
    {
        var view = new FindResultView
        {
            Title = "Search",
            Results = new List<FindRow>
            {
                new() { Pattern = "Foo", Type = "Class", Kind = "public", Similarity = "1.00" },
                new() { Pattern = "Bar", Type = "Struct", Kind = "internal", Similarity = "1.00" }
            }
        };

        var mdf = MarkoutSerializer.Serialize(view, IgnoreColumnWhenTestContext.Default);

        // Pattern is NOT uniform → shown
        Assert.Contains("Pattern", mdf);
        // Similarity IS uniform → hidden
        Assert.DoesNotContain("| Similarity", mdf);
        Assert.Contains("Foo", mdf);
        Assert.Contains("Bar", mdf);
    }

    [Fact]
    public void BothUniform_HidesBothColumns()
    {
        var view = new FindResultView
        {
            Title = "Search",
            Results = new List<FindRow>
            {
                new() { Pattern = "Foo", Type = "Class", Kind = "public", Similarity = "1.00" },
                new() { Pattern = "Foo", Type = "Struct", Kind = "internal", Similarity = "1.00" }
            }
        };

        var mdf = MarkoutSerializer.Serialize(view, IgnoreColumnWhenTestContext.Default);

        Assert.DoesNotContain("| Pattern", mdf);
        Assert.DoesNotContain("| Similarity", mdf);
        Assert.Contains("Type", mdf);
        Assert.Contains("Kind", mdf);
    }

    [Fact]
    public void NeitherUniform_ShowsAllColumns()
    {
        var view = new FindResultView
        {
            Title = "Search",
            Results = new List<FindRow>
            {
                new() { Pattern = "Foo", Type = "Class", Kind = "public", Similarity = "1.00" },
                new() { Pattern = "Bar", Type = "Struct", Kind = "internal", Similarity = "0.85" }
            }
        };

        var mdf = MarkoutSerializer.Serialize(view, IgnoreColumnWhenTestContext.Default);

        Assert.Contains("Pattern", mdf);
        Assert.Contains("Similarity", mdf);
        Assert.Contains("Type", mdf);
        Assert.Contains("Kind", mdf);
    }

    [Fact]
    public void SingleCondition_HidesWhenTrue()
    {
        var view = new SingleConditionView
        {
            Title = "Test",
            Items = new List<FindRow>
            {
                new() { Pattern = "Foo", Type = "Class", Kind = "public", Similarity = "1.00" },
                new() { Pattern = "Bar", Type = "Struct", Kind = "public", Similarity = "0.85" }
            }
        };

        var mdf = MarkoutSerializer.Serialize(view, IgnoreColumnWhenTestContext.Default);

        // Kind is uniform → hidden
        Assert.DoesNotContain("| Kind", mdf);
        // Other columns shown
        Assert.Contains("Pattern", mdf);
        Assert.Contains("Type", mdf);
        Assert.Contains("Similarity", mdf);
    }

    [Fact]
    public void SingleCondition_ShowsWhenFalse()
    {
        var view = new SingleConditionView
        {
            Title = "Test",
            Items = new List<FindRow>
            {
                new() { Pattern = "Foo", Type = "Class", Kind = "public", Similarity = "1.00" },
                new() { Pattern = "Bar", Type = "Struct", Kind = "internal", Similarity = "0.85" }
            }
        };

        var mdf = MarkoutSerializer.Serialize(view, IgnoreColumnWhenTestContext.Default);

        // Kind is NOT uniform → shown
        Assert.Contains("Kind", mdf);
        Assert.Contains("Pattern", mdf);
    }

    [Fact]
    public void MixedWithStaticIgnore_BothApply()
    {
        var view = new MixedIgnoreView
        {
            Title = "Test",
            Items = new List<FindRow>
            {
                new() { Pattern = "Foo", Type = "Class", Kind = "public", Similarity = "1.00" },
                new() { Pattern = "Foo", Type = "Struct", Kind = "internal", Similarity = "0.85" }
            }
        };

        var mdf = MarkoutSerializer.Serialize(view, IgnoreColumnWhenTestContext.Default);

        // Similarity is statically ignored via IgnoreProperty
        Assert.DoesNotContain("Similarity", mdf);
        // Pattern is dynamically hidden (uniform)
        Assert.DoesNotContain("| Pattern", mdf);
        // Remaining columns shown
        Assert.Contains("Type", mdf);
        Assert.Contains("Kind", mdf);
    }

    [Fact]
    public void MixedWithStaticIgnore_DynamicShowsWhenNotUniform()
    {
        var view = new MixedIgnoreView
        {
            Title = "Test",
            Items = new List<FindRow>
            {
                new() { Pattern = "Foo", Type = "Class", Kind = "public", Similarity = "1.00" },
                new() { Pattern = "Bar", Type = "Struct", Kind = "internal", Similarity = "0.85" }
            }
        };

        var mdf = MarkoutSerializer.Serialize(view, IgnoreColumnWhenTestContext.Default);

        // Similarity still statically ignored
        Assert.DoesNotContain("Similarity", mdf);
        // Pattern is NOT uniform → shown
        Assert.Contains("Pattern", mdf);
        Assert.Contains("Type", mdf);
    }

    // --- GroupBy + IgnoreColumnWhen integration ---

    [Fact]
    public void GroupBy_WithIgnoreColumnWhen_HidesUniformColumn()
    {
        var view = new GroupByWithIgnoreColumnWhenView
        {
            Title = "Search",
            Results = new List<GroupedFindRow>
            {
                new() { Category = "Classes", Pattern = "Foo", Type = "Class", Kind = "public" },
                new() { Category = "Classes", Pattern = "Foo", Type = "Class", Kind = "internal" },
                new() { Category = "Structs", Pattern = "Foo", Type = "Struct", Kind = "public" }
            }
        };

        var mdf = MarkoutSerializer.Serialize(view, IgnoreColumnWhenTestContext.Default);

        // GroupBy creates subheadings
        Assert.Contains("### Classes", mdf);
        Assert.Contains("### Structs", mdf);
        // Pattern is uniform → hidden from grouped tables
        Assert.DoesNotContain("| Pattern", mdf);
        // Other columns visible
        Assert.Contains("Type", mdf);
        Assert.Contains("Kind", mdf);
    }

    [Fact]
    public void GroupBy_WithIgnoreColumnWhen_ShowsNonUniformColumn()
    {
        var view = new GroupByWithIgnoreColumnWhenView
        {
            Title = "Search",
            Results = new List<GroupedFindRow>
            {
                new() { Category = "Classes", Pattern = "Foo", Type = "Class", Kind = "public" },
                new() { Category = "Classes", Pattern = "Bar", Type = "Class", Kind = "internal" },
                new() { Category = "Structs", Pattern = "Baz", Type = "Struct", Kind = "public" }
            }
        };

        var mdf = MarkoutSerializer.Serialize(view, IgnoreColumnWhenTestContext.Default);

        Assert.Contains("### Classes", mdf);
        // Pattern is NOT uniform → shown in grouped tables
        Assert.Contains("Pattern", mdf);
        Assert.Contains("Foo", mdf);
        Assert.Contains("Bar", mdf);
    }

    // --- OneLineFormatter + IgnoreColumnWhen + IgnoreFields integration ---

    [Fact]
    public void OneLineFormatter_WithIgnoreColumnWhen_HidesUniformColumn()
    {
        var view = new OneLineIgnoreColumnWhenView
        {
            Title = "Search",
            Count = 2,
            Results = new List<FindRow>
            {
                new() { Pattern = "Foo", Type = "Class", Kind = "public", Similarity = "1.00" },
                new() { Pattern = "Foo", Type = "Struct", Kind = "internal", Similarity = "0.85" }
            }
        };

        var sw = new StringWriter();
        MarkoutSerializer.Serialize(view, sw, new OneLineFormatter(), IgnoreColumnWhenTestContext.Default);
        var output = sw.ToString();

        // OneLineFormatter renders table rows as pipe-separated lines
        Assert.Contains("Class", output);
        Assert.Contains("Struct", output);
        // Pattern is uniform → hidden
        Assert.DoesNotContain("Foo", output);
    }

    [Fact]
    public void OneLineFormatter_WithIgnoreColumnWhen_ShowsNonUniformColumn()
    {
        var view = new OneLineIgnoreColumnWhenView
        {
            Title = "Search",
            Count = 2,
            Results = new List<FindRow>
            {
                new() { Pattern = "Foo", Type = "Class", Kind = "public", Similarity = "1.00" },
                new() { Pattern = "Bar", Type = "Struct", Kind = "internal", Similarity = "0.85" }
            }
        };

        var sw = new StringWriter();
        MarkoutSerializer.Serialize(view, sw, new OneLineFormatter(), IgnoreColumnWhenTestContext.Default);
        var output = sw.ToString();

        // Pattern is NOT uniform → shown
        Assert.Contains("Foo", output);
        Assert.Contains("Bar", output);
    }

    [Fact]
    public void OneLineFormatter_WithIgnoreFields_NoFieldWarning()
    {
        var errWriter = new StringWriter();
        var origErr = Console.Error;
        Console.SetError(errWriter);
        try
        {
            var view = new OneLineIgnoreColumnWhenView
            {
                Title = "Search",
                Count = 42,
                Results = new List<FindRow>
                {
                    new() { Pattern = "Foo", Type = "Class", Kind = "public", Similarity = "1.00" }
                }
            };

            var sw = new StringWriter();
            MarkoutSerializer.Serialize(view, sw, new OneLineFormatter(), IgnoreColumnWhenTestContext.Default);

            // No warnings - OneLineFormatter supports Fields
            Assert.Equal("", errWriter.ToString());
            Assert.Contains("Class", sw.ToString());
        }
        finally
        {
            Console.SetError(origErr);
        }
    }
}
