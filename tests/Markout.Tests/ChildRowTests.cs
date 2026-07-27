using Markout;

namespace Markout.Tests;

// A table row type with a semantic [MarkoutChild] flag: a true value nests the row under the
// previous one. The flag is never a column; rich sinks render it as the configurable child glyph.
public class OrgRow
{
    public string Name { get; set; } = "";
    public int Count { get; set; }

    [MarkoutChild]
    public bool IsChild { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class OrgCard
{
    [MarkoutIgnore] public string Title => "Org";

    [MarkoutSection(Name = "Teams")]
    public List<OrgRow> Rows { get; set; } = new();
}

[MarkoutContext(typeof(OrgCard))]
public partial class OrgCardContext : MarkoutSerializerContext
{
}

public class GroupedChildRow
{
    public string Group { get; set; } = "";
    public string Name { get; set; } = "";
    public int Count { get; set; }

    [MarkoutChild] public bool IsChild { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class GroupedChildCard
{
    [MarkoutIgnore] public string Title => "Grouped";

    [MarkoutSection(Name = "Teams", GroupBy = nameof(GroupedChildRow.Group))]
    public List<GroupedChildRow> Rows { get; set; } = new();
}

[MarkoutContext(typeof(GroupedChildCard))]
public partial class GroupedChildCardContext : MarkoutSerializerContext
{
}

public class ChildRowTests
{
    private static OrgCard Card() => new()
    {
        Rows =
        {
            new OrgRow { Name = "Platform", Count = 12 },
            new OrgRow { Name = "Runtime", Count = 5, IsChild = true },
            new OrgRow { Name = "Tools", Count = 3, IsChild = true },
            new OrgRow { Name = "Product", Count = 8 },
        },
    };

    [Fact]
    public void Markdown_ChildRow_GetsGlyphPrefix_ParentUnchanged_NoChildColumn()
    {
        var md = MarkoutSerializer.Serialize(Card(), OrgCardContext.Default);

        // The child flag is not a column.
        Assert.Contains("| Name | Count |", md);
        Assert.DoesNotContain("IsChild", md);
        Assert.DoesNotContain("Child |", md);

        // Non-child rows are untouched; child rows lead with the default child glyph.
        Assert.Contains("| Platform | 12 |", md);
        Assert.Contains("| \u21b3 Runtime | 5 |", md);
        Assert.Contains("| \u21b3 Tools | 3 |", md);
        Assert.Contains("| Product | 8 |", md);
    }

    [Fact]
    public void Markdown_ConfigurableChildGlyph_Override()
    {
        var md = MarkoutSerializer.Serialize(Card(), OrgCardContext.Default,
            new MarkoutWriterOptions { Glyphs = new MarkoutGlyphs { Child = "\u00bb" } });

        Assert.Contains("| \u00bb Runtime | 5 |", md);
        Assert.Contains("| Platform | 12 |", md);
    }

    [Fact]
    public void Markdown_EmptyChildGlyph_SuppressesPrefix()
    {
        var md = MarkoutSerializer.Serialize(Card(), OrgCardContext.Default,
            new MarkoutWriterOptions { Glyphs = new MarkoutGlyphs { Child = "" } });

        Assert.Contains("| Runtime | 5 |", md);
        Assert.DoesNotContain("\u21b3", md);
    }

    [Fact]
    public void Markdown_ComposeGlyph_ChildSlot_TakesControl()
    {
        var options = new MarkoutWriterOptions
        {
            ComposeGlyph = ctx => ctx.Slot == GlyphSlot.ChildRow
                ? "-- " + ctx.Text
                : ctx.Combine(),
        };
        var md = MarkoutSerializer.Serialize(Card(), OrgCardContext.Default, options);

        Assert.Contains("| -- Runtime | 5 |", md);
        Assert.Contains("| Platform | 12 |", md);
    }

    [Fact]
    public void PlainText_NoChildGlyph()
    {
        var sw = new StringWriter();
        MarkoutSerializer.Serialize(Card(), sw, new PlainTextFormatter(), OrgCardContext.Default);
        var text = sw.ToString();

        Assert.Contains("Runtime", text);
        Assert.DoesNotContain("\u21b3", text);
    }

    [Fact]
    public void Tsv_NoChildGlyph_NoChildColumn()
    {
        var sw = new StringWriter();
        MarkoutSerializer.Serialize(Card(), sw, new TableFormatter(), OrgCardContext.Default);
        var tsv = sw.ToString();

        Assert.DoesNotContain("\u21b3", tsv);
        Assert.DoesNotContain("IsChild", tsv);
        Assert.DoesNotContain("child", tsv);
        Assert.Contains("Runtime", tsv);
    }

    [Fact]
    public void Jsonl_NoChildField()
    {
        var sw = new StringWriter();
        MarkoutSerializer.Serialize(Card(), sw, new TableFormatter(), OrgCardContext.Default,
            new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl });
        var jsonl = sw.ToString();

        Assert.DoesNotContain("\u21b3", jsonl);
        Assert.DoesNotContain("isChild", jsonl);
        Assert.DoesNotContain("child", jsonl);
    }

    [Fact]
    public void Projection_Reorder_ChildGlyphMovesToFirstDisplayedCell()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection { IncludeColumns = ["Count", "Name"] },
        };
        var md = MarkoutSerializer.Serialize(Card(), OrgCardContext.Default, options);

        // Count is now the first displayed column; the child glyph leads it, not the (hidden-order) Name.
        Assert.Contains("| \u21b3 5 | Runtime |", md);
        Assert.Contains("| 12 | Platform |", md);
    }

    [Fact]
    public void Projection_DropLabelColumn_ChildGlyphNotLost()
    {
        var options = new MarkoutWriterOptions
        {
            Projection = new MarkoutProjection { IncludeColumns = ["Count"] },
        };
        var md = MarkoutSerializer.Serialize(Card(), OrgCardContext.Default, options);

        // Even with the original first column projected away, the marker survives on the first
        // displayed cell rather than vanishing.
        Assert.Contains("| \u21b3 5 |", md);
        Assert.Contains("| 12 |", md);
    }

    [Fact]
    public void Grouped_ChildRow_GlyphPrefix_NoChildColumn()
    {
        var card = new GroupedChildCard
        {
            Rows =
            {
                new GroupedChildRow { Group = "East", Name = "Parent", Count = 1 },
                new GroupedChildRow { Group = "East", Name = "Kid", Count = 2, IsChild = true },
            },
        };
        var md = MarkoutSerializer.Serialize(card, GroupedChildCardContext.Default);

        Assert.Contains("| Name | Count |", md);
        Assert.DoesNotContain("IsChild", md);
        Assert.Contains("| Parent | 1 |", md);
        Assert.Contains("| \u21b3 Kid | 2 |", md);
    }
}
