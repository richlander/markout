# Markout

**Markup adds instructions to content. Markout removes structure from data.**

Markout is a source-generated .NET serializer that projects objects into structured, human-readable documents. You annotate your models with attributes that describe data relationships — identity, enumeration, tabulation, measurement, hierarchy — and the source generator emits code that writes through an abstract renderer. The same object graph produces Markdown tables, ANSI terminal output with colored bars, plain text with aligned columns, or one-line summaries, without the developer making visual decisions.

## Quick Start

```csharp
using Markout;

var artist = new Artist(
    Name: "Sarah McLachlan",
    Genre: "Pop / Adult Contemporary",
    Origin: "Halifax, Nova Scotia",
    DebutYear: 1988,
    BestKnownFor: "Angel, Building a Mystery, Adia");

MarkoutSerializer.Serialize(artist, Console.Out, ArtistContext.Default);

[MarkoutSerializable(TitleProperty = nameof(Artist.Name))]
public record Artist(
    string Name,
    string Genre,
    string Origin,
    int DebutYear,
    string BestKnownFor);

[MarkoutContext(typeof(Artist))]
public partial class ArtistContext : MarkoutSerializerContext { }
```

**Output:**

```markdown
# Sarah McLachlan

| Field | Value |
| ----- | ----- |
| Genre | Pop / Adult Contemporary |
| Origin | Halifax, Nova Scotia |
| Debut Year | 1988 |
| Best Known For | Angel, Building a Mystery, Adia |
```

Three things: a record, a context, one line of serialization. The `TitleProperty` becomes a heading; everything else renders as a field table.

## Field Layouts

The same model renders differently with `FieldLayout`. The default is a two-column table. Switch to `Inline` for a compact summary line:

```csharp
[MarkoutSerializable(TitleProperty = nameof(Artist.Name), FieldLayout = FieldLayout.Inline)]
public record Artist( ... );
```

```markdown
# Sarah McLachlan

Genre: Pop / Adult Contemporary | Origin: Halifax, Nova Scotia | Debut Year: 1988 | Best Known For: Angel, Building a Mystery, Adia
```

Or `Bulleted` for a list:

```csharp
[MarkoutSerializable(TitleProperty = nameof(Artist.Name), FieldLayout = FieldLayout.Bulleted)]
public record Artist( ... );
```

```markdown
# Sarah McLachlan

- Genre: Pop / Adult Contemporary
- Origin: Halifax, Nova Scotia
- Debut Year: 1988
- Best Known For: Angel, Building a Mystery, Adia
```

Or `Numbered`:

```markdown
# Sarah McLachlan

1. Genre: Pop / Adult Contemporary
2. Origin: Halifax, Nova Scotia
3. Debut Year: 1988
4. Best Known For: Angel, Building a Mystery, Adia
```

Or `Plain` — bare lines, no markers. Each line ends with two trailing spaces, which is the markdown signal for `<br>`:

```markdown
# Sarah McLachlan

Genre: Pop / Adult Contemporary  
Origin: Halifax, Nova Scotia  
Debut Year: 1988  
Best Known For: Angel, Building a Mystery, Adia  
```

The data model doesn't change — only the attribute controls the shape.

## Adding Sections and Tables

Add `[MarkoutSection]` to group properties with headings, and use `List<T>` for tables:

```csharp
[MarkoutSerializable(TitleProperty = nameof(Title))]
public class CityReport
{
    public string Title { get; set; } = "";
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
[MarkoutContext(typeof(LandmarkRow))]
public partial class ReportContext : MarkoutSerializerContext { }

MarkoutSerializer.Serialize(city, Console.Out, ReportContext.Default);
```

**Output:**

```markdown
# Vancouver

| Field | Value |
| ----- | ----- |
| Province | British Columbia |
| Population | 2632000 |

## Landmarks

| Name | Type | Year |
| ----- | ----- | ----- |
| Stanley Park | Park | 1888 |
| Gastown | Historic | 1867 |
| Science World | Museum | 1989 |
```

## Real-World Example: GitHub Repository Report

The [GitHubRepo](samples/GitHubRepo) sample fetches four GitHub API endpoints in parallel and projects the combined JSON into a single report — fields, bar charts, contributor metrics, and release tables — all from one model:

```csharp
[MarkoutSerializable(TitleProperty = nameof(Title), DescriptionProperty = nameof(Description))]
public class RepoInfo
{
    public string Title { get; set; } = "";

    [MarkoutIgnore]
    public string Description { get; set; } = "";

    [MarkoutDisplayFormat("{0:N0}")]
    public int Stars { get; set; }

    [MarkoutDisplayFormat("{0:N0}")]
    public int Forks { get; set; }

    [MarkoutDisplayFormat("{0:N0}")]
    public int OpenIssues { get; set; }

    public string Language { get; set; } = "";
    public string License { get; set; } = "";

    [MarkoutIgnoreInTable]
    [MarkoutSkipDefault]
    public Callout ArchivedWarning { get; set; }

    [MarkoutSection(Name = "Languages")]
    [MarkoutIgnoreInTable]
    public List<Breakdown>? Languages { get; set; }

    [MarkoutSection(Name = "Top Contributors")]
    [MarkoutIgnoreInTable]
    public List<Metric>? TopContributors { get; set; }

    [MarkoutSection(Name = "Releases")]
    public List<ReleaseRow>? Releases { get; set; }
}
```

