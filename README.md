# Markout

**Markup adds instructions to content. Markout removes structure from data.**

Markout is a source-generated .NET serializer that projects objects into human-readable documents instead of data formats like JSON. You annotate view models with attributes that describe data relationships — identity, enumeration, tabulation, measurement, hierarchy — and the source generator emits code that writes through an abstract renderer. The same object graph produces Markdown tables, ANSI terminal output with colored bars, plain text with aligned columns, or one-line summaries, without the developer making visual decisions.

The name captures the philosophy: where markup layers formatting instructions onto content, Markout works in the opposite direction, stripping an object graph down to what the data *is* — a measurement, a breakdown, a hierarchy — and letting the renderer decide what it *looks like*. The word also nods to an older tradition. Long before digital markup languages, typesetters performed two complementary acts: marking *up* a manuscript with rendering instructions, and marking *out* content that didn't belong in the final form. Computing formalized markup into an entire paradigm (GML, SGML, HTML, XML) but largely forgot its counterpart. Markout reclaims that half of the craft.

## Two Lines of Code

```csharp
// Define a view model
[MarkoutSerializable(TitleProperty = nameof(Title))]
public class CityReport
{
    [MarkoutIgnore] public string Title { get; set; } = "";
    public string Province { get; set; } = "";
    public int Population { get; set; }

    [MarkoutSection(Name = "Landmarks")]
    public List<LandmarkRow>? Landmarks { get; set; }
}

[MarkoutSerializable]
public class LandmarkRow
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public int Year { get; set; }
}

[MarkoutContext(typeof(CityReport))]
public partial class ReportContext : MarkoutSerializerContext { }

// Serialize — one line
MarkoutSerializer.Serialize(city, Console.Out, ReportContext.Default);
```

**Markdown output:**

```markdown
# Vancouver

Province: British Columbia
Population: 2632000

## Landmarks

| Name             | Type       | Year |
| ---------------- | ---------- | ---- |
| Stanley Park     | Park       | 1888 |
| Gastown          | Historic   | 1867 |
| Science World    | Museum     | 1989 |
```

Same object, different renderer:

```csharp
// ANSI terminal — colored headings, bold field names
MarkoutSerializer.Serialize(city, Console.Out, ReportContext.Default, new AnsiWriter(Console.Out));

// One-line summary — tables only, no headings or fields
MarkoutSerializer.Serialize(city, Console.Out, ReportContext.Default, new OneLineWriter(Console.Out));
```

## Shape Library

Markout is a serializer for a **shape library**. Each property on a view model maps to a data relationship, not a visual element. Renderers decide how to present each shape.

| Relationship | C# type | What it means | Markdown | ANSI |
|---|---|---|---|---|
| **Identity** | `string`, `int`, `bool` | Named value | `Key: value` | Bold key, value |
| **Enumeration** | `string[]` | Sequence of items | `- item` | Bullet list |
| **Tabulation** | `List<T>` | Uniform records | `\| col \| col \|` | Space-padded table |
| **Section** | `[MarkoutSection]` | Logical grouping | `## Heading` | Colored heading |
| **Description** | `List<Description>` | Terms with explanations | `- **Term:** text` | Bold term, text |
| **Measurement** | `List<Metric>` | Comparative quantities | `Label ████░░ 45` | Colored bars |
| **Composition** | `List<Breakdown>` | Parts of a whole | `██▓▓▒░` stacked | Colored segments |
| **Hierarchy** | `List<TreeNode>` | Parent-child structure | `├── node` | Box-drawing tree |
| **Quotation** | `CodeSection` | Verbatim content | ` ```code``` ` | Syntax display |
| **Attention** | `Callout` | Important messages | `> [!WARNING]` | Colored label |

Plus structural shapes: **Blockquote** (prose quotation), **Matrix** (2D pivot grid), **Pairs** (aligned name-value), **HorizontalRule** (section separator).

### Record Types

Shapes that need structured input provide record types named for what the data *is*, not what it *looks like*:

```csharp
new Metric("Build Time", 4.2)                                    // measurement
new Description("dotnet-inspect", "API surface inspection tool")  // term + explanation
new Breakdown("Jan 2025", [new("Critical", 1), new("High", 3)])  // proportional composition
new Callout(CalloutSeverity.Warning, "3 vulnerabilities found")   // attention
new CodeSection("csharp", "public class Foo { }")                 // verbatim content
```

## Renderers

Markout ships four renderers. The serializer writes through `MarkoutWriter` — swap the writer, change the output.

| Renderer | Output | Use case |
|---|---|---|
| **MarkdownWriter** | GitHub-Flavored Markdown | Documentation, LLM tool output, rendered reports |
| **MarkoutWriter** | Plain text, space-padded | Log files, piped output, terminals without ANSI |
| **OneLineWriter** | Tables only, no headings | Compact summaries, grep-friendly output |
| **DiagramWriter** | Trees and structural diagrams | Dependency graphs, file trees |

Optional packages:

| Package | Renderer | Use case |
|---|---|---|
| **Markout.Ansi** | `AnsiWriter` | Colored terminal output with bold, gradients |
| **Markout.Ansi.Spectre** | `SpectreWriter` | Rich terminal UI via Spectre.Console |

Renderers declare which shapes they support via `SupportedShapes`. Unsupported shapes are silently skipped — the data is never lost, only the visual sophistication changes.

## Customization Layers

Markout provides multiple layers of control, from zero-config to full custom:

**Layer 1 — Attributes** (compile-time): Control what's rendered and how.

```csharp
[MarkoutPropertyName("Born")]          // rename a field
[MarkoutSkipNull]                      // hide when null
[MarkoutSection(Name = "Details")]     // group into a section
[MarkoutDisplayFormat("N0")]           // format numbers
[MarkoutShowWhen(nameof(HasDetails))]  // conditional rendering
[MarkoutMaxItems(10)]                  // truncate long lists
```

**Layer 2 — Writer Options** (runtime): Control which sections appear.

```csharp
var options = new MarkoutWriterOptions
{
    IncludeSections = new HashSet<string> { "Summary", "Errors" },  // only these sections
    BoldFieldNames = true
};
var writer = new MarkdownWriter(Console.Out, options);
```

**Layer 3 — Renderer Subclass** (code): Override any shape for custom visual treatment.

```csharp
public class MyWriter : MarkdownWriter
{
    protected override void WriteDescription(Description item)
    {
        // Custom rendering for descriptions
        Writer.WriteLine($"📌 {item.Term}: {item.Text}");
    }
}
```

## Installation

```bash
dotnet add package Markout
```

The package includes the source generator — no additional packages needed.

## Samples

- **[CanadianContent](samples/CanadianContent)** — Canadian actors and shows with tables, trees, metrics, and multiple renderers
- **[LatestCves](samples/LatestCves)** — .NET security advisories with trees and severity breakdowns
- **[DotNetReleases](samples/DotNetReleases)** — .NET release information from GitHub
- **[Serialization](samples/Serialization)** — Shape gallery, section filtering, and writer API examples

## Real-World Usage

Markout was created for [dotnet-inspect](https://github.com/richlander/dotnet-inspect), which uses all ten data relationships across 49 view models to generate API inspection reports, diff analysis, dependency trees, and security summaries.

## Documentation

- **[User Guide](docs/user-guide.md)** — Complete tutorial with attribute reference
- **[Shape System Design](docs/design/shape-system.md)** — Data projection model, shape tiers, admission criteria
- **[Specification](docs/specification.md)** — Format grammar and type inference rules
- **[Nested Lists Guide](docs/nested-lists-guide.md)** — Strategies for nested data structures

## License

MIT
