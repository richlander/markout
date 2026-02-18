using MarkdownTable.Formatting;
using System.Text;

namespace MarkdownTable.Tests;

public class FieldDocumentTests
{
    private static FieldDocument ParseText(string text)
    {
        return FieldDocument.Parse(Encoding.UTF8.GetBytes(text));
    }

    // --- Bold fields ---

    [Fact]
    public void Parse_BoldField_GetString_ReturnsValue()
    {
        using var doc = ParseText("**name:** Alice");
        Assert.Equal("Alice", doc.GetString("name"));
    }

    [Fact]
    public void Parse_BoldField_GetBool_ReturnsTrue()
    {
        using var doc = ParseText("**enabled:** true");
        Assert.True(doc.GetBool("enabled"));
    }

    [Fact]
    public void Parse_BoldField_GetInt32_ReturnsValue()
    {
        using var doc = ParseText("**count:** 42");
        Assert.Equal(42, doc.GetInt32("count"));
    }

    // --- Plain fields ---

    [Fact]
    public void Parse_PlainField_GetString_ReturnsValue()
    {
        using var doc = ParseText("name: Alice");
        Assert.Equal("Alice", doc.GetString("name"));
    }

    [Fact]
    public void Parse_PlainField_GetBool_ReturnsTrue()
    {
        using var doc = ParseText("enabled: true");
        Assert.True(doc.GetBool("enabled"));
    }

    [Fact]
    public void Parse_PlainField_GetInt32_ReturnsValue()
    {
        using var doc = ParseText("count: 42");
        Assert.Equal(42, doc.GetInt32("count"));
    }

    [Fact]
    public void Parse_PlainField_GetArray_ReturnsNull_ForScalar()
    {
        using var doc = ParseText("name: Alice");
        Assert.Null(doc.GetArray("name"));
    }

    // --- Mixed bold and plain ---

    [Fact]
    public void Parse_MixedBoldAndPlain_BothAccessible()
    {
        using var doc = ParseText("**name:** Alice\nage: 30");
        Assert.Equal("Alice", doc.GetString("name"));
        Assert.Equal(30, doc.GetInt32("age"));
    }

    // --- Array fields ---

    [Fact]
    public void Parse_BoldArrayField_GetArray_ReturnsItems()
    {
        using var doc = ParseText("**items:**\n- apple\n- banana\n- cherry");
        var items = doc.GetArray("items");
        Assert.NotNull(items);
        Assert.Equal(["apple", "banana", "cherry"], items);
    }

    [Fact]
    public void Parse_PlainArrayField_GetArray_ReturnsItems()
    {
        using var doc = ParseText("items:\n- apple\n- banana\n- cherry");
        var items = doc.GetArray("items");
        Assert.NotNull(items);
        Assert.Equal(["apple", "banana", "cherry"], items);
    }

    [Fact]
    public void Parse_ArrayField_WithBlankLineBetweenHeaderAndItems()
    {
        using var doc = ParseText("items:\n\n- apple\n- banana");
        var items = doc.GetArray("items");
        Assert.NotNull(items);
        Assert.Equal(["apple", "banana"], items);
    }

    // --- BOM handling ---

    [Fact]
    public void Parse_WithBom_SkipsBomAndParsesFields()
    {
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var content = Encoding.UTF8.GetBytes("name: Alice");
        var bytes = new byte[bom.Length + content.Length];
        bom.CopyTo(bytes, 0);
        content.CopyTo(bytes, bom.Length);

        using var doc = FieldDocument.Parse(bytes);
        Assert.Equal("Alice", doc.GetString("name"));
    }

    // --- Case-insensitive key lookup ---

    [Fact]
    public void GetString_CaseInsensitiveKey()
    {
        using var doc = ParseText("Name: Alice");
        Assert.Equal("Alice", doc.GetString("name"));
        Assert.Equal("Alice", doc.GetString("NAME"));
        Assert.Equal("Alice", doc.GetString("Name"));
    }

    // --- ContainsKey ---

