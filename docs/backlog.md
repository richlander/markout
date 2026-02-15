# Backlog

## CodeSection — code blocks as section content

A `CodeSection` record type recognized by the source generator, allowing code blocks
to appear as properties in serializable types — including inside nested subsection items.

```csharp
record CodeSection(string Language, string Content);
```

When used as a property on a `[MarkoutSerializable]` type, the generator emits
`WriteCodeBlockStart(language)` / `WriteParagraph(content)` / `WriteCodeBlockEnd()`.

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
- Emit `WriteCodeBlockStart`/`WriteParagraph`/`WriteCodeBlockEnd` sequence
- No new shape flag needed — uses existing `CodeBlocks` shape
- Also add `List<CodeSection>` recognition for types with multiple code blocks

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

## DefinitionItem — definition lists

A `List<DefinitionItem>` type that renders as a definition list. Similar to `LabeledItem`
but semantically different — no bullet prefix, renders as `<dl>` in HTML.

```csharp
record DefinitionItem(string Term, string Definition);
```

Rendered as:

```text
**Term**
  Definition text here.
```

In markdown: bold term on its own line, indented definition. In ANSI: bold/colored term.
In HTML: `<dt>`/`<dd>` tags. Follows the BarItem/LabeledItem source-gen pattern.

## StatusItem — progress gauges

A `List<StatusItem>` type that renders as horizontal progress bars.

```csharp
record StatusItem(string Label, double Progress, double Max = 100);
```

Rendered as:

```text
Build     [████████░░░░░░░░] 50%
Tests     [████████████░░░░] 75%
Coverage  [██████████████░░] 92%
```

ANSI renderer can color-code by percentage (green/yellow/red). Follows the BarItem pattern
but with fill-gauge rendering instead of proportional bars.

## LinkItem — link lists

A `List<LinkItem>` type that renders as a list of hyperlinks.

```csharp
record LinkItem(string Text, string Url);
```

Rendered as:

- Markdown: `- [Text](url)`
- ANSI: underlined clickable link (OSC 8)
- Plain: `- Text (url)`

## Blockquote

A writer-level method (no source-gen collection type needed):

```csharp
writer.WriteBlockquote("This is a quoted passage.");
```

Renders as `> text` in markdown, indented/colored in ANSI, indented in plain text.

## HorizontalRule

A writer-level separator method:

```csharp
writer.WriteHorizontalRule();
```

Renders as `---` in markdown, `────────` in ANSI, blank line in plain text.
Useful between sections when headings are suppressed.

## OrderedList

A numbered variant of the existing bullet list:

```csharp
writer.WriteOrderedList(items);
```

Renders as `1. item` / `2. item` instead of `- item`. Could also be a
`WriteArray` overload with a `numbered: true` parameter.

## Callout / Admonition

Severity-tagged message blocks:

```csharp
writer.WriteCallout(CalloutSeverity.Warning, "This API is deprecated.");
```

Renders as GitHub-flavored `> [!WARNING]` in markdown, colored box in ANSI,
`WARNING: ...` in plain text. Severities: Note, Tip, Important, Warning, Caution.

## Diagram shapes

Future diagram-oriented renderers (extending DiagramWriter):

### FlameGraph

Nested call stack visualization. Record type TBD — likely a tree of
`(string Name, double Duration)` nodes rendered as stacked horizontal bars.

### Gantt / Timeline

Sequential labeled time spans for build pipelines, deployment stages, etc.

```csharp
record TimelineItem(string Label, DateTimeOffset Start, DateTimeOffset End, string? Status = null);
```

### Matrix

2D grid with row/column headers and cell values (pivot table).
Could extend the existing table shape with a `WriteMatrix` method that takes
row headers, column headers, and a 2D value array.
