# Working with Nested Lists in Markout

When you have nested data structures like `List<Group>` where each `Group` contains another `List<Item>`, Markout can't represent them directly in a single table. This is because Markdown tables are inherently two-dimensional and can't contain lists in cells.

**Instead of treating this as a limitation, think of it as a design question:** *What insight does your reader need?*

## The Example: Product Catalog

Throughout this guide, we'll use a product catalog with regions and products:

```csharp
public class Catalog
{
    public string StoreName { get; set; }
    public List<Region> Regions { get; set; }  // ❌ This will trigger MARKOUT001
}

public class Region
{
    public string Name { get; set; }           // e.g., "North America"
    public string Currency { get; set; }       // e.g., "USD"
    public List<Product> Products { get; set; } // ❌ Nested list!
}

public class Product
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
}
```

**Sample Data:**
- **North America** (USD): Laptop ($1299, 45 units), Mouse ($29, 150 units)
- **Europe** (EUR): Laptop (€1199, 30 units), Mouse (€25, 200 units), Keyboard (€89, 75 units)
- **Asia** (JPY): Laptop (¥145000, 20 units), Keyboard (¥9800, 100 units)

## Understanding the Error

If you try to serialize `Catalog` with the structure above, you'll see:

```
error MARKOUT001: Property 'Products' in type 'Region' is an array of complex 
objects and cannot be rendered in a table cell. Use [MarkoutIgnore], [MarkoutSection], 
or transform the data.
```

This error prevents your code from producing useless `ToString()` output like:
```markdown
| North America | USD | System.Collections.Generic.List`1[Product] |
```

**What data gets lost?** Without transformation, you'd see the regions but ALL product information (names, prices, stock) would be invisible!

---

## The Four Solutions

Choose the strategy that best answers your reader's question about our product catalog.

### Strategy 1: **Pivot Table** - "How do product prices compare across regions?"

**Reader Question:** "Is the Laptop cheaper in Europe or North America?"

**Transform your data:**
```csharp
// Instead of List<Region> with nested List<Product>
// Create a matrix where products are rows, regions are columns
var priceMatrix = catalog.Regions
    .SelectMany(r => r.Products.Select(p => new { Region = r.Name, p.Name, p.Price, r.Currency }))
    .GroupBy(x => x.Name)
    .Select(g => new ProductPriceMatrix
    {
        ProductName = g.Key,
        NorthAmerica = FormatPrice(g, "North America"),
        Europe = FormatPrice(g, "Europe"),
        Asia = FormatPrice(g, "Asia")
    })
    .ToList();

string FormatPrice(IGrouping<string, ...> group, string region)
{
    var item = group.FirstOrDefault(x => x.Region == region);
    return item != null ? $"{item.Price} {item.Currency}" : "-";
}
```

**Result:**
```markdown
## Product Prices by Region

| Product  | North America | Europe      | Asia         |
|----------|---------------|-------------|--------------|
| Laptop   | $1299 USD     | €1199 EUR   | ¥145000 JPY  |
| Mouse    | $29 USD       | €25 EUR     | -            |
| Keyboard | -             | €89 EUR     | ¥9800 JPY    |
```

**What you see:** Price comparison across regions at a glance  
**What's hidden:** Stock levels (but could add with another pivot table)  
**Best for:** Comparing the same items across 2-6 groups

---

### Strategy 2: **Multiple Tables** - "What products are available in each region?"

**Reader Question:** "What can I buy in Europe?" or "Show me the Asia catalog"

**Structure your type:**
```csharp
[MdfSerializable(TitleProperty = nameof(StoreName))]
public class CatalogWithSections
{
    public string StoreName { get; set; }
    
    [MdfSection(Name = "North America (USD)")]
    public List<Product> NorthAmericaProducts { get; set; }
    
    [MdfSection(Name = "Europe (EUR)")]
    public List<Product> EuropeProducts { get; set; }
    
    [MdfSection(Name = "Asia (JPY)")]
    public List<Product> AsiaProducts { get; set; }
}

[MarkoutSerializable]
public class Product
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
}
```

**Result:**
```markdown
# Global Electronics Store

## North America (USD)

| Name   | Price | Stock |
|--------|-------|-------|
| Laptop | 1299  | 45    |
| Mouse  | 29    | 150   |

## Europe (EUR)

| Name     | Price | Stock |
|----------|-------|-------|
| Laptop   | 1199  | 30    |
| Mouse    | 25    | 200   |
| Keyboard | 89    | 75    |

## Asia (JPY)

| Name     | Price  | Stock |
|----------|--------|-------|
| Laptop   | 145000 | 20    |
| Keyboard | 9800   | 100   |
```

**What you see:** Complete product details for each region  
**What's hidden:** Nothing! All data is visible, organized by region  
**Best for:** Examining one group at a time, 2-5 groups with detailed items

---

### Strategy 3: **Multiple Lists** - "What products are in each region?"

**Reader Question:** "Quick scan - what's available where?"

**Structure your type:**
```csharp
[MdfSerializable(TitleProperty = nameof(StoreName))]
public class CatalogWithLists
{
    public string StoreName { get; set; }
    
    [MdfSection(Name = "North America")]
    public List<string> NorthAmericaProducts { get; set; }
    
    [MdfSection(Name = "Europe")]
    public List<string> EuropeProducts { get; set; }
    
    [MdfSection(Name = "Asia")]
    public List<string> AsiaProducts { get; set; }
}
```

**Result:**
```markdown
# Global Electronics Store

## North America

- Laptop
- Mouse

## Europe

