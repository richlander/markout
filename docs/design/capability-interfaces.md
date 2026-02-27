# Capability Interfaces and Orchestrator

## Problem

MarkoutWriter uses inheritance for rendering customization, infrastructure,
and shape vocabulary — three roles in one class. Subclasses override virtual
methods, but there's no type-system way to check what a writer supports.
The `SupportedShapes` flags enum is checked at runtime and unsupported shapes
write warnings to stderr — a side effect the caller can't inspect or react to.

## Design Principles

1. **No data is lost.** Unsupported shapes are reported through the type
   system (return values), not silently dropped.
2. **No mixing of formats.** The formatter owns the rendering. There is no
   plain-text fallback injected into a format with its own syntax (JSON, CSV,
   Markdown). Unsupported means not rendered, period.
3. **Markout only writes to the streams it is given.** No stderr, no
   Console.Error, no side effects. Diagnostics flow through return values.
4. **Capabilities are type-checked.** The formatter declares what it supports
   via which interfaces it implements. The orchestrator checks at the call
   site.

## Architecture

Three roles, separated:

### MarkoutOrchestrator

The thing the serializer (and hand-written callers) talk to. Owns:

- Shape vocabulary (WriteHeading, WriteFields, WriteTable, etc.)
- Section tracking and filtering (IncludeSections/ExcludeSections)
- Blank line and spacing management
- State (HasContent, NeedsBlankLine, InCode, InTable)
- Streaming table buffering (WriteTableStart/Row/End)
- Capability dispatch to the formatter
- `bool` return from all Write methods (true = rendered, false = unsupported)

Does NOT own rendering. Does NOT implement any formatter interface. Does NOT
write to stderr.

### Formatter interfaces (capability model)

Pure rendering contracts. Each method takes `TextWriter` + data, writes
formatted output. No state management, no section awareness.

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

### Formatter implementations

Concrete classes implementing subsets of the interfaces. No base class.
No inheritance relationship between them.

| Formatter | Interfaces | Notes |
|-----------|-----------|-------|
| MarkdownFormatter | All 7 | Full capability. Pipe tables, fences, bold fields. |
| OneLineFormatter | ITableFormatter, IFieldFormatter, IListFormatter | Space-padded columns, uppercase headers. Field buffering is orchestration concern. |
| UnicodeFormatter | (later) | Box-drawing, bar charts. Can adopt interfaces incrementally. |
| DiagramFormatter | (later) | Trees and metrics only. |

## Interface hierarchy

### Marker interface

`IMarkoutFormatter` is a bare marker (no members). It serves as a generic
constraint for `MarkoutOrchestrator<TFormatter>`, enabling JIT devirtualization
of capability checks. Implementing it alone gives zero capabilities — every
Write method returns `false`.

### Capability interfaces (fine-grained)

Individual rendering contracts. A formatter implements the subset it supports:

```
IHeadingFormatter       — headings (H1–H6)
IFieldFormatter         — key-value fields (bold keys, colon separators)
ITableFormatter         — batch tabular data (headers + all rows)
IStreamingTableFormatter — streaming tabular data (Begin/Data/End)
IListFormatter          — single-column lists and labeled arrays
ICodeBlockFormatter     — fenced code blocks
IBlockFormatter         — callouts, quotations, rules, descriptions
IMetricsFormatter       — breakdowns, bar charts, vertical metrics
```

### Aggregate interfaces (convenience groupings)

Sum types that compose the fine-grained interfaces. A formatter can implement
an aggregate instead of listing every interface individually.

```csharp
// Full document rendering — the set of capabilities for structured output
public interface IDocumentFormatter :
    IHeadingFormatter, IFieldFormatter, ITableFormatter,
    IListFormatter, ICodeBlockFormatter, IBlockFormatter { }

// MarkdownWriter implements IDocumentFormatter + IMetricsFormatter
// OneLineWriter implements ITableFormatter + IFieldFormatter + IListFormatter
```

