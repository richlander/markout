using Markout;
using Markout.Ansi;
using Microsoft.Extensions.Terminal;

namespace Markout.Tests;

public class AnsiWriterTests
{
    /// <summary>
    /// Simple ITerminal that captures ANSI output including escape codes.
    /// </summary>
    private class CapturingTerminal : ITerminal
    {
        private readonly System.Text.StringBuilder _sb = new();

        public int Width => 80;
        public int Height => 24;
        public string Output => _sb.ToString();

        public void Append(char value) => _sb.Append(value);
        public void Append(string value) => _sb.Append(value);
        public void AppendLine() => _sb.AppendLine();
        public void AppendLine(string value) => _sb.AppendLine(value);
        public void AppendLink(string path, int? lineNumber) => _sb.Append(path);

        public void SetColor(TerminalColor color) =>
            _sb.Append($"\x1b[{(int)color}m");

        public void ResetColor() =>
            _sb.Append("\x1b[m");

        public void ShowCursor() { }
        public void HideCursor() { }
        public void StartUpdate() { }
        public void StopUpdate() { }
        public void StartBusyIndicator() { }
        public void StopBusyIndicator() { }
    }

    private static (AnsiWriter writer, CapturingTerminal terminal) Create()
    {
        var terminal = new CapturingTerminal();
        var writer = new AnsiWriter(terminal);
        return (writer, terminal);
    }

    private static (AnsiWriter writer, CapturingTerminal terminal) Create(MarkoutWriterOptions options)
    {
        var terminal = new CapturingTerminal();
        var writer = new AnsiWriter(terminal, options);
        return (writer, terminal);
    }

    // ── Headings ──

    [Fact]
    public void WriteHeading_H1_RendersRule()
    {
        var (writer, terminal) = Create();
        writer.WriteHeading(1, "Package");

        var output = terminal.Output;
        Assert.Contains("Package", output);
        Assert.Contains("─", output); // Rule characters
    }

    [Fact]
    public void WriteHeading_H2_RendersBoldCyan()
    {
        var (writer, terminal) = Create();
        writer.WriteHeading(2, "Dependencies");

        var output = terminal.Output;
        Assert.Contains("Dependencies", output);
        Assert.Contains($"\x1b[{(int)TerminalColor.Cyan}m", output); // Cyan color
        Assert.Contains(AnsiCodes.SetBold, output); // Bold
    }

    [Fact]
    public void WriteHeading_WithContext_IncludesContext()
    {
        var (writer, terminal) = Create();
        writer.WriteHeading(2, "Dependencies", "net8.0");

        var output = terminal.Output;
        Assert.Contains("Dependencies (net8.0)", output);
    }

    // ── Fields ──

    [Fact]
    public void WriteFieldBareList_String_RendersBoldKey()
    {
        var (writer, terminal) = Create();
        writer.WriteFieldBareList([new("Name", "Markout")]);

        var output = terminal.Output;
        Assert.Contains(AnsiCodes.SetBold, output); // Bold key
        Assert.Contains("Name", output);
        Assert.Contains("Markout", output);
    }

    [Fact]
    public void WriteFieldBareList_MultipleFields_RendersBoldKeys()
    {
        var (writer, terminal) = Create();
        writer.WriteFieldBareList(
            new MarkoutField("Signed", "yes"),
            new MarkoutField("Count", "42"));

        var output = terminal.Output;
        Assert.Contains("Signed", output);
        Assert.Contains("yes", output);
        Assert.Contains("Count", output);
        Assert.Contains("42", output);
    }

    // ── Tables ──

    [Fact]
    public void WriteTable_Batch_RendersSpacePaddedColumns()
    {
        var (writer, terminal) = Create();
        writer.WriteTable(
            ["Name", "Version"],
            [["Markout", "0.5.1"], ["xUnit", "3.2.2"]]);

        var output = terminal.Output;
        Assert.Contains("NAME", output); // Uppercase headers
        Assert.Contains("VERSION", output);
        Assert.Contains("Markout", output);
        Assert.Contains("0.5.1", output);
        Assert.Contains("─", output); // Separator
    }

