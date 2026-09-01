# Markout Shape System

## Vision

HTML defines elements by visual form: `<table>`, `<ul>`, `<blockquote>`.
Markout defines shapes by data relationship: **tabulation**, **enumeration**, **description**.

An HTML element prescribes appearance. A Markout shape describes what the data *is* and leaves each renderer to choose the best visual form for its medium. The same `Metric` property renders as text-art bars in Markdown, colored blocks in ANSI, and an interactive chart in a web UI — without the developer making visual decisions.

## The Data Projection Model

Every C# property on a serializable type has a **data topology** — the structural relationship between its elements. Markout's source generator recognizes nine fundamental data relationships and projects each onto a document element:

| Relationship | What it captures | C# type pattern | Document form |
|---|---|---|---|
| **Identity** | A named value | `string`, `int`, `bool`, `DateTime`, ... | `Key: value` |
| **Enumeration** | An ordered sequence of items | `string[]`, `List<string>` | `- item` |
| **Tabulation** | Uniform records with fields | `List<T>` where T has properties | `\| col \| col \|` |
| **Section** | A logical grouping of related data | `[MarkoutSection]`, nested object | `## Heading` + content |
| **Description** | Terms with explanations | `List<Description>` | **Term:** text |
| **Measurement** | Labeled quantities | `List<Metric>` | `Label ████░░ 45` |
| **Composition** | Parts of a whole | `List<Breakdown>` | `██▓▓▒░ 1 crit, 3 high` |
| **Hierarchy** | Parent-child structure | `List<TreeNode>` | `├── node` |
| **Quotation** | Verbatim content | `CodeSection` | `` ```lang ... ``` `` |
| **Attention** | Important messages | `Callout` | `> [!WARNING] ...` |

These relationships are domain-independent. A Kubernetes status report has identity (fields), enumeration (pod names), tabulation (container specs), sections (resource groups), hierarchy (cluster → namespace → pod), and attention (warnings). A build report has sections (per project), measurement (timings), composition (test results by outcome), and quotation (compiler output).

## Document Structure

Markout maps object nesting to heading depth. The heading level is not chosen by the developer — it's determined by where the data lives in the object graph:

```
Object nesting                          Document heading
─────────────────────────────────       ────────────────
Root object (TitleProperty)         →   # H1
├── [MarkoutSection] property       →   ## H2
│   ├── Nested object (Title)       →   ### H3
│   │   └── [MarkoutSection]        →   #### H4
│   └── Collection item (Title)     →   ### H3
└── Scalar properties               →   (fields under H1)
```

This means a complex document with H1–H4 headings is just a nested object graph:

```csharp
[MarkoutSerializable(TitleProperty = nameof(Title))]        // → # Package Report
public class PackageReport
{
    [MarkoutIgnore] public string Title { get; set; }
    public string Author { get; set; }                       // → Author: Alice
    public string License { get; set; }                      // → License: MIT

    [MarkoutSection(Name = "Assemblies")]                    // → ## Assemblies
    public List<Assembly> Assemblies { get; set; }       //   each item → ### name
}

[MarkoutSerializable(TitleProperty = nameof(Name))]          // → ### Foo.dll
public class Assembly
{
    [MarkoutIgnore] public string Name { get; set; }
    public string Architecture { get; set; }                 //   Architecture: AnyCPU
    public bool Signed { get; set; }                         //   Signed: yes

    [MarkoutSection(Name = "API Surface")]                   // → #### API Surface
    public ApiSurface? Surface { get; set; }                 //   Types: 42, Methods: 156
}
```

Produces:

```markdown
# Package Report

Author: Alice
License: MIT

## Assemblies

### Foo.dll

Architecture: AnyCPU
Signed: yes

#### API Surface

Types: 42
Methods: 156

### Bar.dll

Architecture: x64
Signed: no
```

The heading hierarchy emerges from the object structure — no heading levels are specified in code. The source generator assigns H1 to the root `TitleProperty`, H2 to top-level `[MarkoutSection]` properties, and increments for each nesting level. This means:

