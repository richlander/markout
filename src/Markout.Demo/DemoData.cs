using System.Reflection;
using System.Text.Json;

namespace Markout.Demo;

/// <summary>
/// Loads and projects demo data from the single shoes.json source.
/// </summary>
internal static class DemoData
{
    private static readonly Lazy<ShoeData> _data = new(LoadData);
    
    public static ShoeData Data => _data.Value;
    
    private static ShoeData LoadData()
    {
        var assembly = typeof(DemoData).Assembly;
        var resourceName = "Markout.Demo.Data.shoes.json";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Resource not found: {resourceName}");
        
        return JsonSerializer.Deserialize(stream, DemoJsonContext.Default.ShoeData)
            ?? throw new InvalidOperationException("Failed to deserialize shoes.json");
    }
    
    /// <summary>
    /// Simple view: first shoe with basic fields.
    /// </summary>
    public static SimpleShoeView GetSimpleView(string shoeId)
    {
        var shoe = Data.GetShoe(shoeId) ?? Data.Shoes?.First()
            ?? throw new InvalidOperationException("No shoes found");
            
        return new SimpleShoeView
        {
            Name = $"Altra {shoe.Model}",
            Description = "This demo shows a basic product with scalar fields.",
            Id = shoe.Id,
            Category = shoe.Category,
            Price = shoe.Price,
            InStock = shoe.InStock
        };
    }
    
    /// <summary>
    /// Bullet list view: shoes as formatted strings.
    /// </summary>
    public static ShoeBulletListView GetBulletListView()
    {
        var shoes = Data.Shoes ?? new List<Shoe>();
        
        return new ShoeBulletListView
        {
            Name = Data.Catalog?.Name ?? "Shoes",
            Description = "This demo shows products as a bullet list with custom formatting.",
            Products = shoes.Select(s => 
            {
                var status = s.InStock ? "" : "; backordered";
                return $"{s.Model} (${s.Price}{status})";
            }).ToList()
        };
    }
    
    /// <summary>
    /// Table view: all shoes as table rows.
    /// </summary>
    public static ShoeTableView GetTableView()
    {
        var shoes = Data.Shoes ?? new List<Shoe>();
        
        return new ShoeTableView
        {
            Name = Data.Catalog?.Name ?? "Shoes",
            Description = "This demo shows products as a table with a section heading.",
            Count = shoes.Count,
            LowestPrice = shoes.Min(s => s.Price),
            Products = shoes.Select(s => new ShoeTableRow
            {
                Model = s.Model,
                Category = s.Category,
                Price = s.Price,
                InStock = s.InStock
            }).ToList()
        };
    }
    
    /// <summary>
    /// Detail view: shoes with nested features and reviews.
    /// </summary>
    public static ShoeDetailView GetDetailView()
    {
        var shoes = Data.Shoes ?? new List<Shoe>();
        
        return new ShoeDetailView
        {
            Name = Data.Catalog?.Name ?? "Shoes",
            Description = "This demo shows detailed products with nested features and reviews.",
            Count = shoes.Count,
            LowestPrice = shoes.Min(s => s.Price),
            Products = shoes.Select(s => new ShoeDetailItem
            {
                Name = s.Model,
                Category = s.Category,
                Price = s.Price,
                Features = s.Features,
                Reviews = s.Reviews
            }).ToList()
        };
    }
    
    /// <summary>
    /// Sections view: single shoe with specs and reviews.
    /// </summary>
    public static ShoeSectionsView GetSectionsView(string shoeId)
    {
        var shoe = Data.GetShoe(shoeId) ?? Data.Shoes?.First()
            ?? throw new InvalidOperationException("No shoes found");
            
        return new ShoeSectionsView
        {
            Name = $"Altra {shoe.Model}",
            Description = "This demo shows a product with separate sections for specs and reviews.",
            Id = shoe.Id,
            Price = shoe.Price,
            Specifications = shoe.Features,
            Reviews = shoe.Reviews
        };
    }
    
    /// <summary>
    /// Inventory view: pivoted by size and color.
    /// </summary>
    public static ShoeInventoryView GetInventoryView(string shoeId)
    {
        var shoe = Data.GetShoe(shoeId) ?? Data.Shoes?.First()
            ?? throw new InvalidOperationException("No shoes found");
            
        var entries = Data.Inventory?.Where(i => i.ShoeId == shoeId).ToList()
            ?? new List<InventoryEntry>();
            
        return new ShoeInventoryView
        {
            Name = $"Altra {shoe.Model}",
            Description = "This demo shows pivoted inventory data: rows are sizes, columns are colors.",
            Price = shoe.Price,
            TotalUnits = entries.Sum(e => e.Total),
            Inventory = entries.Select(e => new SizeColorRow
            {
                Size = e.Size,
                Black = e.Black,
                Green = e.Green,
                Red = e.Red
            }).ToList()
        };
    }
    
    /// <summary>
    /// Tree view: shoes with reviews for tree rendering.
    /// </summary>
    public static List<Shoe> GetShoesForTree(params string[] shoeIds)
    {
        if (shoeIds.Length == 0)
            return Data.Shoes?.Take(3).ToList() ?? new List<Shoe>();
            
        return shoeIds
            .Select(id => Data.GetShoe(id))
            .Where(s => s != null)
            .Cast<Shoe>()
            .ToList();
    }
}