Aggregates don't add methods — they're purely for convenience and constraint
clarity. The orchestrator still checks individual interfaces at runtime.

### LINQ-style cascade

The orchestrator tests from most specific to most general when dispatching.
For example, when rendering fields:

```
1. formatter is IFieldFormatter      → field-specific rendering (bold keys, etc.)
2. formatter is ITableFormatter      → render as 2-column table (batch)
3. formatter is IStreamingTableFormatter → stream as 2-column data
4. return false                      → unsupported
```

This means a formatter that only implements `ITableFormatter` can still render
fields — the orchestrator falls through to the table path automatically.
No buffering option needed; the formatter's interface list IS the configuration.

## Data shapes and interaction patterns

### Column count is the data concern

| Shape | Columns | Interface |
|-------|---------|-----------|
| List | 1 | IListFormatter |
| Fields | 2 (fixed: key + value) | IFieldFormatter or ITableFormatter |
| Table | N | ITableFormatter |

Fields are a specialization of tabular data where N = 2 and column 0 has key
semantics. The data shape is not lossy — the formatter knows the first column
is a key and can style it accordingly (bold, colon, etc.). The app chooses the
rendering path explicitly:

- `WriteFieldsInline(fields)` → pipe-separated, no table
- `WriteFieldsTable(fields)` → 2-column table
- `WriteFieldsBulleted(fields)` → `- Key: Value` list

### Interaction patterns for rows

Three interaction patterns, from simplest to most efficient:

| Pattern | Type | Streaming? | Notes |
|---------|------|-----------|-------|
| Batch | `T[]` / `IList<T>` | No | Caller has everything; formatter can calculate widths |
| Enumerable | `IEnumerable<T>` | Yes (pull) | Default API; orchestrator can check `is IList<T>` for fast path |
| Manual | `BeginTable` / `WriteData` / `EndTable` | Yes (push) | Zero-alloc hot path; row written immediately |

The batch interface (`ITableFormatter.FormatTable`) is the "I have everything"
fast path — like LINQ checking for `IList<T>` before enumerating.

The streaming interface (`IStreamingTableFormatter`) follows the
Begin/Data/End pattern. Each row is rendered immediately without buffering.
Column width strategies vary by formatter:

- **Min-width**: standard padding, compact output (jagged right edge)
- **Header-estimating**: uses header widths as alignment targets (good
  alignment without seeing all data)
- **Full-width**: buffers all rows for perfect alignment (batch fallback)