- **Rearranging sections** (reordering properties) doesn't change heading levels
- **Adding depth** (nesting another object) automatically gets the right heading
- **Extracting a subtree** (serializing an inner object standalone) still produces valid Markdown starting at H1

## Shape Tiers

Shapes are organized into three tiers based on how fundamental they are to document rendering:

### Tier 1: Document Primitives

Every document format supports these. They map directly to Markdown elements and represent the core structural vocabulary.

| Shape | Relationship | Writer method | Record type |
|---|---|---|---|
| **Headings** | Section | `WriteHeading` | — |
| **Paragraphs** | Identity (prose) | `WriteParagraph` | — |
| **Fields** | Identity | `WriteField`, `WriteFields`, `WriteFieldsInline`, `WriteFieldsBulleted`, `WriteFieldsNumbered`, `WriteFieldsTable` | `MarkoutField` |
| **Lists** | Enumeration | `WriteListItem`, `WriteArray` | — |
| **Tables** | Tabulation | `WriteTable`, `WriteTableStart/Row/End` | — |
| **Code** | Quotation (code) | `WriteCodeStart/End` | `CodeSection` |
| **Quotation** | Quotation (prose) | `WriteQuotation` | — |
| **Rule** | Structure | `WriteRule` | — |

### Tier 2: Semantic Extensions

These add meaning beyond raw document structure. They represent specific data relationships that appear frequently across domains.

| Shape | Relationship | Writer method | Record type |
|---|---|---|---|
| **Descriptions** | Description | `WriteDescriptions` | `Description` |
| **Callouts** | Attention | `WriteCallout` | `Callout` |
| **Trees** | Hierarchy | `WriteTree` | `TreeNode` |
| **Graphs** | Directed relationship | `WriteGraph` | `Graph` |
| **Text diffs** | Correspondence between ordered sequences | `WriteTextDiff` | `MappedTextDiff` |

### Tier 3: Data Visualizations

These render quantitative patterns. They may degrade gracefully in simpler renderers (e.g., falling back to a table).

| Shape | Relationship | Writer method | Record type |
|---|---|---|---|
| **Metrics** | Measurement | `WriteMetrics` | `Metric` |
| **Breakdowns** | Composition | `WriteBreakdown` | `Breakdown` |

Renderers declare which tiers they support via `SupportedShapes`. A minimal plain-text renderer might support only Tier 1. A full ANSI renderer supports all three tiers with colored output.

## Record Types

Markout provides record types for shapes that need structured input beyond scalar values. Each captures a specific data relationship:

```csharp
// Measurement — a labeled quantity for comparative display
public readonly record struct Metric(string Label, double Value);

// Description — a term with explanatory text
public readonly record struct Description(string Term, string Text, string? Detail = null);

// Composition — a labeled breakdown of proportional parts (slices of a shared whole)
public readonly record struct Slice(string Category, int Count);
public readonly record struct Breakdown(string Label, Slice[] Slices);

// Composite cells — data-only cells that render densely and decompose into typed columns.
// The type picks the rendering; [MarkoutDelta]/[MarkoutUnit]/[MarkoutGoal]/[MarkoutDeltaNoun]/[MarkoutNumberFormat] configure derivation/format.
public readonly record struct Change<V>(V Before, V After);        // before → after (+ derived delta)
public readonly record struct Fraction(double Count, double Total); // 24/24
public readonly record struct Share(double Value, double Whole);    // 5056 (24%)
public readonly record struct Percent(double Part, double Whole);   // 93%
public readonly record struct Segment(string Label, double Value);
public readonly record struct Segments(params Segment[] Parts);     // 21/171/236

// Goal-aware derivation — [MarkoutGoal] (or a Goal on MetricChange<T>/MarkoutCellFormat) derives a
// structural Direction and a goal-applied GateStatus polarity from a numeric Change<V>, as separate
// direction/status fields. A caller-supplied Status overrides the derived polarity. Composite
// Change<Share|Percent|Fraction|Segments> derive from IGoalMagnitude (Share→Value, Percent/Fraction→ratio, Segments→sum of parts).
public enum Goal { Context, Higher, Lower }                         // which direction is "good"
public enum Direction { Unchanged, Increased, Decreased, Introduced, Resolved }; // structural, goal-neutral
// Goal/polarity render as glyphs on rich sinks (Markout.MarkoutGlyphs, IGlyphFormatter: Markdown/ANSI/Unicode):
// the metric label carries a goal glyph (↑/↓) and a derived GateStatus a polarity glyph (✓/✗); a goal-annotated
// standalone Change<V> (card row or element-table column) trails the glyph too; a MultiSourceRow with a Goal
// adds pairwise polarity to each scalar column vs its predecessor. Plain text keeps the (-)/(+) marker + status
// word; TSV/JSONL keep the direction/status slug words. Easy mode: MarkoutWriterOptions.Glyphs (defaults ↑/↓/✓/✗).
// Advanced mode: MarkoutWriterOptions.ComposeGlyph (Func<GlyphContext,string>) composes each glyph onto its text —
// replace with a word, integrate it, or condition on GlyphContext.Slot/GateStatus (GlyphContext.Combine() = default).
public enum Delta { None, Percent, Absolute, Multiple }             // derived-change suffix mode (Multiple: 3× fewer/more)
public interface IGoalMagnitude { double GoalMagnitude { get; } }   // composite cell's comparable magnitude (goal)
public interface IDeltaCountable { double DeltaCount { get; } }     // composite cell's count for [MarkoutDeltaNoun] (Fraction→count, Share→value)
// [MarkoutNumberFormat("N0")] (MarkoutCellFormat.NumberFormat) applies a .NET numeric format string to the numbers Markout
// renders for a Change<V>: scalar before/after operands + the Absolute/delta-noun/IDeltaCountable delta — keeping a grouped cell
// and its folded delta consistent (165 → 1,168 (+1,003)). Composite operands stay shape-owned; %/multiple deltas and structured
// (TSV/JSONL) output are unaffected (structured stays raw/ungrouped for machine parsing).

// Card shapes — list-shapes that render as multi-format cards (Markdown table + typed JSONL/TSV).
public readonly record struct MetricChange<T>(string Name, T Before, T After,
    T? Target = null, string? TargetLabel = null,
    GateStatus Status = GateStatus.Unknown, string? StatusLabel = null) where T : struct; // gated metric
public readonly record struct Source(string Role, IMarkoutCell? Value, MarkoutCellFormat Format = default);
public readonly struct MultiSourceRow(string label, params Source[] sources); // role matrix (roles → columns)
// A MultiSourceRow { Emphasis = MarkoutEmphasis.AtLeast(cut) | AtMost(cut) } bolds (Markdown **…**) each
// scalar cell clearing the threshold — declared "which numbers matter" (point at the bad side for an alarm);
// scalar cells only, Markdown-only (IEmphasisFormatter), composes with the Goal glyph/polarity. Plain/TSV unstyled.
public sealed record MarkoutEmphasis { EmphasisComparison Comparison; double Cut; } // AtLeast/AtMost factories
public enum GateStatus { Unknown, Good, Neutral, Warning, Bad }    // verdict polarity
public readonly record struct Verdict(GateStatus Status, string? Label = null); // first-class verdict cell

// Child rows — a [MarkoutChild] bool on an element-table row type marks that row as a semantic child
// of the preceding row (single level). It is a data relationship, not indentation: the flag is never a
// column; rich sinks (IGlyphFormatter: Markdown/ANSI/Unicode) prefix the child row's first cell with the
// configurable child glyph (default ↳, MarkoutGlyphs.Child / GlyphSlot.ChildRow). TSV/JSONL and plain
// text omit it (glyph-only v1). Easy mode: override MarkoutGlyphs.Child; advanced: ComposeGlyph.
[AttributeUsage(AttributeTargets.Property)] public sealed class MarkoutChildAttribute : Attribute { }

// Hierarchy — a recursive node with children
public class TreeNode { ... }

// Quotation — verbatim content with optional language
public readonly record struct CodeSection(string? Language, string Content);

// Attention — a message with severity level
public readonly record struct Callout(CalloutSeverity Severity, string Message);

// Association — a dynamic key-value pair
public readonly record struct MarkoutField(string Key, string? Value);
```

