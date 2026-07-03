---
name: markout
description: >-
  Source-generated .NET serializer that renders objects as Markdown (also ANSI terminal,
  plain text, pretty tables, TSV). Reach for it when a CLI or tool needs structured, human-
  or agent-readable output instead of hand-built strings. It looks like System.Text.Json
  source generation but the rules differ — there is NO reflection fallback, so it requires a
  generated MarkoutSerializerContext and Markout-specific attributes. See the body for the
  required pattern.
---

# Markout — produce Markdown/structured output from objects

Source-generated serializer. Default output is Markdown. Package `Markout` (includes the
source generator; no extra package needed).

## The required pattern (3 parts — all mandatory)

```csharp
using Markout;

// 1. Annotate every model type. List<T> -> table, scalar -> field.
[MarkoutSerializable(TitleProperty = nameof(Title))]   // TitleProperty -> the H1 heading
public class Report
{
    public string Title { get; set; } = "";
    public int Count { get; set; }
    [MarkoutSection(Name = "Items")]                    // -> "## Items" heading
    public List<Row>? Items { get; set; }              // List<T> -> a table
    [MarkoutIgnoreInTable] public Callout Alert { get; set; } // ONE scalar Callout per alert -> "> [!WARNING]"; a List<Callout> renders a table
}

[MarkoutSerializable]
public class Row { public string Name { get; set; } = ""; public string Value { get; set; } = ""; }

// 2. Register EVERY type on a partial context (the source generator fills it in).
[MarkoutContext(typeof(Report))]
[MarkoutContext(typeof(Row))]
public partial class ReportContext : MarkoutSerializerContext { }

// 3. Serialize THROUGH the context.
MarkoutSerializer.Serialize(report, Console.Out, ReportContext.Default);
```

## Gotchas (where intuition from System.Text.Json is wrong)

- **No reflection fallback.** There is no `Serialize(obj)` overload. EVERY `MarkoutSerializer.Serialize`
  overload takes a `MarkoutSerializerContext` (or `MarkoutTypeInfo<T>`). Skipping the context does
  not compile. This is the #1 mistake.
- **Register every type.** A model used in output but missing `[MarkoutSerializable]` +
  `[MarkoutContext(typeof(T))]` will not serialize. The context class MUST be `partial`.
- **Attributes are Markout's, not System.Text.Json's:** `[MarkoutSerializable]` (not
  `[JsonSerializable]`), `[MarkoutContext]`, `[MarkoutSection(Name=...)]`, `TitleProperty` (H1) /
  `DescriptionProperty` (intro paragraph under the heading), `[MarkoutPropertyName]`, `[MarkoutIgnore]`. Do not use `Json*` attributes.
- **Rendering is driven by type, not markup:** `List<T>` -> table; scalar (`string`/`int`/`bool`) ->
  a `Field | Value` row; `[MarkoutSection(Name="X")]` -> a `## X` heading above the property.
- **Put `[MarkoutIgnoreInTable]` on non-tabular list properties** (`List<Metric>`, `List<Breakdown>`,
  `List<TreeNode>`, `List<Description>`) — and on a **scalar** `Callout` (one per alert, never a list) — or they get mistreated as table columns.

## Built-in shape types (use as model properties for rich output)

`Metric` (bar chart), `Breakdown`/`Segment` (stacked bar), `Callout` (alert; `CalloutSeverity.Warning/Caution/Note/Tip/Important`), `TreeNode`
(hierarchy), `Description` (term + text), `CodeSection` (code block). e.g. `new Metric("Build", 4.2)`.

## Shape Library (data relationship -> rendering)

Each property maps to a data relationship, not a visual element; the renderer decides presentation.

| Relationship | C# type | Means | Markdown |
|---|---|---|---|
| Identity | `string`/`int`/`bool` | named value | `Key: value` |
| Enumeration | `string[]` | sequence of items | `- item` |
| Tabulation | `List<T>` | uniform records | `\| col \| col \|` |
| Section | `[MarkoutSection]` | logical grouping | `## Heading` |
| Description | `List<Description>` | terms + explanations | `- **Term:** text` |
| Measurement | `List<Metric>` | comparative quantities | `Label ████░░ 45` |
| Composition | `List<Breakdown>` | parts of a whole | stacked bar |
| Hierarchy | `List<TreeNode>` | parent-child | `├── node` |
| Quotation | `CodeSection` | verbatim content | code block |
| Attention | `Callout` | important message | `> [!WARNING]` |

Record-type constructors (named for what the data *is*):

```csharp
new Metric("Build Time", 4.2)                                    // measurement
new Description("dotnet-inspect", "API surface inspection tool")  // term + explanation
new Breakdown("Jan 2025", [new("Critical", 1), new("High", 3)])  // proportional composition
new Callout(CalloutSeverity.Warning, "3 vulnerabilities found")  // attention
new CodeSection("csharp", "public class Foo { }")                // verbatim content
new TreeNode("root", [new TreeNode("child")])                    // hierarchy
```

## Renderers (swap the formatter, change the output)

Serialize writes through a formatter; pass a different one to change output.

| Formatter | Output | Use |
|---|---|---|
| `MarkdownFormatter` | GitHub-Flavored Markdown (default) | reports, tool output |
| `PlainTextFormatter` | plain text, no markup | minimal output |
| `UnicodeFormatter` | box-drawing chars | bordered tables |
| `TableFormatter` | tables / lists / fields | compact summaries, TSV/JSONL rows |
| `DiagramFormatter` | trees / structural diagrams | dependency graphs |

`TableFormatter` + `MarkoutWriterOptions.TableMode`:
- `Tsv` — stable snake_case headers; never emits embedded tabs/newlines in cells.
- `Jsonl` — stable snake_case names; one JSON object per row.
- `Pretty` — same projection as TSV with each column at a uniform position.

e.g. TSV: `MarkoutSerializer.Serialize(report, Console.Out, new TableFormatter(), ReportContext.Default, new MarkoutWriterOptions { TableMode = MarkoutTableMode.Tsv });`

## Advanced property attributes (links, badges, grouping)

- **`[MarkoutLink(TextProperty = nameof(Title))]`** on a URL/string property -> renders it as
  `[Title](url)` (the `TextProperty` supplies the link text; the annotated property is the href).
- **`[MarkoutValueMap("open=✗", "closed=✓", ...)]`** -> maps a property's raw values to display
  strings (badges/icons) before rendering.
- **`[MarkoutSection(GroupBy = nameof(Milestone))]`** on a `List<T>` -> groups the items under a
  `###` sub-heading per distinct `Milestone` value, instead of one flat table.
- Also: `[MarkoutMaxItems(n)]` (truncate a list), `[MarkoutDisplayFormat("{0:N0}")]` (numeric
  format), `[MarkoutSkipNull]` / `[MarkoutSkipDefault]` (hide empty/default values).
