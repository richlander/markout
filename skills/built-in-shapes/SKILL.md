---
name: built-in-shapes
version: 0.22.0
description: >-
  Use when a report needs rich visual elements — bar charts, stacked/proportional bars, alert
  boxes, tree hierarchies, term/definition glossaries, or code blocks — instead of hand-drawn
  ASCII or manual Markdown. Markout ships these as data types (Metric, Breakdown/Slice, Callout,
  TreeNode, Description, CodeSection) you attach as model properties. Requires the base `markout`
  pattern. Don't decompile the assembly or web-search the API — the shape types are here.
---

# Built-in shapes — declare the meaning, get the visual

Attach these types as properties; the formatter draws the right visual. The anti-pattern is
building bars/trees by hand (`new string('█', n)`, indented dashes). Say *what the data is* — a
measurement, a breakdown, an alert — not *how to draw it*.

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

    // Code block.
    [MarkoutIgnoreInTable]
    public CodeSection? Snippet { get; set; }           // new CodeSection("csharp", "class Foo { }")
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
- **`Callout` is a value type** — pair it with `[MarkoutSkipDefault]` so an unset callout disappears.
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
