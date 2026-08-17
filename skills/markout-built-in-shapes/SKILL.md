---
name: markout-built-in-shapes
version: 0.35.2
description: >-
  Use when a report needs rich visual elements — bar charts, stacked/proportional bars, alert
  boxes, tree hierarchies, term/definition glossaries, or code blocks — instead of hand-drawn
  ASCII or manual Markdown. Markout ships these as data types (Metric, Breakdown/Slice, Callout,
  TreeNode, Description, CodeSection, MarkoutTable) you attach as model properties. If the task
  requests terminal/Unicode, plain text, ANSI, or another non-Markdown sink, also invoke
  markout-output-formats for the formatter call. Don't decompile the assembly or web-search the
  API — the shape types are here.
---

# Built-in shapes — declare the meaning, get the visual

Attach these types as properties; the formatter draws the right visual. The anti-pattern is
building bars/trees by hand (`new string('█', n)`, indented dashes). Say *what the data is* — a
measurement, a breakdown, an alert — not *how to draw it*.

## Required setup

Markout has **no reflection fallback**. Every report needs an annotated model, a partial context
registering each model type, and a `Serialize` call that passes it — there is no `Serialize(obj)`
overload, and omitting the context does not compile.

```csharp
using Markout;

[MarkoutContext(typeof(Dashboard))]   // your model types only — the shape types below are built in
public partial class DashboardContext : MarkoutSerializerContext { }

MarkoutSerializer.Serialize(dashboard, Console.Out, DashboardContext.Default);
```

## Route visual shapes to the requested sink

Default serialization is Markdown. A request for terminal or Unicode bars needs the output-format
companion skill and an explicit formatter:

```csharp
MarkoutSerializer.Serialize(
    dashboard,
    Console.Out,
    new UnicodeFormatter(),
    DashboardContext.Default);
```

Invoke `markout-output-formats` for plain text, ANSI, pretty tables, TSV/JSONL, or multi-format
dispatch. Do not guess a `MarkoutWriterOptions.Format` property or add an ANSI package unless the
requested sink specifically needs it.

## The shapes

```csharp
[MarkoutSerializable(TitleProperty = nameof(Title))]
public class Dashboard
{
    public string Title { get; set; } = "";

    // Bar chart — comparative quantities.
    [MarkoutSection(Name = "Timings"), MarkoutIgnoreInTable]
    public List<Metric>? Timings { get; set; }          // new Metric("Build", 4.2)

    // Stacked/proportional bar.
    [MarkoutSection(Name = "Severity"), MarkoutIgnoreInTable]
    public List<Breakdown>? Severity { get; set; }
    // new Breakdown("Issues", [new Slice("Critical", 3), new Slice("Low", 12)])

    // Alert box. [MarkoutSkipDefault] hides it when unset.
    [MarkoutIgnoreInTable, MarkoutSkipDefault]
    public Callout Warning { get; set; }                // new Callout(CalloutSeverity.Warning, "3 issues")

    // Tree hierarchy. Children are a COLLECTION, never trailing args.
    [MarkoutIgnoreInTable]
    public List<TreeNode>? Deps { get; set; }
    // new TreeNode("root", [new TreeNode("child")]) { Badge = "📁" }

    // Term + explanation list.
    [MarkoutSection(Name = "Glossary"), MarkoutIgnoreInTable]
    public List<Description>? Glossary { get; set; }    // new Description("API", "Application ...")

    // Code block. Like Callout it is a value type, so pair it with [MarkoutSkipDefault]
    // rather than making it nullable — the generator cannot unwrap a CodeSection?.
    [MarkoutIgnoreInTable, MarkoutSkipDefault]
    public CodeSection Snippet { get; set; }            // new CodeSection("csharp", "class Foo { }")

    // Table whose columns are runtime data, not attributed properties. Use it when the column
    // set is only known at run time (rows projected out of a foreign schema, a heap dump, etc.).
    [MarkoutSection(Name = "Metadata"), MarkoutIgnoreInTable]
    public MarkoutTable? Metadata { get; set; }
    // new MarkoutTable(["Property", "Value"], [["Machine", "Amd64"]])
}
```

