using System.IO;
using Markout;
using Markout.Formatting;

namespace Markout.Tests;

// A lone Metric/Breakdown property (not a List<T>) must render as its bar shape, same as the list
// form. Regression guard for the CT10 footgun where a scalar Breakdown fell through to a
// Field/Value + Slices table (plus MARKOUT001/CS8073 warnings) instead of a bar.

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class ScalarShapeReport
{
    public string Title { get; set; } = "";

    [MarkoutIgnoreInTable]
    public Metric Score { get; set; }

    [MarkoutIgnoreInTable]
    public Breakdown Coverage { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class NullableScalarShapeReport
{
    public string Title { get; set; } = "";

    [MarkoutIgnoreInTable]
    public Metric? Score { get; set; }

    [MarkoutIgnoreInTable]
    public Breakdown? Coverage { get; set; }
}

[MarkoutContext(typeof(ScalarShapeReport))]
[MarkoutContext(typeof(NullableScalarShapeReport))]
public partial class ScalarShapeTestContext : MarkoutSerializerContext
{
}

public class ScalarShapeTests
{
    private static string Unicode<T>(T value, MarkoutSerializerContext context)
    {
        var sw = new StringWriter();
        MarkoutSerializer.Serialize(value, sw, new UnicodeFormatter(), context);
        return sw.ToString();
    }

    [Fact]
    public void ScalarMetric_RendersAsBar()
    {
        var report = new ScalarShapeReport
        {
            Title = "Report",
            Score = new Metric("Score", 87),
            Coverage = new Breakdown("Coverage", new[] { new Slice("Covered", 82), new Slice("Uncovered", 18) }),
        };

        var md = Unicode(report, ScalarShapeTestContext.Default);

        // Bars, not a Field/Value complex-object fallback table.
        Assert.Contains("█", md);
        Assert.Contains("Score", md);
        Assert.Contains("Coverage", md);
        Assert.DoesNotContain("| Field | Value |", md);
    }

    [Fact]
    public void ScalarBreakdown_RendersBarLine()
    {
        var report = new ScalarShapeReport
        {
            Title = "Report",
            Coverage = new Breakdown("Coverage", new[] { new Slice("Covered", 82), new Slice("Uncovered", 18) }),
        };

        var md = Unicode(report, ScalarShapeTestContext.Default);

        // The CT10 assertion shape: the Coverage label followed by a bar.
        Assert.Matches(@"Coverage[\s\S]*█", md);
    }

    [Fact]
    public void ScalarBreakdown_Markdown_RoutesToBreakdownTable_NotFieldValue()
    {
        var report = new ScalarShapeReport
        {
            Title = "Report",
            Coverage = new Breakdown("Coverage", new[] { new Slice("Covered", 82), new Slice("Uncovered", 18) }),
        };

        // Default markdown formatter renders a breakdown as its category table, not the generic
        // complex-object Field/Value fallback the scalar shape used to fall through to.
        var md = MarkoutSerializer.Serialize(report, ScalarShapeTestContext.Default);

        Assert.Contains("| Category | Count | % |", md);
        Assert.DoesNotContain("| Field | Value |", md);
    }

    [Fact]
    public void ScalarShapes_Unset_RenderNothing()
    {
        var report = new ScalarShapeReport { Title = "Empty" };

        var md = Unicode(report, ScalarShapeTestContext.Default);

        Assert.DoesNotContain("█", md);
    }

    [Fact]
    public void NullableScalarShapes_Set_RenderBars()
    {
        var report = new NullableScalarShapeReport
        {
            Title = "Report",
            Score = new Metric("Score", 55),
            Coverage = new Breakdown("Coverage", new[] { new Slice("A", 40), new Slice("B", 60) }),
        };

        var md = Unicode(report, ScalarShapeTestContext.Default);

        Assert.Contains("█", md);
        Assert.Matches(@"Coverage[\s\S]*█", md);
    }

    [Fact]
    public void NullableScalarShapes_Unset_RenderNothing()
    {
        var report = new NullableScalarShapeReport { Title = "Empty" };

        var md = Unicode(report, ScalarShapeTestContext.Default);

        Assert.DoesNotContain("█", md);
    }
}
