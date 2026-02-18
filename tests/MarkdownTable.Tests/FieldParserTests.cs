using MarkdownTable.Formatting;

namespace MarkdownTable.Tests;

public class FieldParserTests
{
    // --- Single bold field ---

    [Fact]
    public void ParseFields_SingleBoldField_ReturnsOneEntry()
    {
        var result = FieldParser.ParseFields("**Name:** Alice");

        Assert.Single(result);
        Assert.Equal("Name", result[0].Key);
        Assert.Equal("Alice", result[0].Value.Text);
        Assert.True(result[0].Value.IsScalar);
    }

    // --- Multiple bold fields ---

    [Fact]
    public void ParseFields_MultipleBoldFields_ReturnsAllEntries()
    {
        var text = """
            **Name:** Alice
            **Role:** Developer
            **Team:** Platform
            """;

        var result = FieldParser.ParseFields(text);

        Assert.Equal(3, result.Count);
        Assert.Equal("Name", result[0].Key);
        Assert.Equal("Alice", result[0].Value.Text);
        Assert.Equal("Role", result[1].Key);
        Assert.Equal("Developer", result[1].Value.Text);
        Assert.Equal("Team", result[2].Key);
        Assert.Equal("Platform", result[2].Value.Text);
    }

    // --- Bold field with empty value ---

    [Fact]
    public void ParseFields_BoldFieldWithEmptyValue_ReturnsEmptyScalar()
    {
        var result = FieldParser.ParseFields("**Status:**");

        Assert.Single(result);
        Assert.Equal("Status", result[0].Key);
        Assert.Equal("", result[0].Value.Text);
        Assert.True(result[0].Value.IsScalar);
    }

    // --- Array field (bold key, blank line, bullet list) ---

    [Fact]
    public void ParseFields_ArrayFieldWithBlankLine_ReturnsArrayValue()
    {
        var text = """
            **Tags:**

            - alpha
            - beta
            - gamma
            """;

        var result = FieldParser.ParseFields(text);

        Assert.Single(result);
        Assert.Equal("Tags", result[0].Key);
        Assert.True(result[0].Value.IsArray);
        Assert.Equal(["alpha", "beta", "gamma"], result[0].Value.Items);
    }

    // --- Array field without blank line between header and bullets ---

    [Fact]
    public void ParseFields_ArrayFieldWithoutBlankLine_ReturnsArrayValue()
    {
        var text = """
            **Items:**
            - one
            - two
            """;

        var result = FieldParser.ParseFields(text);

        Assert.Single(result);
        Assert.Equal("Items", result[0].Key);
        Assert.True(result[0].Value.IsArray);
        Assert.Equal(["one", "two"], result[0].Value.Items);
    }

    // --- Plain fields (Key: Value) ---

    [Fact]
    public void ParseFields_PlainField_ReturnsEntry()
    {
        var result = FieldParser.ParseFields("Status: Active");

        Assert.Single(result);
        Assert.Equal("Status", result[0].Key);
        Assert.Equal("Active", result[0].Value.Text);
    }

    [Fact]
    public void ParseFields_MultiplePlainFields_ReturnsAllEntries()
    {
        var text = """
            Name: Bob
            Age: 30
            """;

        var result = FieldParser.ParseFields(text);

        Assert.Equal(2, result.Count);
        Assert.Equal("Name", result[0].Key);
        Assert.Equal("Bob", result[0].Value.Text);
        Assert.Equal("Age", result[1].Key);
        Assert.Equal("30", result[1].Value.Text);
    }

    // --- OneLine fields with pipe separator ---

    [Fact]
    public void ParseFields_OneLineFields_ParsesPipeSeparatedFields()
    {
        var result = FieldParser.ParseFields("Name: Alice | Role: Dev | Team: Core");

        Assert.Equal(3, result.Count);
        Assert.Equal("Name", result[0].Key);
        Assert.Equal("Alice", result[0].Value.Text);
        Assert.Equal("Role", result[1].Key);
        Assert.Equal("Dev", result[1].Value.Text);
        Assert.Equal("Team", result[2].Key);
        Assert.Equal("Core", result[2].Value.Text);
    }

    // --- URL exclusion ---

    [Fact]
    public void ParseFields_UrlLine_IsNotParsedAsField()
    {
        var result = FieldParser.ParseFields("https://example.com");

        Assert.Empty(result);
    }