    [Fact]
    public void ContainsKey_ExistingKey_ReturnsTrue()
    {
        using var doc = ParseText("name: Alice");
        Assert.True(doc.ContainsKey("name"));
    }

    [Fact]
    public void ContainsKey_MissingKey_ReturnsFalse()
    {
        using var doc = ParseText("name: Alice");
        Assert.False(doc.ContainsKey("age"));
    }

    [Fact]
    public void ContainsKey_CaseInsensitive()
    {
        using var doc = ParseText("Name: Alice");
        Assert.True(doc.ContainsKey("name"));
        Assert.True(doc.ContainsKey("NAME"));
    }

    // --- Missing key defaults ---

    [Fact]
    public void GetString_MissingKey_ReturnsNull()
    {
        using var doc = ParseText("name: Alice");
        Assert.Null(doc.GetString("missing"));
    }

    [Fact]
    public void GetBool_MissingKey_ReturnsFalse()
    {
        using var doc = ParseText("name: Alice");
        Assert.False(doc.GetBool("missing"));
    }

    [Fact]
    public void GetInt32_MissingKey_ReturnsZero()
    {
        using var doc = ParseText("name: Alice");
        Assert.Equal(0, doc.GetInt32("missing"));
    }

    [Fact]
    public void GetArray_MissingKey_ReturnsNull()
    {
        using var doc = ParseText("name: Alice");
        Assert.Null(doc.GetArray("missing"));
    }

    // --- URL not treated as field ---

    [Fact]
    public void Parse_UrlInValue_NotTreatedAsField()
    {
        using var doc = ParseText("https://example.com/path");
        Assert.False(doc.ContainsKey("https"));
    }

    [Fact]
    public void Parse_FieldWithUrlValue_ParsedCorrectly()
    {
        using var doc = ParseText("homepage: https://example.com");
        Assert.Equal("https://example.com", doc.GetString("homepage"));
    }

    // --- Empty document ---

    [Fact]
    public void Parse_EmptyDocument_NoFields()
    {
        using var doc = ParseText("");
        Assert.Null(doc.GetString("anything"));
        Assert.False(doc.ContainsKey("anything"));
    }

    // --- GetArrayList ---

    [Fact]
    public void GetArrayList_ArrayField_ReturnsList()
    {
        using var doc = ParseText("items:\n- one\n- two\n- three");
        var list = doc.GetArrayList("items");
        Assert.NotNull(list);
        Assert.IsType<List<string>>(list);
        Assert.Equal(["one", "two", "three"], list);
    }

    [Fact]
    public void GetArrayList_MissingKey_ReturnsNull()
    {
        using var doc = ParseText("name: Alice");
        Assert.Null(doc.GetArrayList("missing"));
    }

    [Fact]
    public void GetArrayList_ScalarField_ReturnsNull()
    {
        using var doc = ParseText("name: Alice");
        Assert.Null(doc.GetArrayList("name"));
    }

    // --- GetString on array field ---

    [Fact]
    public void GetString_ArrayField_ReturnsJoinedText()
    {
        using var doc = ParseText("items:\n- apple\n- banana\n- cherry");
        Assert.Equal("apple, banana, cherry", doc.GetString("items"));
    }

    // --- GetBool case-insensitive ---

    [Theory]
    [InlineData("true")]
    [InlineData("True")]
    [InlineData("TRUE")]
    [InlineData("tRuE")]
    public void GetBool_CaseInsensitiveTrue(string trueValue)
    {
        using var doc = ParseText($"flag: {trueValue}");
        Assert.True(doc.GetBool("flag"));
    }

    [Fact]
    public void GetBool_FalseValue_ReturnsFalse()
    {
        using var doc = ParseText("flag: false");
        Assert.False(doc.GetBool("flag"));
    }

    [Fact]
    public void GetBool_NonBoolValue_ReturnsFalse()
    {
        using var doc = ParseText("flag: yes");
        Assert.False(doc.GetBool("flag"));
    }

    // --- Negative integer ---

    [Fact]
    public void GetInt32_NegativeValue_ReturnsNegative()
    {
        using var doc = ParseText("offset: -5");
        Assert.Equal(-5, doc.GetInt32("offset"));
    }