## Runtime-column tables and runtime-named sections

`MarkoutTable` carries headers and rows as runtime values, so the generator does not need to know
the columns at compile time. It still flows through the serializer like a generated table —
`SectionOrder`, `RowWindow`, `IncludeSections`, column projection, and TSV/JSONL decomposition all
apply for free. Two things to know:

- **Projection is per table, checked per document.** A projection (`IncludeColumns`) that names none
  of a table's columns renders that table as nothing, because the same projection may be aimed at a
  sibling section whose columns differ. Generated tables and `MarkoutTable` follow the same rule, and
  a miss in any one table is silent. A selection that matched nothing in *any* table it was offered
  to is a caller error, and `Flush`/`Complete` throws `No columns matched projection: <names>` rather
  than hand back a document the caller's request never reached. A selection is its names and its
  comparison, so mutating either poses a new question that must match on its own. An empty
  `IncludeColumns` list is a caller error and is reported where the projection is offered. A
  projection may be spelled with either the display header or the canonical snake_case key that
  TSV/JSONL emits.
- **Structured column keys.** Pass a second `headerNames` list to key TSV/JSONL output on stable
  names while the display headers stay human-facing:
  `new MarkoutTable(["Property", "Value"], ["prop", "val"], rows)`. A table validates itself at
  construction: every row must have one cell per header, and no two columns may share a canonical
  key. It is a view over the caller's arrays, not a copy, so do not mutate them afterwards.

To emit a *runtime-determined set of named sections* — each carrying its own runtime-column table —
put the tables on title-bearing elements and `[MarkoutUnwrap]` the list. Each element becomes its
own level-2 section named from its runtime title, and every markout feature still applies:

```csharp
[MarkoutSerializable(TitleProperty = nameof(Name))]
public class MetadataSection
{
    [MarkoutIgnore] public string Name { get; set; } = "";      // drives the heading only
    [MarkoutIgnoreInTable] public MarkoutTable? Body { get; set; }
}

[MarkoutSerializable]
public class MetadataDocument
{
    [MarkoutUnwrap] public List<MetadataSection> Sections { get; set; } = [];
}
```

## Critical guardrails

- **`[MarkoutIgnoreInTable]` on every shape list/property.** Without it, `List<Metric>` etc. get
  mistreated as a table of columns instead of rendering as the shape. This is the #1 shapes mistake.
- **A single `Breakdown` property (not a list) renders as ONE labeled proportional bar** — use it for a
  covered-vs-uncovered coverage bar rather than a `List<Breakdown>`:
  `[MarkoutIgnoreInTable] public Breakdown Coverage { get; set; }` with
  `new Breakdown("Coverage", [new Slice("Covered", 82), new Slice("Uncovered", 18)])`. Under a Unicode/
  terminal formatter this shows the group label + `█` bar, not per-slice table rows.
- **Children go in a collection expression**, never as trailing constructor arguments:
  `new TreeNode("root", [new TreeNode("leaf")])`. `Badge` is an optional object-initializer property.
- **`Callout` and `CodeSection` are value types** — declare them non-nullable and pair with
  `[MarkoutSkipDefault]` so an unset one disappears. `Callout?` / `CodeSection?` does not compile.
- Do not hand-draw bars/trees; if you're building glyphs by hand you're using the wrong tool.

## Shape cheat-sheet

| Type | Meaning | Construct |
|---|---|---|
| `Metric` | one measured value (bar) | `new Metric("Build", 4.2)` |
| `Breakdown` + `Slice` | proportional composition | `new Breakdown("By type", [new Slice("A", 3)])` |
| `Callout` | alert/severity box | `new Callout(CalloutSeverity.Warning, "…")` |
| `TreeNode` | hierarchy | `new TreeNode("root", [children]) { Badge = "📁" }` |
| `Description` | term + text | `new Description("API", "…")` |
| `CodeSection` | fenced code block | `new CodeSection("csharp", "…")` |
| `MarkoutTable` | runtime-column table | `new MarkoutTable(["Property", "Value"], [["Machine", "Amd64"]])` |