- Laptop
- Mouse
- Keyboard

## Asia

- Laptop
- Keyboard
```

**What you see:** Quick overview of product availability  
**What's hidden:** Prices, stock levels, currency - all the detail!  
**Best for:** Simple items (just names), quick scanning, overview

---

### Strategy 4: **Flatten the Structure** - "Show me all products with their region"

**Reader Question:** "Give me one searchable/sortable table of everything"

**Transform your data:**
```csharp
// Flatten into a single list where region becomes a column
var flatProducts = catalog.Regions
    .SelectMany(r => r.Products.Select(p => new FlatProduct
    {
        Region = r.Name,
        Currency = r.Currency,
        ProductName = p.Name,
        Price = p.Price,
        Stock = p.Stock
    }))
    .ToList();
```

**Result:**
```markdown
## Global Product Inventory

| Region        | Currency | Product  | Price  | Stock |
|---------------|----------|----------|--------|-------|
| North America | USD      | Laptop   | 1299   | 45    |
| North America | USD      | Mouse    | 29     | 150   |
| Europe        | EUR      | Laptop   | 1199   | 30    |
| Europe        | EUR      | Mouse    | 25     | 200   |
| Europe        | EUR      | Keyboard | 89     | 75    |
| Asia          | JPY      | Laptop   | 145000 | 20    |
| Asia          | JPY      | Keyboard | 9800   | 100   |
```

**What you see:** All data in one searchable/sortable table  
**What's hidden:** Nothing - but grouping/scanning by region is harder  
**Best for:** Searchable/filterable logs, when region is just metadata

---

## Quick Decision Guide for Our Product Catalog

| Reader Question | Strategy | What You See | What's Hidden |
|----------------|----------|--------------|---------------|
| "Is Laptop cheaper in Europe?" | **Pivot** | Price comparison grid | Stock levels |
| "What can I buy in Asia?" | **Multiple Tables** | Complete Asia catalog | Cross-region comparison |
| "What's available where?" | **Multiple Lists** | Product names by region | Prices, stock, currency |
| "Find all Keyboards" | **Flatten** | Sortable inventory | Visual grouping by region |

---

## Applying This to Your Domain

The same principles apply to any nested structure:

### NuGet Dependencies (dotnet-inspector)
**Original:** `List<DependencyGroup>` with nested `List<Dependency>`
- **Pivot:** Package × Framework version matrix (compare versions across frameworks) ✓ Best choice
- **Multiple Tables:** Dependencies per framework (complete package info per framework)
- **Multiple Lists:** Package names per framework (quick overview of what's needed)
- **Flatten:** Framework as column in package table (searchable dependency list)

**See Example:** `tests/MarkdownData.Tests/NestedStructureTests.cs` - `Serialize_PackageWithDependencyGroups_CreatesTables()`

### Build Configurations
**Original:** `List<BuildConfig>` with nested `List<Project>`
- **Pivot:** Project × Config status matrix (see what built where)
- **Multiple Tables:** Projects per config (detailed build results) ✓ Best choice
- **Multiple Lists:** Project names per config (quick status)
- **Flatten:** Config as column in project table (searchable logs)

**See Example:** `tests/MarkdownData.Tests/BuildResultsTests.cs` - `Serialize_BuildResult_GroupsByConfiguration()`

### Feature Tiers
**Original:** `List<Tier>` with nested `List<Feature>`
- **Pivot:** Feature × Tier availability matrix (which tiers have what features)
- **Multiple Tables:** Features per tier (complete feature details)
- **Multiple Lists:** Feature names per tier (quick comparison) ✓ Best choice
- **Flatten:** Tier as column in feature table (searchable)

---

## What if I Just Want It to Work?

If you're getting the MARKOUT001 error and just want to compile:

**Quick fix:** Add `[MarkoutIgnore]` to the nested list property

```csharp
[MarkoutSerializable]
public class Region
{
    public string Name { get; set; }
    public string Currency { get; set; }
    
    [MarkoutIgnore]  // ⚠️ This HIDES ALL PRODUCT DATA!
    public List<Product> Products { get; set; }
}
```

**Result:**
```markdown
## Regions

| Name          | Currency |
|---------------|----------|
| North America | USD      |
| Europe        | EUR      |
| Asia          | JPY      |
```

**WARNING:** You'll see regions but **ALL product information is lost!** (Names, prices, stock - everything)

This is why you should choose one of the four transformation strategies instead.

---

## Working Code Examples

All four strategies are implemented with the product catalog example:

**Main Demo:** `tests/MarkdownData.Tests/RenderingStrategyTests.cs`
- `Strategy1_PivotTable_ComparesAcrossGroups()` - Pivot transformation with full code
- `Strategy2_MultipleTables_ShowsCompleteGroupDetails()` - Multiple tables approach
- `Strategy3_MultipleLists_ProvidesQuickOverview()` - Multiple lists approach  
- `Strategy4_Flatten_CreatesSearchableTable()` - Flatten transformation
- `DecisionMatrix_DocumentsWhenToUseEachStrategy()` - Complete comparison with guidance

**Additional Examples:**
- `tests/MarkdownData.Tests/PivotTableTests.cs` - More pivot transformations
- `tests/MarkdownData.Tests/NestedStructureTests.cs` - NuGet dependency patterns
- `tests/MarkdownData.Tests/BuildResultsTests.cs` - Build configuration patterns
- `tests/MarkdownData.Tests/ListRenderingStrategyTests.cs` - Before/after comparisons

Run `dotnet test --logger "console;verbosity=detailed"` to see the actual Markdown output in test logs!
