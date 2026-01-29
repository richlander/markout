using Markout;

namespace Markout.Samples.Serialization;

/// <summary>
/// Demonstrates section filtering with MarkoutWriter.
/// </summary>
public static class SectionFiltering
{
    /// <summary>
    /// Shows how to include only specific sections in output.
    /// </summary>
    public static void IncludeSpecificSections()
    {
        #region IncludeSpecificSections
        var writer = new MarkoutWriter
        {
            // Only include sections 1 and 3 (H2 boundaries)
            IncludeSections = new HashSet<int> { 1, 3 }
        };

        writer.WriteHeading(1, "Product Details");
        
        writer.WriteHeading(2, "Overview");        // Section 1 - included
        writer.WriteField("Name", "Widget Pro");
        
        writer.WriteHeading(2, "Specifications");  // Section 2 - excluded
        writer.WriteField("Weight", "1.5 kg");
        
        writer.WriteHeading(2, "Reviews");         // Section 3 - included
        writer.WriteField("Rating", "4.5 stars");

        Console.WriteLine(writer.ToString());
        // # Product Details
        //
        // ## Overview
        //
        // Name: Widget Pro  
        //
        // ## Reviews
        //
        // Rating: 4.5 stars  
        #endregion
    }

    /// <summary>
    /// Shows how to exclude specific sections from output.
    /// </summary>
    public static void ExcludeSpecificSections()
    {
        #region ExcludeSpecificSections
        var writer = new MarkoutWriter
        {
            // Exclude section 2 (Specifications)
            ExcludeSections = new HashSet<int> { 2 }
        };

        writer.WriteHeading(1, "Product Details");
        
        writer.WriteHeading(2, "Overview");        // Section 1 - included
        writer.WriteField("Name", "Widget Pro");
        
        writer.WriteHeading(2, "Specifications");  // Section 2 - excluded
        writer.WriteField("Weight", "1.5 kg");
        
        writer.WriteHeading(2, "Reviews");         // Section 3 - included
        writer.WriteField("Rating", "4.5 stars");

        Console.WriteLine(writer.ToString());
        #endregion
    }

    /// <summary>
    /// Shows section filtering via MarkoutSerializerContext.
    /// </summary>
    public static void FilterViaContext()
    {
        #region FilterViaContext
        var context = SampleContext.Default;
        context.ExcludeSections = new HashSet<int> { 2 };  // Skip section 2
        context.BoldFieldNames = true;                      // Enable bold field names

        ProductView product = new ProductView
        {
            Name = "Widget Pro",
            Category = "Electronics",
            Price = 99.99m,
            InStock = true
        };

        string markdown = MarkoutSerializer.Serialize(product, context);
        // # Widget Pro
        //
        // **Category:** Electronics  
        // **Price:** 99.99  
        // **InStock:** yes  
        #endregion

        Console.WriteLine(markdown);
    }
}
