using Markout;

namespace Markout.Tests;

// --- Nullable value type test models ---

[MarkoutSerializable]
public class NullableScalarsOneLine
{
    public string? Label { get; set; }
    public int? Count { get; set; }
    public bool? Active { get; set; }
    public DateTimeOffset? Modified { get; set; }
}

[MarkoutSerializable(FieldLayout = FieldLayout.Vertical)]
public class NullableScalarsLineBreaks
{
    public string? Label { get; set; }
    public int? Count { get; set; }
    public bool? Active { get; set; }
    public double? Score { get; set; }
}

[MarkoutSerializable(FieldLayout = FieldLayout.Vertical)]
public class NullableScalarsDoubleSpace
{
    public string? Label { get; set; }
    public int? Count { get; set; }
    public bool? Active { get; set; }
}

[MarkoutSerializable(FieldLayout = FieldLayout.Bulleted)]
public class NullableScalarsList
{
    public string? Label { get; set; }
    public int? Count { get; set; }
    public bool? Active { get; set; }
}

[MarkoutSerializable]
public class NullableInTableRow
{
    public string? Name { get; set; }
    public int? Priority { get; set; }
    public bool? Completed { get; set; }
}

[MarkoutSerializable]
public class NullableTableContainer
{
    public string? Title { get; set; }
    [MarkoutSection(Name = "Items")]
    public List<NullableInTableRow>? Items { get; set; }
}

// --- String null-skipping test model (OneLine, strings only) ---

[MarkoutSerializable]
public class StringOnlyOneLine
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int Count { get; set; }
}

// --- Title dedup test models ---

[MarkoutSerializable(TitleProperty = nameof(Name), AutoFields = true)]
public class TitleDedupRecord
{
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";
    public int Count { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Name), DescriptionProperty = nameof(Summary), AutoFields = true)]
public class TitleAndDescDedupRecord
{
    public string Name { get; set; } = "";
    public string? Summary { get; set; }
    public string Kind { get; set; } = "";
    public int Count { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Name), TitleContextProperty = nameof(Version), AutoFields = true)]
public class TitleContextDedupRecord
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Kind { get; set; } = "";
}

// --- Contexts ---

[MarkoutContext(typeof(NullableScalarsOneLine))]
[MarkoutContext(typeof(NullableScalarsLineBreaks))]
[MarkoutContext(typeof(NullableScalarsDoubleSpace))]
[MarkoutContext(typeof(NullableScalarsList))]
[MarkoutContext(typeof(NullableTableContainer))]
[MarkoutContext(typeof(StringOnlyOneLine))]
[MarkoutContext(typeof(TitleDedupRecord))]
[MarkoutContext(typeof(TitleAndDescDedupRecord))]
[MarkoutContext(typeof(TitleContextDedupRecord))]
public partial class NullableAndTitleTestContext : MarkoutSerializerContext
{
}

public class NullableAndTitleTests
{
    // --- Nullable: OneLine layout ---

    [Fact]
    public void Nullable_OneLine_AllSet_RendersAll()
    {
        var record = new NullableScalarsOneLine
        {
            Label = "Test",
            Count = 42,
            Active = true,
            Modified = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero)
        };

        var mdf = MarkoutSerializer.Serialize(record, NullableAndTitleTestContext.Default);

