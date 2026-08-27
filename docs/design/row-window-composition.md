# Row-window composition

**Status:** Proposed for issue #215

This document defines how Markout selects table rows when a caller combines
multiple row windows. It owns the interaction and evaluation-order contract;
API documentation and the user guide should describe that contract rather than
redefine it.

## Problem

`MarkoutWriterOptions.RowWindow` currently accepts one `Head`, `Tail`, or
absolute `Range` window. A consumer may have more than one independent row
constraint. For example, a CLI can select the last four items and independently
restrict output to original row addresses 3 through 6.

Applying those windows sequentially is incorrect:

1. `Tail(4)` over eight rows selects original rows 5 through 8.
2. Treating that result as a new four-row table restarts its positions at 1.
   Applying `Range(3, 6)` to those new positions selects the third and fourth
   values: original rows 7 and 8.

The intended result is original rows 5 and 6. Both windows must resolve against
the same original table.

## Goals

- Compose any number of `Head`, `Tail`, and `Range` constraints.
- Preserve original row selection coordinates throughout selection.
- Define one calculation order for batch and streaming tables.
- Preserve output order; composition only removes rows.
- Keep row selection distinct from `MaxItems` summarization.
- Bound retained streaming row payloads by the smallest tail constraint.
- Resolve the same configured selection independently for every table.
- Preserve the existing singular `RowWindow` surface for source compatibility.

## Non-goals

- Union, complement, or disjoint row selections.
- Reordering rows.
- Filtering rows by cell values.
- Sharing row ordinals across tables or sections.
- Changing column projection, section selection, or shape support.
- Treating an omitted row as an item dropped by `MaxItems`.

## Terms

**Original row ordinal**
: The 1-based selection coordinate of a data row in one logical table presented
  to the table writer. A semantic lowering defines its own data rows: graph
  edges, metrics, filtered field-table entries, and flattened breakdown slices
  are rows. Headers, separators, section headings, and truncation footers are
  structural output and are not rows.

**Window**
: One contiguous row constraint: `Head(count)`, `Tail(count)`, or
  `Range(start, end)`.

**Selection plan**
: One or more windows whose resolved intervals are intersected.

**Selected rows**
: Original rows present in every window in the selection plan.

**Displayed rows**
: Selected rows that remain after `MarkoutWriterOptions.MaxItems` applies.

**Logical table**
: One batch `WriteTable` call, one streaming `WriteTableStart` through
  `WriteTableEnd` scope, or one semantic shape lowering that reaches the table
  writer.

## Semantic model

For a table containing `N` original data rows, each window resolves to one
half-open interval of zero-based positions:

| Window | Resolved interval |
| --- | --- |
| `Head(count)` | `[0, min(count, N))` |
| `Tail(count)` | `[max(0, N - count), N)` |
| `Range(start, end)` | `[min(start - 1, N), min(end, N))` |
| `Range(start, null)` | `[min(start - 1, N), N)` |

Counts must be non-negative. Range starts are 1-based and must be positive. A
finite range end must be greater than or equal to its start. A range beginning
past the table end resolves to an empty interval rather than failing.

The complete selection is:

```text
keepStart = max(window.KeepStart)
keepEnd   = min(window.KeepEnd)

if keepEnd < keepStart:
    keepEnd = keepStart
```

With no selection plan, Markout selects all original rows. Because every window
is contiguous, their intersection is also one contiguous interval or empty.

### Consequences

- Every window resolves against the same `N`.
- Windows never resolve against another window's output.
- Composition is commutative and associative.
- Repeating a window is idempotent.
- Selection never renumbers surviving rows.
- Selection preserves the producer's row order.
- A zero-count or disjoint constraint makes the complete selection empty.

## Evaluation order

Markout evaluates a table in this order:

1. **Select the document region.** Section filtering and projection-section
   matching determine whether content is in scope and whether finer projection
   is bypassed.
2. **Check shape support.** Unsupported or suppressed shapes do not create a
   logical table.
3. **Adapt the semantic shape.** A graph edge-table lowering emits edge rows in
   graph edge order followed by isolated-node rows in graph node order. Metrics
   become metric rows, a breakdown becomes flattened slice rows, and a
   field-table request applies field projection before producing rows.
   Producer-side transformations, including `[MarkoutMaxItems]`, happen here.
4. **Resolve column projection.** A projection that matches columns may drop or
   reorder cells without changing rows. A column projection that matches no
   columns suppresses the table before row enumeration.
5. **Establish the original row sequence.** The rows delivered to one logical
   table establish `N` and their table-local selection coordinates.
6. **Resolve every row window.** Each constraint resolves independently against
   `N`.
7. **Intersect resolved intervals.** The intersection identifies selected rows.
8. **Apply `MarkoutWriterOptions.MaxItems`.** The writer cap keeps the first
   configured number of selected rows.
9. **Render.** The formatter receives displayed rows in original order and the
   number omitted by the writer cap.

