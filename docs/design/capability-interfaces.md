# Capability Interfaces on Writers

## Problem

MarkoutWriter uses inheritance-only for rendering customization. Subclasses
override virtual methods, but there's no type-system way to check what a writer
supports — `SupportedShapes` flags enum is the only mechanism, checked at
runtime. OneLineWriter is awkwardly special-cased, and the serializer hardcodes
`new MarkdownWriter()`, making all writer types reachable (no trimmability).

## Design

Split each virtual method into **infrastructure** (stays in base) and
**rendering** (dispatched via capability interface). Each writer implements only
the interfaces matching its capabilities.

### Capability interfaces

```
IHeadingFormatter      — FormatHeading(TextWriter, level, text, context)
IFieldFormatter        — FormatFields(TextWriter, fields, bold)
                         FormatFieldName(TextWriter, key, bold)
ITableFormatter        — FormatTable(TextWriter, headers, rows, skippedRows, options)
IListFormatter         — FormatListItem(TextWriter, text)
                         FormatArray(TextWriter, key, items, bold)
ICodeBlockFormatter    — FormatCodeStart(TextWriter, language)
                         FormatCodeEnd(TextWriter)
IBlockFormatter        — FormatCallout(TextWriter, severity, message)
                         FormatQuotation(TextWriter, text)
                         FormatRule(TextWriter)
                         FormatDescription(TextWriter, item)
IMetricsFormatter      — FormatBreakdown(TextWriter, items, ...)
                         FormatMetrics(TextWriter, items, ...)
                         FormatVerticalMetrics(TextWriter, items, ...)
```

### Base class dispatch pattern

Every virtual method follows the same pattern:

```csharp
public virtual void WriteHeading(int level, string text, string? context)
{
    // 1. Validation
    // 2. Infrastructure (section tracking, filtering)
    // 3. Spacing (blank lines)
    // 4. Dispatch: if (this is IHeadingFormatter hf) hf.FormatHeading(...)
    //    else { plain-text fallback }
    // 5. Post-render state (HasContent, NeedsBlankLine)
}
```

### Writer implementations

| Writer | Interfaces | Notes |
|--------|-----------|-------|
| MarkdownWriter | All 7 | Overrides removed; all rendering via explicit interface impls |
| OneLineWriter | ITableFormatter, IFieldFormatter, IListFormatter | Orchestration overrides stay (field buffering, heading flush) |
| UnicodeWriter | — (unchanged) | Stays override-based; can adopt interfaces later |
| DiagramWriter | — (unchanged) | Stays override-based; can adopt interfaces later |
| MarkoutWriter base | — | Dispatches to interfaces; plain-text fallback when none implemented |

### Key detail: WriteFieldName

`WriteFieldName` is `protected virtual` and called from multiple base class
methods (WriteFieldsInline, WriteFieldsBulleted, WriteFieldsNumbered). The base
class changes it from virtual to a non-virtual dispatch:

```csharp
protected void WriteFieldName(string key)
{
    if (this is IFieldFormatter ff)
        ff.FormatFieldName(Writer, key, BoldFieldNames);
    else
    {
        Writer.Write(key);
        Writer.Write(": ");
    }
}
```

UnicodeWriter currently overrides WriteFieldName but its override is identical
to the base — no behavior change.

### API surface

No new public API on MarkoutWriter or MarkoutSerializer. The interfaces are the
new public surface. Existing `Serialize` overloads work unchanged.

## Verification

- `dotnet build -c Release` — zero warnings
- `dotnet test -c Release` — all tests pass (output unchanged)
- Interface capability checks:
  - `new MarkdownWriter() is ITableFormatter` → true
  - `new OneLineWriter(tw) is ITableFormatter` → true
  - `new OneLineWriter(tw) is IHeadingFormatter` → false
  - `new MarkoutWriter() is ITableFormatter` → false

## Files

### New
- `src/Markout/Formatting/` — one file per interface (or single file)

### Modified
- `src/Markout/MarkoutWriter.cs` — infrastructure/dispatch split
- `src/Markout/MarkdownWriter.cs` — overrides → explicit interface impls
- `src/Markout/OneLineWriter.cs` — rendering → interface impls, keep orchestration

### Unchanged
- UnicodeWriter, DiagramWriter, MarkoutSerializer, source generator
