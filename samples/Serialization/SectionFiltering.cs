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
        var writer = new MarkoutOrchestrator(new MarkdownFormatter(), new MarkoutWriterOptions
        {
            // Only include sections matching these heading names
            IncludeSections = ["Overview", "Reviews"]
        });

        writer.WriteHeading(1, "Product Details");

        writer.WriteHeading(2, "Overview");        // included
        writer.WriteFields([new("Name", "Widget Pro")]);

        writer.WriteHeading(2, "Specifications");  // excluded
        writer.WriteFields([new("Weight", "1.5 kg")]);

        writer.WriteHeading(2, "Reviews");         // included
        writer.WriteFields([new("Rating", "4.5 stars")]);

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
        var writer = new MarkoutOrchestrator(new MarkdownFormatter(), new MarkoutWriterOptions
        {
            // Exclude the Specifications section
            ExcludeSections = ["Specifications"]
        });

        writer.WriteHeading(1, "Product Details");

        writer.WriteHeading(2, "Overview");        // included
        writer.WriteFields([new("Name", "Widget Pro")]);

        writer.WriteHeading(2, "Specifications");  // excluded
        writer.WriteFields([new("Weight", "1.5 kg")]);

        writer.WriteHeading(2, "Reviews");         // included
        writer.WriteFields([new("Rating", "4.5 stars")]);

        Console.WriteLine(writer.ToString());
        #endregion
    }

    /// <summary>
    /// Shows section filtering via MarkoutSerializerContext.
    /// </summary>
    public static void FilterViaContext()
    {
        #region FilterViaContext
        var context = new SampleContext(new MarkoutWriterOptions
        {
            ExcludeSections = ["Specifications"],  // Skip Specifications section
            BoldFieldNames = true                       // Enable bold field names
        });

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
