using Markout;

namespace Markout.Tests;

// --- Test types ---

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class TypeWithFields
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
[MarkoutContext(typeof(TypeWithFields))]
public partial class IgnoreFieldsTestContext : MarkoutSerializerContext
{
}

// --- Tests ---

[Collection("ConsoleError")]
public class IgnoreFieldsTests
{
    [Fact]
    public void Serialize_TableFormatter_RendersFieldsAsTable()
    {
        var view = new TypeWithFields
        {
            Title = "Test",
            Count = 42,
            Items = [new SimpleRow { Name = "A", Value = "1" }]
        };

        var sw = new StringWriter();
        MarkoutSerializer.Serialize(view, sw, new TableFormatter(), IgnoreFieldsTestContext.Default);

        var output = sw.ToString();
        // TableFormatter renders field-compatible content in compact table form.
        Assert.Contains("A", output);
    }

    [Fact]
    public void Serialize_MarkdownFormatter_RendersFields()
    {
        var view = new TypeWithFields
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