        Assert.Contains("Label: Test", mdf);
        Assert.Contains("Count: 42", mdf);
        Assert.Contains("Active: yes", mdf);
        Assert.Contains("Modified: 2024-06-15T12:00:00", mdf);
    }

    [Fact]
    public void Nullable_OneLine_NullsSkipped()
    {
        var record = new NullableScalarsOneLine
        {
            Label = "Test",
            Count = null,
            Active = null,
            Modified = null
        };

        var mdf = MarkoutSerializer.Serialize(record, NullableAndTitleTestContext.Default);

        Assert.Contains("Label: Test", mdf);
        Assert.DoesNotContain("Count:", mdf);
        Assert.DoesNotContain("Active:", mdf);
        Assert.DoesNotContain("Modified:", mdf);
    }

    [Fact]
    public void Nullable_OneLine_AllNull_AllSkipped()
    {
        var record = new NullableScalarsOneLine
        {
            Label = null,
            Count = null,
            Active = null,
            Modified = null
        };

        var mdf = MarkoutSerializer.Serialize(record, NullableAndTitleTestContext.Default);

        // Null strings and nullable value types are all skipped
        Assert.DoesNotContain("Label:", mdf);
        Assert.DoesNotContain("Count:", mdf);
        Assert.DoesNotContain("Active:", mdf);
        Assert.DoesNotContain("Modified:", mdf);
    }

    // --- Nullable: LineBreaks layout ---

    [Fact]
    public void Nullable_LineBreaks_AllSet_RendersAll()
    {
        var record = new NullableScalarsLineBreaks
        {
            Label = "Test",
            Count = 10,
            Active = false,
            Score = 9.5
        };

        var mdf = MarkoutSerializer.Serialize(record, NullableAndTitleTestContext.Default);

        Assert.Contains("Label: Test", mdf);
        Assert.Contains("Count: 10", mdf);
        Assert.Contains("Active: no", mdf);
        Assert.Contains("Score: 9.5", mdf);
    }

    [Fact]
    public void Nullable_LineBreaks_NullsSkipped()
    {
        var record = new NullableScalarsLineBreaks
        {
            Label = "Test",
            Count = null,
            Active = null,
            Score = null
        };

        var mdf = MarkoutSerializer.Serialize(record, NullableAndTitleTestContext.Default);

        Assert.Contains("Label: Test", mdf);
        Assert.DoesNotContain("Count:", mdf);
        Assert.DoesNotContain("Active:", mdf);
        Assert.DoesNotContain("Score:", mdf);
    }

    // --- Nullable: LineBreaksDoubleSpace layout ---

    [Fact]
    public void Nullable_DoubleSpace_NullsSkipped()
    {
        var record = new NullableScalarsDoubleSpace
        {
            Label = "Test",
            Count = null,
            Active = true
        };

        var mdf = MarkoutSerializer.Serialize(record, NullableAndTitleTestContext.Default);

        Assert.Contains("Label: Test", mdf);
        Assert.DoesNotContain("Count:", mdf);
        Assert.Contains("Active: yes", mdf);
    }

    // --- Nullable: List layout ---

    [Fact]
    public void Nullable_List_NullsSkipped()
    {
        var record = new NullableScalarsList
        {
            Label = "Test",
            Count = null,
            Active = true
        };

        var mdf = MarkoutSerializer.Serialize(record, NullableAndTitleTestContext.Default);

        Assert.Contains("- Label: Test", mdf);
        Assert.DoesNotContain("Count:", mdf);
        Assert.Contains("- Active: yes", mdf);
    }

    // --- Nullable: Table cells ---

    [Fact]
    public void Nullable_InTable_NullRendersEmpty()
    {
        var container = new NullableTableContainer
        {
            Title = "My List",
            Items = new List<NullableInTableRow>
            {
                new() { Name = "Item1", Priority = 1, Completed = true },
                new() { Name = "Item2", Priority = null, Completed = null }
            }
        };

        var mdf = MarkoutSerializer.Serialize(container, NullableAndTitleTestContext.Default);

        // Table should contain Item1 with values
        Assert.Contains("| Item1 |", mdf);
        Assert.Contains("| 1 |", mdf);
        Assert.Contains("| yes |", mdf);
        // Item2 should have empty cells for null values
        Assert.Contains("| Item2 |", mdf);
    }

    // --- Title property deduplication ---

    [Fact]
    public void TitleProperty_NotDuplicatedAsScalar()
    {
        var record = new TitleDedupRecord
        {
            Name = "MyWidget",
            Kind = "gadget",
            Count = 3
        };

        var mdf = MarkoutSerializer.Serialize(record, NullableAndTitleTestContext.Default);

        // Title should appear as H1
        Assert.Contains("# MyWidget", mdf);
        // Name should NOT appear again as a scalar field
        Assert.DoesNotContain("Name: MyWidget", mdf);
        // Other scalars should still appear
        Assert.Contains("Kind: gadget", mdf);
        Assert.Contains("Count: 3", mdf);
    }

    [Fact]
    public void DescriptionProperty_NotDuplicatedAsScalar()
    {
        var record = new TitleAndDescDedupRecord
        {
            Name = "MyWidget",
            Summary = "A useful widget",
            Kind = "gadget",
            Count = 3
        };

        var mdf = MarkoutSerializer.Serialize(record, NullableAndTitleTestContext.Default);

        // Title should appear as H1
        Assert.Contains("# MyWidget", mdf);
        // Description should appear as paragraph
        Assert.Contains("A useful widget", mdf);
        // Neither Name nor Summary should appear as scalar fields
        Assert.DoesNotContain("Name: MyWidget", mdf);
        Assert.DoesNotContain("Summary: A useful widget", mdf);
        // Other scalars should still appear
        Assert.Contains("Kind: gadget", mdf);
        Assert.Contains("Count: 3", mdf);
    }

    [Fact]
    public void TitleContextProperty_NotDuplicatedAsScalar()
    {
        var record = new TitleContextDedupRecord
        {
            Name = "MyWidget",
            Version = "2.0",
            Kind = "gadget"
        };

        var mdf = MarkoutSerializer.Serialize(record, NullableAndTitleTestContext.Default);

        // Title with context should appear as H1
        Assert.Contains("# MyWidget (2.0)", mdf);
        // Neither Name nor Version should appear as scalar fields
        Assert.DoesNotContain("Name: MyWidget", mdf);
        Assert.DoesNotContain("Version: 2.0", mdf);
        // Other scalars should still appear
        Assert.Contains("Kind: gadget", mdf);
    }

    // --- String null-skipping in OneLine mode ---

    [Fact]
    public void OneLine_NullString_Skipped()
    {
        var record = new StringOnlyOneLine { Name = null, Description = "hello", Count = 5 };
        var mdf = MarkoutSerializer.Serialize(record, NullableAndTitleTestContext.Default);

        Assert.DoesNotContain("Name:", mdf);
        Assert.Contains("Description: hello", mdf);
        Assert.Contains("Count: 5", mdf);
    }

    [Fact]
    public void OneLine_EmptyString_Skipped()
    {
        var record = new StringOnlyOneLine { Name = "", Description = "hello", Count = 5 };
        var mdf = MarkoutSerializer.Serialize(record, NullableAndTitleTestContext.Default);

        Assert.DoesNotContain("Name:", mdf);
        Assert.Contains("Description: hello", mdf);
    }

    [Fact]
    public void OneLine_NonNullString_Rendered()
    {
        var record = new StringOnlyOneLine { Name = "Widget", Description = "A thing", Count = 1 };
        var mdf = MarkoutSerializer.Serialize(record, NullableAndTitleTestContext.Default);

        Assert.Contains("Name: Widget", mdf);
        Assert.Contains("Description: A thing", mdf);
        Assert.Contains("Count: 1", mdf);
    }
}
