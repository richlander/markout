# Markout CT Eval Guide (24 scenarios)

A reviewer-oriented rendering of 24 CT scenarios from eval.yaml for Markout 0.23.0.

- Scenario count: 24
- Package: Markout 0.23.0
- Prompt note: Prompts describe the library functionally and never name it.

## CT01: minimal report — title + scalar field table

- Target skill: markout
- Tool restriction: No web search / fetch allowed.
- Fixtures: [CT01/Report.csproj](fixtures/ct/CT01/Report.csproj), [CT01/Program.cs](fixtures/ct/CT01/Program.cs)

```text
This console project references a source-generated .NET serializer that projects annotated objects into Markdown (see the package reference in the .csproj). Program.cs holds
one plain BuildReport object. Using the serializer, print a Markdown report whose H1 title is the
project name, followed by a Field | Value table of the remaining scalar values (Configuration,
Warnings, Errors). Drive the output through the serializer — do not hand-write the
Markdown. Build and run the project to confirm it prints the expected report.
```

### Rubric

- Registers the model on a partial `MarkoutSerializerContext` and drives output through `MarkoutSerializer.Serialize`
- Uses `TitleProperty` for the H1 and lets scalars render as Field | Value rows — does NOT hand-write headings or tables
- Builds and prints the report

### Checks

| Check | Evidence |
| ----- | -------- |
| Project builds | dotnet build |
| Drives output through MarkoutSerializer.Serialize | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutSerializer.Serialize . |
| Registers a Markout serializer context | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutContext . |
| No hand-written Markdown tables | Program.cs: &#124; --- |
| Rendered output matches the expected structure | dotnet run --no-build |

## CT02: scalar cell formatters (number, date, bool)

- Target skill: markout
- Tool restriction: No web search / fetch allowed.
- Fixtures: [CT02/Report.csproj](fixtures/ct/CT02/Report.csproj), [CT02/Program.cs](fixtures/ct/CT02/Program.cs)

```text
This console project references a source-generated Markdown serializer (see the .csproj).
Program.cs holds one plain PackageInfo object. Using the serializer, print a Markdown report with an H1
title from the package Id and a Field | Value table where:
  - Downloads is shown thousands-separated AND suffixed with the word "downloads", as one cell
    (e.g. 5,100,000,000 downloads) — the label text must be produced by the formatter, not by
    concatenating a string yourself
  - Published is shown as an ISO date (yyyy-MM-dd)
  - Signed is shown as "Yes"/"No"
Presentation must live on the model via the serializer's formatting attributes — do NOT pre-format the
values with inline ToString/string.Format or a ternary. Drive output through the serializer; do
not hand-write Markdown. Build and run to confirm.
```

### Rubric

- Produces the Downloads cell (thousands-separated value + the word "downloads") through a declarative formatter — any of `[MarkoutDisplayFormat("{0:N0} downloads")]`, `[MarkoutFormat]` with a literal-section numeric format, or a custom `IMarkoutCell` + `[MarkoutUnit]` — plus `[MarkoutBoolFormat("Yes","No")]` for the flag and a format for the ISO date
- Formatting lives on the view via attributes — does NOT inline ToString/string.Format/ternary
- Drives output through the serializer; builds and prints the formatted table

### Checks

| Check | Evidence |
| ----- | -------- |
| Project builds | dotnet build |
| Drives output through MarkoutSerializer.Serialize | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutSerializer.Serialize . |
| Uses declarative boolean formatting | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutBoolFormat . |
| No hand-written Markdown tables | Program.cs: &#124; --- |
| Rendered output matches the expected structure | dotnet run --no-build |

## CT03: rename and hide fields

- Target skill: markout
- Tool restriction: No web search / fetch allowed.
- Fixtures: [CT03/Report.csproj](fixtures/ct/CT03/Report.csproj), [CT03/Program.cs](fixtures/ct/CT03/Program.cs)

```text
This console project references a source-generated .NET Markdown serializer (see the .csproj). Program.cs holds one plain TestRun
object. Using the serializer, print a Markdown report with an H1 from the suite name and a Field | Value
table that (a) shows the TotalTests value under the column name "Total", and (b) OMITS the
InternalRunId field entirely. Use the serializer's attributes to rename and hide — do not restructure
the data or hand-write the table. Drive output through the serializer. Build and run to confirm.
```

### Rubric

- Uses `[MarkoutPropertyName("Total")]` to rename and `[MarkoutIgnore]` to hide the id field
- Output shows 'Total' (not 'TotalTests') and no InternalRunId row
- Drives output through the serializer; does NOT hand-write the table