    // --- Non-integer value ---

    [Fact]
    public void GetInt32_NonIntegerValue_ReturnsZero()
    {
        using var doc = ParseText("count: abc");
        Assert.Equal(0, doc.GetInt32("count"));
    }

    [Fact]
    public void GetInt32_FloatValue_ReturnsZero()
    {
        using var doc = ParseText("count: 3.14");
        Assert.Equal(0, doc.GetInt32("count"));
    }

    // --- Field with colon in value ---

    [Fact]
    public void Parse_PlainField_ColonInValue_PreservesFullValue()
    {
        using var doc = ParseText("message: hello: world");
        Assert.Equal("hello: world", doc.GetString("message"));
    }

    [Fact]
    public void Parse_BoldField_ColonInValue_PreservesFullValue()
    {
        using var doc = ParseText("**message:** hello: world");
        Assert.Equal("hello: world", doc.GetString("message"));
    }

    // --- Round-trip: plain format ---

    [Fact]
    public void Parse_CacheWriterPattern_AllFieldTypes()
    {
        var text = """
            packageName: MyPackage
            version: 1.2.3
            assemblyCount: 5
            isPrerelease: true
            targetFrameworks:
            - net8.0
            - net9.0
            """;

        using var doc = ParseText(text);
        Assert.Equal("MyPackage", doc.GetString("packageName"));
        Assert.Equal("1.2.3", doc.GetString("version"));
        Assert.Equal(5, doc.GetInt32("assemblyCount"));
        Assert.True(doc.GetBool("isPrerelease"));
        var tfms = doc.GetArray("targetFrameworks");
        Assert.NotNull(tfms);
        Assert.Equal(["net8.0", "net9.0"], tfms);
    }

    // --- Additional edge cases ---

    [Fact]
    public void Parse_EmptyPlainFieldWithNoBullets_EmptyString()
    {
        using var doc = ParseText("title:\nsomethingElse: value");
        Assert.Equal("", doc.GetString("title"));
    }

    [Fact]
    public void Parse_MultipleArrayFields()
    {
        var text = "fruits:\n- apple\n- banana\ncolors:\n- red\n- blue";
        using var doc = ParseText(text);
        Assert.Equal(["apple", "banana"], doc.GetArray("fruits")!);
        Assert.Equal(["red", "blue"], doc.GetArray("colors")!);
    }

    [Fact]
    public void Parse_BoldFieldWithTrailingWhitespace_Trimmed()
    {
        using var doc = ParseText("**name:** Alice   ");
        Assert.Equal("Alice", doc.GetString("name"));
    }

    [Fact]
    public void Parse_PlainFieldWithTrailingWhitespace_Trimmed()
    {
        using var doc = ParseText("name: Alice   ");
        Assert.Equal("Alice", doc.GetString("name"));
    }

    [Fact]
    public void GetBool_ArrayField_ReturnsFalse()
    {
        using var doc = ParseText("flags:\n- true\n- false");
        Assert.False(doc.GetBool("flags"));
    }

    [Fact]
    public void GetInt32_ArrayField_ReturnsZero()
    {
        using var doc = ParseText("nums:\n- 1\n- 2");
        Assert.Equal(0, doc.GetInt32("nums"));
    }

    [Fact]
    public void GetInt32_EmptyValue_ReturnsZero()
    {
        using var doc = ParseText("count:");
        Assert.Equal(0, doc.GetInt32("count"));
    }

    [Fact]
    public void Parse_WindowsLineEndings_ParsesCorrectly()
    {
        using var doc = ParseText("name: Alice\r\nage: 30\r\n");
        Assert.Equal("Alice", doc.GetString("name"));
        Assert.Equal(30, doc.GetInt32("age"));
    }

    [Fact]
    public void Parse_DuplicateKeys_FirstWins()
    {
        using var doc = ParseText("name: Alice\nname: Bob");
        Assert.Equal("Alice", doc.GetString("name"));
    }

    // --- Static targeted lookup ---

