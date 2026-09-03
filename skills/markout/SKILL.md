---
name: markout
version: 0.36.0
description: >-
  Use when generating Markdown or other structured output (plain text, ANSI, pretty tables,
  TSV/JSONL) from C# objects, or when diagnosing Markout source-generator errors such as
  MARKOUT006. Markout replaces hand-built strings in CLIs, tools, reports, and agent output.
  It looks like System.Text.Json source-gen but the rules differ (NO reflection fallback), so it
  needs a generated MarkoutSerializerContext and Markout-specific attributes. Covers the required
  pattern: registered models, the partial context, scalar field shaping, typed value formatters,
  context-wide options, table diagnostics, semantic child rows, and serialization.
  Route non-Markdown output to markout-output-formats; data-selected/filtered views to
  markout-conditional-composition; and visual bars, trees, callouts, definitions, or code blocks
  to markout-built-in-shapes. Composite cells are covered separately. Don't decompile the Markout
  assembly or web-search its API — every idiom you need is in the skills.
---

# Markout — structured output from objects

Package `Markout` (the source generator ships in it — no extra package). Default output is
Markdown. Reach for Markout whenever a tool would otherwise build strings with
`Console.WriteLine` / `StringBuilder`.

> **Everything you need is in the Markout skills.** Do NOT `web_search` / `web_fetch` for
> Markout usage — they are authoritative and version-matched to the package. This skill covers
> the core pattern; conditional composition, output formats, built-in shapes, and composite
> cells are covered separately.
>
> **Routing gate:** invoke the matching companion skill before coding:
> - TSV, JSONL, plain text, Unicode/terminal, ANSI, pretty output, or multiple formats:
>   `markout-output-formats`
> - show/hide conditions, exactly-one section variants, selected sections, quiet/detail views:
>   `markout-conditional-composition`
> - bars, proportional breakdowns, trees, callouts, definitions, or code blocks:
>   `markout-built-in-shapes`

## The required pattern (registration + serialization are mandatory)

```csharp
using Markout;

// 1. Annotate a model when you need to customize its rendering. Registration is what is required.
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

`[MarkoutSerializable]` is optional. Types from dependencies can remain untouched; register them
on the context and put serializer-wide behavior on that context.

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
- **Register every type.** `[MarkoutContext(typeof(T))]` is mandatory; `[MarkoutSerializable]` is
  optional customization. The context class MUST be `partial`.
- **Markout attributes, not Json:** `[MarkoutSerializable]` (not `[JsonSerializable]`),
  `[MarkoutContext]`, `[MarkoutSection(Name=...)]`, `[MarkoutPropertyName]`, `[MarkoutIgnore]`.
- **Type drives rendering, not markup:** `List<T>` -> table; scalar -> `Field | Value` row;
  `[MarkoutSection(Name="X")]` -> a `## X` heading above the property.
- **Inline code needs semantic tags.** Raw backticks are escaped in table cells. Store
  `<code>...</code>` instead; `markout-output-formats` covers its cross-format behavior.
- **`[MarkoutIgnoreInTable]` on non-tabular list properties** (`List<Metric>`, `List<Breakdown>`,
  `List<TreeNode>`, `List<Description>`, `Callout`) or they get mistreated as table columns.

## Typed custom value formatters

For transformations beyond a format string, keep the property strongly typed and implement
`IMarkoutValueFormatter<T>`:

```csharp
public sealed class ByteSizeFormatter : IMarkoutValueFormatter<long>
{
    public string Format(long value) => value switch
    {
        >= 1_073_741_824 => $"{value / 1_073_741_824.0:0.#} GB",
        >= 1_048_576 => $"{value / 1_048_576.0:0.#} MB",
        >= 1_024 => $"{value / 1_024.0:0.#} KB",
        _ => $"{value} B",
    };
}

[MarkoutPropertyName("Package Size")]
[MarkoutValueFormatter(typeof(ByteSizeFormatter))]
public long PackageSizeBytes { get; set; }
```

Do not replace the numeric property with a string or format it in a getter. The generated
serializer calls `Format(T)` through the attribute.

## Context-wide options and table warnings

Put defaults that apply to every serialization on the generated context:

```csharp
[MarkoutContextOptions(SuppressTableWarnings = true)]
[MarkoutContext(typeof(Report))]
[MarkoutContext(typeof(DependencyRow))]
public partial class ReportContext : MarkoutSerializerContext { }
```

Use `SuppressTableWarnings` when registered dependency-owned types intentionally contain
non-tabular properties that produce `MARKOUT001`. Do not mutate those types with
`[MarkoutIgnoreInTable]`, add a pragma, or suppress the diagnostic in the project file.

## Table-row diagnostics and semantic children

`MARKOUT006` means a `List<T>` is being rendered as a table but its row type has no visible
columns. Fix the model: expose at least one scalar property, or intentionally render the data as
sections instead. Do not suppress the diagnostic, remove the collection, or stringify the rows.

Use `[MarkoutChild]` on a bool row property when `true` rows are semantic children of the preceding
parent:

```csharp
public sealed class OrganizationRow
{
    public string Name { get; set; } = "";
    public int Count { get; set; }

    [MarkoutChild]
    public bool IsChild { get; set; }
}
```

The flag is not a column. Rich output prefixes the first visible cell of child rows with the child
glyph; TSV/JSONL/plain output keeps the row data unstyled. A child flag does not count as a visible
column, so the row still needs a scalar such as `Name`.

## Most common workflow: JSON API → model → report

Fetch JSON, project to a Markout model (a plain data model is fine — no separate visual layer),
serialize. Keep the JSON DTO and the Markout model separate; project between them with LINQ.

## Author declaratively

Describe the data and let the type plus attributes drive the output. Conditional sections and
columns, alternate output formats, visual shapes, and composite cells are all declared on the
model — do NOT hand-roll `if`/`StringBuilder` for them. Each of those is covered separately.
