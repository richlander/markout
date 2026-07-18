---
name: markout
version: 0.22.0
description: >-
  Use when generating Markdown or other structured output (plain text, ANSI, pretty tables,
  TSV/JSONL) from C# objects instead of hand-built strings — CLIs, tools, reports, agent
  output. Markout is a source-generated .NET serializer: it looks like System.Text.Json
  source-gen but the rules differ (NO reflection fallback), so it needs a generated
  MarkoutSerializerContext and Markout-specific attributes. Start here for the required
  pattern; branch to the domain skills for conditional reports, multi-view/verbosity,
  output formats, built-in shapes, and composite cells/cards. Don't decompile the Markout
  assembly or web-search its API — every idiom you need is in these skills.
---

# Markout — structured output from objects

Package `Markout` (the source generator ships in it — no extra package). Default output is
Markdown. Reach for Markout whenever a tool would otherwise build strings with
`Console.WriteLine` / `StringBuilder`.

> **Everything you need is in these skills.** Do NOT `web_search` / `web_fetch` for Markout usage —
> this base skill plus the domain skills below are authoritative and version-matched to the package.
> If an idiom isn't here, pull the matching domain skill (listed at the end); don't go to the web.

## The required pattern (3 parts — all mandatory)

```csharp
using Markout;

// 1. Annotate every model type. List<T> -> table, scalar -> field.
[MarkoutSerializable(TitleProperty = nameof(Title))]   // TitleProperty -> the H1 heading
public class Report
{
    public string Title { get; set; } = "";
    public int Count { get; set; }                     // scalar -> "Count | 3" field row
    [MarkoutSection(Name = "Items")]                   // -> "## Items" heading
    public List<Row>? Items { get; set; }              // List<T> -> a table
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

## Scalar field shaping (title, description, per-value formatting)

Shape scalar properties with attributes — never pre-format strings in the model or hand-write rows:

```csharp
[MarkoutSerializable(
    TitleProperty = nameof(Name),               // -> the H1 heading
    DescriptionProperty = nameof(Summary),      // -> a paragraph under the H1 (NOT a Field | Value row)
    FieldLayout = FieldLayout.Inline)]          // Table (default) | Inline | Bulleted | Numbered | Plain
public class Component
{
    public string Name { get; set; } = "";
    public string Summary { get; set; } = "";

    [MarkoutDisplayFormat("{0:N0} downloads")]  // 5100000 -> "5,100,000 downloads"
    public long Downloads { get; set; }

    [MarkoutDisplayFormat("{0:yyyy-MM-dd}")]    // DateTime -> "2024-06-01"
    public DateTime Published { get; set; }

    [MarkoutBoolFormat("Yes", "No")]            // true -> "Yes", false -> "No"
    public bool Verified { get; set; }
}
```

- `DescriptionProperty` renders a property as a description paragraph, not a table row.
- `FieldLayout.Inline` puts the scalar fields on one line (`Owner: … | Status: …`) instead of a table.
- `[MarkoutDisplayFormat("{0:…}")]` / `[MarkoutBoolFormat(t,f)]` format a value **in place** — do not bake
  the formatting into the getter or build the cell string yourself.

## Gotchas (where System.Text.Json intuition is wrong)

- **No reflection fallback.** There is no `Serialize(obj)` overload. EVERY `Serialize` call takes a
  `MarkoutSerializerContext`. Omitting it does not compile — the #1 mistake.
- **Register every type.** A model missing `[MarkoutSerializable]` + `[MarkoutContext(typeof(T))]`
  won't serialize. The context class MUST be `partial`.
- **Markout attributes, not Json:** `[MarkoutSerializable]` (not `[JsonSerializable]`),
  `[MarkoutContext]`, `[MarkoutSection(Name=...)]`, `[MarkoutPropertyName]`, `[MarkoutIgnore]`.
- **Type drives rendering, not markup:** `List<T>` -> table; scalar -> `Field | Value` row;
  `[MarkoutSection(Name="X")]` -> a `## X` heading above the property.
- **`[MarkoutIgnoreInTable]` on non-tabular list properties** (`List<Metric>`, `List<Breakdown>`,
  `List<TreeNode>`, `List<Description>`, `Callout`) or they get mistreated as table columns.

## Most common workflow: JSON API → model → report

Fetch JSON, project to a Markout model (a plain data model is fine — no separate visual layer),
serialize. Keep the JSON DTO and the Markout model separate; project between them with LINQ.

## Which skill for what

Author declaratively — describe the data, let the type/attributes drive output. Do NOT hand-roll
`if`/`StringBuilder` for things below. Pull the matching skill:

- **conditional-composition** — show/hide sections & columns from the data; filter to sections;
  one model, many shapes (`ShowWhenProperty`, `IgnoreColumnWhen`, `IncludeSections`, same-name sections).
- **multi-view-verbosity** — quiet/detail/`-v` levels and summary-vs-full from one model.
- **output-formats** — plain text, ANSI/Spectre, pretty tables, TSV/JSONL, multi-format dispatch.
- **built-in-shapes** — `Metric`, `Breakdown`, `Callout`, `TreeNode`, `Description`, `CodeSection`.
- **composite-cells-cards** — dense-Markdown-cell ↔ decomposed-column data (`Change<V>`, `Fraction`,
  `Share`, `Percent`, `Segments`, `Delta`/`Goal`) and metric/role/verdict cards.