### Checks

| Check | Evidence |
| ----- | -------- |
| Project builds | dotnet build |
| Drives output through MarkoutSerializer.Serialize | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutSerializer.Serialize . |
| Uses declarative field renaming | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutPropertyName . |
| Uses declarative field hiding | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutIgnore . |
| No hand-written Markdown tables | Program.cs: &#124; --- |
| Rendered output matches the expected structure | dotnet run --no-build |

## CT04: simple list section

- Target skill: markout
- Tool restriction: No web search / fetch allowed.
- Fixtures: [CT04/Report.csproj](fixtures/ct/CT04/Report.csproj), [CT04/Program.cs](fixtures/ct/CT04/Program.cs)

```text
This console project references a source-generated .NET Markdown serializer (see the .csproj). Program.cs holds one plain
DependencyReport with a project name and a list of dependencies. Using the serializer, print a Markdown
report with an H1 from the project name and a "## Dependencies" section rendering the list as a
table (Name, Version). Drive output through the serializer — do not hand-write the heading or the
table. Build and run to confirm.
```

### Rubric

- Uses `[MarkoutSection(Name = "Dependencies")]` on the List<T> so it renders as a titled table
- Registers the element type on the context; both rows render
- Drives output through the serializer; does NOT hand-write the heading or table

### Checks

| Check | Evidence |
| ----- | -------- |
| Project builds | dotnet build |
| Drives output through MarkoutSerializer.Serialize | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutSerializer.Serialize . |
| Declares rendered sections | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutSection . |
| No hand-written Markdown tables | Program.cs: &#124; --- |
| Rendered output matches the expected structure | dotnet run --no-build |

## CT05: one model, two output formats (Markdown + plain text)

- Target skill: markout
- Tool restriction: No web search / fetch allowed.
- Fixtures: [CT05/Report.csproj](fixtures/ct/CT05/Report.csproj), [CT05/Program.cs](fixtures/ct/CT05/Program.cs)

```text
This console project references a source-generated .NET Markdown serializer (see the .csproj). Program.cs holds one plain
ReleaseReport. Using the serializer, print the SAME report twice from ONE model definition, giving each
report an H1 heading taken from the release version: first as
GitHub-flavored Markdown, then as plain text — ASCII, space-aligned columns, no pipe tables and no
box-drawing characters (the library ships a dedicated plain-text formatter for exactly this). Do not
hand-write either rendering or define two models. Build and run to confirm both are printed.
```

### Rubric

- Renders ONE model twice, passing a `PlainTextFormatter` for the second (ASCII, space-aligned) rendering
- Markdown output uses '#'/pipe tables; plain-text output is space-aligned with ASCII rules (no pipe table, no Unicode box-drawing)
- Drives both through the serializer; does NOT hand-write either format

### Checks

| Check | Evidence |
| ----- | -------- |
| Project builds | dotnet build |
| Drives output through MarkoutSerializer.Serialize | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutSerializer.Serialize . |
| Uses the plain-text formatter | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin PlainTextFormatter . |
| No hand-written Markdown tables | Program.cs: &#124; --- |
| Rendered output matches the expected structure | dotnet run --no-build |

## CT06: description paragraph + inline field layout

- Target skill: markout
- Tool restriction: No web search / fetch allowed.
- Fixtures: [CT06/Report.csproj](fixtures/ct/CT06/Report.csproj), [CT06/Program.cs](fixtures/ct/CT06/Program.cs)

```text
This console project references a source-generated .NET Markdown serializer (see the .csproj). Program.cs holds one plain Component
with a Name, a one-sentence Summary, and two more scalar fields. Using the serializer, print a Markdown
report where the H1 is the component Name, the Summary renders as a description paragraph
(not a table row), and the remaining fields render INLINE (on one line, e.g. "Owner: ... | Status:
...") rather than as a Field | Value table. Use the serializer's attributes for the title, description,
and field layout. Drive output through the serializer. Build and run to confirm.
```

### Rubric

- Uses `TitleProperty`, `DescriptionProperty`, and `FieldLayout` = `FieldLayout`.Inline on `[MarkoutSerializable]`
- Summary is a paragraph; Owner/Status render inline, NOT as a Field | Value table
- Drives output through the serializer

### Checks

