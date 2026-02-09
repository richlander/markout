# Markout User Guide

Markout is a source-generated .NET library that serializes objects to clean, readable Markdown. Define your view models with attributes, and Markout generates efficient serialization code at compile time — no reflection, no runtime overhead.

- [Quick Start](#quick-start)
- [Defining View Models](#defining-view-models)
- [Serialization](#serialization)
- [Scalar Fields](#scalar-fields)
- [Field Layout](#field-layout)
- [Formatting Values](#formatting-values)
- [Conditional Rendering](#conditional-rendering)
- [Sections and Collections](#sections-and-collections)
- [Tables](#tables)
- [Trees](#trees)
- [Links](#links)
- [Custom Value Formatters](#custom-value-formatters)
- [Writer Options](#writer-options)
- [Low-Level Writer API](#low-level-writer-api)
- [Attribute Reference](#attribute-reference)

## Quick Start

The simplest Markout program is a view model, a context, and a serialize call.

```csharp
using Markout;

var city = new CityView
{
    Name = "Vancouver",
    Country = "Canada",
    Temperature = 6.2,
    Latitude = 49.2827,
    Longitude = -123.1207,
    Altitude = 0
};

MarkoutSerializer.Serialize(city, Console.Out, CityContext.Default);

[MarkoutSerializable(TitleProperty = nameof(Name))]
public class CityView
{
    public string Name { get; set; } = "";
    public string Country { get; set; } = "";
    public double Temperature { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Altitude { get; set; }
}

[MarkoutContext(typeof(CityView))]
public partial class CityContext : MarkoutSerializerContext { }
```

Output:

```markdown
# Vancouver

Country: Canada | Temperature: 6.2 | Latitude: 49.2827 | Longitude: -123.1207 | Altitude: 0
```

> This is the [HelloMarkout](../samples/HelloMarkout/HelloMarkout.cs) sample.

Three things are required:

1. **A view model** — a class, optionally decorated with `[MarkoutSerializable]` for customization.
2. **A context** — a `partial class` inheriting `MarkoutSerializerContext` with `[MarkoutContext(typeof(...))]` for each type.
3. **A serialize call** — `MarkoutSerializer.Serialize(value, context)`.

The source generator fills in the `partial class` with all the serialization logic at compile time.

## Defining View Models

Markout has two kinds of attributes: **type-level attributes** on the view model class, and **property-level attributes** on individual properties.

A type becomes serializable when it is registered on a context class with `[MarkoutContext(typeof(T))]`. The `[MarkoutSerializable]` attribute on the type itself is **optional** — use it only when you need to customize behavior like `TitleProperty`, `FieldLayout`, or `AutoFields`. Without it, the type uses sensible defaults (all fields rendered, `OneLine` layout, no title heading).

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
public class ReleasesView
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
[MarkoutContext(typeof(ReleasesView))]
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
public class PackageView
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
public class ApiView
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

```csharp
var options = new MarkoutWriterOptions { BoldFieldNames = true };
string markdown = MarkoutSerializer.Serialize(product, SampleContext.Default, options);
```

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

The `FieldLayout` property controls how scalar fields are arranged. The default is `OneLine`.

### OneLine (default)

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

### LineBreaks

Each field on its own line:

```csharp
[MarkoutSerializable(TitleProperty = nameof(Name), FieldLayout = FieldLayout.LineBreaks)]
public class PackageView
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

### LineBreaksDoubleSpace

Like `LineBreaks` but appends two trailing spaces for Markdown hard line breaks. Useful when rendering in contexts that collapse single line breaks.

### List

Each field as a bullet list item:

```csharp
[MarkoutSerializable(FieldLayout = FieldLayout.List)]
public class ConfigView
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
public class PackageView
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
[MarkoutSerializable(TitleProperty = nameof(Name), FieldLayout = FieldLayout.LineBreaks)]
public class PackageView
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

## Sections and Collections

When a property is a `List<T>` of complex objects, it renders as a section with a heading and table. Use `[MarkoutSection]` to name the section:

```csharp
[MarkoutSerializable(TitleProperty = nameof(Title))]
public class CanConOverview
{
    public string Title { get; set; } = "";

    [MarkoutSection(Name = "Actors")]
    [MarkoutMaxItems(5)]
    public List<ActorRow>? Actors { get; set; }

    [MarkoutSection(Name = "Shows")]
    [MarkoutMaxItems(5)]
    public List<ShowRow>? Shows { get; set; }
}
```

Output:

```markdown
# Canadian Content Database

## Actors

| Name | Birthplace | Born | Citizenship |
|------|------------|------|-------------|
| Ryan Gosling | London, Ontario | 1980 | Canadian |
| Ryan Reynolds | Vancouver, BC | 1976 | Canadian, American |

... and 3 more

## Shows

| Title | Type | Years | Filmed In |
|-------|------|-------|-----------|
| The Expanse | TV Series | 2015-2022 | Toronto |

... and 4 more
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
public class DynamicView
{
    public List<MarkoutField>? Fields { get; set; }
}
```

## Tables

When a `List<T>` of complex objects is part of a section, Markout renders it as a table. Each public scalar property of the element type becomes a column.

Properties that cannot be rendered in table context (nested objects, arrays) emit a `MARKOUT001` compile-time warning. Use `[MarkoutIgnoreInTable]` to acknowledge and suppress:

```csharp
[MarkoutSerializable]
public class LatestCvesView
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
[MarkoutContext(typeof(MyView))]
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

Link formatting works in all field layouts (OneLine, LineBreaks, List) and in table cells.

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

// Render all sections except these
var options = new MarkoutWriterOptions
{
    ExcludeSections = ["Specifications"]
};
```

> From the [SectionFiltering](../samples/Serialization/SectionFiltering.cs) sample.

### Context-Level Options

Set defaults via `[MarkoutContextOptions]`:

```csharp
[MarkoutContextOptions(BoldFieldNames = true, SuppressTableWarnings = true)]
[MarkoutContext(typeof(MyView))]
public partial class MyContext : MarkoutSerializerContext { }
```

Or pass options to the context constructor:

```csharp
var context = new SampleContext(new MarkoutWriterOptions
{
    ExcludeSections = ["Specifications"],
    BoldFieldNames = true
});

string markdown = MarkoutSerializer.Serialize(product, context);
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

### Compact Fields

```csharp
writer.WriteCompactFields(
    new MarkoutField("Name", "Widget"),
    new MarkoutField("Price", "$29.99"),
    new MarkoutField("Stock", "Yes")
);
// Name: Widget | Price: $29.99 | Stock: Yes
```

### Code Blocks

```csharp
writer.WriteCodeBlockStart("json");
writer.WriteParagraph("{ \"key\": \"value\" }");
writer.WriteCodeBlockEnd();
```

> From the [WriterUsage](../samples/Serialization/WriterUsage.cs) sample.

## Attribute Reference

| Attribute | Target | Purpose |
|-----------|--------|---------|
| `[MarkoutSerializable]` | Class/Struct | **Optional.** Customizes type serialization. Properties: `TitleProperty`, `TitleContextProperty`, `DescriptionProperty`, `AutoFields`, `FieldLayout`. Not needed for simple types — registration via `[MarkoutContext]` is sufficient. |
| `[MarkoutContext(typeof(T))]` | Context class | **Required.** Registers a type with a serializer context. Apply multiple times for multiple types. |
| `[MarkoutContextOptions]` | Context class | Sets default options on a context. Properties: `BoldFieldNames`, `IncludeIcons`, `IncludeDescription`, `SuppressTableWarnings`. |
| `[MarkoutPropertyName("...")]` | Property | Sets the display name for a field or column. |
| `[MarkoutIgnore]` | Property | Excludes the property from all output. |
| `[MarkoutIgnoreInTable]` | Property | Excludes the property in table context only. |
| `[MarkoutSection]` | Property | Renders the property as a headed section. Properties: `Name`, `Level`, `ShowWhenProperty`, `IgnoreProperty`, `FormatProperty`, `Formatter`, `ColumnName`. |
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
