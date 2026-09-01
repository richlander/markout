# Mapped text diff

## Status and ownership

This document defines the Markout-owned presentation contract for mapped text
diffs. It is proposed by
[issue #218](https://github.com/richlander/markout/issues/218).

Markout owns validation, formatter dispatch, context selection, provenance
through lowerings, and presentation of caller-issued mappings between two
ordered text sequences.

The caller owns:

- the compared subjects and their identity;
- the text selected for each side;
- every equality, correspondence, replacement, movement, and annotation claim;
- failure and availability outcomes that precede a diff; and
- any language, syntax, or domain interpretation.

The normative claim is:

> Markout presents owner-issued mappings between two ordered text sequences;
> formatters choose a rich view or a GNU-compatible unified lowering.

Markout never computes a diff or infers a mapping from text.

## Relationship and admission

A mapped text diff is the relationship between two immutable ordered text
sequences and an ordered set of changes mapping ranges on the first side to
ranges on the second.

It is distinct from existing shapes:

- `Change<T>` compares one scalar or composite cell.
- A table relates uniform records but does not establish correspondence between
  two ordered sequences.
- `CodeSection` quotes opaque text and cannot expose changes, coordinates, or
  annotations to another formatter.
- A graph relates deduplicated nodes through edges rather than preserving two
  ordered sides.

The shape passes the
[shape admission criteria](shape-system.md#shape-admission-criteria):

1. Its public type is recognizable by the source generator.
2. Mapped sequence correspondence is a distinct relationship.
3. Markdown, plain text, ANSI, and structured formatters have meaningful
   lowerings.
4. The shape composes as a property, section, or complete document.
5. Source, configuration, instruction, schema, artifact, and log comparisons
   all use the relationship.

Mapped text diff is a Tier 2 semantic extension. Rich renderers may add layout
and emphasis, but the shape itself is not a visualization.

## Conceptual model

One diff contains:

- a **Before sequence** with an optional label, ordered logical lines, and an
  optional final-line-terminator assertion;
- an **After sequence** with an optional label, ordered logical lines, and an
  optional final-line-terminator assertion; and
- an ordered **change population** mapping non-overlapping ranges between the
  sequences.

Each line and sequence label is one logical line without its line terminator.
An empty string is a valid blank line. Sequence position is a zero-based
document-local coordinate; human renderers normally display it as a one-based
line number.

The optional final-line-terminator assertion is `present` or `absent` and is
valid only for a non-empty sequence. It preserves the GNU-visible distinction
between a terminated and unterminated final line when the producer knows it.
It does not preserve the spelling of individual line terminators.

Each change contains:

- one half-open Before line range;
- one half-open After line range;
- zero or more caller-issued inner text mappings when it is a replacement;
- zero or more caller-issued annotations; and
- its zero-based position in the change population.

The range counts determine the change form:

| Before count | After count | Form |
| ---: | ---: | --- |
| `0` | positive | Addition |
| positive | `0` | Removal |
| positive | positive | Replacement |
| `0` | `0` | Invalid |

Replacement is a relation between ranges, not a `Changed` line kind. It may be
one-to-one, one-to-many, many-to-one, or many-to-many.

An inner mapping relates one side-local text span to another inside a
replacement. An empty span on one side represents an intraline insertion or
removal. Inner mappings provide display evidence only; Markout does not use
them to validate or derive the enclosing line-range mapping.

An annotation carries caller-issued text and targets either:

- the complete change;
- one side-local line; or
- one side-local text span.

Annotation severity may reuse Markout's existing callout vocabulary. Domain
categories, compiler concepts, CSS classes, colors, and arbitrary property
bags are outside the shape.

## Construction invariants

A valid mapped text diff satisfies all of the following:

1. Both sequence and change collections are initialized immutable snapshots.
2. Every line and sequence label is well-formed text and contains no carriage
   return or line feed.
3. Every change range is within its owning sequence.
4. No change has two empty ranges.
5. For each adjacent pair, the earlier change's Before end is less than or
   equal to the later change's Before start, and its After end is less than or
   equal to the later change's After start. Equality is valid; collection order
   resolves ties at empty-range insertion points.
6. Every leading, intervening, and trailing gap has the same line count on
   Before and After. The sequence starts and ends act as implicit boundaries
   for this calculation.
7. Inner mappings are valid only on replacements, are contained by their
   declared side and enclosing change, and follow the same monotonic,
   non-overlapping order on each side.
8. Every annotation target is contained by its declared side and enclosing
   change.
9. A final-line-terminator assertion is present only for a non-empty sequence.
10. When both sequences assert different final-line-terminator states, each
    final line is contained by a caller-issued change.
11. Collection order is significant and preserved.

Equal-cardinality gaps between changes are unchanged sequence ranges whose
lines correspond by position. The constructor validates their cardinality but
does not compare their text; accepting the caller's mapping is accepting the
caller's claim that those lines correspond.

Construction rejects invalid input. It does not normalize, sort, trim, or
repair caller data.

## Population and provenance

The ordered change collection is the canonical change population. A stable
change address is its zero-based position within one immutable diff value.
Sequence lines retain their side and zero-based sequence position.

Every lowering that expands one change into multiple physical rows or lines
must retain enough provenance to answer:

- which change produced this output;
- whether the output belongs to Before, After, or both;
- which sequence line or lines it presents; and
- whether it is semantic content, annotation geometry, or an omission notice.

This avoids the population mismatch identified for Graph in
[issue #175](https://github.com/richlander/markout/issues/175). Different
layouts may enumerate different physical elements, but they must report their
relationship to the same change and sequence populations.

Shape-level cardinality means **change count**. A structured side-line
lowering may contain more physical rows, but those rows carry their change
address and do not redefine cardinality.

Wrapping, intraline spans, annotation rows, headings, and omission notices are
presentation elements. They never create changes.

## Context selection and completeness

The input shape retains both complete sequences and every caller-issued change.
Formatters may hide unchanged ranges, but they must not discard changes.

A context selection selects unchanged lines around changes and produces
explicit omission records. Each omitted record names:

- its Before range;
- its After range; and
- the number of unchanged lines hidden.

The ranges have equal cardinality because construction validates every
unchanged gap. Static output renders an omission notice. An interactive
consumer may make the same range revealable. Neither represents omitted
content as an ambiguous literal `...` line.

The caller or host chooses the context-line policy. Markout owns applying that
policy consistently and reporting its exact omissions.

Context selection is presentation-only. It does not change the complete input,
change count, mappings, annotations, or final-line-terminator assertions.

Mapped text diff is not a table even when a formatter internally lowers it to
rows. `MarkoutProjection` column and field filters, `RowWindow`, `MaxItems`,
and `[MarkoutMaxItems]` do not trim records inside a selected diff. Section
selection may omit the complete containing section, but it does not produce a
partial diff.

Output-size limiting is not part of context selection. If a host transport
cannot carry every change and exact omission record, it rejects the rendering
visibly rather than presenting a success-shaped truncated diff.

## Formatter contract

A formatter supporting mapped text diff consumes the validated shape and
selected context. Layout is formatter policy.

### GNU-compatible unified lowering

Markdown and portable plain text support unified output:

```diff
--- Before
+++ After
@@ -3,2 +3,2 @@
-    if (value < 0)
-        return 0;
+    if (value <= 0)
+        return 1;
```

Unified output uses only:

- space for context;
- `-` for Before lines; and
- `+` for After lines.

A replacement expands to its removed lines followed by its added lines. This
follows the
[GNU unified format](https://www.gnu.org/software/diffutils/manual/html_node/Detailed-Unified.html)
and Git patch convention.

When an emitted final line has an `absent` final-line-terminator assertion, the
lowering emits the conventional `\ No newline at end of file` marker
immediately after that side's line. `present` and unknown assertions emit no
marker. Construction requires a side-specific final-line-termination
difference to belong to a change, so a shared context line never has
contradictory marker state.

The Markdown renderer chooses a fence longer than any fence run in the content.
Caller text never occupies the fence language or another structural syntax
position.

### Rich inline lowering

A rich narrow renderer may keep one replacement together and emphasize
caller-issued inner mappings:

```text
3 ~     if (value [-<-]{+<=+} 0)
4 ~         return [-0-]{+1+};
```

The `~` and span delimiters above illustrate renderer chrome. They are not
line kinds in the semantic model.

### Side-by-side lowering

A wide renderer may align the mapped ranges:

```text
3  if (value < 0)   |  3  if (value <= 0)
4      return 0;    |  4      return 1;
```

Unequal range lengths use empty display cells. A formatter does not infer
additional line-to-line correspondence inside a many-to-many replacement.
Caller-issued inner mappings may guide emphasis without asserting a complete
line pairing.

The model is informed by the public VS Code/Monaco diff surface:

- [`ILineChange`](https://microsoft.github.io/monaco-editor/typedoc/interfaces/editor_editor_api.editor.ILineChange.html)
  maps original and modified line ranges;
- [`ICharChange`](https://microsoft.github.io/monaco-editor/typedoc/interfaces/editor_editor_api.editor.ICharChange.html)
  retains inner text mappings; and
- [`IDiffEditorBaseOptions`](https://microsoft.github.io/monaco-editor/typedoc/interfaces/editor_editor_api.editor.IDiffEditorBaseOptions.html)
  keeps unified, side-by-side, responsive, folded-context, accessibility, and
  move presentation separate from the change model.

These are design precedents, not dependencies.

### Structured lowering

Table, TSV, and JSONL lowerings expose fixed, unique fields sufficient to
recover change and side provenance. The vocabulary includes:

- change address and form;
- side;
- side-local line coordinate;
- Before and After range coordinates;
- text;
- inner mapping coordinates when present;
- annotation target and text when present; and
- final-line-terminator assertions when known.

The structured schema does not use rendered `-`, `+`, `~`, color, or spacing as
data. Tabs, line breaks, and non-graphic text follow the formatter's existing
machine-output containment rules.

One change may lower to several side-line records. Every record carries the
same change address, so consumers can regroup it without parsing display text.
Mandatory provenance fields cannot be removed through generic table column
projection, and generic row windows or caps cannot split or discard changes.

### ANSI lowering

ANSI renderers may add:

- line and side coordinates;
- addition, removal, and intraline emphasis;
- unified or side-by-side layout;
- wrapping;
- visible annotation gutters; and
- navigation hints when the host supports interaction.

The model does not contain colors or terminal escape sequences. Caller-supplied
control text remains inert.

[delta](https://github.com/dandavison/delta) demonstrates that these
capabilities can coexist with GNU/Git compatibility: syntax and word emphasis,
side-by-side wrapping, line numbers, navigation, copy-friendly source, and
moved-line styling are presentation choices over conventional diff data.

## Annotations

Annotations never alter the sequence text or change mapping. A copy operation
can therefore copy either exact side without removing carets, labels, or diff
markers.

A spatial renderer may place one annotation below its target:

```text
+    if (value <= 0)
                  ^^
                  Boundary now includes zero
```

A renderer without stable spatial layout may preserve the same annotation as a
subordinate record:

```text
After line 3: Boundary now includes zero
```

Both presentations retain the same target coordinates. The renderer may wrap
annotation prose, but it may not move the target or silently omit the
annotation.

Interactive selection, focus, hover, activation, annotation membership, and
editor state are consumer concerns. This shape supplies static targets only.

## Safety and platform contract

Mapped text diffs commonly present untrusted package, source, log, or generated
content. Each formatter must keep caller data out of structural syntax and
apply context-appropriate containment.

Sequence lines, labels, and annotation text are inert caller data. They cannot
inject a header, hunk, record, annotation geometry, or formatter control.

The implementation must define and test:

- malformed UTF-16;
- embedded carriage returns and line feeds;
- tabs and long lines;
- Markdown fence runs and inline syntax;
- table delimiters;
- terminal escapes and non-graphic text;
- bidirectional and zero-width controls;
- empty sequences and files without a final line terminator; and
- sequence labels and annotations containing syntax significant to each
  formatter.

The core shape and its validation remain reflection-free,
NativeAOT-compatible, and Browser/Wasm-compatible. A formatter may have a
narrower platform contract only when that exception is explicit and does not
infect the core shape.

## Source generation and capability boundary

The built-in mapped text diff type is recognizable by exact type identity.
Generated serializers dispatch it as a section shape in the same manner as
`Graph`.

Imperative callers use one diff-writing operation. Formatters advertise diff
support through a dedicated capability interface. Unsupported formatters
return unsupported through the existing capability contract; Markout does not
inject a foreign fallback syntax.

Reusable lowerings may adapt a diff to table rows or unified lines. They retain
the provenance contract above, bypass generic table projection and row
trimming, and do not return display strings without their source coordinates.

## Adopter evidence

The public contract is proved against at least two independent domains before
release:

1. A line-oriented source comparison projected from
   `FindingComparison<string>`.
2. An instruction comparison projected from `IlDiffDisplayResult`.

A C# replacement projected from `CSharpDiffDisplayResult` is the preferred
one-to-one replacement example when it fits the same development slice.

The adapters retain producer-owned wording, failure outcomes, coordinates, and
identity. They do not move comparison logic into Markout.

Development follows the established peer-checkout loop:

1. Develop and review Markout with the adopter using temporary source project
   references.
2. Merge and release Markout.
3. Return the adopter to the released package.
4. Raise the adopter change only against that package.

## Demo contract

The Markout PR demonstrates one replacement with two inner mappings and one
annotation through:

- GNU-compatible Markdown;
- structured rows with change and side provenance; and
- rich terminal-oriented output.

It also demonstrates a neighboring instruction or configuration comparison to
show that the public shape contains no source-language assumptions.

The demo calls the public shape and formatter APIs. It does not construct
expected output through a parallel template.

## Non-goals

- Computing line, word, syntax, or semantic differences.
- Inferring replacement, movement, or correspondence.
- Establishing whether equal-cardinality unchanged gaps contain equal text.
- Replacing caller-owned failure, Finding, provenance, or annotated-document
  models.
- Defining an editor, merge operation, or web interaction state.
- Requiring syntax highlighting or language parsing.
- Converting every API, metric, or implementation comparison table into a text
  diff.
- Defining a general span-annotated document shape outside a mapped diff.
