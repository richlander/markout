# Backlog

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
