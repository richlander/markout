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
  `[JsonSerializable]`), `[MarkoutContext]`, `[MarkoutSection(Name=...)]`, `TitleProperty`,
  `[MarkoutPropertyName]`, `[MarkoutIgnore]`. Do not use `Json*` attributes.
- **Rendering is driven by type, not markup:** `List<T>` -> table; scalar (`string`/`int`/`bool`) ->
  a `Field | Value` row; `[MarkoutSection(Name="X")]` -> a `## X` heading above the property.
- **Put `[MarkoutIgnoreInTable]` on non-tabular list properties** (`List<Metric>`, `List<Breakdown>`,
  `List<TreeNode>`, `List<Description>`, `Callout`) or they get mistreated as table columns.

## Built-in shape types (use as model properties for rich output)

`Metric` (bar chart), `Breakdown`/`Slice` (stacked bar), `Callout` (alert), `TreeNode`
(hierarchy), `Description` (term + text), `CodeSection` (code block). e.g. `new Metric("Build", 4.2)`.
Pass children as a list/array/collection expression, never as trailing arguments:
`new TreeNode("root", [new TreeNode("leaf")])`. `Badge` is an optional property:
`new TreeNode("root") { Badge = "📁" }`.

## Composite table cells (dense Markdown + decomposed columns from one model)

Composite cells are data-only scalar properties: `FieldLayout.Table` renders a dense Markdown
value, and `TableFormatter` (TSV/JSONL) decomposes each into typed columns from one declaration.

