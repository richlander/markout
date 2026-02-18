using MarkdownTable.Formatting;

namespace MarkdownTable.Tests;

public class FieldValueTests
{
    // --- FromText (scalar) ---

    [Fact]
    public void FromText_IsScalar_ReturnsTrue()
    {
        var value = FieldValue.FromText("hello");
        Assert.True(value.IsScalar);
    }

    [Fact]
    public void FromText_IsArray_ReturnsFalse()
    {
        var value = FieldValue.FromText("hello");
        Assert.False(value.IsArray);
    }

    [Fact]
    public void FromText_Text_ReturnsOriginalText()
    {
        var value = FieldValue.FromText("hello");
        Assert.Equal("hello", value.Text);
    }

    [Fact]
    public void FromText_Items_ReturnsSingleElementArray()
    {
        var value = FieldValue.FromText("hello");
        Assert.Equal(["hello"], value.Items);
    }

    [Fact]
    public void FromText_Count_ReturnsOne()
    {
        var value = FieldValue.FromText("hello");
        Assert.Equal(1, value.Count);
    }

    [Fact]
    public void FromText_EmptyString_TextReturnsEmpty()
    {
        var value = FieldValue.FromText("");
        Assert.Equal("", value.Text);
    }

    [Fact]
    public void FromText_EmptyString_IsScalar()
    {
        var value = FieldValue.FromText("");
        Assert.True(value.IsScalar);
        Assert.False(value.IsArray);
    }

    [Fact]
    public void FromText_EmptyString_ItemsReturnsSingleElementArray()
    {
        var value = FieldValue.FromText("");
        Assert.Equal([""], value.Items);
    }

    [Fact]
    public void FromText_EmptyString_CountReturnsOne()
    {
        var value = FieldValue.FromText("");
        Assert.Equal(1, value.Count);
    }

    // --- FromItems (array) ---

    [Fact]
    public void FromItems_IsArray_ReturnsTrue()
    {
        var value = FieldValue.FromItems(["a", "b", "c"]);
        Assert.True(value.IsArray);
    }

    [Fact]
    public void FromItems_IsScalar_ReturnsFalse()
    {
        var value = FieldValue.FromItems(["a", "b", "c"]);
        Assert.False(value.IsScalar);
    }

    [Fact]
    public void FromItems_Text_ReturnsItemsJoinedWithCommaSpace()
    {
        var value = FieldValue.FromItems(["a", "b", "c"]);
        Assert.Equal("a, b, c", value.Text);
    }

    [Fact]
    public void FromItems_Items_ReturnsOriginalArray()
    {
        string[] items = ["a", "b", "c"];
        var value = FieldValue.FromItems(items);
        Assert.Equal(items, value.Items);
    }

    [Fact]
    public void FromItems_Count_ReturnsArrayLength()
    {
        var value = FieldValue.FromItems(["a", "b", "c"]);
        Assert.Equal(3, value.Count);
    }

    [Fact]
    public void FromItems_SingleItem_TextReturnsThatItem()
    {
        var value = FieldValue.FromItems(["only"]);
        Assert.Equal("only", value.Text);
    }

    [Fact]
    public void FromItems_SingleItem_CountReturnsOne()
    {
        var value = FieldValue.FromItems(["only"]);
        Assert.Equal(1, value.Count);
    }

    [Fact]
    public void FromItems_EmptyArray_TextReturnsEmpty()
    {
        var value = FieldValue.FromItems([]);
        Assert.Equal("", value.Text);
    }

    [Fact]
    public void FromItems_EmptyArray_CountReturnsZero()
    {
        var value = FieldValue.FromItems([]);
        Assert.Equal(0, value.Count);
    }

    // --- Implicit string conversion ---

    [Fact]
    public void ImplicitStringConversion_Scalar_ReturnsText()
    {
        FieldValue value = FieldValue.FromText("hello");
        string result = value;
        Assert.Equal("hello", result);
    }

    [Fact]
    public void ImplicitStringConversion_Array_ReturnsJoinedText()
    {
        FieldValue value = FieldValue.FromItems(["x", "y"]);
        string result = value;
        Assert.Equal("x, y", result);
    }

    // --- ToString ---

    [Fact]
    public void ToString_Scalar_ReturnsText()
    {
        var value = FieldValue.FromText("hello");
        Assert.Equal("hello", value.ToString());
    }

    [Fact]
    public void ToString_Array_ReturnsJoinedText()
    {
        var value = FieldValue.FromItems(["a", "b"]);
        Assert.Equal("a, b", value.ToString());
    }

    // --- Default value ---

    [Fact]
    public void Default_Text_ReturnsEmpty()
    {
        FieldValue value = default;
        Assert.Equal("", value.Text);
    }

    [Fact]
    public void Default_Items_ReturnsEmptyArray()
    {
        FieldValue value = default;
        Assert.Empty(value.Items);
    }

    [Fact]
    public void Default_Count_ReturnsZero()
    {
        FieldValue value = default;
        Assert.Equal(0, value.Count);
    }

    [Fact]
    public void Default_IsScalar_ReturnsTrue()
    {
        FieldValue value = default;
        Assert.True(value.IsScalar);
    }

    [Fact]
    public void Default_IsArray_ReturnsFalse()
    {
        FieldValue value = default;
        Assert.False(value.IsArray);
    }

    [Fact]
    public void Default_ToString_ReturnsEmpty()
    {
        FieldValue value = default;
        Assert.Equal("", value.ToString());
    }

    [Fact]
    public void Default_ImplicitString_ReturnsEmpty()
    {
        FieldValue value = default;
        string result = value;
        Assert.Equal("", result);
    }
}