This design is informed by the streaming table model in
[smooth-markdown-table](https://github.com/richlander/smooth-markdown-table),
which demonstrated that column widths don't need all data upfront — they can
be decided per-row at render time using a `ColumnPlan` struct with
`ContentWidth`, `LeadingPadding`, and `TrailingPadding`.

### Streaming table interface

```csharp
public interface IStreamingTableFormatter
{
    /// Called once when the table begins. Formatter sees column count + header widths.
    void BeginTable(TextWriter writer, string[] headers, MarkoutWriterOptions options);

    /// Called for each data row. Formatter decides padding, row written immediately.
    void WriteRow(TextWriter writer, string[] values);

    /// Called when the table ends. Formatter performs cleanup.
    void EndTable(TextWriter writer, int skippedRows);
}
```

### Orchestrator dispatch for tables

```csharp
// Batch path
public bool WriteTable(string[] headers, IEnumerable<string[]> rows)
{
    // LINQ-style: check for materialized collection first
    if (_formatter is ITableFormatter tf && rows is IList<string[]> list)
    {
        tf.FormatTable(_writer, headers, list, skipped, _options);
        return true;
    }

    // Streaming path
    if (_formatter is IStreamingTableFormatter stf)
    {
        stf.BeginTable(_writer, headers, _options);
        foreach (var row in rows)
            stf.WriteRow(_writer, row);
        stf.EndTable(_writer, 0);
        return true;
    }

    // Batch fallback: materialize and use batch formatter
    if (_formatter is ITableFormatter tf2)
    {
        var rowList = rows.ToList();
        tf2.FormatTable(_writer, headers, rowList, skipped, _options);
        return true;
    }

    return false;
}
```

## Dispatch pattern

Every Write method on the orchestrator follows:

```csharp
public bool WriteHeading(int level, string text, string? context)
{
    // 1. Validation
    if (level < 1 || level > 6)
        throw new ArgumentOutOfRangeException(nameof(level));

    // 2. Infrastructure (section tracking)
    UpdateSectionState(level, text);
    if (SectionExcluded) return true; // filtered, not unsupported

    // 3. Capability check
    if (_formatter is not IHeadingFormatter hf)
        return false; // unsupported shape — caller decides what to do

    // 4. Spacing
    if (HasContent) _writer.WriteLine();

    // 5. Dispatch
    hf.FormatHeading(_writer, level, text, context);

    // 6. Post-render state
    _writer.WriteLine();
    HasContent = true;
    NeedsBlankLine = true;
    return true;
}
```

Key differences from the current model:
- Returns `bool`, not `void` — unsupported is a value, not a side effect
- No fallback rendering — `return false` with nothing written
- No stderr warnings — caller inspects the return value
- Formatter is a composed dependency (`_formatter`), not `this`

## Construction

```csharp
// Serializer path — formatter chosen by caller
var formatter = new MarkdownFormatter();
var orch = new MarkoutOrchestrator(Console.Out, formatter);
orch.WriteHeading(1, "My Report");
orch.WriteFields(new("Status", "OK"));
var rendered = orch.WriteBreakdown(items); // false if formatter can't

// Convenience — string result
var orch = new MarkoutOrchestrator(new MarkdownFormatter());
orch.WriteHeading(1, "Title");
string md = orch.ToString();

// dotnet-inspect — format selection
var formatter = format switch
{
    OutputFormat.Markdown => new MarkdownFormatter(),
    OutputFormat.OneLine => new OneLineFormatter(),
    _ => new MarkdownFormatter()
};
var orch = new MarkoutOrchestrator(output, formatter, options);
context.Serialize(value, orch);
```

## Migration path

### Phase 1 (done)
Capability interfaces extracted. MarkdownWriter and OneLineWriter implement
them as explicit interface impls. Base class dispatches via `this is IXxx`.
All tests pass.

### Phase 2 (done)
- `MarkoutOrchestrator<TFormatter>` introduced as standalone generic class
- `IMarkoutFormatter` marker interface added
- MarkdownWriter and OneLineWriter implement `IMarkoutFormatter`
- Write methods return `bool` (true = rendered/filtered, false = unsupported)
- No stderr warnings — unsupported is a return value
- Static factory `MarkoutOrchestrator.Create()` for type inference
- 70 orchestrator tests pass alongside all existing tests
- Existing MarkoutWriter path unchanged (source generator still targets it)

### Phase 3 (done)
- Aggregate `IDocumentFormatter` interface (composes 6 core interfaces)
- `IStreamingTableFormatter` with BeginTable/WriteRow/EndTable pattern
- LINQ-style cascade dispatch in orchestrator (field → table → streaming fallback)
- `BufferFieldsAsTable` removed (cascade makes it unnecessary)
- UnicodeWriter implements `IMarkoutFormatter` + all capability interfaces
- 13 new orchestrator tests for cascade, streaming, aggregate, and UnicodeWriter

### Phase 4 (later)
- Rename MarkdownWriter → MarkdownFormatter, OneLineWriter → OneLineFormatter
- Update serializer to target orchestrator
- Source generator emits orchestrator API
- Remove MarkoutWriter base class entirely
- `SupportedShapes` flags enum removed (interfaces are the capability model)
