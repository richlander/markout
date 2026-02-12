# Backlog

## LineBreaksBr field layout

Add a `LineBreaksBr` value to `FieldLayout` that renders each field on its own line using an explicit HTML `<br>` tag instead of trailing double-space (`  `) or plain newlines.

- `LineBreaks` — plain newlines (for terminals / plain text)
- `LineBreaksDoubleSpace` — trailing `  ` (markdown hard line break)
- `LineBreaksBr` — trailing `<br>` (explicit HTML tag)

## MarkoutField — structured labeled list

A `List<MarkoutField>` type that renders as a numbered list with a bold label,
separator, description, and optional detail line.

```csharp
record MarkoutField(string Label, string Description, string? Detail = null);
```

Rendered output:

```text
  1. **Insight** -- What does the generic math hierarchy look like?
     dotnet-inspect api System.Runtime "INumber<TSelf>" --shape
  2. **Discovery** -- What can JsonSerializer do?
     dotnet-inspect api System.Text.Json JsonSerializer
```

Markout handles numbering, column alignment, bold rendering (ANSI on TTY),
and the `--` separator. The `Detail` line is optional and indented to align
with the description. This is a general-purpose pattern useful for any
categorized list (demos, search results, diagnostics).