| Check | Evidence |
| ----- | -------- |
| Project builds | dotnet build |
| Drives output through MarkoutSerializer.Serialize | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutSerializer.Serialize . |
| Uses declarative field layout | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin FieldLayout . |
| Uses a description paragraph | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin DescriptionProperty . |
| No hand-written Markdown tables | Program.cs: &#124; --- |
| Rendered output matches the expected structure | dotnet run --no-build |

## CT07: conditional section via ShowWhenProperty

- Target skill: conditional-composition
- Tool restriction: No web search / fetch allowed.
- Fixtures: [CT07/Report.csproj](fixtures/ct/CT07/Report.csproj), [CT07/Program.cs](fixtures/ct/CT07/Program.cs)

```text
This console project references a source-generated .NET Markdown serializer (see the .csproj). Program.cs holds two plain PackageInfo
objects — one WITH deprecations, one WITHOUT. Using the serializer, print a Markdown report for each with
an H1 from the Id, a Field | Value table of the scalars, and a "## Deprecations" section that
appears ONLY for a package that has deprecations. ONE rendering definition must handle both
packages. Drive output through the serializer; do not hand-write the heading or gate it with
imperative string-building. Build and run to confirm.
```

### Rubric

- Defines ONE view with `[MarkoutSection(ShowWhenProperty = ...)]` that both packages flow through
- The Deprecations section renders for the deprecated package and is omitted for the clean one (appears exactly once total)
- Uses the declarative gate — does NOT hand-write or imperatively gate the section

### Checks

| Check | Evidence |
| ----- | -------- |
| Project builds | dotnet build |
| Drives output through MarkoutSerializer.Serialize | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutSerializer.Serialize . |
| Gates sections declaratively | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin ShowWhenProperty . |
| No hand-written Markdown tables | Program.cs: &#124; --- |
| Rendered output matches the expected structure | dotnet run --no-build |

## CT08: render a section subset with IncludeSections

- Target skill: conditional-composition
- Tool restriction: No web search / fetch allowed.
- Fixtures: [CT08/Report.csproj](fixtures/ct/CT08/Report.csproj), [CT08/Program.cs](fixtures/ct/CT08/Program.cs)

```text
This console project references a source-generated .NET Markdown serializer (see the .csproj). Program.cs holds one plain ServiceStatus
with scalar fields and two list sections (Incidents, Metrics). Using the serializer, print the SAME model
TWICE from ONE definition: first a "quiet" report that shows only the title and scalar fields (no
named sections), then a "full" report that includes the Incidents and Metrics sections. Select the
rendered sections at serialize time (do not define two models or hand-write the omitted sections).
Build and run to confirm.
```

### Rubric

- Renders one model twice, using `MarkoutWriterOptions.IncludeSections` to select which sections appear
- Quiet omits both sections; full includes each exactly once (each heading appears exactly once total)
- Does not fork into two models or hand-write sections

### Checks

| Check | Evidence |
| ----- | -------- |
| Project builds | dotnet build |
| Drives output through MarkoutSerializer.Serialize | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutSerializer.Serialize . |
| Selects sections at serialization time | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin IncludeSections . |
| No hand-written Markdown tables | Program.cs: &#124; --- |
| Rendered output matches the expected structure | dotnet run --no-build |

## CT09: machine-readable TSV output

- Target skill: output-formats
- Tool restriction: No web search / fetch allowed.
- Fixtures: [CT09/Report.csproj](fixtures/ct/CT09/Report.csproj), [CT09/Program.cs](fixtures/ct/CT09/Program.cs)

```text
This console project references a source-generated .NET Markdown serializer (see the .csproj). Program.cs holds one plain Roster with a
list of members. Using the serializer, print the members as tab-separated values (TSV) — one header row of
column names, then one tab-delimited row per member — using the serializer's table formatter and TSV mode.
Do not hand-build the TSV strings. Build and run to confirm.
```

### Rubric

- Uses a TableFormatter with `MarkoutWriterOptions.TableMode` = `MarkoutTableMode.Tsv`
- Output is tab-delimited with a header row; no Markdown pipe table
- Does NOT hand-build the TSV

### Checks

| Check | Evidence |
| ----- | -------- |
| Project builds | dotnet build |
| Drives output through MarkoutSerializer.Serialize | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutSerializer.Serialize . |
| Uses table formatter modes | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin TableMode . |
| No hand-written Markdown tables | Program.cs: &#124; --- |
| Rendered output matches the expected structure | dotnet run --no-build |

## CT10: metric bars and a breakdown

- Target skill: built-in-shapes
- Tool restriction: No web search / fetch allowed.
- Fixtures: [CT10/Report.csproj](fixtures/ct/CT10/Report.csproj), [CT10/Program.cs](fixtures/ct/CT10/Program.cs)

