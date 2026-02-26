using Markout;

namespace Markout.Samples.Serialization;

/// <summary>
/// Demonstrates every Markout shape using MarkoutWriter directly.
/// This is the writer-level companion to source-generated serialization.
/// </summary>
public static class ShapeGallery
{
    /// <summary>
    /// Renders all ten data relationships through the writer API.
    /// </summary>
    public static void WriteAllShapes()
    {
        var writer = new MarkdownWriter();

        // Identity — heading
        writer.WriteHeading(1, "Shape Gallery");

        // Paragraph
        writer.WriteParagraph("Markout projects objects into documents. " +
            "Each shape represents a data relationship, not a visual form.");

        // Attention — callout
        writer.WriteCallout(CalloutSeverity.Note, "This gallery shows every built-in shape.");

        // Fields (identity continued)
        writer.WriteHeading(2, "Fields");
        writer.WriteFields(
            new("Name", "Markout"),
            new("Version", "0.6.0"),
            new("License", "MIT"));

        // Field list
        writer.WriteHeading(2, "Field List");
        writer.WriteFieldsInline(
            new("Language", "C#"),
            new("Runtime", ".NET 10"),
            new("Source Gen", "yes"));

        // Enumeration — list
        writer.WriteHeading(2, "Features");
        writer.WriteArray("Capabilities", ["Source generation", "Multiple renderers", "Shape taxonomy"]);

        // Tabulation — table
        writer.WriteHeading(2, "Renderers");
        writer.WriteTable(
            ["Renderer", "Output", "Use Case"],
            [
                ["MarkdownWriter", "Markdown", "Documentation, GitHub issues"],
                ["AnsiWriter", "ANSI terminal", "CLI tools with color"],
                ["MarkoutWriter", "Plain text", "Log files, piping"],
                ["OneLineWriter", "Columnar", "Compact summaries"],
            ]);

        // Description
        writer.WriteHeading(2, "Shape Descriptions");
        writer.WriteDescriptions([
            new Description("Identity", "Names an object — becomes a heading."),
            new Description("Enumeration", "Ordered or unordered items — becomes a list."),
            new Description("Tabulation", "Rows with uniform columns — becomes a table."),
        ]);

        // Measurement — metrics
        writer.WriteHeading(2, "Test Results");
        writer.WriteMetrics([
            new Metric("Unit Tests", 342),
            new Metric("Integration", 48),
            new Metric("Performance", 12),
        ]);

        // Composition — breakdown
        writer.WriteHeading(2, "Code Breakdown");
        writer.WriteBreakdown([
            new Breakdown("By Language", [
                new Segment("C#", 85),
                new Segment("MSBuild", 10),
                new Segment("JSON", 5),
            ]),
        ]);

        // Hierarchy — tree
        writer.WriteHeading(2, "Project Structure");
        writer.WriteTree(
            new TreeNode("src", null,
                new TreeNode("Markout", null, new TreeNode("MarkoutWriter.cs"), new TreeNode("MarkoutShape.cs")),
                new TreeNode("Markout.SourceGeneration", null, new TreeNode("Parser"), new TreeNode("Emitter")),
                new TreeNode("Markout.Ansi", null, new TreeNode("AnsiWriter.cs"))),
            new TreeNode("tests", null,
                new TreeNode("Markout.Tests")));

        // Quotation — code section
        writer.WriteHeading(2, "Quick Start");
        writer.WriteCodeStart("csharp");
        writer.WriteParagraph("var md = MarkoutSerializer.Serialize(obj, MyContext.Default);");
        writer.WriteCodeEnd();

        // Quotation — blockquote
        writer.WriteHeading(2, "Philosophy");
        writer.WriteQuotation("Markup adds instructions to content.\nMarkout removes structure from data.");

        // Separator
        writer.WriteRule();

        Console.WriteLine(writer.ToString());
    }
}