    [Fact]
    public void ParseFields_HttpUrl_IsNotParsedAsField()
    {
        var result = FieldParser.ParseFields("http://example.com/path");

        Assert.Empty(result);
    }

    // --- ParseToDictionary case-insensitive lookup ---

    [Fact]
    public void ParseToDictionary_CaseInsensitiveLookup()
    {
        var text = """
            **Name:** Alice
            **Status:** Active
            """;

        var dict = FieldParser.ParseToDictionary(text);

        Assert.True(dict.ContainsKey("name"));
        Assert.True(dict.ContainsKey("NAME"));
        Assert.True(dict.ContainsKey("Name"));
        Assert.Equal("Alice", dict["name"].Text);
        Assert.Equal("Active", dict["STATUS"].Text);
    }

    // --- Mixed content (heading, table lines, fields — only fields extracted) ---

    [Fact]
    public void ParseFields_MixedContent_OnlyExtractsFields()
    {
        var text = """
            # Heading
            | Col1 | Col2 |
            | ---- | ---- |
            | val1 | val2 |
            **Name:** Alice
            > This is a callout
            ```code block```
            **Role:** Developer
            """;

        var result = FieldParser.ParseFields(text);

        Assert.Equal(2, result.Count);
        Assert.Equal("Name", result[0].Key);
        Assert.Equal("Alice", result[0].Value.Text);
        Assert.Equal("Role", result[1].Key);
        Assert.Equal("Developer", result[1].Value.Text);
    }

    // --- Parse() returns flat key-value pairs ---

    [Fact]
    public void Parse_ReturnsFlatKeyValuePairs()
    {
        var text = """
            **Name:** Alice
            **Role:** Developer
            """;

        var result = FieldParser.Parse(text);

        Assert.Equal(2, result.Count);
        Assert.Equal("Name", result[0].Key);
        Assert.Equal("Alice", result[0].Value);
        Assert.Equal("Role", result[1].Key);
        Assert.Equal("Developer", result[1].Value);
    }

    [Fact]
    public void Parse_ArrayField_ReturnsJoinedString()
    {
        var text = """
            **Tags:**
            - alpha
            - beta
            """;

        var result = FieldParser.Parse(text);

        Assert.Single(result);
        Assert.Equal("Tags", result[0].Key);
        Assert.Equal("alpha, beta", result[0].Value);
    }

    // --- ParseFields() returns FieldValue entries ---

    [Fact]
    public void ParseFields_ScalarField_ReturnsFieldValueWithIsScalar()
    {
        var result = FieldParser.ParseFields("**Key:** Value");

        Assert.Single(result);
        Assert.True(result[0].Value.IsScalar);
        Assert.False(result[0].Value.IsArray);
        Assert.Equal("Value", result[0].Value.Text);
    }

    [Fact]
    public void ParseFields_ArrayField_ReturnsFieldValueWithIsArray()
    {
        var text = """
            **List:**
            - x
            - y
            """;

        var result = FieldParser.ParseFields(text);

        Assert.Single(result);
        Assert.True(result[0].Value.IsArray);
        Assert.False(result[0].Value.IsScalar);
        Assert.Equal(2, result[0].Value.Count);
    }

    // --- Empty input returns empty results ---

    [Fact]
    public void ParseFields_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(FieldParser.ParseFields(""));
    }

    [Fact]
    public void Parse_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(FieldParser.Parse(""));
    }

    [Fact]
    public void ParseToDictionary_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(FieldParser.ParseToDictionary(""));
    }

    // --- Field with colon in value ---

    [Fact]
    public void ParseFields_BoldFieldWithColonInValue_PreservesFullValue()
    {
        var result = FieldParser.ParseFields("**time:** 12:30:00");

        Assert.Single(result);
        Assert.Equal("time", result[0].Key);
        Assert.Equal("12:30:00", result[0].Value.Text);
    }

    [Fact]
    public void ParseFields_PlainFieldWithColonInValue_PreservesFullValue()
    {
        var result = FieldParser.ParseFields("schedule: 09:00-17:00");

        Assert.Single(result);
        Assert.Equal("schedule", result[0].Key);
        Assert.Equal("09:00-17:00", result[0].Value.Text);
    }
}
