using Markout;
using Xunit;

namespace Markout.Tests;

public class MarkoutWriterTests
{
    [Fact]
    public void WriteHeading_Level1_WritesCorrectMarkdown()
    {
        var writer = new MarkoutWriter();
        writer.WriteHeading(1, "Package");

        Assert.Equal("# Package\n", writer.ToString());
    }

    [Fact]
    public void WriteHeading_Level2_WritesCorrectMarkdown()
    {
        var writer = new MarkoutWriter();
        writer.WriteHeading(2, "Dependencies");

        Assert.Equal("## Dependencies\n", writer.ToString());
    }

    [Fact]
    public void WriteHeading_WithContext_WritesCorrectMarkdown()
    {
        var writer = new MarkoutWriter();
        writer.WriteHeading(2, "Dependencies", "net6.0");

        Assert.Equal("## Dependencies (net6.0)\n", writer.ToString());
    }

    [Fact]
    public void WriteField_String_WritesKeyValue()
    {
        var writer = new MarkoutWriter();
        writer.WriteField("Name", "Newtonsoft.Json");

        // Note: WriteField adds two trailing spaces for markdown hard line break
        Assert.Equal("Name: Newtonsoft.Json  \n", writer.ToString());
    }

    [Fact]
    public void WriteField_BooleanTrue_WritesYes()
    {
        var writer = new MarkoutWriter();
        writer.WriteField("Signed", true);

        Assert.Equal("Signed: yes  \n", writer.ToString());
    }

    [Fact]
    public void WriteField_BooleanFalse_WritesNo()
    {
        var writer = new MarkoutWriter();
        writer.WriteField("Signed", false);

        Assert.Equal("Signed: no  \n", writer.ToString());
    }

    [Fact]
    public void WriteField_Integer_WritesNumber()
    {
        var writer = new MarkoutWriter();
        writer.WriteField("Count", 42);

        Assert.Equal("Count: 42  \n", writer.ToString());
    }

    [Fact]
    public void WriteField_Double_WritesNumber()
    {
        var writer = new MarkoutWriter();
        writer.WriteField("Version", 3.14);

        Assert.Equal("Version: 3.14  \n", writer.ToString());
    }

    [Fact]
    public void WriteArray_StringItems_WritesListSyntax()
    {
        var writer = new MarkoutWriter();
        writer.WriteArray("Frameworks", new[] { "netstandard2.0", "net6.0", "net8.0" });

        var expected = """
            Frameworks:
            - netstandard2.0
            - net6.0
            - net8.0

            """;
        Assert.Equal(expected, writer.ToString());
    }

    [Fact]
    public void WriteArray_EmptyArray_WritesKeyOnly()
    {
        var writer = new MarkoutWriter();
        writer.WriteArray("Frameworks", Array.Empty<string>());

        Assert.Equal("Frameworks:\n", writer.ToString());
    }

    [Fact]
    public void WriteTable_WritesMarkdownTable()
    {
        var writer = new MarkoutWriter();
        writer.WriteTableStart("File", "Arch", "Signed");
        writer.WriteTableRow("Foo.dll", "AnyCPU", "yes");
        writer.WriteTableRow("Bar.dll", "x64", "no");
        writer.WriteTableEnd();

        var expected = """
            | File | Arch | Signed |
            | ---- | ---- | ------ |
            | Foo.dll | AnyCPU | yes |
            | Bar.dll | x64 | no |

            """;
        Assert.Equal(expected, writer.ToString());
    }

    [Fact]
    public void WriteSimplePair_WritesTwoColumnData()
    {
        var writer = new MarkoutWriter();
        writer.WriteSimplePair("Microsoft.CSharp", "4.7.0", 32);
        writer.WriteSimplePair("System.Memory", "4.5.5", 32);

        var expected = """
            Microsoft.CSharp                4.7.0
            System.Memory                   4.5.5

            """;
        Assert.Equal(expected, writer.ToString());
    }

    [Fact]
    public void MultipleElements_AddsBlankLinesBetweenSections()
    {
        var writer = new MarkoutWriter();
        writer.WriteHeading(1, "Package");
        writer.WriteField("Name", "Test");
        writer.WriteField("Version", "1.0.0");
        writer.WriteHeading(2, "Dependencies");
        writer.WriteField("Count", 5);

        var expected = """
            # Package

            Name: Test  
            Version: 1.0.0  

            ## Dependencies

            Count: 5  

            """;
        Assert.Equal(expected, writer.ToString());
    }

    [Fact]
    public void WriteDateTime_WritesIso8601Format()
    {
        var writer = new MarkoutWriter();
        var date = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        writer.WriteField("Published", date);

        Assert.StartsWith("Published: 2024-01-15T10:30:00", writer.ToString());
    }