The naming convention is deliberate: types are named for what the data **is** (a metric, a description, a breakdown), not what it **looks like** (a bar, a bullet, a stacked chart).

## Shape Admission Criteria

A new shape belongs in Markout if it passes **all five** criteria:

1. **Type-recognizable** — The source generator can identify it from the C# type signature alone. No runtime configuration or attributes are needed to select the shape.

2. **Semantically distinct** — It captures a data relationship that no existing shape covers. Visual variants of existing shapes (e.g., numbered lists vs. bullet lists) are parameters, not new shapes.

3. **Multi-renderer** — At least three renderers (plain text, Markdown, ANSI) can produce a meaningful representation. If only one medium can express it, it belongs in a renderer-specific extension, not in the core shape vocabulary.

4. **Compositional** — It can appear as a standalone property, inside a `[MarkoutSection]`, or as part of a larger document. Shapes that only work at the top level aren't shapes — they're document templates.

5. **Domain-independent** — The data relationship appears across problem domains: build systems, API documentation, infrastructure monitoring, data analysis. If only one application needs it, it's an application concern, not a shape.

## Evaluating Future Shapes

Applying the admission criteria to candidates:

| Candidate | Relationship | Criteria assessment | Verdict |
|---|---|---|---|
| ~~Quotation~~ | Quotation (prose) | ✅ All five. Distinct from CodeSection (prose vs. code). | **Shipped** |
| StatusItem | Measurement | ❌ #2: Same relationship as Metric (value relative to max). | Metric with options |
| DefinitionItem | Description | ❌ #2: Same relationship as Description. | Use Description |
| LinkItem | Reference | ⚠️ #2: Could be a format attribute on fields. | Attribute preferred |
| OrderedList | Enumeration | ❌ #2: Visual variant of List. | Parameter on WriteArray |
| ~~Mapped text diff~~ | Correspondence between ordered sequences | ✅ All five. | **Shipped:** [Mapped text diff](mapped-text-diff.md) |
| FlameGraph | Hierarchy + Measurement | ⚠️ #3: Hard to express in plain text. | Defer |
| Gantt / Timeline | Measurement + Time | ⚠️ #3: Hard to express in plain text. | Defer |

## Design Principles

1. **Shapes describe data, not appearance.** The name "Metric" tells you the data represents a measurement. The name "BarChart" tells you what it looks like. Markout uses the former. Renderers decide the latter.

2. **Renderers choose visual form.** A `Metric` might render as horizontal bars in text, colored blocks in ANSI, or sparklines in a web UI. The shape contract is "comparative labeled quantities" — the visual representation is a renderer decision.

3. **Tiers enable graceful degradation.** When a renderer doesn't support a Tier 3 visualization, the source generator can fall back to a Tier 1 table. The data is never lost; only the visual sophistication changes.

4. **Source generation maps types to shapes.** Property types are resolved to shapes at compile time. `List<Metric>` is always a measurement visualization. No runtime reflection, no manual writer calls for standard patterns.

5. **Composition over configuration.** Complex documents are built from shape primitives composed in sections, not from complex shape configurations. A build report is headings + fields + metrics + tables + callouts, each as a property.

6. **The shape vocabulary is finite and curated.** Not every data pattern needs a dedicated shape. Shapes are admitted through explicit criteria, not accumulated through feature requests. This keeps the API learnable and the generated code predictable.