```text
This console project references a source-generated .NET Markdown serializer (see the .csproj). Program.cs holds one plain PerfReport
with a list of timing measurements (label + seconds) and a coverage breakdown (covered vs
uncovered). Using the serializer, render this for a TERMINAL (Unicode) so the timings display as labeled
bars and the coverage renders as a breakdown. Use the serializer's built-in shape types for the metrics
and the breakdown rather than hand-built rows. Build and run to confirm.
```

### Rubric

- Uses Metric (and Breakdown/Slice) shape types as model properties, rendered via a `UnicodeFormatter`
- Terminal output shows the timing metric bars (block glyphs) and the coverage rendered as a proportional breakdown bar (Unicode breakdown shows the group label + bar, not per-slice text labels)
- Does NOT hand-build the bars/rows

### Checks

| Check | Evidence |
| ----- | -------- |
| Project builds | dotnet build |
| Drives output through MarkoutSerializer.Serialize | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutSerializer.Serialize . |
| Uses the Unicode formatter for terminal shapes | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin UnicodeFormatter . |
| No hand-written Markdown tables | Program.cs: &#124; --- |
| Rendered output matches the expected structure | dotnet run --no-build |

## CT11: before -&gt; after change cells

- Target skill: composite-cells-cards
- Tool restriction: No web search / fetch allowed.
- Fixtures: [CT11/Report.csproj](fixtures/ct/CT11/Report.csproj), [CT11/Program.cs](fixtures/ct/CT11/Program.cs)

```text
This console project references a source-generated .NET Markdown serializer (see the .csproj). Program.cs holds one plain
RegressionReport with two before -> after change values (Change cells) for Errors and
Coverage. Using the serializer, print a Markdown report with an H1 title and a Field | Value table where
each change renders as "before -> after (signed absolute delta)", e.g. "12 -> 3 (-9)". Annotate the
change cells so the absolute delta is appended. Do not hand-format the arrows/deltas. Build and run.
```

### Rubric

- Models each value as a Change<int> cell with `[MarkoutDelta(Delta.Absolute)]`
- Output shows 'before → after (signed delta)' for each row
- Does NOT hand-format the arrow or delta

### Checks

| Check | Evidence |
| ----- | -------- |
| Project builds | dotnet build |
| Drives output through MarkoutSerializer.Serialize | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutSerializer.Serialize . |
| Uses declarative change deltas | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutDelta . |
| No hand-written Markdown tables | Program.cs: &#124; --- |
| Rendered output matches the expected structure | dotnet run --no-build |

## CT12: conditional table column via IgnoreColumnWhen

- Target skill: conditional-composition
- Tool restriction: No web search / fetch allowed.
- Fixtures: [CT12/Report.csproj](fixtures/ct/CT12/Report.csproj), [CT12/Program.cs](fixtures/ct/CT12/Program.cs)

```text
This console project references a source-generated .NET Markdown serializer (see the .csproj). Program.cs holds two plain PackageDeps
objects — one whose dependencies are ALL required, and one with a MIX of required and optional.
Using the serializer, print a Markdown report for each with an H1 from the Id and a "## Dependencies"
table (Name, Version, and whether each dependency is Optional). The "Optional" column must be
HIDDEN for the all-required package (it carries no information there) and SHOWN for the mixed one.
ONE rendering definition must handle both. Drive output through the serializer; do not hand-write
or imperatively build the table. Build and run to confirm.
```

### Rubric

- Applies [`MarkoutIgnoreColumnWhen`(nameof(<predicate>), "Optional")] with a static bool predicate that hides the column when values are uniform
- Optional column is absent for the all-required package and present for the mixed one (appears exactly once total)
- Does NOT hand-write or imperatively gate the column

### Checks

| Check | Evidence |
| ----- | -------- |
| Project builds | dotnet build |
| Drives output through MarkoutSerializer.Serialize | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutSerializer.Serialize . |
| Uses declarative field hiding | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutIgnoreColumnWhen . |
| No hand-written Markdown tables | Program.cs: &#124; --- |
| Rendered output matches the expected structure | dotnet run --no-build |

## CT13: verbosity-gated detail sections (quiet vs verbose)

- Target skill: conditional-composition
- Tool restriction: No web search / fetch allowed.
- Fixtures: [CT13/Report.csproj](fixtures/ct/CT13/Report.csproj), [CT13/Program.cs](fixtures/ct/CT13/Program.cs)

