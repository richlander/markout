using MarkdownData;
using Xunit;

namespace MarkdownData.Tests;

public class MdfWriterTests
{
    [Fact]
    public void WriteHeading_Level1_WritesCorrectMarkdown()
    {
        var writer = new MdfWriter();
        writer.WriteHeading(1, "Package");

        Assert.Equal("# Package\n", writer.ToString());
    }

    [Fact]
    public void WriteHeading_Level2_WritesCorrectMarkdown()
    {
        var writer = new MdfWriter();
        writer.WriteHeading(2, "Dependencies");

        Assert.Equal("## Dependencies\n", writer.ToString());
    }

    [Fact]
    public void WriteHeading_WithContext_WritesCorrectMarkdown()
    {
        var writer = new MdfWriter();
        writer.WriteHeading(2, "Dependencies", "net6.0");

        Assert.Equal("## Dependencies (net6.0)\n", writer.ToString());
    }

    [Fact]
    public void WriteField_String_WritesKeyValue()
    {
        var writer = new MdfWriter();
        writer.WriteField("Name", "Newtonsoft.Json");

        Assert.Equal("Name: Newtonsoft.Json\n", writer.ToString());
    }

    [Fact]
    public void WriteField_BooleanTrue_WritesYes()
    {
        var writer = new MdfWriter();
        writer.WriteField("Signed", true);

        Assert.Equal("Signed: yes\n", writer.ToString());
    }

    [Fact]
    public void WriteField_BooleanFalse_WritesNo()
    {
        var writer = new MdfWriter();
        writer.WriteField("Signed", false);

        Assert.Equal("Signed: no\n", writer.ToString());
    }

    [Fact]
    public void WriteField_Integer_WritesNumber()
    {
        var writer = new MdfWriter();
        writer.WriteField("Count", 42);

        Assert.Equal("Count: 42\n", writer.ToString());
    }

    [Fact]
    public void WriteField_Double_WritesNumber()
    {
        var writer = new MdfWriter();
        writer.WriteField("Version", 3.14);

        Assert.Equal("Version: 3.14\n", writer.ToString());
    }

    [Fact]
    public void WriteArray_StringItems_WritesListSyntax()
    {
        var writer = new MdfWriter();
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
        var writer = new MdfWriter();
        writer.WriteArray("Frameworks", Array.Empty<string>());

        Assert.Equal("Frameworks:\n", writer.ToString());
    }

    [Fact]
    public void WriteTable_WritesMarkdownTable()
    {
        var writer = new MdfWriter();
        writer.WriteTableStart("File", "Arch", "Signed");
        writer.WriteTableRow("Foo.dll", "AnyCPU", "yes");
        writer.WriteTableRow("Bar.dll", "x64", "no");
        writer.WriteTableEnd();

        var expected = """
            | File | Arch | Signed |
            |------|------|--------|
            | Foo.dll | AnyCPU | yes |
            | Bar.dll | x64 | no |

            """;
        Assert.Equal(expected, writer.ToString());
    }

    [Fact]
    public void WriteSimplePair_WritesTwoColumnData()
    {
        var writer = new MdfWriter();
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
        var writer = new MdfWriter();
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
        var writer = new MdfWriter();
        var date = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        writer.WriteField("Published", date);

        Assert.StartsWith("Published: 2024-01-15T10:30:00", writer.ToString());
    }
}
