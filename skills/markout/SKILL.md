---
name: markout
version: 0.15.0
description: Source-generated .NET serializer that renders objects as Markdown (also ANSI terminal, plain text, pretty tables, TSV/JSONL). Use it when a CLI or tool needs structured, human- or agent-readable output instead of hand-built strings; it requires a generated MarkoutSerializerContext and Markout attributes (no reflection fallback).
---

# Markout — Coding Agent Integration Guide

Markout is a .NET source-generated serializer that turns objects into readable documents and compact tabular output (Markdown, ANSI terminal, plain text, pretty tables, TSV). Use it whenever a CLI tool needs structured output instead of raw `Console.WriteLine`.

## Installation

```bash
dotnet add package Markout
```

This single package includes the source generator. No additional packages needed for Markdown or plain text output.

For ANSI terminal output with Spectre.Console:

```bash
dotnet add package Markout.Ansi.Spectre
```

## Core Pattern

Every markout integration follows three steps:

### 1. Define a model

Annotate a class or record with `[MarkoutSerializable]`. Scalar properties become fields. `List<T>` properties become tables. Built-in types like `Metric`, `Breakdown`, `Callout`, and `TreeNode` produce charts, bars, alerts, and trees.

```csharp
using Markout;

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class ToolReport
{
    public string Title { get; set; } = "";
    public string Status { get; set; } = "";
    public int ItemCount { get; set; }

    [MarkoutSection(Name = "Results")]
    public List<ResultRow>? Results { get; set; }
}

[MarkoutSerializable]
public class ResultRow
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
}
```

### 2. Define a context

Create a partial class that registers your models for source generation:

```csharp
[MarkoutContext(typeof(ToolReport))]
[MarkoutContext(typeof(ResultRow))]
public partial class ToolContext : MarkoutSerializerContext { }
```

### 3. Serialize

```csharp
MarkoutSerializer.Serialize(report, Console.Out, ToolContext.Default);
```

That's it. The output is Markdown by default.

## Attribute Reference

### Type-level attributes

| Attribute | Purpose | Example |
|---|---|---|
| `[MarkoutSerializable]` | Mark type for serialization | `[MarkoutSerializable(TitleProperty = "Name")]` |
| `[MarkoutContext(typeof(T))]` | Register type with context | `[MarkoutContext(typeof(MyReport))]` |

`MarkoutSerializable` properties:

- `TitleProperty` — Property to render as the H1 heading
- `DescriptionProperty` — Property to render as a paragraph below the heading
- `AutoFields` — Auto-render scalar properties as fields (default: `true`)
- `FieldLayout` — `Table` (two-column, default), `Inline` (pipe-separated), `Bulleted`, `Numbered`, or `Plain` (bare lines)

### Property-level attributes

| Attribute | Purpose | Example |
|---|---|---|
| `[MarkoutSection(Name = "...")]` | Render as a `## Heading` section | `[MarkoutSection(Name = "Errors")]` |
| `[MarkoutPropertyName("...")]` | Custom display name | `[MarkoutPropertyName("Open Issues")]` |
| `[MarkoutDisplayFormat("...")]` | Format string | `[MarkoutDisplayFormat("{0:N0}")]` |
| `[MarkoutBoolFormat("✓", "✗")]` | Custom bool display | `[MarkoutBoolFormat("Yes", "No")]` |
| `[MarkoutSkipNull]` | Hide when null | |
| `[MarkoutSkipDefault]` | Hide when default value | |
| `[MarkoutShowWhen(nameof(Flag))]` | Conditional rendering | `[MarkoutShowWhen(nameof(HasErrors))]` |
| `[MarkoutIgnore]` | Exclude from output | |
| `[MarkoutIgnoreInTable]` | Exclude from table columns | |
| `[MarkoutMaxItems(N)]` | Truncate long lists | `[MarkoutMaxItems(10)]` |
| `[MarkoutLink(TextProperty = nameof(...))]` | Render a URL property as a hyperlink | `[MarkoutLink(TextProperty = nameof(Title))]` on the URL property |
| `[MarkoutUnwrap]` | Inline collection without section heading | |
| `[MarkoutIgnoreColumnWhen(...)]` | Conditionally hide table column | |
| `[MarkoutValueMap("k=v", ...)]` | Map values to display strings | |

### Section properties

`[MarkoutSection]` supports:

- `Name` — Section heading text
- `Level` — Heading level (default: 2 for `##`)
- `GroupBy` — Group items by a property value
- `ShowWhenProperty` — Boolean property controlling visibility
- `EmptyText` — Fallback paragraph shown when the collection is non-null but empty (`null` still omits the section)
- `IgnoreProperty` — Comma-separated column names to hide
- `FieldOrder = MarkoutFieldOrder.Alphabetical` — Alphabetically order field rows in `List<MarkoutField>` or scalar field sections

## Built-in Shape Types

Use these types as properties on your model to get rich output:

```csharp
// Bar chart — comparative quantities
[MarkoutSection(Name = "Performance")]
[MarkoutIgnoreInTable]
public List<Metric>? Metrics { get; set; }
// Usage: new Metric("Build Time", 4.2)

// Stacked bar — proportional composition
[MarkoutSection(Name = "Distribution")]
[MarkoutIgnoreInTable]
public List<Breakdown>? Distribution { get; set; }
// Usage: new Breakdown("By Type", [new Slice("Critical", 3), new Slice("Low", 12)])

// Alert box
[MarkoutIgnoreInTable]
[MarkoutSkipDefault]
public Callout Warning { get; set; }
// Usage: new Callout(CalloutSeverity.Warning, "3 issues found")

// Tree hierarchy
[MarkoutIgnoreInTable]
public List<TreeNode>? Dependencies { get; set; }
// Usage: new TreeNode("root", [new TreeNode("child1"), new TreeNode("child2")])

// Term + explanation list
[MarkoutSection(Name = "Glossary")]
[MarkoutIgnoreInTable]
public List<Description>? Terms { get; set; }
// Usage: new Description("API", "Application Programming Interface")

// Code block
[MarkoutIgnoreInTable]
public CodeSection? SourceCode { get; set; }
// Usage: new CodeSection("csharp", "public class Foo { }")
```

## Composite Table Cells (one model → dense Markdown + decomposed columns)

Composite cells are data-only value types used as **scalar properties**. With
`FieldLayout.Table` (the default) each property becomes a row: Markdown renders a dense,
human-readable value, while `TableFormatter` (TSV/JSONL) decomposes the same cell into typed
columns — no pre-stringifying, one declaration serves both.

| Shape | Dense Markdown | Decomposed columns |
|---|---|---|
| `Change<V>` (+`[MarkoutDelta(Delta.Percent)]`) | `98555 → 61190 (−38%)` | `before`, `after`, `delta_pct` |
| `Fraction(count, total)` | `24/24` | `count`, `total` |
| `Share(value, whole)` (+`[MarkoutUnit("s")]`) | `5056 (24%)` / `103s (93%)` | `value`, `pct` |
| `Percent(part, whole)` | `93%` | `pct` |
| `Segments(Segment(label, value)…)` | `21/171/236` | one column per `label` |
| `Change<Segments>` | `21/171/236 → 0/75/183` | `{label}_before`, `{label}_after` |
| `Change<Fraction>` | `24/24 → 24/24` | `before_count`, `before_total`, `after_count`, `after_total` |

`Change<V>` is named `Change` (not `Comparison`) to avoid colliding with `System.Comparison<T>`.
A zero denominator renders `—` instead of `NaN`/`Inf`.

```csharp
[MarkoutSerializable]   // FieldLayout.Table is the default — properties become rows
public class QualityCard
{
    [MarkoutPropertyName("tasks correct")]
    public Change<Fraction> TasksCorrect { get; set; }              // 24/24 → 24/24

    [MarkoutPropertyName("tool calls: web / bash / other")]
    public Change<Segments> ToolCalls { get; set; }                 // 21/171/236 → 0/75/183

    [MarkoutPropertyName("tool-turn secs (% of turn time)"), MarkoutUnit("s")]
    public Change<Share> ToolTurnSecs { get; set; }                 // 103s (93%) → 61s (90%)

    [MarkoutPropertyName("Session IET"), MarkoutDelta(Delta.Percent)]
    public Change<long> SessionIet { get; set; }                    // 98555 → 61190 (−38%)

    public string? Verdict { get; set; }                            // BETTER
}

[MarkoutContext(typeof(QualityCard))]
public partial class QualityCardContext : MarkoutSerializerContext { }

// Dense Markdown table:
MarkoutSerializer.Serialize(card, Console.Out, QualityCardContext.Default);

// Same rows, decomposed to JSONL — one record per property, typed columns:
var jsonl = new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl };
MarkoutSerializer.Serialize(card, Console.Out, new TableFormatter(), QualityCardContext.Default, jsonl);
// {"field":"Session IET","before":"98555","after":"61190","delta_pct":"-38", ...}
// {"field":"tool calls: web / bash / other","web_before":"21","bash_before":"171", ...}
```