```text
This console project references a source-generated .NET Markdown serializer (see the .csproj). Program.cs holds one plain PackageReport
(scalars plus Dependencies and Tags collections). Using the serializer, print BOTH a quiet and a verbose
report from ONE model definition: the quiet report shows only the H1 and scalar fields; the verbose
report additionally includes a "## Dependencies" table and a "## Tags" section. The detail sections
must be DECLARED ONCE on the model and gated on a verbosity flag (do not define two models or
hand-write the omitted sections). Drive output through the serializer. Build and run to confirm.
```

### Rubric

- Declares the detail sections once, each gated by a verbosity flag (via `ShowWhenProperty` or `IncludeSections`)
- Renders the same model twice: quiet omits both; verbose includes each exactly once
- Does not fork into two models or hand-write the sections

### Checks

| Check | Evidence |
| ----- | -------- |
| Project builds | dotnet build |
| Drives output through MarkoutSerializer.Serialize | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutSerializer.Serialize . |
| Gates sections declaratively | grep -rqE --include=*.cs --exclude-dir=obj --exclude-dir=bin ShowWhenProperty&#124;IncludeSections . |
| No hand-written Markdown tables | Program.cs: &#124; --- |
| Rendered output matches the expected structure | dotnet run --no-build |

## CT14: JSONL with typed numeric values

- Target skill: output-formats
- Tool restriction: No web search / fetch allowed.
- Fixtures: [CT14/Report.csproj](fixtures/ct/CT14/Report.csproj), [CT14/Program.cs](fixtures/ct/CT14/Program.cs)

```text
This console project references a source-generated .NET Markdown serializer (see the .csproj). Program.cs holds one plain BenchReport
with a list of benchmark rows (a name plus numeric OpsPerSec and AllocatedKb). Using the serializer, print
the rows as JSONL (one JSON object per line) using the serializer's table formatter and JSONL mode, with
the numeric columns emitted as JSON NUMBERS (not quoted strings). Do not hand-build the JSON.
Build and run to confirm.
```

### Rubric

- Uses TableFormatter with `TableMode` = `MarkoutTableMode.Jsonl` and `MarkoutWriterOptions.JsonTypedValues` = true
- Numeric columns are emitted as JSON numbers (unquoted); one object per line
- Does NOT hand-build the JSON

### Checks

| Check | Evidence |
| ----- | -------- |
| Project builds | dotnet build |
| Drives output through MarkoutSerializer.Serialize | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutSerializer.Serialize . |
| Emits typed JSONL values | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin JsonTypedValues . |
| No hand-written Markdown tables | Program.cs: &#124; --- |
| Rendered output matches the expected structure | dotnet run --no-build |

## CT15: dependency tree with badges

- Target skill: built-in-shapes
- Tool restriction: No web search / fetch allowed.
- Fixtures: [CT15/Report.csproj](fixtures/ct/CT15/Report.csproj), [CT15/Program.cs](fixtures/ct/CT15/Program.cs)

```text
This console project references a source-generated .NET Markdown serializer (see the .csproj). Program.cs holds one plain DependencyTree
whose Root is a TreeNode with nested children (some carrying a badge marker). Using the serializer,
render this for a TERMINAL (Unicode) so the dependency hierarchy displays as a tree with branch
connectors and the badges appear next to their nodes. Use the serializer's tree shape rather than
hand-built indentation. Build and run to confirm.
```

### Rubric

- Renders the TreeNode hierarchy via a `UnicodeFormatter` (branch connectors like └─/├─)
- Nested children render under their parents and badges appear next to nodes
- Uses the tree shape — does NOT hand-build indentation

### Checks

| Check | Evidence |
| ----- | -------- |
| Project builds | dotnet build |
| Drives output through MarkoutSerializer.Serialize | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutSerializer.Serialize . |
| Uses the Unicode formatter for terminal shapes | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin UnicodeFormatter . |
| No hand-written Markdown tables | Program.cs: &#124; --- |
| Rendered output matches the expected structure | dotnet run --no-build |

## CT16: gated metric-change card with goals

- Target skill: composite-cells-cards
- Tool restriction: No web search / fetch allowed.
- Fixtures: [CT16/Report.csproj](fixtures/ct/CT16/Report.csproj), [CT16/Program.cs](fixtures/ct/CT16/Program.cs)

