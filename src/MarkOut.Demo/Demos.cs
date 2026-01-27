using MarkOut;

namespace MarkOut.Demo;

/// <summary>
/// Registry of available demos.
/// </summary>
public static class Demos
{
    private static readonly Dictionary<string, Action<TextWriter>> _demos = new(StringComparer.OrdinalIgnoreCase)
    {
        ["simple"] = Simple,
        ["sections"] = Sections,
        ["list"] = ListDemo,
        ["table"] = TableDemo,
        ["nested"] = Nested,
        ["pivot"] = Pivot,
        ["tree"] = Tree,
        ["schema"] = Schema,
    };

    private static readonly string[] _orderedNames = ["simple", "sections", "list", "table", "nested", "pivot", "tree", "schema"];

    public static IEnumerable<string> List() => _orderedNames;

    public static Action<TextWriter>? Get(string name) =>
        _demos.TryGetValue(name, out var demo) ? demo : null;

    /// <summary>
    /// Simple demo: Single shoe with basic scalar fields.
    /// </summary>
    private static void Simple(TextWriter output)
    {
        var view = DemoData.GetSimpleView("torin-7");
        MarkOutSerializer.Serialize(view, output, DemoContext.Default);
    }

    /// <summary>
    /// List demo: All shoes as bullet list.
    /// </summary>
    private static void ListDemo(TextWriter output)
    {
        var view = DemoData.GetBulletListView();
        MarkOutSerializer.Serialize(view, output, DemoContext.Default);
    }

    /// <summary>
    /// Table demo: All shoes as table rows.
    /// </summary>
    private static void TableDemo(TextWriter output)
    {
        var view = DemoData.GetTableView();
        MarkOutSerializer.Serialize(view, output, DemoContext.Default);
    }

    /// <summary>
    /// Nested demo: Shoes with detailed features and reviews.
    /// </summary>
    private static void Nested(TextWriter output)
    {
        var view = DemoData.GetDetailView();
        MarkOutSerializer.Serialize(view, output, DemoContext.Default);
    }

    /// <summary>
    /// Sections demo: Single shoe with specs and reviews sections.
    /// </summary>
    private static void Sections(TextWriter output)
    {
        var view = DemoData.GetSectionsView("lone-peak-8");
        MarkOutSerializer.Serialize(view, output, DemoContext.Default);
    }

    /// <summary>
    /// Pivot demo: Inventory pivoted by size and color.
    /// </summary>
    private static void Pivot(TextWriter output)
    {
        var view = DemoData.GetInventoryView("torin-7");
        MarkOutSerializer.Serialize(view, output, DemoContext.Default);
    }

    /// <summary>
    /// Schema demo: Shows how types map to MarkOut output.
    /// </summary>
    private static void Schema(TextWriter output)
    {
        output.WriteLine("# MarkOut Schema");
        output.WriteLine();
        
        // Shoe
        var shoeSchema = DemoContext.Default.GetSchemaInfo<Shoe>();
        if (shoeSchema != null)
        {
            output.WriteLine("## Shoe");
            output.WriteLine();
            output.WriteLine("```");
            output.Write(shoeSchema.ToTreeString());
            output.WriteLine("```");
            output.WriteLine();
        }
        
        // InventoryEntry
        var inventorySchema = DemoContext.Default.GetSchemaInfo<InventoryEntry>();
        if (inventorySchema != null)
        {
            output.WriteLine("## InventoryEntry");
            output.WriteLine();
            output.WriteLine("```");
            output.Write(inventorySchema.ToTreeString());
            output.WriteLine("```");
            output.WriteLine();
        }
        
        // Feature
        var featureSchema = DemoContext.Default.GetSchemaInfo<Feature>();
        if (featureSchema != null)
        {
            output.WriteLine("## Feature");
            output.WriteLine();
            output.WriteLine("```");
            output.Write(featureSchema.ToTreeString());
            output.WriteLine("```");
            output.WriteLine();
        }
        
        // Review
        var reviewSchema = DemoContext.Default.GetSchemaInfo<Review>();
        if (reviewSchema != null)
        {
            output.WriteLine("## Review");
            output.WriteLine();
            output.WriteLine("```");
            output.Write(reviewSchema.ToTreeString());
            output.WriteLine("```");
        }
    }

    /// <summary>
    /// Tree demo: Shows nested list data as a tree structure.
    /// </summary>
    private static void Tree(TextWriter output)
    {
        var shoes = DemoData.GetShoesForTree("torin-7", "escalante-4", "lone-peak-8");

        // Project to TreeNode structure
        var tree = shoes.Select(s => new TreeNode(
            $"{s.Model} ({s.Category}, ${s.Price})",
            s.Reviews?.Select(r => 
            {
                var comment = r.Comment.Length > 40 
                    ? r.Comment.Substring(0, 37) + "..." 
                    : r.Comment;
                return $"\"{comment}\" — {r.Author} ({r.Rating}★)";
            })
        ));

        var writer = new MarkOutWriter(output);
        
        writer.WriteHeading(1, "Altra Running Shoes");
        writer.WriteParagraph("This demo shows `List<List<T>>` rendered as a tree. Each shoe shows its reviews, which would be unsupported in table format.");
        
        writer.WriteHeading(2, "Products with Reviews");
        writer.WriteCodeBlockStart();
        writer.WriteTree(tree);
        writer.WriteCodeBlockEnd();
        writer.Flush();
    }
}
