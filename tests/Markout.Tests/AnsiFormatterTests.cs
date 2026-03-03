using Markout;
using Markout.Ansi;
using Microsoft.Extensions.Terminal;

namespace Markout.Tests;

public class AnsiFormatterTests
{
    /// <summary>
    /// Simple ITerminal required by AnsiFormatter constructor.
    /// </summary>
    private class CapturingTerminal : ITerminal
    {
        public int Width => 80;
        public int Height => 24;

        public void Append(char value) { }
        public void Append(string value) { }
        public void AppendLine() { }
        public void AppendLine(string value) { }
        public void AppendLink(string path, int? lineNumber) { }
        public void SetColor(TerminalColor color) { }
        public void ResetColor() { }
        public void ShowCursor() { }
        public void HideCursor() { }
        public void StartUpdate() { }
        public void StopUpdate() { }
        public void StartBusyIndicator() { }
        public void StopBusyIndicator() { }
    }

    private const string SetBold = "\x1b[1m";

    private static (MarkoutWriter orch, StringWriter output) Create()
    {
        var sw = new StringWriter();
        var terminal = new CapturingTerminal();
        var orch = MarkoutWriter.Create(sw, new AnsiFormatter(terminal));
        return (orch, sw);
    }

    private static (MarkoutWriter orch, StringWriter output) Create(MarkoutWriterOptions options)
    {
        var sw = new StringWriter();
        var terminal = new CapturingTerminal();
        var orch = MarkoutWriter.Create(sw, new AnsiFormatter(terminal), options);
        return (orch, sw);
    }

    // ── Headings ──

    [Fact]
    public void WriteHeading_H1_RendersRule()
    {
        var (orch, sw) = Create();
        orch.WriteHeading(1, "Package");

        var output = sw.ToString();
        Assert.Contains("Package", output);
        Assert.Contains("─", output); // Rule characters
    }

    [Fact]
    public void WriteHeading_H2_RendersBoldCyan()
    {
        var (orch, sw) = Create();
        orch.WriteHeading(2, "Dependencies");

        var output = sw.ToString();
        Assert.Contains("Dependencies", output);
        Assert.Contains("\x1b[36m", output); // Cyan color (SGR 36)
        Assert.Contains(SetBold, output); // Bold
    }

    [Fact]
    public void WriteHeading_WithContext_IncludesContext()
    {
        var (orch, sw) = Create();
        orch.WriteHeading(2, "Dependencies", "net8.0");

        var output = sw.ToString();
        Assert.Contains("Dependencies (net8.0)", output);
    }

    // ── Fields ──

    [Fact]
    public void WriteFields_String_RendersBoldKey()
    {
        var (orch, sw) = Create();
        orch.WriteFields([new("Name", "Markout")]);

        var output = sw.ToString();
        Assert.Contains(SetBold, output); // Bold key
        Assert.Contains("Name", output);
        Assert.Contains("Markout", output);
    }

    [Fact]
    public void WriteFields_MultipleFields_RendersBoldKeys()
    {
        var (orch, sw) = Create();
        orch.WriteFields(
            new MarkoutField("Signed", "yes"),
            new MarkoutField("Count", "42"));

        var output = sw.ToString();
        Assert.Contains("Signed", output);
        Assert.Contains("yes", output);
        Assert.Contains("Count", output);
        Assert.Contains("42", output);
    }

    // ── Tables ──

    [Fact]
    public void WriteTable_Batch_RendersSpacePaddedColumns()
    {
        var (orch, sw) = Create();
        orch.WriteTable(
            ["Name", "Version"],
            [["Markout", "0.5.1"], ["xUnit", "3.2.2"]]);

        var output = sw.ToString();
        Assert.Contains("NAME", output); // Uppercase headers
        Assert.Contains("VERSION", output);
        Assert.Contains("Markout", output);
        Assert.Contains("0.5.1", output);
        Assert.Contains("─", output); // Separator
    }

    [Fact]
    public void WriteTableStart_Stream_RendersUppercaseHeaders()
    {
        var (orch, sw) = Create();
        orch.WriteTableStart("File", "Arch");
        orch.WriteTableRow("Foo.dll", "x64");
        orch.WriteTableEnd();

        var output = sw.ToString();
        Assert.Contains("FILE", output);
        Assert.Contains("ARCH", output);
        Assert.Contains("Foo.dll", output);
    }

    // ── Trees ──

    [Fact]
    public void WriteTree_RendersBoxDrawing()
    {
        var (orch, sw) = Create();
        orch.WriteTree(new TreeNode("Root", null, new TreeNode("Child1"), new TreeNode("Child2")));

        var output = sw.ToString();
        Assert.Contains("Root", output);
        Assert.Contains("Child1", output);
        Assert.Contains("└─", output);
        Assert.Contains("├─", output);
    }

    [Fact]
    public void WriteTree_WithIcons_RendersIcons()
    {
        var (orch, sw) = Create();
        orch.WriteTree(new TreeNode("lib", "📁", new TreeNode("net8.0")));

        var output = sw.ToString();
        Assert.Contains("📁", output);
        Assert.Contains("lib", output);
    }

    // ── Lists ──

    [Fact]
    public void WriteListItem_RendersBullet()
    {
        var (orch, sw) = Create();
        orch.WriteListItem("first item");

        var output = sw.ToString();
        Assert.Contains("•", output);
        Assert.Contains("first item", output);
    }

    [Fact]
    public void WriteArray_RendersBoldLabel()
    {
        var (orch, sw) = Create(new MarkoutWriterOptions { BoldFieldNames = true });
        orch.WriteArray("Frameworks", "net8.0", "net10.0");

        var output = sw.ToString();
        Assert.Contains("Frameworks", output);
        Assert.Contains(SetBold, output);
        Assert.Contains("net8.0", output);
        Assert.Contains("net10.0", output);
    }

    // ── Field list ──

    [Fact]
    public void WriteFieldsInline_RendersBoldKeysWithSeparator()
    {
        var (orch, sw) = Create();
        orch.WriteFieldsInline(
            new MarkoutField("Type", "Library"),
            new MarkoutField("TFM", "net8.0"));

        var output = sw.ToString();
        Assert.Contains("Type", output);
        Assert.Contains("Library", output);
        Assert.Contains("|", output); // Pipe separator
        Assert.Contains("TFM", output);
    }

    // ── Code ──

    [Fact]
    public void WriteCode_RendersDimText()
    {
        var (orch, sw) = Create();
        orch.WriteCodeStart("csharp");
        orch.WriteParagraph("var x = 1;");
        orch.WriteCodeEnd();

        var output = sw.ToString();
        Assert.Contains("var x = 1;", output);
        Assert.Contains("\x1b[90m", output); // DarkGray (SGR 90)
    }

    // ── Section filtering ──

    [Fact]
    public void SectionExcluded_SuppressesOutput()
    {
        var (orch, sw) = Create(new MarkoutWriterOptions { IncludeSections = [] });
        orch.WriteHeading(2, "Hidden");
        orch.WriteFields([new("Key", "Value")]);

        Assert.DoesNotContain("Value", sw.ToString());
    }
}
