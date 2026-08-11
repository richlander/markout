using Markout;

namespace Markout.Samples.Serialization;

/// <summary>
/// Demonstrates low-level MarkoutWriter usage for custom formatting.
/// </summary>
public static class WriterUsage
{
    /// <summary>
    /// Shows how to use MarkoutWriter directly for fine-grained control.
    /// </summary>
    public static void UseMarkoutWriter()
    {
        #region UseMarkoutWriter
        var writer = new MarkoutWriter(new MarkdownFormatter());

        writer.WriteHeading(1, "Product Report");

        writer.WriteFields(
            new("Product", "Widget Pro"),
            new("Price", "99.99"),
            new("In Stock", "yes"));

        writer.WriteArray("Features", new[] { "Durable", "Lightweight", "Waterproof" });

        Console.WriteLine(writer.Complete());
        // # Product Report
        //
        // Product: Widget Pro  
        // Price: 99.99  
        // In Stock: yes  
        //
        // Features:
        // - Durable
        // - Lightweight
        // - Waterproof
        #endregion
    }

    /// <summary>
    /// Shows how to create table output with MarkoutWriter.
    /// </summary>
    public static void WriteTable()
    {
        #region WriteTable
        var writer = new MarkoutWriter(new MarkdownFormatter());

        writer.WriteHeading(1, "Inventory");

        writer.WriteTableStart("Product", "Category", "Price", "Stock");

        writer.WriteTableRow("Widget A", "Electronics", "$29.99", "Yes");
        writer.WriteTableRow("Widget B", "Electronics", "$49.99", "No");
        writer.WriteTableRow("Gadget X", "Tools", "$19.99", "Yes");

        writer.WriteTableEnd();

        Console.WriteLine(writer.Complete());
        // # Inventory
        //
        // | Product | Category | Price | Stock |
        // |---------|----------|-------|-------|
        // | Widget A | Electronics | $29.99 | Yes |
        // | Widget B | Electronics | $49.99 | No |
        // | Gadget X | Tools | $19.99 | Yes |
        #endregion
    }

    /// <summary>
    /// Shows how to render hierarchical data as a tree.
    /// </summary>
    public static void WriteTree()
    {
        #region WriteTree
        var writer = new MarkoutWriter(new MarkdownFormatter());

        writer.WriteHeading(1, "Organization");

        writer.WriteTree(
            new TreeNode("CEO", [
                new TreeNode("VP Engineering", [
                    new TreeNode("Dev Team Lead"),
                    new TreeNode("QA Team Lead")]),
                new TreeNode("VP Sales", [
                    new TreeNode("Account Manager"),
                    new TreeNode("Sales Rep")])]));

        Console.WriteLine(writer.Complete());
        // # Organization
        //
        // └─ CEO
        //    ├─ VP Engineering
        //    │  ├─ Dev Team Lead
        //    │  └─ QA Team Lead
        //    └─ VP Sales
        //       ├─ Account Manager
        //       └─ Sales Rep
        #endregion
    }
}
