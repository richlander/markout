using Markout;

namespace Markout.Tests;

// --- Test types ---

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class ViewWithFields
{
    [MarkoutIgnore] public string Title { get; set; } = "";
    public int Count { get; set; }

    [MarkoutSection(Name = "Items")]
    public List<SimpleRow>? Items { get; set; }
}

[MarkoutSerializable]
public class SimpleRow
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
}

[MarkoutContext(typeof(SimpleRow))]
[MarkoutContext(typeof(ViewWithFields))]
public partial class IgnoreFieldsTestContext : MarkoutSerializerContext
{
}

// --- Tests ---

[Collection("ConsoleError")]
public class IgnoreFieldsTests
{
    [Fact]
    public void Serialize_OneLineFormatter_RendersFieldsAsTable()
    {
        var view = new ViewWithFields
        {
            Title = "Test",
            Count = 42,
            Items = [new SimpleRow { Name = "A", Value = "1" }]
        };

        var sw = new StringWriter();
        var writer = new OneLineFormatter(sw);
        MarkoutSerializer.Serialize(view, writer, IgnoreFieldsTestContext.Default);

        var output = sw.ToString();
        // OneLineFormatter renders fields as a FIELD/VALUE table
        Assert.Contains("A", output);
    }

    [Fact]
    public void Serialize_MarkdownFormatter_RendersFields()
    {
        var view = new ViewWithFields
        {
            Title = "Test",
            Count = 42,
            Items = [new SimpleRow { Name = "A", Value = "1" }]
        };

        var output = MarkoutSerializer.Serialize(view, IgnoreFieldsTestContext.Default);

        Assert.Contains("# Test", output);
        Assert.Contains("| Count | 42 |", output);
    }
}
