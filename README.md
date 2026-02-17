# Markout

**Human-readable structured data serialization to Markdown**

Markout serializes .NET objects to clean, readable Markdown format. Perfect for logs, reports, documentation, and any output that humans and LLMs need to read.

## Why Markdown for Structured Output?

CLI tools need to produce output that works for multiple audiences: humans reading in terminals, scripts parsing programmatically, and increasingly, LLMs consuming tool output. Each format has tradeoffs:

| Format | Human-Readable | Machine-Parseable | LLM-Friendly | Challenges |
|--------|---------------|-------------------|--------------|------------|
| **Terminal text** | ✓ Good | ✗ Poor | ~ Moderate | Custom formatting, non-uniform delimiters, animation artifacts, ANSI codes |
| **JSON** | ✗ Poor | ✓ Excellent | ~ Moderate | Verbose, hard to scan, quotes everywhere, nested structures hard to follow |
| **Markdown** | ✓ Excellent | ✓ Good | ✓ Excellent | Limited nesting in tables, requires careful structure |

**Terminal loggers** (like the dotnet CLI's animated output) optimize for interactive human use but produce output that's difficult to parse. Custom spacing, non-uniform separators, and ANSI escape codes make programmatic consumption fragile.

**JSON** is perfectly machine-parseable but painful to read. Deeply nested structures, required quoting, and lack of visual hierarchy make it poor for human consumption. LLMs can parse JSON, but the token overhead is significant.

**Markdown** hits the sweet spot: tables are scannable by humans, headings provide natural hierarchy, and the format is both well-defined enough for parsing and natural enough for LLMs to understand without special handling.

### Data Shape vs. Data Results

JSON serialization is typically oriented on pure data shape—mirroring the structure of objects and their relationships. Markout can do that too. However, much of the time what's desired is serializing a *data result*: a transformed, filtered, or aggregated view of data tailored for a specific query or report.

We call this approach a **pivot table**. Instead of dumping raw object graphs, you project your data into view models designed for human consumption—flattening nested structures, joining related data, and presenting exactly what the reader needs to see.

## Features

- **Tables** - `List<T>` serializes to Markdown tables
- **Sections** - Nested objects become H2 sections
- **Type-safe** - Source generator provides compile-time validation
- **Zero allocation** - Direct string writing, no intermediate objects
- **Compile-time errors** - Prevents common mistakes like nested lists in tables

## Quick Start

This example from [samples/CanadianContent](samples/CanadianContent) shows actors with their filmography:

```csharp
using Markout;

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class ActorFilmography
{
    [MarkoutIgnore]
    public string Title { get; set; } = "";
    public string Birthplace { get; set; } = "";
    
    [MarkoutPropertyName("Born")]
    public int BirthYear { get; set; }

    [MarkoutSection(Name = "Filmography")]
    public List<ShowRow>? Filmography { get; set; }
}

[MarkoutSerializable]
public class ShowRow
{
    public string Title { get; set; } = "";
    public string Type { get; set; } = "";
    public string Years { get; set; } = "";

    [MarkoutPropertyName("Filmed In")]
    public string FilmingLocation { get; set; } = "";
}

[MarkoutContext(typeof(ActorFilmography))]
[MarkoutContext(typeof(ShowRow))]
public partial class CanConContext : MarkoutSerializerContext { }

// Serialize
MarkoutSerializer.Serialize(actor, Console.Out, CanConContext.Default);
```

**Output:**

```markdown
# Ryan Gosling

Birthplace: London, Ontario  
Born: 1980  

## Filmography

| Title | Type | Years | Filmed In |
| ----- | ---- | ----- | --------- |
| The Notebook | Movie | 2004 | Filmed in South Carolina |
| Blade Runner 2049 | Movie | 2017 | Filmed in Budapest |
| Barbie | Movie | 2023 | Filmed in London |
```

## Installation

```bash
dotnet add package Markout
```

The package includes the source generator - no additional packages needed.

## Common Patterns

### List as Table

From [samples/CanadianContent](samples/CanadianContent) — querying shows filmed in Toronto:

```csharp
[MarkoutSerializable(TitleProperty = nameof(Title))]
public class LocationSearchResult
{
    [MarkoutIgnore]
    public string Title { get; set; } = "";

    [MarkoutSection(Name = "Results")]
    public List<ShowRow>? Results { get; set; }
}

[MarkoutSerializable]
public class ShowRow
{
    public string Title { get; set; } = "";
    public string Type { get; set; } = "";
    public string Years { get; set; } = "";

    [MarkoutPropertyName("Filmed In")]
    public string FilmingLocation { get; set; } = "";
}
```

**Output:**

```markdown
# Shows Filmed in Toronto

## Results

| Title | Type | Years | Filmed In |
| ----- | ---- | ----- | --------- |
| The Expanse | TV Series | 2015-2022 | Filmed in Toronto |
| Station Eleven | TV Miniseries | 2021-2022 | Filmed in Toronto |
| Lost Girl | TV Series | 2010-2016 | Filmed in Toronto |
| Orphan Black | TV Series | 2013-2017 | Filmed in Toronto |
| The Umbrella Academy | TV Series | 2019-2023 | Filmed in Toronto |
```

### Nested Objects as Sections

From [samples/DotNetReleases](samples/DotNetReleases) — fetching .NET release info from GitHub:

```csharp
[MarkoutSerializable(TitleProperty = nameof(Title))]
public class ReleasesView
{
    [MarkoutIgnore]
    public string Title { get; set; } = "";
    
    [MarkoutPropertyName("Latest Major")]
    public string? LatestMajor { get; set; }
    
    [MarkoutPropertyName("Latest LTS")]
    public string? LatestLtsMajor { get; set; }
    
    [MarkoutSection(Name = "All Releases")]
    public List<ReleaseRow>? Releases { get; set; }
}

[MarkoutSerializable]
public class ReleaseRow
{
    public string Version { get; set; } = "";
    public string Type { get; set; } = "";
    public bool Supported { get; set; }
}

// Serialize to console
MarkoutSerializer.Serialize(view, Console.Out, ReleasesContext.Default);
```

**Output:**

```markdown
# .NET Releases

Latest Major: 10.0  
Latest LTS: 10.0  

## All Releases

| Version | Type | Supported |
| ------- | ---- | --------- |
| 10.0 | lts | yes |
| 9.0 | sts | yes |
| 8.0 | lts | yes |
| 7.0 | sts | no |
| 6.0 | lts | no |
```

## Attributes

- **`[MarkoutSerializable]`** - Marks a type for serialization
  - `TitleProperty` - Property to use as H1 title
  - `DescriptionProperty` - Property to render as paragraph after title
  - `AutoFields` - When `false`, only sections and field collections render (default: `true`)
- **`[MarkoutPropertyName("...")]`** - Custom property display name
- **`[MarkoutIgnore]`** - Excludes a property from output
- **`[MarkoutIgnoreInTable]`** - Excludes a property only in table context (silences MARKOUT001 warning)
- **`[MarkoutSection(Name = "...")]`** - Groups property under an H2 section (scalars grouped by name, collections as tables/lists)
- **`[MarkoutBoolFormat]`** - Custom true/false display values
- **`[MarkoutValueMap("key=badge", ...)]`** - Maps string values to badge-prefixed output (e.g., `"class"` → `"📦 class"`)
- **`[MarkoutContext(typeof(...))]`** - Registers types for source generation

## Field Collections

For dynamic metadata, use `List<MarkoutField>` properties:

```csharp
[MarkoutSerializable(AutoFields = false)]
public class PackageInfo
{
    public string Name { get; set; }
    
    // Renders as: Type: Library | TFM: net8.0 | Updated: 2026-01-15
    public List<MarkoutField> Summary => GetSummaryFields();
    
    // Renders as H2 section with Property/Value table
    [MarkoutSection(Name = "Metadata")]
    public List<MarkoutField> Metadata => GetMetadataFields();
    
    private List<MarkoutField> GetSummaryFields() =>
    [
        new("Type", PackageType),
        MarkoutField.Create("Updated", LastUpdated)
    ];
}
```

Field collections must use `List<MarkoutField>`, `IReadOnlyList<MarkoutField>`, or `MarkoutField[]` (not `IEnumerable<MarkoutField>`) to avoid double-enumeration issues.

## Trees

For hierarchical data, use `List<TreeNode>` properties:

```csharp
[MarkoutSerializable(TitleProperty = nameof(Name))]
public class TypeShape
{
    [MarkoutIgnore]
    public string Name { get; set; } = "";
    
    public string Kind { get; set; } = "";
    
    // Renders as tree with box-drawing characters
    public List<TreeNode> Members { get; set; } = [];
}

// Build the tree
var type = new TypeShape
{
    Name = "MyClass",
    Kind = "class",
    Members = new List<TreeNode>
    {
        new("Inherits", new[] { "BaseClass" }),
        new("Properties (2)", new[] { "string Name", "int Count" })
    }
};
```

**Output:**

```markdown
# MyClass

Kind: class

├─ Inherits
│  └─ BaseClass
└─ Properties (2)
   ├─ string Name
   └─ int Count
```

TreeNode supports an optional `Icon` property for visual indicators:

```csharp
new TreeNode("local.dll", icon: "📁"),
new TreeNode("platform.dll", icon: "🚢")
// Renders as: └─ 📁 local.dll
```

## Nested Lists

If you have `List<Group>` where `Group` contains `List<Item>`, you'll get a compile-time warning:

```
warning MARKOUT001: Property 'Items' in type 'Group' is an array of complex 
objects and will be skipped in table context. Add [MarkoutIgnoreInTable] to 
silence this warning.
```

This is intentional! Markdown tables can't contain lists. Choose a transformation strategy:

1. **Pivot Table** - Compare items across groups
2. **Multiple Tables** - One table per group
3. **Multiple Lists** - Simple bullet lists per group
4. **Flatten** - Single table with group as a column

📖 **See [Nested Lists Guide](docs/nested-lists-guide.md)** for complete examples and code

## Samples

- **[CanadianContent](samples/CanadianContent)** - Query Canadian actors and shows with searchable filters (file-based app)
- **[DotNetReleases](samples/DotNetReleases)** - Fetch and display .NET release information from GitHub (file-based app)
- **[Serialization](samples/Serialization)** - Basic usage patterns, section filtering, and MarkoutWriter examples

## Real-World Usage

Markout was created for [dotnet-inspect](https://github.com/richlander/dotnet-inspect) to generate readable inspection reports. It excels at:

- Build/test results
- Dependency reports
- API inspection output
- Configuration summaries
- Error reports

## Documentation

- **[Nested Lists Guide](docs/nested-lists-guide.md)** - Handling nested data structures
- **[Specification](docs/specification.md)** - Complete format specification
- **[Design Docs](docs/design/)** - Implementation details

## License

MIT