    [Fact]
    public void WriteTableStart_Stream_RendersUppercaseHeaders()
    {
        var (writer, terminal) = Create();
        writer.WriteTableStart("File", "Arch");
        writer.WriteTableRow("Foo.dll", "x64");
        writer.WriteTableEnd();

        var output = terminal.Output;
        Assert.Contains("FILE", output);
        Assert.Contains("ARCH", output);
        Assert.Contains("Foo.dll", output);
    }

    // ── Trees ──

    [Fact]
    public void WriteTree_RendersWithDimBoxDrawing()
    {
        var (writer, terminal) = Create();
        writer.WriteTree(new TreeNode("Root", null, new TreeNode("Child1"), new TreeNode("Child2")));

        var output = terminal.Output;
        Assert.Contains("Root", output);
        Assert.Contains("Child1", output);
        Assert.Contains("└─", output);
        Assert.Contains("├─", output);
        Assert.Contains($"\x1b[{(int)TerminalColor.DarkGray}m", output); // Dim box drawing
    }

    [Fact]
    public void WriteTree_WithIcons_RendersIcons()
    {
        var (writer, terminal) = Create();
        writer.WriteTree(new TreeNode("lib", "📁", new TreeNode("net8.0")));

        var output = terminal.Output;
        Assert.Contains("📁", output);
        Assert.Contains("lib", output);
    }

    // ── Lists ──

    [Fact]
    public void WriteListItem_RendersBullet()
    {
        var (writer, terminal) = Create();
        writer.WriteListItem("first item");

        var output = terminal.Output;
        Assert.Contains("•", output);
        Assert.Contains("first item", output);
    }

    [Fact]
    public void WriteArray_RendersBoldLabel()
    {
        var (writer, terminal) = Create();
        writer.WriteArray("Frameworks", new[] { "net8.0", "net10.0" });

        var output = terminal.Output;
        Assert.Contains("Frameworks", output);
        Assert.Contains(AnsiCodes.SetBold, output);
        Assert.Contains("net8.0", output);
        Assert.Contains("net10.0", output);
    }

    // ── Field list ──

    [Fact]
    public void WriteFieldLine_RendersBoldKeysWithSeparator()
    {
        var (writer, terminal) = Create();
        writer.WriteFieldLine(
            new MarkoutField("Type", "Library"),
            new MarkoutField("TFM", "net8.0"));

        var output = terminal.Output;
        Assert.Contains("Type", output);
        Assert.Contains("Library", output);
        Assert.Contains("│", output); // Box-drawing separator
        Assert.Contains("TFM", output);
    }

    // ── Code ──

    [Fact]
    public void WriteCode_RendersDimText()
    {
        var (writer, terminal) = Create();
        writer.WriteCodeStart("csharp");
        writer.WriteParagraph("var x = 1;");
        writer.WriteCodeEnd();

        var output = terminal.Output;
        Assert.Contains("var x = 1;", output);
        Assert.Contains($"\x1b[{(int)TerminalColor.DarkGray}m", output); // Dim
    }

    // ── Section filtering ──

    [Fact]
    public void SectionExcluded_SuppressesOutput()
    {
        var (writer, terminal) = Create(new MarkoutWriterOptions { ExcludeSections = ["Hidden"] });
        writer.WriteHeading(2, "Hidden");
        writer.WriteFieldBareList([new("Key", "Value")]);

        Assert.DoesNotContain("Value", terminal.Output);
    }

    // ── Source-gen compatibility ──

    [Fact]
    public void AnsiWriter_AcceptedAsMarkoutWriter()
    {
        // Verify polymorphism works — AnsiWriter can be used where MarkoutWriter is expected
        var terminal = new CapturingTerminal();
        MarkoutWriter writer = new AnsiWriter(terminal);
        writer.WriteHeading(1, "Test");
        writer.WriteFieldBareList([new("Key", "Value")]);

        var output = terminal.Output;
        Assert.Contains("Test", output);
        Assert.Contains("Key", output);
        Assert.Contains("Value", output);
    }
}