    [Fact]
    public void IncludeSections_FiltersToSpecifiedSections()
    {
        var writer = new MarkoutWriter(new MarkoutWriterOptions { IncludeSections = new HashSet<int> { 1 } });

        writer.WriteHeading(1, "Title");
        writer.WriteParagraph("Intro");
        writer.WriteHeading(2, "First");    // Section 1 - included
        writer.WriteField("A", "1");
        writer.WriteHeading(2, "Second");   // Section 2 - excluded
        writer.WriteField("B", "2");
        writer.WriteHeading(2, "Third");    // Section 3 - excluded
        writer.WriteField("C", "3");

        var expected = """
            # Title

            Intro

            ## First

            A: 1  

            """;
        Assert.Equal(expected, writer.ToString());
    }

    [Fact]
    public void ExcludeSections_SkipsSpecifiedSections()
    {
        var writer = new MarkoutWriter(new MarkoutWriterOptions { ExcludeSections = new HashSet<int> { 2 } });

        writer.WriteHeading(1, "Title");
        writer.WriteHeading(2, "First");    // Section 1 - included
        writer.WriteField("A", "1");
        writer.WriteHeading(2, "Second");   // Section 2 - excluded
        writer.WriteField("B", "2");
        writer.WriteHeading(2, "Third");    // Section 3 - included
        writer.WriteField("C", "3");

        var expected = """
            # Title

            ## First

            A: 1  

            ## Third

            C: 3  

            """;
        Assert.Equal(expected, writer.ToString());
    }

    [Fact]
    public void SectionFiltering_ContentBeforeFirstH2_AlwaysIncluded()
    {
        var writer = new MarkoutWriter(new MarkoutWriterOptions { IncludeSections = new HashSet<int> { 2 } });

        writer.WriteHeading(1, "Title");
        writer.WriteParagraph("This is before any H2");
        writer.WriteHeading(2, "First");    // Section 1 - excluded
        writer.WriteField("A", "1");
        writer.WriteHeading(2, "Second");   // Section 2 - included
        writer.WriteField("B", "2");

        var expected = """
            # Title

            This is before any H2

            ## Second

            B: 2  

            """;
        Assert.Equal(expected, writer.ToString());
    }

    [Fact]
    public void SectionFiltering_TableSpanningExcludedSection_NotWritten()
    {
        var writer = new MarkoutWriter(new MarkoutWriterOptions { ExcludeSections = new HashSet<int> { 1 } });

        writer.WriteHeading(1, "Title");
        writer.WriteHeading(2, "Data");     // Section 1 - excluded
        writer.WriteTableStart("Name", "Value");
        writer.WriteTableRow("Foo", "Bar");
        writer.WriteTableEnd();
        writer.WriteHeading(2, "Other");    // Section 2 - included
        writer.WriteField("X", "Y");

        var expected = """
            # Title

            ## Other

            X: Y  

            """;
        Assert.Equal(expected, writer.ToString());
    }

    [Fact]
    public void WriteCompactFields_SingleField_WritesSinglePair()
    {
        var writer = new MarkoutWriter();
        writer.WriteCompactFields(new MarkoutField("Type", "Library"));

        Assert.Equal("Type: Library\n", writer.ToString());
    }

    [Fact]
    public void WriteCompactFields_MultipleFields_WritesPipeSeparated()
    {
        var writer = new MarkoutWriter();
        writer.WriteCompactFields(
            new MarkoutField("Type", "Library"),
            new MarkoutField("TFM", "net8.0"),
            new MarkoutField("Updated", "2026-01-15"));

        Assert.Equal("Type: Library | TFM: net8.0 | Updated: 2026-01-15\n", writer.ToString());
    }

    [Fact]
    public void WriteCompactFields_EmptyFields_WritesNothing()
    {
        var writer = new MarkoutWriter();
        writer.WriteCompactFields();

        Assert.Equal("", writer.ToString());
    }

    [Fact]
    public void WriteCompactFields_NullValue_WritesEmptyString()
    {
        var writer = new MarkoutWriter();
        writer.WriteCompactFields(
            new MarkoutField("Status", null),
            new MarkoutField("Count", "5"));

        Assert.Equal("Status:  | Count: 5\n", writer.ToString());
    }

    [Fact]
    public void WriteCompactFields_AfterHeading_AddsBlankLine()
    {
        var writer = new MarkoutWriter();
        writer.WriteHeading(1, "Package 1.0.0");
        writer.WriteCompactFields(
            new MarkoutField("Type", "Library"),
            new MarkoutField("TFM", "net8.0"));

        var expected = """
            # Package 1.0.0

            Type: Library | TFM: net8.0

            """;
        Assert.Equal(expected, writer.ToString());
    }

    [Fact]
    public void MarkoutField_CreateWithBool_RendersYesNo()
    {
        var field = MarkoutField.Create("Active", true);
        Assert.Equal("Active", field.Key);
        Assert.Equal("yes", field.Value);

        var field2 = MarkoutField.Create("Active", false);
        Assert.Equal("no", field2.Value);
    }

