using Markout;

namespace Markout.Samples.Serialization;

#region ProductViewObject
[MarkoutSerializable(TitleProperty = nameof(Name))]
public class ProductView
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public bool InStock { get; set; }
}

[MarkoutContext(typeof(ProductView))]
public partial class SampleContext : MarkoutSerializerContext
{
}
#endregion

/// <summary>
/// Demonstrates basic Markout serialization usage.
/// </summary>
public static class BasicUsage
{
    /// <summary>
    /// Shows how to serialize a simple type with the MarkoutSerializer.
    /// </summary>
    public static void SerializeSimpleType()
    {
        #region SerializeSimpleType
        ProductView product = new ProductView
        {
            Name = "Widget Pro",
            Category = "Electronics",
            Price = 99.99m,
            InStock = true
        };

        string markdown = MarkoutSerializer.Serialize(product, SampleContext.Default);
        // # Widget Pro
        //
        // Category: Electronics  
        // Price: 99.99  
        // InStock: yes  
        #endregion

        Console.WriteLine(markdown);
    }

    /// <summary>
    /// Shows how to serialize directly to a TextWriter or Stream.
    /// </summary>
    public static void WriteToStream()
    {
        #region WriteToStream
        ProductView product = new ProductView
        {
            Name = "Gadget X",
            Category = "Tools",
            Price = 49.99m,
            InStock = false
        };

        // Write to Console.Out
        MarkoutSerializer.Serialize(product, Console.Out, SampleContext.Default);

        // Or write to a file stream
        using var stream = File.Create("output.md");
        MarkoutSerializer.Serialize(product, stream, SampleContext.Default);
        #endregion
    }
}