Composite cells derive only intrinsics (delta from before/after, percent from part/whole).
Markout does not aggregate or bind external data — hand it already-correct values.

## Choosing a Formatter

```csharp
// Markdown (default) — documentation, LLM output, reports
MarkoutSerializer.Serialize(report, Console.Out, context);

// Markdown with explicit formatter
MarkoutSerializer.Serialize(report, Console.Out, new MarkdownFormatter(), context);

// Spectre terminal — colored, interactive
using Markout.Ansi.Spectre;
using Spectre.Console;
MarkoutSerializer.Serialize(report, Console.Out, new SpectreFormatter(AnsiConsole.Console), context);

// Table — compact, pretty rows
MarkoutSerializer.Serialize(report, Console.Out, new TableFormatter(), context);

// TSV — normalized rows with stable snake_case headers
var options = new MarkoutWriterOptions
{
    TableMode = MarkoutTableMode.Tsv,
    IncludeDescription = false,
    IncludeSections = new HashSet<string> { "Results" }
};
MarkoutSerializer.Serialize(report, Console.Out, new TableFormatter(), context, options);

// Plain text — log files, piped output
MarkoutSerializer.Serialize(report, Console.Out, new UnicodeFormatter(), context);
```

Format promises:

- Markdown table cell values normalize pipe characters to `&#124;`; they do not emit escaped pipes (`\|`).
- `TableFormatter` with `MarkoutTableMode.Tsv` uses stable snake_case headers by default and never emits embedded tabs or newlines in table cells.
- `TableFormatter` with `MarkoutTableMode.Pretty` renders the same projection as TSV, with each column starting at a uniform position across rows.

## Common Recipe: JSON API to Report

This is the most common pattern — fetch JSON, project to a model, serialize:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using Markout;

// 1. Fetch and deserialize
using var http = new HttpClient();
var json = await http.GetStringAsync("https://api.example.com/data");
var data = JsonSerializer.Deserialize(json, ApiJsonContext.Default.ApiResponse)!;

// 2. Project to model
var report = new Report
{
    Title = data.Name,
    Count = data.Items.Count,
    Items = data.Items.Select(i => new ItemRow
    {
        Name = i.Name,
        Status = i.Status
    }).ToList()
};

// 3. Serialize
MarkoutSerializer.Serialize(report, Console.Out, ReportContext.Default);

// --- Model ---
[MarkoutSerializable(TitleProperty = nameof(Title))]
public class Report
{
    public string Title { get; set; } = "";
    public int Count { get; set; }

    [MarkoutSection(Name = "Items")]
    public List<ItemRow>? Items { get; set; }
}

[MarkoutSerializable]
public class ItemRow
{
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
}

[MarkoutContext(typeof(Report))]
[MarkoutContext(typeof(ItemRow))]
public partial class ReportContext : MarkoutSerializerContext { }

// --- JSON Model (separate from report model) ---
public class ApiResponse { public string Name { get; set; } = ""; public List<ApiItem> Items { get; set; } = []; }
public class ApiItem { public string Name { get; set; } = ""; public string Status { get; set; } = ""; }

[JsonSerializable(typeof(ApiResponse))]
internal partial class ApiJsonContext : JsonSerializerContext { }
```

## Key Principles

1. **Data models work directly with Markout.** In the simplest case, your data model is your Markout model — no separate projection layer needed. For more complex scenarios (e.g., combining multiple API responses), a dedicated report model is fine, but it's still just a data model.
2. **Attributes describe data relationships, not visuals.** Say "this is a measurement" (`Metric`), not "draw a bar."
3. **Register every type.** Every class used in serialization needs `[MarkoutSerializable]` and must be registered via `[MarkoutContext(typeof(T))]` on the context.
4. **The context must be `partial`.** The source generator fills in the implementation.
5. **`List<T>` → table, scalar → field.** The type system drives rendering. A `List<MyRow>` automatically becomes a table; a `string` or `int` property becomes a key-value field.
6. **Sections group content.** Use `[MarkoutSection(Name = "...")]` on any property to create a `## Heading` above it.
7. **Use `[MarkoutIgnoreInTable]`** on properties that aren't tabular (metrics, breakdowns, callouts, trees) to prevent them from being treated as table columns.
