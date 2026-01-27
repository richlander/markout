using Markout;
using System.Text.Json.Serialization;

namespace Markout.Demo;

#region Source Data (from JSON)

/// <summary>
/// Root structure of shoes.json - the single source of truth.
/// </summary>
public class ShoeData
{
    public CatalogInfo? Catalog { get; set; }
    public List<Shoe>? Shoes { get; set; }
    public List<InventoryEntry>? Inventory { get; set; }
    
    /// <summary>
    /// Gets a shoe by ID.
    /// </summary>
    public Shoe? GetShoe(string id) => Shoes?.FirstOrDefault(s => s.Id == id);
    
    /// <summary>
    /// Gets the display name for a shoe ID.
    /// </summary>
    public string GetDisplayName(string id) => GetShoe(id)?.Model ?? id;
}

public class CatalogInfo
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}

[MarkoutSerializable]
public class Shoe
{
    public string Id { get; set; } = "";
    public string Model { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public bool InStock { get; set; }
    [MarkoutIgnoreInTable]
    public List<Feature>? Features { get; set; }
    [MarkoutIgnoreInTable]
    public List<Review>? Reviews { get; set; }
}

[MarkoutSerializable]
public class InventoryEntry
{
    public string ShoeId { get; set; } = "";
    public string Size { get; set; } = "";
    public int Black { get; set; }
    public int Green { get; set; }
    public int Red { get; set; }
    
    [MarkoutIgnore] // Computed property
    public int Total => Black + Green + Red;
}

#endregion

#region Markout View Models

/// <summary>
/// Simple view: single shoe with basic scalar fields.
/// </summary>
[MarkoutSerializable(TitleProperty = nameof(Name), DescriptionProperty = nameof(Description))]
public class SimpleShoeView
{
    public string Name { get; set; } = "";
    
    [MarkoutIgnore]
    public string? Description { get; set; }
    
    public string Id { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public bool InStock { get; set; }
}

/// <summary>
/// List view: catalog with shoes as bullet list.
/// </summary>
[MarkoutSerializable(TitleProperty = nameof(Name), DescriptionProperty = nameof(Description))]
public class ShoeBulletListView
{
    public string Name { get; set; } = "";
    
    [MarkoutIgnore]
    public string? Description { get; set; }
    
    [MarkoutSection(Name = "Available Models")]
    public List<string>? Products { get; set; }
}

/// <summary>
/// Table view: catalog with shoes as table rows.
/// </summary>
[MarkoutSerializable(TitleProperty = nameof(Name), DescriptionProperty = nameof(Description))]
public class ShoeTableView
{
    public string Name { get; set; } = "";
    
    [MarkoutIgnore]
    public string? Description { get; set; }
    
    public int Count { get; set; }
    
    [MarkoutPropertyName("Lowest Price")]
    public decimal LowestPrice { get; set; }
    
    [MarkoutSection(Name = "Products")]
    public List<ShoeTableRow>? Products { get; set; }
}

[MarkoutSerializable]
public class ShoeTableRow
{
    public string Model { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    
    [MarkoutPropertyName("In Stock")]
    public bool InStock { get; set; }
}

/// <summary>
/// Nested view: detailed shoes with features and reviews.
/// </summary>
[MarkoutSerializable(TitleProperty = nameof(Name), DescriptionProperty = nameof(Description))]
public class ShoeDetailView
{
    public string Name { get; set; } = "";
    
    [MarkoutIgnore]
    public string? Description { get; set; }
    
    public int Count { get; set; }
    
    [MarkoutPropertyName("Lowest Price")]
    public decimal LowestPrice { get; set; }
    
    [MarkoutSection(Name = "Products")]
    public List<ShoeDetailItem>? Products { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Name))]
public class ShoeDetailItem
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    
    [MarkoutSection(Name = "Features")]
    public List<Feature>? Features { get; set; }
    
    [MarkoutSection(Name = "Reviews")]
    public List<Review>? Reviews { get; set; }
}

/// <summary>
/// Sections view: single shoe with specs and reviews sections.
/// </summary>
[MarkoutSerializable(TitleProperty = nameof(Name), DescriptionProperty = nameof(Description))]
public class ShoeSectionsView
{
    public string Name { get; set; } = "";
    
    [MarkoutIgnore]
    public string? Description { get; set; }
    
    public string Id { get; set; } = "";
    public decimal Price { get; set; }
    
    [MarkoutSection(Name = "Specifications")]
    public List<Feature>? Specifications { get; set; }
    
    [MarkoutSection(Name = "Customer Reviews")]
    public List<Review>? Reviews { get; set; }
}

/// <summary>
/// Pivot view: inventory pivoted by size and color.
/// </summary>
[MarkoutSerializable(TitleProperty = nameof(Name), DescriptionProperty = nameof(Description))]
public class ShoeInventoryView
{
    public string Name { get; set; } = "";
    
    [MarkoutIgnore]
    public string? Description { get; set; }
    
    public decimal Price { get; set; }
    
    [MarkoutPropertyName("Total Units")]
    public int TotalUnits { get; set; }
    
    [MarkoutSection(Name = "Inventory by Size")]
    public List<SizeColorRow>? Inventory { get; set; }
}

[MarkoutSerializable]
public class SizeColorRow
{
    public string Size { get; set; } = "";
    public int Black { get; set; }
    public int Green { get; set; }
    public int Red { get; set; }
}

#endregion

#region Shared Types

[MarkoutSerializable]
public class Feature
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
}

[MarkoutSerializable]
public class Review
{
    public string Author { get; set; } = "";
    public int Rating { get; set; }
    public string Comment { get; set; } = "";
}

#endregion

#region Markout Serializer Context

// Source data types (for schema introspection)
[MarkoutContext(typeof(Shoe))]
[MarkoutContext(typeof(InventoryEntry))]
[MarkoutContext(typeof(Feature))]
[MarkoutContext(typeof(Review))]
// View types (for serialization)
[MarkoutContext(typeof(SimpleShoeView))]
[MarkoutContext(typeof(ShoeBulletListView))]
[MarkoutContext(typeof(ShoeTableView))]
[MarkoutContext(typeof(ShoeTableRow))]
[MarkoutContext(typeof(ShoeDetailView))]
[MarkoutContext(typeof(ShoeSectionsView))]
[MarkoutContext(typeof(ShoeInventoryView))]
public partial class DemoContext : MarkoutSerializerContext
{
}

#endregion

#region JSON Serializer Context

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ShoeData))]
internal partial class DemoJsonContext : JsonSerializerContext
{
}

#endregion
