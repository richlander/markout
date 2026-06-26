# Markout User Guide

Markout is a source-generated .NET library that serializes objects to clean, readable Markdown. Define your models with attributes, and Markout generates efficient serialization code at compile time — no reflection, no runtime overhead.

- [Quick Start](#quick-start)
- [Quick Tweaks](#quick-tweaks)
- [Defining Models](#defining-models)
- [Serialization](#serialization)
- [Scalar Fields](#scalar-fields)
- [Field Layout](#field-layout)
- [Formatting Values](#formatting-values)
- [Conditional Rendering](#conditional-rendering)
- [Sections and Collections](#sections-and-collections)
- [Section Field Order](#section-field-order)
- [Tables](#tables)
- [Trees](#trees)
- [Links](#links)
- [Custom Value Formatters](#custom-value-formatters)
- [Writer Options](#writer-options)
- [Table, TSV, and JSONL Output](#table-tsv-and-jsonl-output)
- [Low-Level Writer API](#low-level-writer-api)
- [Attribute Reference](#attribute-reference)

## Quick Start

The simplest Markout program is a model, a context, and a serialize call.

```csharp
using Markout;

var city = new City
{
    Name = "Vancouver",
    Country = "Canada",
    Population = 2_632_000,
    Temperature = 6.2,
    Latitude = 49.2827,
    Longitude = -123.1207,
    Altitude = 0
};

MarkoutSerializer.Serialize(city, Console.Out, CityContext.Default);

[MarkoutSerializable(TitleProperty = nameof(Name))]
public class City
{
    public string Name { get; set; } = "";
    public string Country { get; set; } = "";
    [MarkoutDisplayFormat("{0:N0}")]
    public int Population { get; set; }

    [MarkoutSection(Name = "Geography")]
    public double Latitude { get; set; }

    [MarkoutSection(Name = "Geography")]
    public double Longitude { get; set; }

    [MarkoutSection(Name = "Geography")]
    [MarkoutPropertyName("Altitude (m)")]
    public int Altitude { get; set; }

    [MarkoutSection(Name = "Geography")]
    [MarkoutDisplayFormat("{0:0.0} °C")]
    public double Temperature { get; set; }
}

[MarkoutContext(typeof(City))]
public partial class CityContext : MarkoutSerializerContext { }
```

Output:

```markdown
# Vancouver

Country: Canada | Population: 2,632,000

## Geography

Latitude: 49.2827 | Longitude: -123.1207 | Altitude (m): 0 | Temperature: 6.2 °C
```

> This is the [HelloMarkout](../samples/HelloMarkout/HelloMarkout.cs) sample.

Three things are required:

1. **A model** — a class, optionally decorated with `[MarkoutSerializable]` for customization.
2. **A context** — a `partial class` inheriting `MarkoutSerializerContext` with `[MarkoutContext(typeof(...))]` for each type.
3. **A serialize call** — `MarkoutSerializer.Serialize(value, context)`.

The source generator fills in the `partial class` with all the serialization logic at compile time.

## Quick Tweaks

The same city can be rendered differently by changing just the `FieldLayout`. `Vertical` puts one field per line with trailing double-spaces for proper rendering as HTML line breaks:

```csharp
[MarkoutSerializable(TitleProperty = nameof(Name), FieldLayout = FieldLayout.Vertical)]
public class City { /* same properties */ }
```

```markdown
# Vancouver

Country: Canada
Population: 2,632,000

## Geography

Latitude: 49.2827
Longitude: -123.1207
Altitude (m): 0
Temperature: 6.2 °C
```

> **Note:** In standard Markdown, adjacent lines without a blank line between them collapse into a single paragraph when rendered as HTML. `Vertical` appends two trailing spaces (`  `) to each line, which Markdown renders as `<br>`.

`Bulleted` renders each field as a bullet item:

```csharp
[MarkoutSerializable(TitleProperty = nameof(Name), FieldLayout = FieldLayout.Bulleted)]
public class City { /* same properties */ }
```

```markdown
# Vancouver

- Country: Canada
- Population: 2,632,000

## Geography

- Latitude: 49.2827
- Longitude: -123.1207
- Altitude (m): 0
- Temperature: 6.2 °C
```

## Defining Models

Markout has two kinds of attributes: **type-level attributes** on the model class, and **property-level attributes** on individual properties.

A type becomes serializable when it is registered on a context class with `[MarkoutContext(typeof(T))]`. The `[MarkoutSerializable]` attribute on the type itself is **optional** — use it only when you need to customize behavior like `TitleProperty`, `FieldLayout`, or `AutoFields`. Without it, the type uses sensible defaults (all fields rendered, `Inline` layout, no title heading).

For simple row types used in tables, you typically don't need `[MarkoutSerializable]`:

```csharp
// No [MarkoutSerializable] needed — registered via [MarkoutContext] on the context class
public class ActorRow
{
    public string Name { get; set; } = "";
    public string Birthplace { get; set; } = "";
    public int BirthYear { get; set; }
    public string Citizenship { get; set; } = "";
}
```

> From the [CanadianContent](../samples/CanadianContent/CanadianContent.cs) sample.

For top-level document types, use `[MarkoutSerializable]` to configure the title, layout, or description:

```csharp
[MarkoutSerializable(TitleProperty = nameof(Title))]
public class Releases
{
    public string Title { get; set; } = "";

    [MarkoutPropertyName("Latest Major")]
    [MarkoutSkipNull]
    public string? LatestMajor { get; set; }

    [MarkoutSection(Name = "All Releases")]
    public List<ReleaseRow>? Releases { get; set; }
}
```

All types must be registered on the context class:

```csharp
[MarkoutContext(typeof(Releases))]
[MarkoutContext(typeof(ReleaseRow))]
public partial class ReleasesContext : MarkoutSerializerContext { }
```

> From the [DotNetReleases](../samples/DotNetReleases/DotNetReleases.cs) sample.

### Title Property

Set `TitleProperty` to render a property as a Markdown `#` heading instead of a field. The title property is automatically excluded from the field list — `[MarkoutIgnore]` is not needed. The same applies to `TitleContextProperty` and `DescriptionProperty`.

### Title Context Property

Use `TitleContextProperty` when you want a secondary identifier rendered in parentheses after the heading:

```csharp
[MarkoutSerializable(TitleProperty = nameof(Name), TitleContextProperty = nameof(Version))]
public class Package
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
}
// Renders: # PackageName (1.0.0)
```

### Description Property

Set `DescriptionProperty` to render a property as a paragraph below the heading:

```csharp
[MarkoutSerializable(TitleProperty = nameof(Name), DescriptionProperty = nameof(Summary))]
public class ApiReport
{
    public string Name { get; set; } = "";
    public string Summary { get; set; } = "";
}
```

### AutoFields

By default, all scalar properties are rendered as fields (`AutoFields = true`). Set `AutoFields = false` when you only want to render sections:

```csharp
[MarkoutSerializable(TitleProperty = nameof(Title), AutoFields = false)]
public class AuditReport
{
    public string? Title { get; set; }

    public bool HasWarnings { get; set; }
    public bool HasErrors { get; set; }

    [MarkoutSection(Name = "Warnings", ShowWhenProperty = nameof(HasWarnings))]
    public List<string>? Warnings { get; set; }

    [MarkoutSection(Name = "Errors", ShowWhenProperty = nameof(HasErrors))]
    public List<string>? Errors { get; set; }
}
```

> From the [AdvancedFeatures](../samples/Serialization/AdvancedFeatures.cs) sample.

## Serialization

### To a string

```csharp
string markdown = MarkoutSerializer.Serialize(product, SampleContext.Default);
```

### To Console.Out or a TextWriter

```csharp
MarkoutSerializer.Serialize(product, Console.Out, SampleContext.Default);
```

### To a Stream

```csharp
using var stream = File.Create("output.md");
MarkoutSerializer.Serialize(product, stream, SampleContext.Default);
```

### Via the context directly

The context itself also has `Serialize` methods:

```csharp
string markdown = SampleContext.Default.Serialize(product);
```

### With options

Options can be passed per-call:

```csharp
var options = new MarkoutWriterOptions { BoldFieldNames = true };
string markdown = MarkoutSerializer.Serialize(product, SampleContext.Default, options);
```

Or set at compile time on the context class with `[MarkoutContextOptions]`, so every serialization through that context uses them by default:

```csharp
[MarkoutContextOptions(BoldFieldNames = true, SuppressTableWarnings = true)]
[MarkoutContext(typeof(Product))]
public partial class SampleContext : MarkoutSerializerContext { }
```

Per-call options override context-level defaults when both are specified.

Every registered type generates a `Default` singleton on the context class.

## Scalar Fields

Markout recognizes these scalar types: `string`, `bool`, `int`, `long`, `double`, `decimal`, `DateTime`, `DateTimeOffset`, and enums. These are rendered as key-value fields.

### Renaming Fields

Use `[MarkoutPropertyName]` to customize the display name:

```csharp
[MarkoutPropertyName("Born")]
public int BirthYear { get; set; }

[MarkoutPropertyName("Filmed In")]
public string FilmingLocation { get; set; } = "";
```

> From the [CanadianContent](../samples/CanadianContent/CanadianContent.cs) sample.

### Ignoring Properties

Use `[MarkoutIgnore]` to exclude a property from all output. This is useful for properties that drive logic but shouldn't appear in the rendered Markdown:

```csharp
[MarkoutIgnore]
public string InternalId { get; set; } = "";
```

> **Note:** Properties named by `TitleProperty`, `TitleContextProperty`, or `DescriptionProperty` are automatically excluded from the field list — you do not need `[MarkoutIgnore]` on them.

Use `[MarkoutIgnoreInTable]` to exclude a property only in table context (when the type is rendered as a row in a parent's table):

```csharp
[MarkoutIgnoreInTable]
public List<TreeNode>? Releases { get; set; }
```

> From the [LatestCves](../samples/LatestCves/LatestCves.cs) sample.

## Field Layout

The `FieldLayout` property controls how scalar fields are arranged. The default is `Inline`.

### Inline (default)

All fields on a single line separated by pipes:

```csharp
[MarkoutSerializable]
public class ActorRow
{
    public string Name { get; set; } = "";
    public string Birthplace { get; set; } = "";
    [MarkoutPropertyName("Born")]
    public int BirthYear { get; set; }
}
```

```markdown
Name: Ryan Gosling | Birthplace: London, Ontario | Born: 1980
```

### Vertical

Each field on its own line with trailing double-spaces:

```csharp
[MarkoutSerializable(TitleProperty = nameof(Name), FieldLayout = FieldLayout.Vertical)]
public class Package
{
    public string Name { get; set; } = "";
    public string? Homepage { get; set; }
    public string? Repository { get; set; }
}
```

```markdown
# dotnet-inspect

Homepage: https://github.com/richlander/dotnet-inspect
Repository: https://github.com/richlander/dotnet-inspect.git
```

### Bulleted

Each field as a bullet list item:

```csharp
[MarkoutSerializable(FieldLayout = FieldLayout.Bulleted)]
public class Config
{
    public string Host { get; set; } = "";
    public int Port { get; set; }
}
```

```markdown
- Host: localhost
- Port: 8080
```

## Formatting Values

### Boolean Formatting

By default, booleans render as `yes` / `no`. Use `[MarkoutBoolFormat]` to customize:

```csharp
[MarkoutBoolFormat("✓", "✗")]
public bool Supported { get; set; }
```

> From the [DotNetReleases](../samples/DotNetReleases/DotNetReleases.cs) sample.

### Numeric and Date Formatting

Use `[MarkoutFormat]` to apply a standard .NET format string via `ToString()`:

```csharp
[MarkoutFormat("N0")]
public long Downloads { get; set; }    // "1,234,567"

[MarkoutFormat("yyyy-MM-dd")]
public DateTime ReleaseDate { get; set; }  // "2025-01-15"
```

### Display Format (Composite)

Use `[MarkoutDisplayFormat]` to wrap a value in `string.Format()` with surrounding text:

```csharp
[MarkoutDisplayFormat("{0:N0} req/s")]
public long RequestsPerSecond { get; set; }  // "12,500 req/s"

[MarkoutDisplayFormat("{0:P1}")]
public double? CpuUsage { get; set; }  // "73.0 %"
```

The `{0}` placeholder receives the property value. You can include format specifiers inside the placeholder.

> From the [AdvancedFeatures](../samples/Serialization/AdvancedFeatures.cs) sample.

### Table-Specific Display

Use `[MarkoutTableDisplay]` when you want a different format in table columns vs. block context:

```csharp
[MarkoutSerializable]
public class MetricRow
{
    public string Name { get; set; } = "";

    [MarkoutTableDisplay("{0:N0} req")]
    public long Requests { get; set; }

    [MarkoutTableDisplay("{0:P0}")]
    public double ErrorRate { get; set; }
}
```

In a table, `Requests` renders as `"12,500 req"`. When rendered standalone (block context), it renders as `"12500"` using the default formatting.

### Joining Collections as Scalars

Use `[MarkoutJoin]` to render a string collection as a single joined value:

```csharp
[MarkoutJoin(", ")]
public List<string>? Tags { get; set; }    // "dotnet, tools, cli"

[MarkoutJoin(" | ")]
public string[]? Frameworks { get; set; }  // "net8.0 | net9.0"
```

## Conditional Rendering

### Skipping Null Values

Use `[MarkoutSkipNull]` to omit fields when their value is null, empty, or (for collections) has no items:

```csharp
[MarkoutSerializable(TitleProperty = nameof(Name))]
public class Package
{
    public string Name { get; set; } = "";
    [MarkoutSkipNull]
    public string? License { get; set; }
    [MarkoutSkipNull]
    public string? Repository { get; set; }
    public int Downloads { get; set; }
    [MarkoutSkipNull]
    public int? Stars { get; set; }
}
```

When `Repository` is `null` and `Stars` is `null`, those fields are omitted entirely. Note that `Downloads: 0` still renders — `[MarkoutSkipNull]` does **not** skip zero or false.

> This is the [SkipNullDemo](../samples/SkipNullDemo/SkipNullDemo.cs) sample.

### Skipping Default Values

Use `[MarkoutSkipDefault]` to omit fields when they equal their type's default value (`false`, `0`, `null`, `default(DateTime)`, etc.):

```csharp
[MarkoutSkipDefault]
public bool MaintenanceMode { get; set; }  // omitted when false

[MarkoutSkipDefault]
public int ErrorCount { get; set; }  // omitted when 0
```

### Conditional Fields (ShowWhen)

Use `[MarkoutShowWhen]` to render a field only when a bool property on the same type is `true`:

```csharp
[MarkoutSerializable(TitleProperty = nameof(Name), FieldLayout = FieldLayout.Vertical)]
public class Package
{
    public string Name { get; set; } = "";

    public bool IsVerified { get; set; }

    [MarkoutShowWhen(nameof(IsVerified))]
    public string? VerifiedBy { get; set; }

    public bool IsTool { get; set; }

    [MarkoutShowWhen(nameof(IsTool))]
    public string? ToolCommand { get; set; }
}
```

When `IsVerified` is `false`, `VerifiedBy` is not rendered — even if it has a value. This enables declarative conditional visibility without manual code.

> From the [AdvancedFeatures](../samples/Serialization/AdvancedFeatures.cs) sample.

### Value Maps

Use `[MarkoutValueMap]` to prepend a badge to string values based on a lookup:

```csharp
[MarkoutSerializable]
public class CveItem
{
    public string Id { get; set; } = "";

    [MarkoutValueMap("CRITICAL=🔴", "HIGH=🟠", "MEDIUM=🟡", "LOW=🟢")]
    public string Severity { get; set; } = "";
}
```

Each entry is `"value=badge"`. When the property value matches a key, the badge is prepended with a space: `"🔴 CRITICAL"`. Unmatched values pass through unchanged. Works in both field and table contexts.

## Sections and Collections

Use `[MarkoutSection]` to group properties under a heading. It works on both scalar properties and collections.

### Scalar Sections

When scalar properties share the same section name, they are grouped under a single heading:

```csharp
[MarkoutSerializable(TitleProperty = nameof(Name), AutoFields = false)]
public class Package
{
    public string Name { get; set; } = "";

    [MarkoutSection(Name = "Statistics")]
    [MarkoutSkipNull]
    [MarkoutValueFormatter(typeof(CompactNumberFormatter))]
    public long? Downloads { get; set; }

    [MarkoutSection(Name = "Statistics")]
    [MarkoutSkipNull]
    [MarkoutValueFormatter(typeof(ByteSizeFormatter))]
    [MarkoutPropertyName("Package Size")]
    public long? PackageSize { get; set; }
}
```

```markdown
# System.Text.Json

## Statistics

Downloads: 5.1B | Package Size: 2.1 MB
```

When all fields in a scalar section are null or skipped, the heading is suppressed entirely — no empty sections appear.

All property-level attributes work within scalar sections: `[MarkoutSkipNull]`, `[MarkoutValueFormatter]`, `[MarkoutFormat]`, `[MarkoutLink]`, `[MarkoutShowWhen]`, etc.

> See the [HelloMarkout](../samples/HelloMarkout/HelloMarkout.cs) sample for a Geography section using scalar properties.

### Collection Sections

When a `List<T>` of complex objects has `[MarkoutSection]`, it renders as a headed table:

```csharp
[MarkoutSerializable(TitleProperty = nameof(Title))]
public class ShowDetail
{
    public string Title { get; set; } = "";
    public string Type { get; set; } = "";
    public string Years { get; set; } = "";

    [MarkoutSection(Name = "Canadian Cast")]
    public List<ActorRow>? Cast { get; set; }
}
```

```markdown
# The Expanse

Type: TV Series | Years: 2015-2022

## Canadian Cast

| Name | Birthplace | Born | Citizenship |
|------|------------|------|-------------|
| Cara Gee | Calgary, Alberta | 1983 | Canadian |
| Keon Alexander | Toronto, Ontario | 1986 | Canadian |
```

> From the [CanadianContent](../samples/CanadianContent/CanadianContent.cs) sample.

### Conditional Sections

Use `ShowWhenProperty` to render a section only when a bool property is true:

```csharp
[MarkoutSection(Name = "Warnings", ShowWhenProperty = nameof(HasWarnings))]
public List<string>? Warnings { get; set; }

[MarkoutSection(Name = "Errors", ShowWhenProperty = nameof(HasErrors))]
public List<string>? Errors { get; set; }
```

When `HasErrors` is `false`, the entire Errors section — heading and content — is omitted.

### Empty-State Fallback

Use `EmptyText` to render a fallback paragraph when a section's collection is **non-null but empty**. The heading is still emitted, followed by the fallback text in place of the table or list:

```csharp
[MarkoutSection(Name = "Callers", EmptyText = "No callers found in this assembly.")]
public List<CallerRow>? Callers { get; set; }
```

| `Callers` value | Output |
|-----------------|--------|
| non-empty list | `## Callers` + table |
| empty list (`[]`) | `## Callers` + paragraph `No callers found in this assembly.` |
| `null` | section omitted entirely |

This makes the section a union of "table OR fallback description." Because a `null` collection still omits the section, the caller controls whether the fallback appears by choosing an empty collection over `null` — useful when a section is explicitly requested and you want to confirm "nothing found" rather than rendering silence.

`EmptyText` applies to every count-gated collection section (tables, string lists, fields, trees, metrics, descriptions, breakdowns) and composes with `ShowWhenProperty` (a `false` guard still omits the section, fallback included).

### Collection Truncation

Use `[MarkoutMaxItems]` to limit the number of items rendered, with an ellipsis message:

```csharp
[MarkoutMaxItems(5)]
public List<ActorRow>? Actors { get; set; }

[MarkoutMaxItems(10, EllipsisFormat = "({0} more not shown)")]
public List<string>? Files { get; set; }
```

The default ellipsis text is `"... and {0} more"` where `{0}` is the remaining count.

### Section Level

Sections default to heading level 2 (`##`). Set `Level` to change:

```csharp
[MarkoutSection(Name = "Details", Level = 3)]
public List<DetailRow>? Details { get; set; }
```

### String Collections

A `List<string>` section renders as a bullet list:

```csharp
[MarkoutSection(Name = "Files")]
public List<string>? Files { get; set; }
```

```markdown
## Files

- Foo.cs
- Bar.cs
- Baz.cs
```

### Field Collections

Use `List<MarkoutField>` for dynamic key-value fields built at runtime:

```csharp
[MarkoutSerializable(AutoFields = false)]
public class DynamicReport
{
    [MarkoutSection(Name = "Metadata", FieldOrder = MarkoutFieldOrder.Alphabetical)]
    public List<MarkoutField>? Fields { get; set; }
}
```

### Section Field Order

Field-style sections preserve input order by default. Use
`FieldOrder = MarkoutFieldOrder.Alphabetical` on `[MarkoutSection]` when a
metadata inventory is easier to scan alphabetically:

```csharp
[MarkoutSection(Name = "Package Info", FieldOrder = MarkoutFieldOrder.Alphabetical)]
public List<MarkoutField>? PackageInfo { get; set; }
```

`FieldOrder` applies to `List<MarkoutField>` sections and scalar properties
grouped into the same section name. It does not sort arbitrary data-table rows.

## Tables

When a `List<T>` of complex objects is part of a section, Markout renders it as a table. Each public scalar property of the element type becomes a column.

Properties that cannot be rendered in table context (nested objects, arrays) emit a `MARKOUT001` compile-time warning. Use `[MarkoutIgnoreInTable]` to acknowledge and suppress:

```csharp
[MarkoutSerializable]
public class CveReport
{
    public string Span { get; set; } = "";

    [MarkoutIgnoreInTable]
    public List<TreeNode>? Releases { get; set; }
}
```

### Custom Column Names

Use `ColumnName` on the section attribute to override the column header when a property is rendered as a table column in another context:

```csharp
[MarkoutSection(Name = "Assemblies", ColumnName = "Assembly")]
public List<AssemblyRow>? Assemblies { get; set; }
```

### Suppressing Table Warnings

If you register many types and most table warnings are expected, use `[MarkoutContextOptions]`:

```csharp
[MarkoutContextOptions(SuppressTableWarnings = true)]
[MarkoutContext(typeof(MyReport))]
public partial class MyContext : MarkoutSerializerContext { }
```

## Trees

Use `List<TreeNode>` for hierarchical data with box-drawing characters:

```csharp
var tree = new[]
{
    new TreeNode("CEO", new[]
    {
        new TreeNode("VP Engineering", new[]
        {
            new TreeNode("Dev Team Lead"),
            new TreeNode("QA Team Lead")
        }),
        new TreeNode("VP Sales")
    })
};

writer.WriteTree(tree);
```

```
└─ CEO
   ├─ VP Engineering
   │  ├─ Dev Team Lead
   │  └─ QA Team Lead
   └─ VP Sales
```

`TreeNode` supports optional icons:

```csharp
new TreeNode("Critical Issue", "🔴")
new TreeNode("Warning", "🟡")
```

> The [LatestCves](../samples/LatestCves/LatestCves.cs) sample uses trees with icons to display CVE severity.

## Links

Use `[MarkoutLink]` to render a string property as a Markdown link:

```csharp
// Bare link: [url](url)
[MarkoutLink]
[MarkoutSkipNull]
public string? Homepage { get; set; }

// Named link: [Name](url) — text comes from another property
[MarkoutLink(TextProperty = nameof(Name))]
[MarkoutSkipNull]
public string? Repository { get; set; }
```

Output:

```markdown
Homepage: [https://github.com/richlander/dotnet-inspect](https://github.com/richlander/dotnet-inspect)
Repository: [dotnet-inspect](https://github.com/richlander/dotnet-inspect.git)
```

Link formatting works in all field layouts (Inline, Vertical, Bulleted) and in table cells.

> From the [AdvancedFeatures](../samples/Serialization/AdvancedFeatures.cs) sample.

## Custom Value Formatters

For transformations beyond format strings — like compact number display (`"1.2M"`) or byte sizes (`"4.3 MB"`) — implement `IMarkoutValueFormatter<T>`:

```csharp
public class CompactNumberFormatter : IMarkoutValueFormatter<long>
{
    public string Format(long value) => value switch
    {
        >= 1_000_000_000 => $"{value / 1_000_000_000.0:0.#}B",
        >= 1_000_000 => $"{value / 1_000_000.0:0.#}M",
        >= 1_000 => $"{value / 1_000.0:0.#}K",
        _ => value.ToString()
    };
}

public class ByteSizeFormatter : IMarkoutValueFormatter<long>
{
    public string Format(long value) => value switch
    {
        >= 1_073_741_824 => $"{value / 1_073_741_824.0:0.#} GB",
        >= 1_048_576 => $"{value / 1_048_576.0:0.#} MB",
        >= 1_024 => $"{value / 1_024.0:0.#} KB",
        _ => $"{value} B"
    };
}
```

Apply with `[MarkoutValueFormatter]`:

```csharp
[MarkoutValueFormatter(typeof(CompactNumberFormatter))]
public long TotalDownloads { get; set; }  // "5.1B"

[MarkoutValueFormatter(typeof(ByteSizeFormatter))]
public long PackageSize { get; set; }  // "4.3 MB"
```

The formatter's `Format()` method is called in the generated code. It takes priority over all other formatting attributes.

## Writer Options

`MarkoutWriterOptions` controls rendering behavior. Options can be set per-context or per-call.

### Bold Field Names

```csharp
var options = new MarkoutWriterOptions { BoldFieldNames = true };
// **Category:** Electronics
```

### Section Filtering

Include or exclude sections by heading name:

```csharp
// Only render these sections
var options = new MarkoutWriterOptions
{
    IncludeSections = ["Overview", "Reviews"]
};
```

> From the [SectionFiltering](../samples/Serialization/SectionFiltering.cs) sample.

### Heading Level Offset

`HeadingLevelOffset` shifts every rendered heading by a fixed amount (default
`0`). Set it to `1` to render a document as a nested section — "print a section,
elide the H1" — so its title drops from `#` to `##` and any sections shift down
with it. This lets serialized output be appended under an existing document
without introducing a second top-level heading:

```csharp
var options = new MarkoutWriterOptions { HeadingLevelOffset = 1 };
// # Skills        ->  ## Skills
// ## Subsection   ->  ### Subsection
```

Rendered levels are clamped to the valid `1`–`6` range. The offset only affects
rendered heading depth; logical section identity (used by `IncludeSections`)
is unchanged.

### Context-Level Options

Set defaults via `[MarkoutContextOptions]`:

```csharp
[MarkoutContextOptions(BoldFieldNames = true, SuppressTableWarnings = true)]
[MarkoutContext(typeof(MyReport))]
public partial class MyContext : MarkoutSerializerContext { }
```

Or pass options to the context constructor:

```csharp
var context = new SampleContext(new MarkoutWriterOptions
{
    IncludeSections = ["Overview", "Reviews"],
    BoldFieldNames = true
});

string markdown = MarkoutSerializer.Serialize(product, context);
```

## Table, TSV, and JSONL Output

`TableFormatter` renders the same table projection in compact, line-oriented forms:

- `MarkoutTableMode.Pretty` (default) uses display labels and aligns columns with spaces.
- `MarkoutTableMode.Tsv` emits normalized tab-separated rows with stable snake_case headers derived from source property names.
- `MarkoutTableMode.Jsonl` emits one JSON object per table row with stable snake_case property names derived from source property names.

```csharp
// Pretty compact table
MarkoutSerializer.Serialize(report, Console.Out, new TableFormatter(), context);

// TSV with stable headers
var options = new MarkoutWriterOptions { TableMode = MarkoutTableMode.Tsv };
MarkoutSerializer.Serialize(report, Console.Out, new TableFormatter(), context, options);

// JSONL with stable property names
var jsonlOptions = new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl };
MarkoutSerializer.Serialize(report, Console.Out, new TableFormatter(), context, jsonlOptions);
```

Header style is configurable. `Auto` uses display names for pretty tables and stable names for TSV and JSONL.

Semantic inline tags let producers describe presentation without hard-coding Markdown. Use `<code>...</code>` in string values when the content is code-like or literal. Markdown renders it as a code span, while pretty table, TSV, and JSONL output emit the decoded text without tags or backticks.

```csharp
writer.WriteTable(
    ["Select", "Signature"],
    [["<code>Serialize:1</code>", "<code>string Serialize&lt;T&gt;(T value)</code>"]]);
```

```csharp
var options = new MarkoutWriterOptions
{
    TableMode = MarkoutTableMode.Tsv,
    TableHeaderStyle = MarkoutTableHeaderStyle.DisplayName
};
```

For custom policies, use `FormatTableHeader`. The callback receives both the stable source name and the display name.

```csharp
var options = new MarkoutWriterOptions
{
    FormatTableHeader = header => $"{header.Index}:{header.Name}"
};
```

## Low-Level Writer API

`MarkoutWriter` provides direct control when the source generator isn't sufficient.

### Fields and Headings

```csharp
var writer = new MarkoutWriter();

writer.WriteHeading(1, "Product Report");
writer.WriteField("Product", "Widget Pro");
writer.WriteField("Price", 99.99m);
writer.WriteField("In Stock", true);
```

### Arrays

```csharp
writer.WriteArray("Features", new[] { "Durable", "Lightweight", "Waterproof" });
```

```markdown
Features:
- Durable
- Lightweight
- Waterproof
```

### Tables

```csharp
writer.WriteTableStart("Product", "Category", "Price");
writer.WriteTableRow("Widget A", "Electronics", "$29.99");
writer.WriteTableRow("Widget B", "Electronics", "$49.99");
writer.WriteTableEnd();
```

### Inline Fields

```csharp
writer.WriteFieldsInline(
    new MarkoutField("Name", "Widget"),
    new MarkoutField("Price", "$29.99"),
    new MarkoutField("Stock", "Yes")
);
// Name: Widget | Price: $29.99 | Stock: Yes
```

### Code

```csharp
writer.WriteCodeStart("json");
writer.WriteParagraph("{ \"key\": \"value\" }");
writer.WriteCodeEnd();
```

> From the [WriterUsage](../samples/Serialization/WriterUsage.cs) sample.

## Attribute Reference

| Attribute | Target | Purpose |
|-----------|--------|---------|
| `[MarkoutSerializable]` | Class/Struct | **Optional.** Customizes type serialization. Properties: `TitleProperty`, `TitleContextProperty`, `DescriptionProperty`, `AutoFields`, `FieldLayout`. Not needed for simple types — registration via `[MarkoutContext]` is sufficient. |
| `[MarkoutContext(typeof(T))]` | Context class | **Required.** Registers a type with a serializer context. Apply multiple times for multiple types. |
| `[MarkoutContextOptions]` | Context class | Sets default options on a context. Properties: `BoldFieldNames`, `IncludeBadges`, `IncludeDescription`, `SuppressTableWarnings`. |
| `[MarkoutPropertyName("...")]` | Property | Sets the display name for a field or column. |
| `[MarkoutIgnore]` | Property | Excludes the property from all output. |
| `[MarkoutIgnoreInTable]` | Property | Excludes the property in table context only. |
| `[MarkoutSection]` | Property | Renders the property under a headed section. Works on scalar properties (grouped by name) and collections (tables, lists, trees). Properties: `Name`, `Level`, `ShowWhenProperty`, `FieldOrder`, `IgnoreProperty`, `FormatProperty`, `Formatter`, `ColumnName`. |
| `[MarkoutBoolFormat("T", "F")]` | Property | Custom true/false display strings. |
| `[MarkoutFormat("...")]` | Property | .NET format string passed to `ToString()`. |
| `[MarkoutDisplayFormat("...")]` | Property | Composite format string via `string.Format()`. `{0}` is the value. |
| `[MarkoutTableDisplay("...")]` | Property | Format string used only in table columns. |
| `[MarkoutJoin("...")]` | Property | Joins a string collection into a single field value. |
| `[MarkoutSkipNull]` | Property | Omits the field when null or empty. Does not skip `false` or `0`. |
| `[MarkoutSkipDefault]` | Property | Omits the field when it equals the type's default value. |
| `[MarkoutShowWhen(nameof(...))]` | Property | Shows the field only when the referenced bool property is `true`. |
| `[MarkoutLink]` | Property | Renders a string as a Markdown `[url](url)` link. Set `TextProperty` for `[text](url)`. |
| `[MarkoutMaxItems(n)]` | Property | Limits collection rendering to `n` items. Set `EllipsisFormat` to customize the overflow message. |
| `[MarkoutValueFormatter(typeof(...))]` | Property | Uses a custom `IMarkoutValueFormatter<T>` to format the value. |
| `[MarkoutValueMap("k=v", ...)]` | Property | Maps string values to badge-prefixed output. Each entry is `"value=badge"`. Unmatched values pass through unchanged. |
