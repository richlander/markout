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

## Nested serializable types in sections

When a `[MarkoutSection]` contains `List<T>` where `T` is itself `[MarkoutSerializable]`,
recursively serialize each item instead of rendering as table rows. Heading levels
increment automatically (section H2 → item title H3 → nested sections H4, etc.).

Today the serializer treats any `List<SerializableRecord>` as a table. The new behavior
would apply when the element type has its own sections or nested collections — indicating
it's a nested view, not a flat row.

### Use case: API diff hierarchy

`dotnet-inspect`'s diff command renders breaking/additive changes grouped by type.
The current formatter uses imperative `MarkoutWriter` calls because the serializer
can't express the H2→H3→list nesting:

- `DiffOutputFormatter.RenderFullMarkdown` — [src/dotnet-inspect/Output/DiffOutputFormatter.cs](https://github.com/AkkaNetContrib/dotnet-inspect/blob/main/src/dotnet-inspect/Output/DiffOutputFormatter.cs) (lines 67–97)
- `WriteSection` helper — same file (lines 108–143)

With nested serialization, the view model would be:

```csharp
[MarkoutSerializable(TitleProperty = nameof(Title), DescriptionProperty = nameof(Description))]
public class DiffFullView
{
    [MarkoutIgnore] public string Title { get; set; } = "";
    [MarkoutIgnore] [MarkoutSkipNull] public string? Description { get; set; }
    public string Versions { get; set; } = "";
    public string Summary { get; set; } = "";

    [MarkoutSection(Name = "Breaking Changes")]
    public List<DiffTypeChanges>? Breaking { get; set; }

    [MarkoutSection(Name = "Potentially Breaking")]
    public List<DiffTypeChanges>? PotentiallyBreaking { get; set; }

    [MarkoutSection(Name = "Additive Changes")]
    public List<DiffTypeChanges>? Additive { get; set; }
}

[MarkoutSerializable(TitleProperty = nameof(Type))]
public class DiffTypeChanges
{
    [MarkoutIgnore] public string Type { get; set; } = "";
    public List<string>? Changes { get; set; }  // → WriteArray (bullet list)
}
```

Producing:

```markdown
# API Diff: System.Text.Json

## Breaking Changes

### JsonSerializer
- Member removed: Serialize(object)
- Member signature changed: `Deserialize(string)` → `Deserialize(ReadOnlySpan<char>)`

### JsonElement
- Type kind changed

## Additive Changes

### JsonSerializerOptions
- Member added: PropertyNameCaseInsensitive
```

### Implementation notes

Source generator change: add a `PropertyKind.NestedList` (or similar) in `SerializerEmitter`.
When the element type of a `[MarkoutSection]` list is itself `[MarkoutSerializable]` and has
sections or nested collections, emit recursive `Serialize(item, writer)` calls instead of
`WriteTable`. The heading level for nested items should be `parentSectionLevel + 1`.