```text
This console project references a source-generated .NET Markdown serializer (see the .csproj). Program.cs holds one plain QualityGate
with a list of MetricChange rows (Failures 7 -> 0, Coverage 78 -> 85). Using the serializer, render a
"## Gates" card as a Markdown table where each metric shows its before -> after change with the goal
applied — Failures should be treated as "lower is better" and Coverage as "higher is better", and the
derived good/bad status should appear inline. Use the serializer's MetricChange card shape and goals rather
than computing status by hand. Build and run to confirm.
```

### Rubric

- Uses List<MetricChange<int>> with { Goal = Goal.Lower } / { Goal = Goal.Higher } to derive polarity
- Card shows the goal marker on the label ((-)/(+)) and an inline good/bad status per row
- Does NOT compute status/direction by hand

### Checks

| Check | Evidence |
| ----- | -------- |
| Project builds | dotnet build |
| Drives output through MarkoutSerializer.Serialize | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutSerializer.Serialize . |
| Uses metric-change card rows | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MetricChange . |
| Applies goal polarity declaratively | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin Goal . |
| No hand-written Markdown tables | Program.cs: &#124; --- |
| Rendered output matches the expected structure | dotnet run --no-build |

## CT17: same-name polymorphic sections (one heading, two shapes)

- Target skill: conditional-composition
- Tool restriction: No web search / fetch allowed.
- Fixtures: [CT17/Report.csproj](fixtures/ct/CT17/Report.csproj), [CT17/Program.cs](fixtures/ct/CT17/Program.cs)

```text
This console project references a source-generated .NET Markdown serializer (see the .csproj). Program.cs holds two plain PackageInfo
objects — one that exposes APIs, one that is types-only. Using the serializer, print a Markdown report for
each with an H1 from the Id and a "## Signals" section that renders an API table (Member, Kind)
when the package exposes APIs, OR a types table (Type Name, Category) when it is types-only —
exactly one variant, under the SAME "Signals" heading, chosen by the data. Drive output through the
serializer; do not hand-write the heading or branch with imperative string-building. Build and run.
```

### Rubric

- Declares TWO sections with the same Name = "Signals" and mutually-exclusive `ShowWhenProperty` gates
- Each package shows exactly one Signals variant (API table for one, types table for the other)
- Uses declarative gates — does NOT hand-write headings or imperatively branch the output

### Checks

| Check | Evidence |
| ----- | -------- |
| Project builds | dotnet build |
| Drives output through MarkoutSerializer.Serialize | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutSerializer.Serialize . |
| Gates sections declaratively | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin ShowWhenProperty . |
| No hand-written Markdown tables | Program.cs: &#124; --- |
| Rendered output matches the expected structure | dotnet run --no-build |

## CT18: one model rendered at three verbosity levels

- Target skill: conditional-composition
- Tool restriction: No web search / fetch allowed.
- Fixtures: [CT18/Report.csproj](fixtures/ct/CT18/Report.csproj), [CT18/Program.cs](fixtures/ct/CT18/Program.cs)

```text
This console project references a source-generated .NET Markdown serializer (see the .csproj). Program.cs holds one plain PackageReport
with scalar fields and two list sections (Dependencies, Diagnostics). Using the serializer, print the SAME
model at THREE levels from ONE definition: quiet (title + scalars only), normal (adds the
Dependencies section), and detailed (adds both Dependencies and Diagnostics). Choose the rendered
sections at serialize time (do not define separate models or hand-write omitted sections). Drive
output through the serializer. Build and run to confirm.
```

### Rubric

- Renders one model at three levels via `MarkoutWriterOptions.IncludeSections` (progressively adding sections)
- Diagnostics appears only in the detailed render (exactly once total); Dependencies appears in normal and detailed
- Does not define separate models or hand-write sections

### Checks

| Check | Evidence |
| ----- | -------- |
| Project builds | dotnet build |
| Drives output through MarkoutSerializer.Serialize | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutSerializer.Serialize . |
| Selects sections at serialization time | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin IncludeSections . |
| No hand-written Markdown tables | Program.cs: &#124; --- |
| Rendered output matches the expected structure | dotnet run --no-build |

## CT19: central multi-format dispatch with row cap

- Target skill: output-formats
- Tool restriction: No web search / fetch allowed.
- Fixtures: [CT19/Report.csproj](fixtures/ct/CT19/Report.csproj), [CT19/Program.cs](fixtures/ct/CT19/Program.cs)

```text
This console project references a source-generated .NET Markdown serializer (see the .csproj). Program.cs holds one plain SearchReport
with a Query and a list of hits. Using the serializer, write ONE dispatch that renders the SAME model in a
format chosen at runtime by a string — "md" (Markdown, capped to the first 3 rows), "tsv"
(tab-separated), and "jsonl" (one JSON object per line) — by selecting the formatter and writer
options per format. Print all three. Do not hand-build any format or duplicate the model. Build and
run to confirm.
```

