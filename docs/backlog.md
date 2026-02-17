# Backlog

## ~~CodeSection — code blocks as section content~~ ✅ Done

## ~~Callout / Admonition~~ ✅ Done

## Conditional column visibility

A `CodeSection` record type recognized by the source generator, allowing code blocks
to appear as properties in serializable types — including inside nested subsection items.

```csharp
record CodeSection(string Language, string Content);
```

When used as a property on a `[MarkoutSerializable]` type, the generator emits
`WriteCodeStart(language)` / `WriteParagraph(content)` / `WriteCodeEnd()`.

### Use case: Constructor overloads in dotnet-inspect

`RenderConstructorEmphasis` manually writes H3 + code block + parameter table per
overload. With CodeSection + nested serializable types, this becomes declarative:

```csharp
[MarkoutSerializable(TitleProperty = nameof(Title))]
public class ConstructorOverload
{
    [MarkoutIgnore] public string Title { get; set; } = "";
    public CodeSection Signature { get; set; }

    [MarkoutSection(Name = "Parameters")]
    public List<ParameterRow>? Parameters { get; set; }
}
```

Also eliminates the repeated Heading + CodeBlock pattern in IL, source, lowered C#,
and samples rendering (4+ instances in `ApiOutputFormatter`).

### Implementation notes

- New `PropertyKind.CodeSection` in source generator
- Add `CodeSection` to `KnownTypeSymbols`
- Emit `WriteCodeStart`/`WriteParagraph`/`WriteCodeEnd` sequence
- No new shape flag needed — uses existing `Code` shape
- Also add `List<CodeSection>` recognition for types with multiple code sections

## Conditional column visibility

An attribute that controls whether a table column is rendered, eliminating the need
for dual list properties (e.g., `ConstructorRows` / `ConstructorRowsWithDocs`).

```csharp
[MarkoutSerializable]
public class EnumValueRow
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";

    [MarkoutColumnWhen(nameof(ApiTypeView.ShowDocs))]
    public string? Description { get; set; }
}
```

When the controlling property is false/null, the column is omitted from the table
entirely — headers, separator, and all row cells skip it.

### Use case: dotnet-inspect ApiTypeView

Currently has 5 pairs of dual lists to toggle the Description column:

```csharp
[MarkoutSection(Name = "Values", IgnoreProperty = nameof(EnumValueRow.Description))]
public List<EnumValueRow>? EnumValues { get; set; }

[MarkoutSection(Name = "Values")]
public List<EnumValueRow>? EnumValuesWithDocs { get; set; }
```

With `[MarkoutColumnWhen]`, this becomes a single list.

### Implementation notes

- New attribute: `MarkoutColumnWhenAttribute(string propertyName)`
- Source generator: when emitting table headers/rows, check the controlling property
  and skip the column if false/null
- The controlling property must be on the parent type (the one with the section),
  not the element type — requires threading the parent context through emission

## LineBreaksBr field layout

Add a `LineBreaksBr` value to `FieldLayout` that renders each field on its own line using an explicit HTML `<br>` tag instead of trailing double-space (`  `) or plain newlines.

- `LineBreaks` — plain newlines (for terminals / plain text)
- `LineBreaksDoubleSpace` — trailing `  ` (markdown hard line break)
- `LineBreaksBr` — trailing `<br>` (explicit HTML tag)

## ~~DefinitionItem — definition lists~~ → Use Description

Same data relationship as `Description` (term + explanatory text). Per the
[shape admission criteria](design/shape-system.md), this fails criterion #2
(semantically distinct). Use `Description` with renderer-specific formatting
for `<dl>` output.

## ~~StatusItem — progress gauges~~ → Use Metric with options

Same data relationship as `Metric` (labeled quantity). A progress gauge is
a measurement relative to a maximum. This can be a rendering option on the
Metric shape rather than a separate type.

## ~~LinkItem — link lists~~ → Use format attribute

A link is a reference relationship on a field, not a collection shape.
Prefer `[MarkoutLink]` attribute on string properties.

## Quotation ✅ Accepted

A writer-level method (no source-gen collection type needed). Distinct from
CodeSection: prose quotation vs. verbatim code quotation.

```csharp
writer.WriteQuotation("This is a quoted passage.");
```

Renders as `> text` in markdown, indented/colored in ANSI, indented in plain text.

## Rule ✅ Accepted

A writer-level separator method:

```csharp
writer.WriteRule();
```

Renders as `---` in markdown, `────────` in ANSI, blank line in plain text.
Useful between sections when headings are suppressed.

## OrderedList → Parameter on WriteArray

A numbered variant is a visual parameter (`numbered: true`), not a distinct
data relationship. Fails criterion #2 (semantically distinct).

## Matrix ✅ Accepted

2D grid with row/column headers and cell values (pivot table).
Distinct from flat Table: has both row and column headers. Passes all five criteria.

```csharp
writer.WriteMatrix(rowHeaders, colHeaders, values);
```

## Diagram shapes

Future diagram-oriented renderers (extending DiagramWriter):

### FlameGraph — Deferred

Nested call stack visualization. Fails criterion #3 (multi-renderer) — hard
to express meaningfully in plain text. Needs more design work.

### Gantt / Timeline — Deferred

Sequential labeled time spans. Same multi-renderer concern. Needs more design work.