    private static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);

    [Fact]
    public void Static_GetString_FindsPlainField()
    {
        var bytes = Utf8("packageName: Newtonsoft.Json\nversion: 13.0.3");
        Assert.Equal("Newtonsoft.Json", FieldDocument.GetString(bytes, "packageName"));
        Assert.Equal("13.0.3", FieldDocument.GetString(bytes, "version"));
    }

    [Fact]
    public void Static_GetString_FindsBoldField()
    {
        var bytes = Utf8("**name:** Alice\n**age:** 30");
        Assert.Equal("Alice", FieldDocument.GetString(bytes, "name"));
    }

    [Fact]
    public void Static_GetString_ReturnsNullForMissing()
    {
        var bytes = Utf8("name: Alice");
        Assert.Null(FieldDocument.GetString(bytes, "missing"));
    }

    [Fact]
    public void Static_GetString_CaseInsensitive()
    {
        var bytes = Utf8("PackageName: Test");
        Assert.Equal("Test", FieldDocument.GetString(bytes, "packagename"));
        Assert.Equal("Test", FieldDocument.GetString(bytes, "PACKAGENAME"));
    }

    [Fact]
    public void Static_GetBool_ReturnsTrue()
    {
        var bytes = Utf8("hasReadme: true");
        Assert.True(FieldDocument.GetBool(bytes, "hasReadme"));
    }

    [Fact]
    public void Static_GetBool_ReturnsFalseForMissing()
    {
        var bytes = Utf8("name: Alice");
        Assert.False(FieldDocument.GetBool(bytes, "missing"));
    }

    [Fact]
    public void Static_GetBool_ReturnsFalseForNonBool()
    {
        var bytes = Utf8("name: Alice");
        Assert.False(FieldDocument.GetBool(bytes, "name"));
    }

    [Fact]
    public void Static_GetInt32_ReturnsValue()
    {
        var bytes = Utf8("count: 42");
        Assert.Equal(42, FieldDocument.GetInt32(bytes, "count"));
    }

    [Fact]
    public void Static_GetInt32_ReturnsZeroForMissing()
    {
        var bytes = Utf8("name: Alice");
        Assert.Equal(0, FieldDocument.GetInt32(bytes, "missing"));
    }

    [Fact]
    public void Static_GetArray_ReturnsItems()
    {
        var bytes = Utf8("frameworks:\n- net6.0\n- net8.0\n- net9.0");
        var items = FieldDocument.GetArray(bytes, "frameworks");
        Assert.NotNull(items);
        Assert.Equal(["net6.0", "net8.0", "net9.0"], items);
    }

    [Fact]
    public void Static_GetArray_ReturnsNullForMissing()
    {
        var bytes = Utf8("name: Alice");
        Assert.Null(FieldDocument.GetArray(bytes, "missing"));
    }

    [Fact]
    public void Static_ContainsKey_TrueAndFalse()
    {
        var bytes = Utf8("name: Alice\nage: 30");
        Assert.True(FieldDocument.ContainsKey(bytes, "name"));
        Assert.False(FieldDocument.ContainsKey(bytes, "missing"));
    }

    [Fact]
    public void Static_GetString_StopsAtFirstMatch()
    {
        // First field should be found without scanning the rest
        var bytes = Utf8("first: found\nsecond: value\nthird: value");
        Assert.Equal("found", FieldDocument.GetString(bytes, "first"));
    }

    [Fact]
    public void Static_GetString_ArrayFieldReturnsJoined()
    {
        var bytes = Utf8("items:\n- a\n- b\n- c");
        Assert.Equal("a, b, c", FieldDocument.GetString(bytes, "items"));
    }

    [Fact]
    public void Static_GetString_HandlesEmptyValue()
    {
        var bytes = Utf8("key: ");
        Assert.Equal("", FieldDocument.GetString(bytes, "key"));
    }

    [Fact]
    public void Static_GetString_HandlesBom()
    {
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Utf8("name: Alice")).ToArray();
        Assert.Equal("Alice", FieldDocument.GetString(bytes, "name"));
    }
}
