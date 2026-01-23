# Working with Nested Lists in MDF

When you have nested data structures like `List<Group>` where each `Group` contains another `List<Item>`, MDF can't represent them directly in a single table. This is because Markdown tables are inherently two-dimensional and can't contain lists in cells.

**Instead of treating this as a limitation, think of it as a design question:** *What insight does your reader need?*

## Understanding the Error

If you try to serialize a type with nested lists, you'll see:

```
error MDF001: Property 'Dependencies' in type 'DependencyGroup' is an array 
of complex objects and cannot be rendered in a table cell. Use [MdfIgnore], 
[MdfSection], or transform the data.
```

This error prevents your code from producing useless `ToString()` output like:
```markdown
| net6.0 | System.Collections.Generic.List`1[Dependency] |
```

## The Four Solutions

Choose the strategy that best answers your reader's question:

### 1. **Pivot Table** - "How do items compare across groups?"

**Use when:** You want to see the same items across different groups (frameworks, platforms, tiers)

**Transform your data from:**
```csharp
List<DependencyGroup> {
    { Framework: "net6.0", Dependencies: [...] },
    { Framework: "net8.0", Dependencies: [...] }
}
```

**To:**
```csharp
List<DependencyMatrix> {
    { Package: "System.Memory", Net6: "4.5.5", Net8: "4.5.5" },
    { Package: "System.Text.Json", Net6: "6.0.0", Net8: "" }
}
```

**Result:**
```markdown
## Dependencies

| Package          | net6.0 | net8.0 |
|------------------|--------|--------|
| System.Memory    | 4.5.5  | 4.5.5  |
| System.Text.Json | 6.0.0  |        |
```

**Code Example:**
```csharp
// Transform before serialization
var matrix = dependencies
    .SelectMany(g => g.Dependencies.Select(d => new { g.Framework, d.Package, d.Version }))
    .GroupBy(x => x.Package)
    .Select(g => new DependencyMatrix
    {
        Package = g.Key,
        Net6 = g.FirstOrDefault(x => x.Framework == "net6.0")?.Version ?? "",
        Net8 = g.FirstOrDefault(x => x.Framework == "net8.0")?.Version ?? ""
    })
    .ToList();
```

**Best for:** Version matrices, feature comparison, cross-platform compatibility

---

### 2. **Multiple Tables** - "What's in each group?"

**Use when:** Readers examine one group at a time, groups are independent units

**Structure your type:**
```csharp
[MdfSerializable(TitleProperty = nameof(PackageName))]
public class PackageInfo
{
    public string PackageName { get; set; }
    
    [MdfSection(Name = "Dependencies (net6.0)")]
    public List<Dependency> Net6Dependencies { get; set; }
    
    [MdfSection(Name = "Dependencies (net8.0)")]
    public List<Dependency> Net8Dependencies { get; set; }
}

[MdfSerializable]
public class Dependency
{
    public string Name { get; set; }
    public string Version { get; set; }
}
```

**Result:**
```markdown
# Newtonsoft.Json

## Dependencies (net6.0)

| Name             | Version |
|------------------|---------|
| System.Memory    | 4.5.5   |
| System.Text.Json | 6.0.0   |

## Dependencies (net8.0)

| Name             | Version |
|------------------|---------|
| System.Memory    | 4.5.5   |
| Microsoft.CSharp | 4.7.0   |
```

**Best for:** Build configurations, test suites, API versions (2-5 groups)

---

### 3. **Multiple Lists** - "Give me a quick scan"

**Use when:** Items are simple (just names), readers want quick overview

**Structure your type:**
```csharp
[MdfSerializable(TitleProperty = nameof(Name))]
public class Product
{
    public string Name { get; set; }
    
    [MdfSection(Name = "Free Tier Features")]
    public List<string> FreeFeatures { get; set; }
    
    [MdfSection(Name = "Pro Tier Features")]
    public List<string> ProFeatures { get; set; }
}
```

**Result:**
```markdown
# Cloud Storage Service

## Free Tier Features

- 10GB storage
- Basic sharing
- Web access

## Pro Tier Features

- Unlimited storage
- Advanced sharing
- Desktop sync
- Priority support
```

**Best for:** Feature lists, simple categorization, quick reference

---

### 4. **Flatten the Structure** - "Show me all items with their group"

**Use when:** You want a single searchable table with group as a column

**Transform from:**
```csharp
List<DependencyGroup> {
    { Framework: "net6.0", Dependencies: [...] },
    { Framework: "net8.0", Dependencies: [...] }
}
```

**To:**
```csharp
List<FlatDependency> {
    { Framework: "net6.0", Package: "System.Memory", Version: "4.5.5" },
    { Framework: "net6.0", Package: "System.Text.Json", Version: "6.0.0" },
    { Framework: "net8.0", Package: "System.Memory", Version: "4.5.5" }
}
```

**Result:**
```markdown
## Dependencies

| Framework | Package          | Version |
|-----------|------------------|---------|
| net6.0    | System.Memory    | 4.5.5   |
| net6.0    | System.Text.Json | 6.0.0   |
| net8.0    | System.Memory    | 4.5.5   |
```

**Code Example:**
```csharp
var flat = groups
    .SelectMany(g => g.Items.Select(i => new FlatItem
    {
        Group = g.Name,
        ItemName = i.Name,
        ItemValue = i.Value
    }))
    .ToList();
```

**Best for:** Searchable/sortable data, when group is just a category

---

## Quick Decision Guide

Ask yourself: **"What question will my reader ask?"**

| Reader Question | Strategy | Best When |
|----------------|----------|-----------|
| "How does item X differ across groups?" | **Pivot Table** | Comparing same items across 2-6 groups |
| "What's in group Y?" | **Multiple Tables** | 2-5 groups, items have 2-3 properties |
| "What are all the Z items?" | **Multiple Lists** | Simple string items, quick scan |
| "Show me everything, I'll search/sort" | **Flatten** | Need searchability, group is just metadata |

## Common Patterns

### Pattern: NuGet Dependencies
**Reader needs:** Version compatibility across frameworks  
**Solution:** Pivot Table (packages × frameworks)

### Pattern: Build Results
**Reader needs:** What succeeded/failed in each config  
**Solution:** Multiple Tables (one per configuration)

### Pattern: Feature Availability
**Reader needs:** Quick comparison of what's in each tier  
**Solution:** Multiple Lists (one per tier)

### Pattern: Test Results
**Reader needs:** Detailed test info with suite/category  
**Solution:** Flatten (suite as a column in single table)

## What if I Just Want It to Work?

If you're getting the MDF001 error and just want to compile:

**Quick fix:** Add `[MdfIgnore]` to the nested list property

```csharp
[MdfSerializable]
public class DependencyGroup
{
    public string TargetFramework { get; set; }
    
    [MdfIgnore]  // Hides this from serialization
    public List<Dependency> Packages { get; set; }
}
```

**But remember:** This just hides the data. Choose a transformation strategy to actually show it!

## More Examples

See the test suite for working code examples:
- `tests/MarkdownData.Tests/PivotTableTests.cs` - Pivot transformations
- `tests/MarkdownData.Tests/RenderingStrategyTests.cs` - All four strategies
- `tests/MarkdownData.Tests/ListRenderingStrategyTests.cs` - Comparison examples