- `Change<V>` — a `before → after` change (NOT `Comparison`, which collides with `System.Comparison<T>`).
  `[MarkoutDelta(Delta.Percent)]` on a numeric `Change<V>` appends the signed change, e.g.
  `98555 → 61190 (−38%)`; `Delta.Absolute` appends the signed difference; `Delta.Multiple` appends a
  multiplicative factor with a direction word, e.g. `15 → 5 (3× fewer)` / `5 → 15 (3× more)` (a zero
  endpoint renders `—`). With a goal, an aligned `Delta.Multiple` word omits the redundant status word
  (`15 → 5 (3× fewer)` under `Goal.Lower`); a conflicting one keeps it (`5 → 15 (3× more, bad)`).
  `[MarkoutDeltaNoun("solved")]` renders a caller noun on the signed delta —
  `4/6 → 6/6 (+2 solved)` (sibling of `[MarkoutUnit]`; composites use `IDeltaCountable`: `Fraction`→count,
  `Share`→value; Markdown-only).
  `[MarkoutGoal(Goal.Higher)]` / `[MarkoutGoal(Goal.Lower)]` on a numeric `Change<V>` makes Markout
  derive two decomposed fields — a structural `direction`
  (`increased`/`decreased`/`introduced` (0→N)/`resolved` (N→0)/`unchanged`) and a goal-applied polarity
  `status` (`good`/`bad`/`neutral`) — instead of the caller hand-coding ceiling/floor/drift. Optional
  noise, `[MarkoutGoal(Goal.Higher, 0.001)]`, treats sub-threshold movement as `unchanged`. `Goal.Context`
  (default) derives nothing. Composite `Change<Share|Percent|Fraction>` also derive `direction`/`status`
  from a single comparable magnitude (`Share` → raw `Value`; `Percent`/`Fraction` → their ratio;
  `Segments` → the **sum** of its parts' values, i.e. the breakdown total). In dense Markdown the polarity
  renders inline after any delta suffix: on rich sinks (`IGlyphFormatter`: Markdown/ANSI/Unicode) as a
  configurable glyph — `0 → 7 ✗`, `98555 → 61190 (−38%) ✓` — and on plain text as the merged word —
  `0 → 7 (bad)`, `98555 → 61190 (−38%, good)`. TSV/JSONL keep the `direction`/`status` slug words.
- `Fraction(count, total)` → `24/24`; `Share(value, whole)` → `5056 (24%)`
  (`[MarkoutUnit("s")]` → `103s (93%)`); `Percent(part, whole)` → `93%`;
  `Segments(new Segment(label, value), ...)` → `21/171/236` (labels become column names).
- `Change<V>` nests over composites: `Change<Fraction>`, `Change<Share>`, `Change<Segments>`.
  A zero denominator renders `—` rather than `NaN`/`Inf`. e.g.
  `[MarkoutPropertyName("Session IET"), MarkoutDelta(Delta.Percent)] public Change<long> SessionIet { get; init; }`.

JSONL decomposition is **heterogeneous** (each record holds only its own keys); TSV keeps a uniform
column union (absent cells blank). Set `MarkoutWriterOptions.JsonTypedValues = true` to emit numeric
columns as JSON numbers instead of strings.

When a composite cell (`Change<V>`, etc.) is a **column of an element table** (`List<T>` where `T` has a
composite property), structured formatters (TSV/JSONL) **decompose it into typed `{column}_{sub}` columns**
— `score_before`/`score_after`/`score_delta_pct`, `bugs_direction`/`bugs_status`, `tasks_delta_count`/
`tasks_delta_noun` — so the card is reconstructable from the data. Markdown keeps the dense cell
(`100 → 50 (-50%)`); a goal-annotated column also trails a polarity glyph on rich sinks (`7 → 0 ✓`).
Scalar columns and Markdown are otherwise unchanged. `[MarkoutIgnoreColumnWhen]` columns that
are hidden for the table are dropped from the decomposed output too.

## Card shapes (declare a list; the generator picks the layout)

Two list-shapes render as multi-format cards from a `[MarkoutSerializable]` model — Markdown table +
flat typed JSONL/TSV — with no imperative writer calls:

- `List<MetricChange<T>>` — a gated scalar metric per row. `MetricChange<T>(Name, Before, After,
  Target? = null, TargetLabel? = null, Status = GateStatus.Unknown, StatusLabel? = null)` where
  `T : struct`. Set `{ Goal = Goal.Higher|Lower }` (init property; optionally `Noise`) to derive the
  `direction`/`status` axes from `Before → After`; a caller-supplied `Status` overrides the derived
  polarity. **Dense Markdown (default):** `Metric | Change | Target`. On rich sinks (Markdown/ANSI/
  Unicode — `IGlyphFormatter`) the metric label carries a goal glyph (`Failures ↓` / `Fully raised ↑`)
  and the Change cell a polarity glyph (`0 → 7 ✗`); a caller `StatusLabel` stays a parenthesized word
  (`0 → 7 (regression)`). Plain text keeps the ASCII `(-)`/`(+)` marker + status word (`0 → 7 (bad)`).
  Configure glyphs via `MarkoutWriterOptions.Glyphs` (defaults `↑`/`↓`/`✓`/`✗`) — *easy mode*; for
  *advanced mode* set `MarkoutWriterOptions.ComposeGlyph`, a `Func<GlyphContext, string>` that composes
  each glyph onto its text (replace it with a word, integrate it into the cell, or condition on
  `GlyphContext.Slot`/`GateStatus`; `GlyphContext.Combine()` is the default append-with-space). The
  `Status` column is dropped. Set `MarkoutWriterOptions.InlineGoalStatus = false` for the legacy
  `Metric | Change | Target | Status`.
  JSONL/TSV are unaffected: flat `before`/`after`/optional `target`/`target_label`/`direction`/`status`.
  `T` must be a numeric scalar (int/long/double/decimal/...); a composite `T` (e.g. `Segments`) is a
  compile error (`MARKOUT005`).
- `[MarkoutSection(Name = "...", IncludeSectionInStructuredRows = true)]` on a `List<MetricChange<T>>`
  or `List<MultiSourceRow>` section prepends a leading `section` column (the section name) to TSV/JSONL
  rows — for multiplexing several sectioned card shapes into one structured stream. Markdown is unchanged.
- `List<MultiSourceRow>` — a role matrix: `MultiSourceRow(label, params Source[])`,
  `Source(role, IMarkoutCell? value, format = default)`. Roles pivot to **columns** in Markdown
  (caller-supplied order; absent role → `-`); JSONL emits one flat record per row with
  `{role}_{side}_{field}` keys. Values are any `IMarkoutCell` incl. nested `Change<Share>` etc.;
  **scalars work too** — `new Source("baseline", 2)` (int/long/double/decimal) and `Source.Text(role, s)`
  decompose to a single `{role}` field; `new Source(role, null)` is an absent cell. Set the label column
  header with `[MarkoutLabelHeader("Metric")]` (defaults to `Field`). Set `{ Goal = Goal.Higher|Lower }`
  on a row to add a goal glyph to the label and a **pairwise polarity glyph** to each scalar column
  (compared to its previous populated scalar column) on rich sinks — a per-period trend over an
  n-column series; TSV/plain are unaffected.
- `Verdict(GateStatus Status, string? Label = null)` — a first-class verdict cell (typed polarity
  `GateStatus` + optional caller label); use as a `Source` value for a verdict row (decomposes to `{role}`).

Decomposition keys are `snake_case_lower` (a fused role like `claude-opus-4.8` becomes
`claude_opus_4_8_...`). Put `[MarkoutIgnoreInTable]` on these lists only when nested inside a table.

## Other output formats (still Markdown by default)

Pass a formatter to change output: `new MarkdownFormatter()` (default), `PlainTextFormatter`,
`UnicodeFormatter`, `TableFormatter` (compact/TSV/JSONL via `MarkoutWriterOptions.TableMode`).
e.g. `MarkoutSerializer.Serialize(report, Console.Out, new TableFormatter(), ReportContext.Default);`