### Rubric

- One reusable dispatch selects formatter + `MarkoutWriterOptions` per format (Markdown/TSV/JSONL) for one model
- Markdown render is capped via MaxItems (shows a truncation notice); TSV is tab-delimited; JSONL is one object per line
- Does NOT hand-build any format or duplicate the model

### Checks

| Check | Evidence |
| ----- | -------- |
| Project builds | dotnet build |
| Drives output through MarkoutSerializer.Serialize | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutSerializer.Serialize . |
| Caps rendered rows through writer options | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MaxItems . |
| Uses table formatter modes | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin TableMode . |
| No hand-written Markdown tables | Program.cs: &#124; --- |
| Rendered output matches the expected structure | dotnet run --no-build |

## CT20: advisory with a callout, code block, and definitions

- Target skill: built-in-shapes
- Tool restriction: No web search / fetch allowed.
- Fixtures: [CT20/Report.csproj](fixtures/ct/CT20/Report.csproj), [CT20/Program.cs](fixtures/ct/CT20/Program.cs)

```text
This console project references a source-generated .NET Markdown serializer (see the .csproj). Program.cs holds one plain Advisory with
a warning Callout, a Repro CodeSection (a C# snippet), and a list of Description terms. Using
the serializer, print a Markdown advisory where the callout renders as a GitHub alert (e.g. "> [!WARNING]"),
the repro renders as a fenced ```csharp code block, and the terms render as a definition-style list.
Use the serializer's shape types (Callout, CodeSection, Description) as model properties. Build and run to
confirm.
```

### Rubric

- Uses Callout, CodeSection, and Description shape types as model properties (with `MarkoutIgnoreInTable` on the non-tabular list)
- Callout renders as a GitHub alert, repro as a fenced csharp code block, terms as a definition list
- Does NOT hand-write the alert/code fence/definitions

### Checks

| Check | Evidence |
| ----- | -------- |
| Project builds | dotnet build |
| Drives output through MarkoutSerializer.Serialize | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutSerializer.Serialize . |
| Uses declarative field hiding | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutIgnoreInTable . |
| No hand-written Markdown tables | Program.cs: &#124; --- |
| Rendered output matches the expected structure | dotnet run --no-build |

## CT21: multi-source comparison matrix with verdicts

- Target skill: composite-cells-cards
- Tool restriction: No web search / fetch allowed.
- Fixtures: [CT21/Report.csproj](fixtures/ct/CT21/Report.csproj), [CT21/Program.cs](fixtures/ct/CT21/Program.cs)

```text
This console project references a source-generated .NET Markdown serializer (see the .csproj). Program.cs holds one plain ModelComparison
whose rows compare a "baseline" and a "grounded" source across several metrics, using composite cells (Fraction, Share, Percent, Segments) and a Verdict. Using the serializer, render a "##
Results" Markdown matrix where the two sources pivot to COLUMNS and each metric is a row (the label
column headed "Metric"). Use the serializer's MultiSourceRow / Source / Verdict shapes rather than a
hand-built table. Build and run to confirm.
```

### Rubric

- Uses List<MultiSourceRow> with Source values (Fraction/Share/Percent/Segments) and Verdict; roles pivot to columns
- Label column is headed 'Metric'; baseline/grounded appear as columns with the composite cells rendered
- Does NOT hand-build the matrix

### Checks

| Check | Evidence |
| ----- | -------- |
| Project builds | dotnet build |
| Drives output through MarkoutSerializer.Serialize | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutSerializer.Serialize . |
| Uses multi-source comparison rows | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MultiSourceRow . |
| Uses verdict cells | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin Verdict . |
| No hand-written Markdown tables | Program.cs: &#124; --- |
| Rendered output matches the expected structure | dotnet run --no-build |

## CT22: full composed inspection report

- Target skill: conditional-composition
- Tool restriction: No web search / fetch allowed.
- Fixtures: [CT22/Report.csproj](fixtures/ct/CT22/Report.csproj), [CT22/Program.cs](fixtures/ct/CT22/Program.cs)

```text
This console project references a source-generated .NET Markdown serializer (see the .csproj). Program.cs holds one plain PackageInfo.
Using the serializer, print ONE Markdown report composed of these sections in order:
  1. an H1 = Id, then a field table where Downloads is thousands-separated AND suffixed with the
     word "downloads" as one cell (label text produced by the formatter, not concatenated), and
     Signed is Yes/No
  2. a "## Deprecations" section, shown only when the package has deprecations
  3. a "## Dependencies" table whose "Optional" column is hidden when all deps are required
  4. a "## Signals" section rendering an API table when the package exposes APIs, or a types table
     when it is types-only (exactly one variant, under the same heading)