    [Fact]
    public void MarkoutField_CreateWithInt_RendersNumber()
    {
        var field = MarkoutField.Create("Count", 42);
        Assert.Equal("Count", field.Key);
        Assert.Equal("42", field.Value);
    }

    [Fact]
    public void WriteTree_SimpleNodes_RendersTreeStructure()
    {
        var writer = new MarkoutWriter();
        var nodes = new List<TreeNode>
        {
            new("Root", new List<TreeNode>
            {
                new("Child1"),
                new("Child2")
            })
        };
        writer.WriteTree(nodes);

        var output = writer.ToString();
        Assert.Contains("└─ Root", output);
        Assert.Contains("├─ Child1", output);
        Assert.Contains("└─ Child2", output);
    }

    [Fact]
    public void WriteTree_WithIcons_RendersIconBeforeLabel()
    {
        var writer = new MarkoutWriter();
        var nodes = new List<TreeNode>
        {
            new("local.dll", "📁"),
            new("platform.dll", "🚢"),
            new("unknown.dll", "❓")
        };
        writer.WriteTree(nodes);

        var output = writer.ToString();
        Assert.Contains("📁 local.dll", output);
        Assert.Contains("🚢 platform.dll", output);
        Assert.Contains("❓ unknown.dll", output);
    }

    [Fact]
    public void WriteTree_WithNestedIcons_RendersAllIcons()
    {
        var writer = new MarkoutWriter();
        var nodes = new List<TreeNode>
        {
            new("lib", "📁", new List<TreeNode>
            {
                new("net8.0", "📂", new[] { "MyLib.dll" })
            })
        };
        writer.WriteTree(nodes);

        var output = writer.ToString();
        Assert.Contains("📁 lib", output);
        Assert.Contains("📂 net8.0", output);
        Assert.Contains("MyLib.dll", output);
        // Child without icon should not have icon prefix
        Assert.Contains("└─ MyLib.dll", output);
    }

    [Fact]
    public void WriteTree_WithIncludeIconsFalse_OmitsIcons()
    {
        var options = new MarkoutWriterOptions { IncludeIcons = false };
        var writer = new MarkoutWriter(options);
        var nodes = new List<TreeNode>
        {
            new("local.dll", "📁"),
            new("platform.dll", "🚢")
        };
        writer.WriteTree(nodes);

        var output = writer.ToString();
        // Icons should not appear
        Assert.DoesNotContain("📁", output);
        Assert.DoesNotContain("🚢", output);
        // Labels should still appear
        Assert.Contains("local.dll", output);
        Assert.Contains("platform.dll", output);
    }

    [Fact]
    public void WriteFieldNoBreak_Double_WritesNumber()
    {
        var writer = new MarkoutWriter();
        writer.WriteFieldNoBreak("Score", 9.5);

        Assert.Equal("Score: 9.5\n", writer.ToString());
    }

    [Fact]
    public void WriteFieldNoBreak_DateTime_WritesIso8601()
    {
        var writer = new MarkoutWriter();
        var date = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        writer.WriteFieldNoBreak("Created", date);

        Assert.StartsWith("Created: 2024-06-15T12:00:00", writer.ToString());
    }

    [Fact]
    public void WriteFieldNoBreak_DateTimeOffset_WritesIso8601()
    {
        var writer = new MarkoutWriter();
        var date = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
        writer.WriteFieldNoBreak("Modified", date);

        Assert.StartsWith("Modified: 2024-06-15T12:00:00", writer.ToString());
    }

    [Fact]
    public void MarkoutWriterOptions_Default_HasExpectedValues()
    {
        var options = new MarkoutWriterOptions();

        Assert.True(options.IncludeIcons);
        Assert.True(options.IncludeDescription);
        Assert.False(options.BoldFieldNames);
        Assert.Null(options.IncludeSections);
        Assert.Null(options.ExcludeSections);
    }

    [Fact]
    public void MarkoutWriterOptions_Default_IsReadOnly()
    {
        Assert.True(MarkoutWriterOptions.Default.IsReadOnly);
        Assert.Throws<InvalidOperationException>(() => MarkoutWriterOptions.Default.BoldFieldNames = true);
    }

    [Fact]
    public void MarkoutWriterOptions_MakeReadOnly_PreventsModification()
    {
        var options = new MarkoutWriterOptions { BoldFieldNames = true };
        options.MakeReadOnly();

        Assert.Throws<InvalidOperationException>(() => options.BoldFieldNames = false);
        Assert.Throws<InvalidOperationException>(() => options.IncludeIcons = false);
        Assert.Throws<InvalidOperationException>(() => options.IncludeDescription = false);
        Assert.Throws<InvalidOperationException>(() => options.IncludeSections = new HashSet<int> { 1 });
        Assert.Throws<InvalidOperationException>(() => options.ExcludeSections = new HashSet<int> { 2 });
    }
}