Run it:

```bash
dotnet run samples/GitHubRepo/GitHubRepo.cs                                     # ANSI terminal (default)
dotnet run samples/GitHubRepo/GitHubRepo.cs -- dotnet/runtime --format markdown # Markdown
dotnet run samples/GitHubRepo/GitHubRepo.cs -- dotnet/runtime --format oneline  # Compact table
```

**Markdown output** for `dotnet/runtime`:

```markdown
# dotnet/runtime

.NET is a cross-platform runtime for cloud, mobile, desktop, and IoT apps.

| Field | Value |
| ----- | ----- |
| Stars | 17,703 |
| Forks | 5,353 |
| Open Issues | 8,371 |
| Language | C# |
| License | MIT License |

## Languages

| Category | Count | % |
| -------- | ----- | - |
| C# | 80 | 83 |
| C++ | 9 | 9 |
| C | 7 | 7 |

## Top Contributors

| Label | Value |
| ----- | ----- |
| vargaz | 11910 |
| stephentoub | 10418 |
| kumpera | 4074 |
| jkotas | 3245 |

## Releases

| Tag | Name | Published |
| --- | ---- | --------- |
| v8.0.24 | .NET 8.0.24 | 2026-02-10 |
| v9.0.13 | v9.0.13 | 2026-02-10 |
| v10.0.3 | .NET 10.0.3 | 2026-02-10 |
```

**One-line output** — same model, `--oneline` flag:

```text
FIELD        VALUE
Stars        17,703
Forks        5,353
Open Issues  8,371
Language     C#
License      MIT License

TAG      NAME         PUBLISHED
v8.0.24  .NET 8.0.24  2026-02-10
v9.0.13  v9.0.13      2026-02-10
v10.0.3  .NET 10.0.3  2026-02-10
```

The pattern is always the same: deserialize your API data, project to a model, serialize. Markout handles the rest.

## Shape Library

Each property on a model maps to a data relationship, not a visual element. Renderers decide how to present each shape.

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

Plus structural shapes: **Quotation** (prose quotation), **Matrix** (2D pivot grid), **Rule** (section separator).

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

Markout ships five formatters. The serializer writes through `MarkoutWriter` (the orchestrator) — swap the formatter, change the output.

| Formatter | Output | Use case |
|---|---|---|
| **MarkdownFormatter** | GitHub-Flavored Markdown | Documentation, LLM tool output, rendered reports |
| **PlainTextFormatter** | Plain text without markup | Minimal output, no syntax characters |
| **UnicodeFormatter** | Box-drawing characters | Rich tables with borders, no color |
| **OneLineFormatter** | Tables only, no headings | Compact summaries, grep-friendly output |
| **DiagramFormatter** | Trees and structural diagrams | Dependency graphs, file trees |

Optional packages:

| Package | Formatter | Use case |
|---|---|---|
| **Markout.Ansi** | `AnsiFormatter` | Colored terminal output with bold, gradients |
| **Markout.Ansi.Spectre** | `SpectreFormatter` | Rich terminal UI via Spectre.Console |

Formatters declare which shapes they support by implementing capability interfaces (`ITableFormatter`, `IFieldFormatter`, `ITreeFormatter`, etc.). Unsupported shapes are silently skipped — the data is never lost, only the visual sophistication changes.

## Templates

Templates let you author document structure in Markdown and fill data slots at runtime. The same template renders to any format — Markdown, plain text, or ANSI terminal.

**template.md:**

```markdown
# .NET Security Report for {{date}}

The following vulnerabilities were disclosed this month.

{{vuln-table}}

| Level | CVSS Range | Response |
| ----- | ---------- | -------- |
| Critical | 9.0–10.0 | Patch immediately |
| High | 7.0–8.9 | Patch within 30 days |

{{#if commits}}
## Commits

{{commit-table}}
{{/if}}
```

**Program.cs:**

```csharp
using Markout;
using Markout.Templates;

var template = MarkoutTemplate.Load("template.md");
template.TableOptions = new TableFormatterOptions(); // smooth column widths

template.Bind("date", "February 2026");
template.Bind("vuln-table", vulnerabilityData);  // IMarkoutFormattable
template.Bind("commits", hasCommits ? "yes" : null);
template.Bind("commit-table", commitData);

// Markdown output
Console.WriteLine(template.Render(new MarkoutWriterOptions { PrettyTables = true }));

// Plain text output — same template, different formatter
var plainWriter = new MarkoutWriter(Console.Out, new UnicodeFormatter());
template.Render(plainWriter);
```

Templates support:

- **`{{key}}`** — inline substitution in headings and prose, or block-level data rendering
- **`{{#if key}}`** / **`{{/if}}`** — conditional sections (included when key is bound and non-null)
- **Pipe tables** — parsed and re-rendered through the writer's table shape, with optional statistical column-width optimization
- **`IMarkoutFormattable`** and **`MarkoutTypeInfo<T>`** bindings for shape-aware data rendering

```bash
dotnet add package Markout.Templates
```

### Template Runner Tool

A basic CLI tool is included for rendering templates with string bindings:

```bash
dotnet run --project tools/markout-template -- template.md date="February 2026" title="Report"
```

Unbound block placeholders are skipped, so you can render partial templates. Pipe `key=value` lines on stdin for additional bindings.

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
[MarkoutValueMap("k=badge", ...)]      // map values to badge-prefixed output
[MarkoutUnwrap]                        // inline collection items without section heading
[MarkoutIgnoreColumnWhen(...)]         // conditionally hide table columns
```

**Layer 2 — Writer Options** (runtime): Control which sections appear.

```csharp
var options = new MarkoutWriterOptions
{
    IncludeSections = new HashSet<string> { "Summary", "Errors" },  // only these sections
    BoldFieldNames = true
};
MarkoutSerializer.Serialize(view, Console.Out, new MarkdownFormatter(), context, options);
```

**Layer 3 — Custom Formatter** (code): Implement capability interfaces for custom rendering.

```csharp
public class MyFormatter : IMarkoutFormatter, IFieldFormatter, ITableFormatter
{
    // Implement only the interfaces your formatter supports
    void IFieldFormatter.FormatFieldName(TextWriter w, string key, bool bold) { ... }
    void ITableFormatter.FormatTable(TextWriter w, ReadOnlySpan<string> headers, IList<string[]> rows, int skippedRows, MarkoutWriterOptions options) { ... }
}
```

## Installation

```bash
dotnet add package Markout
```

The package includes the source generator — no additional packages needed.

## Samples

- **[HelloMarkout](samples/HelloMarkout)** — Simplest possible example: a class, a context, one line of serialization
- **[RecordDemo](samples/RecordDemo)** — Records as data models
- **[GitHubRepo](samples/GitHubRepo)** — GitHub API → fields, breakdowns, metrics, and tables with Spectre/Markdown/OneLineFormatter output
- **[GitHubActivity](samples/GitHubActivity)** — User profile and recent events from the GitHub API
- **[CanadianContent](samples/CanadianContent)** — Canadian actors and shows with tables, trees, metrics, and multiple renderers
- **[LatestCves](samples/LatestCves)** — .NET security advisories with trees and severity breakdowns
- **[DotNetReleases](samples/DotNetReleases)** — .NET release information from GitHub
- **[Serialization](samples/Serialization)** — Shape gallery, section filtering, and writer API examples
- **[TemplateDemo](samples/TemplateDemo)** — Document template with inline tables, conditional sections, and multi-format rendering

## Formats and Agents

Agents and LLMs are skilled at two things: using tools and reading text. No single text format is best in all cases — a tool that can produce results in multiple formats is more useful to an agent than one locked to a single output.

JSON and Markout are peer options. JSON carries data between systems — APIs, config, round-trip serialization. Markout carries data to readers — humans and agents consuming structured documents. A tool like [dotnet-inspect](https://github.com/richlander/dotnet-inspect) gathers data from NuGet packages, assemblies, and APIs, projects it into Markout models, and offers both `--json` and Markout output. The same section metadata, schema discovery, and projection features enable control and customization upstream of either format generation.

Tools that produce a lot of text need standard controls to segment it. Verbosity levels are useful but blunt — they control *how much* without letting you say *which parts*. Markout's section selection (`-s Dependencies`) and column/field projection (`--columns Name,Version`) are sharper instruments, offering the kind of targeted trimming that [Go templates](https://pkg.go.dev/text/template) provide in other ecosystems, but without requiring users to learn a template language. You say what data to keep; the renderer decides how to present it.

Tools built on a shared output scheme improve together. When eval sessions reveal that a heading level, table layout, or field ordering works better for agents, a new Markout version carries that improvement to every tool that uses it.

## For Coding Agents

If you are an LLM or coding agent building a CLI tool that needs structured, readable output, see **[SKILL.md](SKILL.md)** for step-by-step integration instructions and attribute reference.

## Real-World Usage

Markout was created for [dotnet-inspect](https://github.com/richlander/dotnet-inspect), which uses all ten data relationships across 49 models to generate API inspection reports, diff analysis, dependency trees, and security summaries.

## Documentation

- **[User Guide](docs/user-guide.md)** — Complete tutorial with attribute reference
- **[Shape System Design](docs/design/shape-system.md)** — Data projection model, shape tiers, admission criteria
- **[Specification](docs/specification.md)** — Format grammar and type inference rules
- **[Nested Lists Guide](docs/nested-lists-guide.md)** — Strategies for nested data structures

## License

MIT