A single rendering definition must drive all of it. Drive output through the serializer; do not
hand-write headings or tables. Build and run to confirm.
```

### Rubric

- Composes all four sections in order through ONE view: formatted fields, conditional Deprecations, Dependencies with a conditional Optional column, and a polymorphic Signals section
- Uses declarative attributes throughout (formatters, `ShowWhenProperty`, IgnoreColumnWhen, same-name Signals gates)
- Does NOT hand-write headings or tables; builds and prints the composed report

### Checks

| Check | Evidence |
| ----- | -------- |
| Project builds | dotnet build |
| Drives output through MarkoutSerializer.Serialize | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutSerializer.Serialize . |
| Gates sections declaratively | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin ShowWhenProperty . |
| Uses declarative field hiding | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutIgnoreColumnWhen . |
| No hand-written Markdown tables | Program.cs: &#124; --- |
| Rendered output matches the expected structure | dotnet run --no-build |

## CT23: dense composite cell that decomposes into columns

- Target skill: output-formats
- Tool restriction: No web search / fetch allowed.
- Fixtures: [CT23/Report.csproj](fixtures/ct/CT23/Report.csproj), [CT23/Program.cs](fixtures/ct/CT23/Program.cs)

```text
This console project references a source-generated .NET Markdown serializer (see the .csproj). Program.cs holds one plain BenchReport
whose rows each carry a before -> after change value (a Change cell). Using the serializer, print
the SAME model THREE ways: as Markdown (each change shown DENSELY in one cell, e.g. "98555 -> 61190
(-38%)"), as TSV, and as JSONL — where the structured (TSV/JSONL) outputs DECOMPOSE each change
into separate typed columns (before / after / delta). Annotate the change so the delta is a signed
percent. Do not hand-build any format. Build and run to confirm.
```

### Rubric

- Models the change as a Change<V> cell with `[MarkoutDelta(Delta.Percent)]`; Markdown shows the dense cell
- TSV/JSONL decompose the cell into {col}_before / {col}_after / {col}_delta_pct columns from one declaration
- Does NOT hand-build any of the three formats

### Checks

| Check | Evidence |
| ----- | -------- |
| Project builds | dotnet build |
| Drives output through MarkoutSerializer.Serialize | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutSerializer.Serialize . |
| Uses declarative change deltas | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutDelta . |
| No hand-written Markdown tables | Program.cs: &#124; --- |
| Rendered output matches the expected structure | dotnet run --no-build |

## CT24: section-targeted verbosity views from one model (-D / -S)

- Target skill: conditional-composition
- Tool restriction: No web search / fetch allowed.
- Fixtures: [CT24/Report.csproj](fixtures/ct/CT24/Report.csproj), [CT24/Program.cs](fixtures/ct/CT24/Program.cs)

```text
This console project references a source-generated .NET Markdown serializer (see the .csproj). Program.cs holds one plain PackageReport
with scalar fields and two detail sections (Dependencies, Diagnostics), where Diagnostics is the
deepest/most expensive detail. Using the serializer, print THREE reports from ONE model definition:
  1. a quiet report (title + scalars only, no detail sections);
  2. a report that targets ONLY the Diagnostics section by name — render Diagnostics and NOT
     Dependencies;
  3. a detailed report that includes both Dependencies and Diagnostics.
Select the rendered sections at serialize time (IncludeSections). Do not hand-write omitted
sections. Build and run to confirm.
```

### Rubric

- A section-targeted request renders ONLY the requested section
- Diagnostics appears in both the targeted render and the detailed render (>= twice); Dependencies appears only in the detailed render (exactly once total)
- Section selection uses `IncludeSections`; omitted sections are never hand-written

### Checks

| Check | Evidence |
| ----- | -------- |
| Project builds | dotnet build |
| Drives output through MarkoutSerializer.Serialize | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin MarkoutSerializer.Serialize . |
| Selects sections at serialization time | grep -rq --include=*.cs --exclude-dir=obj --exclude-dir=bin IncludeSections . |
| No hand-written Markdown tables | Program.cs: &#124; --- |
| Rendered output matches the expected structure | dotnet run --no-build |
