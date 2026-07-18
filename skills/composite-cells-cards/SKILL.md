---
name: composite-cells-cards
version: 0.22.0
description: >-
  Use when a single value must render dense in Markdown but decompose into typed columns in
  TSV/JSONL — before/after changes, fractions, shares, percentages, segment breakdowns, deltas
  and goal polarity — or when building metric / role-matrix / verdict cards from one model
  (Change<V>, Fraction, Share, Percent, Segments, Delta/Goal, MetricChange<T>, MultiSourceRow,
  Verdict). This is Markout's most advanced tier. Requires the base `markout` pattern.
  Don't decompile the assembly or web-search the API — these composite types are here.
---

# Composite cells & cards — one declaration, dense cell + decomposed columns

Composite cells are **data-only scalar properties**. With `FieldLayout.Table` (default) each becomes
a row: Markdown renders a dense human value, and `TableFormatter` (TSV/JSONL) decomposes the *same*
cell into typed columns — no pre-stringifying, one declaration serves both. This replaces
pre-formatting numbers into strings (which loses the machine-readable form).

## Composite cell primitives

| Shape | Dense Markdown | Decomposed columns |
|---|---|---|
| `Change<V>` + `[MarkoutDelta(Delta.Percent)]` | `98555 → 61190 (−38%)` | `before`, `after`, `delta_pct` |
| `Fraction(count, total)` | `24/24` | `count`, `total` |
| `Share(value, whole)` + `[MarkoutUnit("s")]` | `103s (93%)` | `value`, `pct` |
| `Percent(part, whole)` | `93%` | `pct` |
| `Segments(new Segment(l, v), …)` | `21/171/236` | one column per label |
| `Change<Fraction>` / `Change<Segments>` | `24/24 → 24/24` | `{sub}_before` / `{sub}_after` |

```csharp
[MarkoutSerializable]   // FieldLayout.Table (default): each property is a row
public class QualityCard
{
    [MarkoutPropertyName("tasks correct")]
    public Change<Fraction> TasksCorrect { get; set; }                 // 24/24 → 24/24

    [MarkoutPropertyName("Session IET"), MarkoutDelta(Delta.Percent)]
    public Change<long> SessionIet { get; set; }                       // 98555 → 61190 (−38%)

    public string? Verdict { get; set; }                               // BETTER
}
```

- `Change<V>` is named `Change` (NOT `Comparison` — collides with `System.Comparison<T>`).
- A **zero denominator renders `—`**, never `NaN`/`Inf`.
- `Change<V>` nests over composites: `Change<Fraction>`, `Change<Share>`, `Change<Segments>`.

## Delta and goal — derived, not hand-coded

- `[MarkoutDelta(Delta.Percent|Absolute|Multiple)]` appends the signed change to a numeric
  `Change<V>`: `(−38%)`, `(+120)`, `15 → 5 (3× fewer)`.
- `[MarkoutDeltaNoun("solved")]` adds a caller noun: `4/6 → 6/6 (+2 solved)`.
- `[MarkoutGoal(Goal.Higher|Lower)]` makes Markout derive two columns — a structural `direction`
  (`increased`/`decreased`/`introduced`/`resolved`/`unchanged`) and a goal-applied `status`
  (`good`/`bad`/`neutral`) — so you don't hand-code ceiling/floor polarity. `Goal.Higher, 0.001`
  treats sub-threshold movement as `unchanged`. In Markdown the polarity word renders inline:
  `98555 → 61190 (−38%, good)`.

## Structured output

```csharp
var jsonl = new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, JsonTypedValues = true };
MarkoutSerializer.Serialize(card, Console.Out, new TableFormatter(), QualityCardContext.Default, jsonl);
// {"field":"Session IET","before":98555,"after":61190,"delta_pct":-38}
```

JSONL is **heterogeneous** (each record only its cell's keys); TSV keeps a uniform column union
(absent cells blank). `JsonTypedValues = true` emits numbers as JSON numbers; without it, all strings.
When a composite is a **column of an element table** (`List<T>` with a composite property), TSV/JSONL
decompose it into `{column}_{sub}` columns (`score_before`, `bugs_status`); Markdown keeps the dense
cell. `[MarkoutIgnoreColumnWhen]`-hidden columns drop from the decomposition too.

## Card shapes (declare a list; the generator picks the layout)

- `List<MetricChange<T>>` — a gated metric per row. `MetricChange<T>(Name, Before, After, Target?,
  TargetLabel?, Status, StatusLabel?)`, `T : struct` numeric (a composite `T` is compile error
  `MARKOUT005`). Set `{ Goal = Goal.Higher|Lower }` to derive `direction`/`status`; Markdown inlines
  the status word and a goal marker on the label (`Failures (-)`), dropping the `Status` column.
- `List<MultiSourceRow>` — a role matrix. `MultiSourceRow(label, params Source[])`,
  `Source(role, IMarkoutCell? value)`; roles pivot to columns in Markdown (absent role → `-`), JSONL
  emits `{role}_{side}_{field}` keys. Scalars work: `new Source("baseline", 2)`; `Source.Text(role, s)`.
  Set the label header with `[MarkoutLabelHeader("Metric")]`.
- `Verdict(GateStatus Status, string? Label = null)` — a first-class verdict cell; use as a `Source`.
- `[MarkoutSection(Name="…", IncludeSectionInStructuredRows = true)]` prepends a `section` column to
  TSV/JSONL — multiplex several card sections into one stream. Decomposition keys are `snake_case`.

## Guardrails

- Hand Markout **already-correct values** — composites derive only intrinsics (delta, percent,
  direction/status); Markout does not aggregate or bind external data.
- Don't pre-stringify numbers; use the composite so the TSV/JSONL form stays typed.
- Put `[MarkoutIgnoreInTable]` on card lists only when nested inside another table.
