using Markout;

namespace Markout.Tests;

// --- Test models ---

[MarkoutSerializable]
public class DateFormatOneLine
{
    public string? Name { get; set; }
    [MarkoutFormat("yyyy-MM-dd")]
    public DateTimeOffset Published { get; set; }
}

[MarkoutSerializable(FieldLayout = FieldLayout.Table)]
public class DateFormatLineBreaks
{
    public string? Name { get; set; }
    [MarkoutFormat("yyyy-MM-dd")]
    public DateTimeOffset Published { get; set; }
}

[MarkoutSerializable(FieldLayout = FieldLayout.Bulleted)]
public class DateFormatList
{
    public string? Name { get; set; }
    [MarkoutFormat("yyyy-MM-dd")]
    public DateTimeOffset Published { get; set; }
}

[MarkoutSerializable]
public class NumericFormatOneLine
{
    public string? Label { get; set; }
    [MarkoutFormat("N0")]
    public int Size { get; set; }
    [MarkoutFormat("P1")]
    public double Rate { get; set; }
}

[MarkoutSerializable(FieldLayout = FieldLayout.Table)]
public class NumericFormatLineBreaks
{
    public string? Label { get; set; }
    [MarkoutFormat("N0")]
    public int Size { get; set; }
}

[MarkoutSerializable]
public class NullableDateFormatOneLine
{
    public string? Name { get; set; }
    [MarkoutFormat("yyyy-MM-dd")]
    public DateTimeOffset? Published { get; set; }
}

[MarkoutSerializable(FieldLayout = FieldLayout.Table)]
public class NullableDateFormatLineBreaks
{
    public string? Name { get; set; }
    [MarkoutFormat("yyyy-MM-dd")]
    public DateTimeOffset? Published { get; set; }
}

[MarkoutSerializable]
public class DateFormatTableRow
{
    public string? Name { get; set; }
    [MarkoutFormat("yyyy-MM-dd")]
    public DateTimeOffset Published { get; set; }
}

[MarkoutSerializable]
public class DateFormatTableContainer
{
    [MarkoutSection(Name = "Items")]
    public List<DateFormatTableRow>? Items { get; set; }
}

// --- Context ---

[MarkoutContext(typeof(DateFormatOneLine))]
[MarkoutContext(typeof(DateFormatLineBreaks))]
[MarkoutContext(typeof(DateFormatList))]
[MarkoutContext(typeof(NumericFormatOneLine))]
[MarkoutContext(typeof(NumericFormatLineBreaks))]
[MarkoutContext(typeof(NullableDateFormatOneLine))]
[MarkoutContext(typeof(NullableDateFormatLineBreaks))]
[MarkoutContext(typeof(DateFormatTableContainer))]
public partial class FormatAttributeTestContext : MarkoutSerializerContext
{
}

public class FormatAttributeTests
{
    [Fact]
    public void DateFormat_OneLine_UsesCustomFormat()
    {
        var record = new DateFormatOneLine
        {
            Name = "Package",
            Published = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero)
        };

        var mdf = MarkoutSerializer.Serialize(record, FormatAttributeTestContext.Default);

        Assert.Contains("| Published | 2024-06-15 |", mdf);
        // Should NOT contain the full ISO format
        Assert.DoesNotContain("T12:00:00", mdf);
    }

    [Fact]
    public void DateFormat_LineBreaks_UsesCustomFormat()
    {
        var record = new DateFormatLineBreaks
        {
            Name = "Package",
            Published = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero)
        };

        var mdf = MarkoutSerializer.Serialize(record, FormatAttributeTestContext.Default);

        Assert.Contains("| Published | 2024-06-15 |", mdf);
        Assert.DoesNotContain("T12:00:00", mdf);
    }

    [Fact]
    public void DateFormat_List_UsesCustomFormat()
    {
        var record = new DateFormatList
        {
            Name = "Package",
            Published = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero)
        };

        var mdf = MarkoutSerializer.Serialize(record, FormatAttributeTestContext.Default);

        Assert.Contains("Published: 2024-06-15", mdf);
        Assert.DoesNotContain("T12:00:00", mdf);
    }

    [Fact]
    public void NumericFormat_OneLine_UsesCustomFormat()
    {
        var record = new NumericFormatOneLine
        {
            Label = "Widget",
            Size = 1234567,
            Rate = 0.856
        };

        var mdf = MarkoutSerializer.Serialize(record, FormatAttributeTestContext.Default);

        Assert.Contains("| Size | 1,234,567 |", mdf);
        Assert.Contains("| Rate | 85.6", mdf);
    }

    [Fact]
    public void NumericFormat_LineBreaks_UsesCustomFormat()
    {
        var record = new NumericFormatLineBreaks
        {
            Label = "Widget",
            Size = 1234567
        };

        var mdf = MarkoutSerializer.Serialize(record, FormatAttributeTestContext.Default);

        Assert.Contains("| Size | 1,234,567 |", mdf);
    }

    [Fact]
    public void NullableDateFormat_OneLine_SetValue_UsesCustomFormat()
    {
        var record = new NullableDateFormatOneLine
        {
            Name = "Package",
            Published = new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero)
        };

        var mdf = MarkoutSerializer.Serialize(record, FormatAttributeTestContext.Default);

        Assert.Contains("| Published | 2024-03-01 |", mdf);
        Assert.DoesNotContain("T00:00:00", mdf);
    }

    [Fact]
    public void NullableDateFormat_OneLine_NullValue_Skipped()
    {
        var record = new NullableDateFormatOneLine
        {
            Name = "Package",
            Published = null
        };

        var mdf = MarkoutSerializer.Serialize(record, FormatAttributeTestContext.Default);

        Assert.Contains("| Name | Package |", mdf);
        Assert.DoesNotContain("Published", mdf);
    }

    [Fact]
    public void NullableDateFormat_LineBreaks_SetValue_UsesCustomFormat()
    {
        var record = new NullableDateFormatLineBreaks
        {
            Name = "Package",
            Published = new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero)
        };

        var mdf = MarkoutSerializer.Serialize(record, FormatAttributeTestContext.Default);

        Assert.Contains("| Published | 2024-03-01 |", mdf);
    }

    [Fact]
    public void DateFormat_InTable_UsesCustomFormat()
    {
        var container = new DateFormatTableContainer
        {
            Items = new List<DateFormatTableRow>
            {
                new() { Name = "Pkg1", Published = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero) },
                new() { Name = "Pkg2", Published = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero) }
            }
        };

        var mdf = MarkoutSerializer.Serialize(container, FormatAttributeTestContext.Default);

        Assert.Contains("2024-06-15", mdf);
        Assert.Contains("2023-01-01", mdf);
        // Should NOT contain full ISO format
        Assert.DoesNotContain("T12:00:00", mdf);
    }
}
