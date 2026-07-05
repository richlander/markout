using Markout;

namespace Markout.Samples.Serialization;

/// <summary>
/// Demonstrates every Markout shape using MarkoutWriter directly.
/// This is the writer-level companion to source-generated serialization.
/// </summary>
public static class ShapeGallery
{
    /// <summary>
    /// Renders the built-in data relationships through the writer API.
    /// </summary>
    public static void WriteAllShapes()
    {
        var writer = new MarkoutWriter(new MarkdownFormatter());

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
                ["MarkdownFormatter", "Markdown", "Documentation, GitHub issues"],
                ["SpectreFormatter", "ANSI terminal", "CLI tools with color"],
                ["MarkoutWriter", "Plain text", "Log files, piping"],
                ["TableFormatter", "Pretty/TSV table", "Compact summaries and shell-friendly rows"],
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
                new Slice("C#", 85),
                new Slice("MSBuild", 10),
                new Slice("JSON", 5),
            ]),
        ]);

        // Composite cells — dense in Markdown, decomposed into typed columns in TSV/JSONL
        writer.WriteHeading(2, "Quality Card");
        writer.WriteCompositeTable(
            new("tasks correct", new Change<Fraction>(new Fraction(24, 24), new Fraction(24, 24))),
            new("tool calls: web / bash / other", new Change<Segments>(
                new Segments(new Segment("web", 21), new Segment("bash", 171), new Segment("other", 236)),
                new Segments(new Segment("web", 0), new Segment("bash", 75), new Segment("other", 183)))),
            new("output tok (% of IET)", new Change<Share>(new Share(5056, 21067), new Share(3129, 13037))),
            new("Session IET", new Change<long>(98555, 61190), new MarkoutCellFormat(Delta.Percent)));

        // Hierarchy — tree
        writer.WriteHeading(2, "Project Structure");
        writer.WriteTree(
            new TreeNode("src", [
                new TreeNode("Markout", [new TreeNode("MarkoutWriter.cs"), new TreeNode("MarkoutShape.cs")]),
                new TreeNode("Markout.SourceGeneration", [new TreeNode("Parser"), new TreeNode("Emitter")]),
                new TreeNode("Markout.Ansi.Spectre", [new TreeNode("SpectreFormatter.cs")])]),
            new TreeNode("tests", [
                new TreeNode("Markout.Tests")]));

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