Ordinary column projection and row-window selection commute because column
projection changes cells without changing row count. Field projection used to
construct a field table does not commute: it defines the table's input rows and
therefore runs before row ordinals exist.

### Selection is not summarization

Row windows define which rows belong to the requested result. They do not emit
an ellipsis row and do not contribute to a skipped-row count.

`MarkoutWriterOptions.MaxItems` summarizes the already-selected result. If an
intersection selects five rows and the writer cap is two, Markout displays the
first two selected rows and reports three skipped rows. It must not cap the
original table before resolving the windows.

`[MarkoutMaxItems]` is different. Source-generated serialization applies that
attribute while producing a collection, before the result reaches the table
writer. It changes the input extent and may emit its own truncation paragraph.
This design does not reorder that producer transformation; row windows resolve
against the rows it supplies.

## Examples

### LINQ-like reading

The following notation is explanatory pseudocode, not the literal C# API.
`Range[a, b]` and `Tail(n)` add constraints to one selection plan; they do not
enumerate or renumber the sequence between calls.

```text
[1, 2, 3, 4, 5, 6, 7, 8]
    .Range[3, 4]
    .Tail(2)

= Range[3, 4] over the original rows
  intersected with
  Tail(2) over the original rows

= [3, 4] intersect [7, 8]
= []
```

An overlapping example produces rows from the same original coordinate space:

```text
[1, 2, 3, 4, 5, 6, 7, 8]
    .Range[3, 6]
    .Tail(4)

= [3, 4, 5, 6] intersect [5, 6, 7, 8]
= [5, 6]
```

Constraint-call order does not change the result:

```text
rows.Range[3, 6].Tail(4)
    == rows.Tail(4).Range[3, 6]
    == [5, 6]
```

`MarkoutWriterOptions.MaxItems` is evaluated after that complete selection:

```text
rows.Range[2, 7].Tail(5)
    == [4, 5, 6, 7]

rows.Range[2, 7].Tail(5).MaxItems(2)
    == [4, 5] with 2 skipped
```

This differs intentionally from ordinary sequential LINQ operators. If each
call enumerated its receiver before the next call, `Range[3, 4].Tail(2)` would
produce `[3, 4]`; Markout instead resolves both constraints against the original
table and produces no rows.

| Original rows | Constraints | Selected original rows |
| --- | --- | --- |
| 1 through 8 | `Tail(4)` and `Range(3, 6)` | 5, 6 |
| 1 through 8 | `Head(4)` and `Range(3, 6)` | 3, 4 |
| 1 through 100 | `Tail(20)` and `Range(50, 85)` | 81 through 85 |
| 1 through 100 | `Tail(20)` and `Range(1, 80)` | none |
| 1 through 3 | `Head(10)` and `Range(2, null)` | 2, 3 |
| none | any valid constraints | none |

Reversing the constraints in any row produces the same result.

## Per-table scope

A selection plan is writer configuration, but its resolution is table-local.
Every table independently establishes its own `N`, resolves every configured
window, and intersects the results.

For `Tail(2)` intersected with `Range(2, 2)`:

- a three-row table selects original row 2;
- a two-row table also selects original row 2;
- a one-row table selects nothing.

No table inherits a resolved interval, row count, or ordinal from a preceding
table. Ordinals restart even when a format such as JSONL renders adjacent
logical tables as one uninterrupted stream with no visible boundary.

Original ordinals are internal selection coordinates, not formatter output.
After selection, a reader counting displayed rows sees a compact sequence.
Consumers that expose stable user-visible addresses must assign and carry those
addresses as row data before any window applies; Markout preserves that data but
does not synthesize it.

## Batch and streaming behavior

Batch and streaming APIs must emit identical rows and `MaxItems` counts.

### Positional plans

A plan containing only `Head` and `Range` constraints can decide whether to
keep a row from its original zero-based position. The streaming path evaluates
all constraints for each arriving row and may emit selected rows directly.

### Plans containing `Tail`

Any `Tail` constraint depends on `N`, so the streaming path must consume the
complete table before it can finalize selection.

For multiple tail constraints, only the smallest tail count can affect the
intersection. The writer therefore retains at most that many live trailing row
payloads for selection. This is a row-count bound, not a byte bound; one row's
cells may be arbitrarily large. Each retained row carries its original
position. After the table ends, the writer resolves the complete plan against
`N` and selects buffered rows by their original positions.

`MarkoutWriterOptions.MaxItems` must also wait until selection is final for a
plan containing `Tail`; applying it to arriving rows would cap the wrong input.

### Configuration snapshot

Each logical table snapshots the row-selection inputs owned by this design:

- A batch call snapshots the complete selection plan and
  `MarkoutWriterOptions.MaxItems` at method entry.
- A streaming call snapshots both at `WriteTableStart`.
- The upstream writer resolves projection for that same logical table before
  rows arrive.

