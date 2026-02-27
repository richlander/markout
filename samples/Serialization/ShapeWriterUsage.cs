using Markout;
using Markout.Formatting;

namespace Markout.Samples.Serialization;

/// <summary>
/// Demonstrates standalone shape writers for rendering individual shapes
/// without document-level state (blank lines, sections, projection).
/// Use shape writers when you need to render one shape directly — no
/// MarkoutWriter or serializer required.
/// </summary>
public static class ShapeWriterUsage
{
    /// <summary>
    /// Render a single table to stdout using TableWriter.
    /// </summary>
    public static void RenderTable()
    {
        #region RenderTable
        var formatter = new MarkdownFormatter();
        var table = new TableWriter(Console.Out, (ITableFormatter)formatter);

        table.WriteTable(
            ["Package", "Version", "License"],
            [
                ["Markout", "1.0.0", "MIT"],
                ["Markout.Templates", "1.0.0", "MIT"],
                ["Markout.Ansi", "1.0.0", "MIT"],
            ]);
        // | Package | Version | License |
        // |---------|---------|---------|
        // | Markout | 1.0.0 | MIT |
        // | Markout.Templates | 1.0.0 | MIT |
        // | Markout.Ansi | 1.0.0 | MIT |
        #endregion
    }

    /// <summary>
    /// Render a streaming table row by row.
    /// </summary>
    public static void RenderStreamingTable()
    {
        #region RenderStreamingTable
        var formatter = new MarkdownFormatter();
        var table = new TableWriter(Console.Out, (IStreamingTableFormatter)formatter);

        table.WriteTableStart("Name", "Status");
        table.WriteTableRow("api-server", "running");
        table.WriteTableRow("db-primary", "running");
        table.WriteTableRow("cache", "degraded");
        table.WriteTableEnd();
        #endregion
    }

    /// <summary>
    /// Render metrics bars to stdout using MetricsWriter.
    /// </summary>
    public static void RenderMetrics()
    {
        #region RenderMetrics
        var formatter = new UnicodeFormatter();
        var metrics = new MetricsWriter(Console.Out, formatter);

        metrics.WriteMetrics([
            new Metric("CPU", 73),
            new Metric("Memory", 4200),
            new Metric("Disk", 180),
        ]);
        #endregion
    }

    /// <summary>
    /// Render a tree to stdout using TreeWriter.
    /// </summary>
    public static void RenderTree()
    {
        #region RenderTree
        var tree = new TreeWriter(Console.Out);

        tree.WriteTree([
            new TreeNode("src", null,
                new TreeNode("Markout", null,
                    new TreeNode("MarkoutWriter.cs"),
                    new TreeNode("MarkdownFormatter.cs")),
                new TreeNode("Markout.Templates")),
            new TreeNode("tests"),
        ]);
        // └─ src
        //    ├─ Markout
        //    │  ├─ MarkoutWriter.cs
        //    │  └─ MarkdownFormatter.cs
        //    └─ Markout.Templates
        // └─ tests
        #endregion
    }
}
