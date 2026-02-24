using Markout;

namespace Markout.Tests;

// --- Test types ---

[MarkoutSerializable(TitleProperty = nameof(Title))]
[MarkoutIgnoreFields(nameof(OneLineWriter))]
public class ViewWithIgnoreFields
{
    [MarkoutIgnore] public string Title { get; set; } = "";
    public int Count { get; set; }  // Field - would warn without the attribute

    [MarkoutSection(Name = "Items")]
    public List<SimpleRow>? Items { get; set; }
}

[MarkoutSerializable]
public class SimpleRow
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
}

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class ViewWithoutIgnoreFields
{
    [MarkoutIgnore] public string Title { get; set; } = "";
    public int Count { get; set; }  // Field - will warn

    [MarkoutSection(Name = "Items")]
    public List<SimpleRow>? Items { get; set; }
}

[MarkoutContext(typeof(SimpleRow))]
[MarkoutContext(typeof(ViewWithIgnoreFields))]
[MarkoutContext(typeof(ViewWithoutIgnoreFields))]
public partial class IgnoreFieldsTestContext : MarkoutSerializerContext
{
}

// --- Tests ---

[Collection("ConsoleError")]
public class IgnoreFieldsTests
{
    [Fact]
    public void SuppressWarning_PreventsWarning()
    {
        var errWriter = new StringWriter();
        var origErr = Console.Error;
        Console.SetError(errWriter);
        try
        {
            var sw = new StringWriter();
            var writer = new OneLineWriter(sw);
            writer.SuppressWarning(MarkoutShape.Fields);
            writer.WriteField("Key", "Value");
            Assert.Equal("", sw.ToString());
            Assert.Equal("", errWriter.ToString());  // No warning
        }
        finally
        {
            Console.SetError(origErr);
        }
    }

    [Fact]
    public void WithoutSuppressWarning_GeneratesWarning()
    {
        var errWriter = new StringWriter();
        var origErr = Console.Error;
        Console.SetError(errWriter);
        try
        {
            var sw = new StringWriter();
            var writer = new OneLineWriter(sw);
            writer.WriteField("Key", "Value");
            Assert.Equal("", sw.ToString());
            Assert.Contains("does not support Fields", errWriter.ToString());
        }
        finally
        {
            Console.SetError(origErr);
        }
    }

    [Fact]
    public void Serialize_WithIgnoreFieldsAttribute_NoWarning()
    {
        var errWriter = new StringWriter();
        var origErr = Console.Error;
        Console.SetError(errWriter);
        try
        {
            var view = new ViewWithIgnoreFields
            {
                Title = "Test",
                Count = 42,
                Items = [new SimpleRow { Name = "A", Value = "1" }]
            };

            var sw = new StringWriter();
            var writer = new OneLineWriter(sw);
            MarkoutSerializer.Serialize(view, writer, IgnoreFieldsTestContext.Default);

            Assert.Contains("A", sw.ToString());
            Assert.Equal("", errWriter.ToString());  // No warning due to [MarkoutIgnoreFields]
        }
        finally
        {
            Console.SetError(origErr);
        }
    }

    [Fact]
    public void Serialize_WithoutIgnoreFieldsAttribute_GeneratesWarning()
    {
        var errWriter = new StringWriter();
        var origErr = Console.Error;
        Console.SetError(errWriter);
        try
        {
            var view = new ViewWithoutIgnoreFields
            {
                Title = "Test",
                Count = 42,
                Items = [new SimpleRow { Name = "A", Value = "1" }]
            };

            var sw = new StringWriter();
            var writer = new OneLineWriter(sw);
            MarkoutSerializer.Serialize(view, writer, IgnoreFieldsTestContext.Default);

            Assert.Contains("A", sw.ToString());
            // FieldList warning because AutoFields renders scalar fields as a field list
            Assert.Contains("does not support FieldList", errWriter.ToString());
        }
        finally
        {
            Console.SetError(origErr);
        }
    }

    [Fact]
    public void Serialize_WithIgnoreFieldsAttribute_MarkdownWriterStillRendersFields()
    {
        // MarkoutIgnoreFields only affects OneLineWriter, not MarkdownWriter
        var view = new ViewWithIgnoreFields
        {
            Title = "Test",
            Count = 42,
            Items = [new SimpleRow { Name = "A", Value = "1" }]
        };

        var output = MarkoutSerializer.Serialize(view, IgnoreFieldsTestContext.Default);

        // MarkdownWriter should still render the Count field
        Assert.Contains("# Test", output);
        Assert.Contains("Count:", output);
        Assert.Contains("42", output);
    }
}
