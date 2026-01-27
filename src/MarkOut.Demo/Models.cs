using MarkOut;
using System.Text.Json.Serialization;

namespace MarkOut.Demo;

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

[MarkOutSerializable]
public class Shoe
{
    public string Id { get; set; } = "";
    public string Model { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public bool InStock { get; set; }
    public List<Feature>? Features { get; set; }
    public List<Review>? Reviews { get; set; }
}

[MarkOutSerializable]
public class InventoryEntry
{
    public string ShoeId { get; set; } = "";
    public string Size { get; set; } = "";
    public int Black { get; set; }
    public int Green { get; set; }
    public int Red { get; set; }
    
    [MarkOutIgnore] // Computed property
    public int Total => Black + Green + Red;
}

#endregion

#region MarkOut View Models

/// <summary>
/// Simple view: single shoe with basic scalar fields.
/// </summary>
[MarkOutSerializable(TitleProperty = nameof(Name), DescriptionProperty = nameof(Description))]
public class SimpleShoeView
{
    public string Name { get; set; } = "";
    
    [MarkOutIgnore]
    public string? Description { get; set; }
    
    public string Id { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public bool InStock { get; set; }
}

/// <summary>
/// List view: catalog with shoes as bullet list.
/// </summary>
[MarkOutSerializable(TitleProperty = nameof(Name), DescriptionProperty = nameof(Description))]
public class ShoeBulletListView
{
    public string Name { get; set; } = "";
    
    [MarkOutIgnore]
    public string? Description { get; set; }
    
    [MarkOutSection(Name = "Available Models")]
    public List<string>? Products { get; set; }
}

/// <summary>
/// Table view: catalog with shoes as table rows.
/// </summary>
[MarkOutSerializable(TitleProperty = nameof(Name), DescriptionProperty = nameof(Description))]
public class ShoeTableView
{
    public string Name { get; set; } = "";
    
    [MarkOutIgnore]
    public string? Description { get; set; }
    
    public int Count { get; set; }
    
    [MarkOutPropertyName("Lowest Price")]
    public decimal LowestPrice { get; set; }
    
    [MarkOutSection(Name = "Products")]
    public List<ShoeTableRow>? Products { get; set; }
}

[MarkOutSerializable]
public class ShoeTableRow
{
    public string Model { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    
    [MarkOutPropertyName("In Stock")]
    public bool InStock { get; set; }
}

/// <summary>
/// Nested view: detailed shoes with features and reviews.
/// </summary>
[MarkOutSerializable(TitleProperty = nameof(Name), DescriptionProperty = nameof(Description))]
public class ShoeDetailView
{
    public string Name { get; set; } = "";
    
    [MarkOutIgnore]
    public string? Description { get; set; }
    
    public int Count { get; set; }
    
    [MarkOutPropertyName("Lowest Price")]
    public decimal LowestPrice { get; set; }
    
    [MarkOutSection(Name = "Products")]
    public List<ShoeDetailItem>? Products { get; set; }
}

[MarkOutSerializable(TitleProperty = nameof(Name))]
public class ShoeDetailItem
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    
    [MarkOutSection(Name = "Features")]
    public List<Feature>? Features { get; set; }
    
    [MarkOutSection(Name = "Reviews")]
    public List<Review>? Reviews { get; set; }
}

/// <summary>
/// Sections view: single shoe with specs and reviews sections.
/// </summary>
[MarkOutSerializable(TitleProperty = nameof(Name), DescriptionProperty = nameof(Description))]
public class ShoeSectionsView
{
    public string Name { get; set; } = "";
    
    [MarkOutIgnore]
    public string? Description { get; set; }
    
    public string Id { get; set; } = "";
    public decimal Price { get; set; }
    
    [MarkOutSection(Name = "Specifications")]
    public List<Feature>? Specifications { get; set; }
    
    [MarkOutSection(Name = "Customer Reviews")]
    public List<Review>? Reviews { get; set; }
}

/// <summary>
/// Pivot view: inventory pivoted by size and color.
/// </summary>
[MarkOutSerializable(TitleProperty = nameof(Name), DescriptionProperty = nameof(Description))]
public class ShoeInventoryView
{
    public string Name { get; set; } = "";
    
    [MarkOutIgnore]
    public string? Description { get; set; }
    
    public decimal Price { get; set; }
    
    [MarkOutPropertyName("Total Units")]
    public int TotalUnits { get; set; }
    
    [MarkOutSection(Name = "Inventory by Size")]
    public List<SizeColorRow>? Inventory { get; set; }
}

[MarkOutSerializable]
public class SizeColorRow
{
    public string Size { get; set; } = "";
    public int Black { get; set; }
    public int Green { get; set; }
    public int Red { get; set; }
}

#endregion

#region Shared Types

[MarkOutSerializable]
public class Feature
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
}

[MarkOutSerializable]
public class Review
{
    public string Author { get; set; } = "";
    public int Rating { get; set; }
    public string Comment { get; set; } = "";
}

#endregion

#region MarkOut Serializer Context

// Source data types (for schema introspection)
[MarkOutContext(typeof(Shoe))]
[MarkOutContext(typeof(InventoryEntry))]
[MarkOutContext(typeof(Feature))]
[MarkOutContext(typeof(Review))]
// View types (for serialization)
[MarkOutContext(typeof(SimpleShoeView))]
[MarkOutContext(typeof(ShoeBulletListView))]
[MarkOutContext(typeof(ShoeTableView))]
[MarkOutContext(typeof(ShoeTableRow))]
[MarkOutContext(typeof(ShoeDetailView))]
[MarkOutContext(typeof(ShoeSectionsView))]
[MarkOutContext(typeof(ShoeInventoryView))]
public partial class DemoContext : MarkOutSerializerContext
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