Changing any of those options while a table is active affects a later table,
not the active one. This design does not require unrelated formatting options
to share the same snapshot.

## Options and compatibility

Assigning `RowWindow`, including assigning `null`, replaces the complete
selection plan. This preserves the existing property's meaning as the simple
single-window configuration and provides an explicit reset operation.

Adding a constraint when no selection exists establishes the first window.
Adding later constraints extends the immutable plan. Copies of writer options
preserve the complete plan without sharing mutable collection state. Read-only
options reject both replacement and intersection.

### API representation

The public API is:

```csharp
public MarkoutRowWindow? RowWindow { get; set; }
public IReadOnlyList<MarkoutRowWindow> RowWindows { get; }
public void IntersectRowWindow(MarkoutRowWindow window);
```

`RowWindow` remains the singular compatibility property. Its getter returns the
first plan entry, or null when the plan is empty. `IntersectRowWindow` adds one
constraint and returns no value. `RowWindows` is never null; it returns a
read-only snapshot of every effective constraint in insertion order.

Assigning `RowWindow` replaces `RowWindows` with zero or one entry.
`IntersectRowWindow` appends an entry and publishes a replacement immutable
snapshot; a previously returned `RowWindows` value never changes. Read-only
options reject both operations.

Callers that inspect, copy, or restore composed state must use `RowWindows`, not
the singular compatibility property. A caller restores a plan by replaying the
snapshot through `IntersectRowWindow` on a fresh options instance. Built-in
options-copy operations preserve the complete immutable plan directly.

This keeps simple initialization source-compatible while avoiding hidden
composed state. An intersection method without `RowWindows` was rejected because
the singular getter would be lossy. A public `MarkoutRowSelection` value was
rejected because it would add a second public selection abstraction and
complicate the existing property without changing the semantic model.

## Implementation ownership

`MarkoutRowWindow` remains the single owner of one window's validation and
resolution. A selection-plan type owns immutable composition, interval
intersection, positional eligibility, and the minimum tail retention bound.
`TableWriter` owns table-local snapshotting and application.

Formatters receive only displayed rows and the `MaxItems` skipped count. They do
not interpret windows, recover original ordinals, or implement composition.
Generated tables, direct tables, metrics lowered to tables, and graphs lowered
to tables must all pass through the same table-writer seam.

## Required gates

- Each window kind paired with every other kind, in both operand orders.
- More than two constraints and repeated constraints.
- Empty tables, zero counts, disjoint intervals, ranges past the end, and
  open-ended ranges.
- Batch and streaming parity in every table mode.
- Original-position preservation after tail buffering.
- Tail retention bounded by the smallest tail count.
- `MarkoutWriterOptions.MaxItems` snapshotted per table and applied after the
  complete intersection with an accurate skipped count.
- `[MarkoutMaxItems]` explicitly covered as an upstream extent transformation.
- Field-table projection defining the input extent, ordinary column projection
  preserving row identity, and a column-projection miss avoiding enumeration.
- Independent resolution for multiple generated tables.
- Independent resolution for adjacent JSONL tables with no visible boundary.
- Graph edge-table ordering, including trailing isolated-node rows, through the
  same selection seam.
- Table-shaped metric lowering through the same selection seam.
- Flattened breakdown and filtered field-table lowering through the same seam.
- Assignment, clearing, read-only options, and options-copy behavior.
- A previously returned `RowWindows` snapshot remaining unchanged after later
  intersections or replacement.
- Batch and streaming tests that mutate or replace the plan after the table
  snapshot point, proving the active table retains its plan and the next table
  observes the replacement.
- Stable consumer addresses preserved as row data without claiming Markout
  emits original ordinals.
- A real consumer combining a semantic item window with an absolute row range.

## Cross-repository co-development

A Markout behavior change is not complete when its own tests pass. Before the
API freezes, the initiating consumer must point at the exact Markout source
branch and prove the contract through its real output path.

The authoritative downstream procedure is dotnet-inspect's
[Markout co-development guide][co-development]. For dotnet-inspect:

1. Record the exact Markout source commit and dotnet-inspect base commit used
   for proof.
2. Replace all three documented Markout package-reference sites locally with
   absolute project references to the exact Markout library and source-generator
   projects. The source-generator reference retains `OutputItemType="Analyzer"`
   and `ReferenceOutputAssembly="false"`.
3. Keep those project-reference edits local and unpushed.
4. Build the complete dotnet-inspect Release solution and run focused
   `OutputFormatterTests` through Markdown, TSV, JSONL, stable address columns,
   and multiple logical tables.
5. Record the exact commands and results in the Markout PR.
6. Land and release Markout first.
7. Restore dotnet-inspect to a package reference at the released version.
8. Only then open the dotnet-inspect implementation PR.

The Markout design must exist before the API implementation is treated as a
candidate. Consumer proof validates the design; it does not replace design.

[co-development]: https://github.com/richlander/dotnet-inspect/blob/main/docs/markout-co-development.md
