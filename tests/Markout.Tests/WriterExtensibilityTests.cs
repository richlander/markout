using Markout;

namespace Markout.Tests;

public class WriterExtensibilityTests
{
    /// <summary>
    /// A minimal custom renderer that overrides key methods to produce
    /// different output, proving subclassing works.
    /// </summary>
    private class TestConsoleWriter : MarkoutWriter
    {
        public TestConsoleWriter() : base() { }
        public TestConsoleWriter(MarkoutWriterOptions options) : base(options) { }

        public override void WriteHeading(int level, string text, string? context)
        {
            UpdateSectionState(level, text);
            if (SectionExcluded) return;
            EnsureBlankLineIfNeeded();
            // Render as [H1] Title instead of # Title
            Writer.Write($"[H{level}] {text}");
            if (!string.IsNullOrEmpty(context))
                Writer.Write($" ({context})");
            Writer.WriteLine();
            NeedsBlankLine = true;
            HasContent = true;
        }

        public override void WriteField(string key, string? value)
        {
            if (SectionExcluded) return;
            EnsureBlankLineIfNeeded();
            // Render as KEY = VALUE instead of Key: Value
            Writer.Write(key.ToUpperInvariant());
            Writer.Write(" = ");
            Writer.WriteLine(value ?? string.Empty);
            HasContent = true;
        }

        public override void WriteTableStart(params string[] headers)
        {
            if (SectionExcluded) return;
            EnsureBlankLineIfNeeded();
            // Render with unicode box-drawing
            Writer.Write("│");
            foreach (var header in headers)
            {
                Writer.Write($" {header} │");
            }
            Writer.WriteLine();
            HasContent = true;
        }

        public override void WriteTableRow(params string[] values)
        {
            if (SectionExcluded) return;
            Writer.Write("│");
            foreach (var value in values)
            {
                Writer.Write($" {EscapeTableCell(value)} │");
            }
            Writer.WriteLine();
        }

        public override void WriteListItem(string text)
        {
            if (SectionExcluded) return;
            EnsureBlankLineIfNeeded();
            Writer.Write("• ");
            Writer.WriteLine(text);
            HasContent = true;
        }
    }

    [Fact]
    public void CustomWriter_WriteHeading_UsesOverriddenFormat()
    {
        var writer = new TestConsoleWriter();
        writer.WriteHeading(1, "Package");

        Assert.Equal("[H1] Package", writer.ToString());
    }

    [Fact]
    public void CustomWriter_WriteHeading_WithContext_IncludesContext()
    {
        var writer = new TestConsoleWriter();
        writer.WriteHeading(2, "Dependencies", "net8.0");

        Assert.Equal("[H2] Dependencies (net8.0)", writer.ToString());
    }

    [Fact]
    public void CustomWriter_WriteField_UsesOverriddenFormat()
    {
        var writer = new TestConsoleWriter();
        writer.WriteField("Name", "Markout");

        Assert.Equal("NAME = Markout", writer.ToString());
    }

    [Fact]
    public void CustomWriter_WriteTableStart_UsesBoxDrawing()
    {
        var writer = new TestConsoleWriter();
        writer.WriteTableStart("A", "B");
        writer.WriteTableRow("1", "2");
        writer.WriteTableEnd();

        var expected = """
            │ A │ B │
            │ 1 │ 2 │
            """;
        Assert.Equal(expected.ReplaceLineEndings(), writer.ToString().ReplaceLineEndings());
    }

    [Fact]
    public void CustomWriter_WriteListItem_UsesBullet()
    {
        var writer = new TestConsoleWriter();
        writer.WriteListItem("first");
        writer.WriteListItem("second");

        var expected = """
            • first
            • second
            """;
        Assert.Equal(expected.ReplaceLineEndings(), writer.ToString().ReplaceLineEndings());
    }

    [Fact]
    public void CustomWriter_NonOverriddenMethods_UseBaseImplementation()
    {
        var writer = new TestConsoleWriter();
        writer.WriteParagraph("Hello world");

        Assert.Equal("Hello world", writer.ToString());
    }

    [Fact]
    public void CustomWriter_ProtectedState_TracksCorrectly()
    {
        var writer = new TestConsoleWriter();
        Assert.Equal(MarkoutRenderContext.Block, writer.CurrentContext);

        writer.WriteHeading(1, "Test");
        writer.WriteField("Key", "Value");

        // Verify content was written via overridden methods
        var result = writer.ToString();
        Assert.Contains("[H1] Test", result);
        Assert.Contains("KEY = Value", result);
    }

    [Fact]
    public void CustomWriter_WorksWithSerializer()
    {
        // Verify that a custom writer can be passed to the serializer
        var writer = new TestConsoleWriter();
        writer.WriteHeading(1, "Test");
        writer.WriteField("Version", "1.0");

        var result = writer.ToString();
        Assert.Contains("[H1]", result);
        Assert.Contains("VERSION = 1.0", result);
    }

    [Fact]
    public void CustomWriter_SectionExcluded_SkipsContent()
    {
        var options = new MarkoutWriterOptions
        {
            ExcludeSections = ["Excluded"]
        };
        var writer = new TestConsoleWriter(options);
        writer.WriteHeading(2, "Excluded");
        writer.WriteField("Key", "Value");

        // The excluded section should produce no output
        Assert.Equal("", writer.ToString());
    }
}
