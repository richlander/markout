# Projection

**Projection trims markout output to specific columns and fields at runtime, without changing view models or source generation.**

## The Problem

Docker's `--format` flag lets users reshape CLI output using Go templates:

```bash
docker images --format "table {{.Repository}}\t{{.Tag}}\t{{.Size}}"
docker images --format "{{.Repository}}:{{.Tag}}"
```

This is powerful but unfriendly. Without pre-built templates in a script or config file, you fall off a cliff — the syntax is hard to remember and easy to get wrong. Docker doesn't ship pre-canned profiles either, so most users never touch `--format` beyond copy-pasting from Stack Overflow.

Markout's projection takes a different approach. Instead of a template language that reconstructs output from scratch, projection is **subtractive** — you start with the full shaped document and trim it down. You say *what data to keep*, not *how to arrange it*. The renderer still makes all visual decisions.

## How It Works

`MarkoutProjection` is a set of include/exclude filters at two granularities:

| Granularity | Include | Exclude | Target |
| --- | --- | --- | --- |
| **Column** | `IncludeColumns` | `ExcludeColumns` | Table headers → cells |
| **Field** | `IncludeFields` | `ExcludeFields` | Scalar field keys |

These compose with existing section filtering (`IncludeSections` / `ExcludeSections`) to give three levels of trimming:

```text
Section  →  narrows to specific H2 blocks
Column   →  narrows table columns within those blocks
Field    →  narrows scalar fields within those blocks
```

### Basic Example

```csharp
var options = new MarkoutWriterOptions
{
    Projection = new MarkoutProjection
    {
        IncludeColumns = ["Name", "TFM"],
        IncludeFields = ["Version", "License"]
    }
};

var writer = new MarkdownWriter(options);
// ... serialize any markout object ...
```

Tables render only the Name and TFM columns. Scalar fields render only Version and License. Everything else is silently trimmed. The same projection works identically with `OneLineWriter`, `AnsiWriter`, `MarkoutWriter`, or any custom renderer.

### Column Selection and Reordering

`IncludeColumns` is an ordered list, not a set. The output column order matches the list order:

```csharp
// Original table: Name | Version | TFM | Signed
// Output:         TFM | Name
Projection = new MarkoutProjection
{
    IncludeColumns = ["TFM", "Name"]
}
```

`ExcludeColumns` removes columns but preserves the original order:

```csharp
// Original table: Name | Version | TFM | Signed
// Output:         Name | TFM | Signed
Projection = new MarkoutProjection
{
    ExcludeColumns = ["Version"]
}
```

Column names match what you see in the table header — the display name, not the C# property name. Matching is case-insensitive by default.

### Field Selection

Works the same way for scalar fields (`WriteField`, `WriteFieldNoBreak`, `WriteFieldList`):

```csharp
// Only Name and License appear in output
Projection = new MarkoutProjection
{
    IncludeFields = ["Name", "License"]
}
```

Field names match the key passed to `WriteField(key, value)` — again the display name, case-insensitive.

### Composition

Projection composes with everything that already exists:

```csharp
var options = new MarkoutWriterOptions
{
    IncludeSections = new HashSet<string> { "Dependencies" },
    Projection = new MarkoutProjection
    {
        IncludeColumns = ["Name", "Version"]
    }
};
```

Evaluation order: **section filtering → shape support → column/field projection**. Each layer narrows further. They never conflict.

## Comparison with Go Templates

| Aspect | Go Templates (`docker --format`) | Markout Projection |
| --- | --- | --- |
| **Approach** | Additive — build output from scratch | Subtractive — trim from full output |
| **Syntax** | `"table {{.Repository}}\t{{.Tag}}"` | `IncludeColumns = ["Name", "Tag"]` |
| **Learning curve** | Steep — must know Go template syntax | Flat — list the names you want |
| **Renderer control** | None — you produce raw text | Full — renderer decides formatting |
| **Reordering** | Yes, by template position | Yes, by list order |
| **String interpolation** | Yes (`{{.Name}}:{{.Tag}}`) | No — output is always shaped |
| **Functions** | `upper`, `lower`, `truncate`, `pad` | No — renderer handles presentation |
| **Works across formats** | No — template produces one format | Yes — same projection for Markdown, ANSI, plain text |

The tradeoff is deliberate. Go templates give you full control over the output string, but you lose structured output and renderer portability. Projection keeps the output shaped and portable, but you can't do arbitrary string interpolation. For CLI tools that render the same data in multiple formats, projection is the better fit.

## Where Projection Lives in the Architecture

### Source Generation: Not Involved

Source generation maps C# property types to shapes at compile time (`string` → Identity, `List<T>` → Tabulation, `List<Metric>` → Measurement). It emits calls like `writer.WriteField("Name", value.Name)` and `writer.WriteTableStart("Col1", "Col2")`.

**Projection doesn't change source generation.** The generated code is the same whether or not a projection is applied. Projection operates at render time, downstream of serialization.

### Writer Base Class: Where Projection Happens

All projection logic lives in `MarkoutWriter` — the base class that all renderers extend. This is the same level where section filtering already lives.

```text
Source Generator → emits calls → MarkoutWriter (base)
                                    ├── section filtering (existing)
                                    ├── column projection (new)
                                    ├── field projection (new)
                                    └── dispatches to virtual methods
                                            ├── MarkdownWriter
                                            ├── OneLineWriter
                                            ├── AnsiWriter
                                            └── ...
```

When `WriteTableStart(headers)` is called, the base class computes a column index map from the projection. Subsequent `WriteTableRow(values)` calls remap cells through this map before adding them to the streaming buffer. By the time `FlushStreamingTable` dispatches to the renderer subclass, the data is already projected.

When `WriteField(key, value)` is called, the base class checks the projection and returns early if the field is excluded. The renderer never sees it.

### Renderers: Zero Complexity

**Renderer authors don't need to know about projection.** If you write a custom `MarkoutWriter` subclass, projection just works — your `FlushStreamingTable` override receives already-projected headers and rows, and excluded fields never reach your `WriteFieldName` override.

This follows the same pattern as section filtering: the base class handles the decision, subclasses handle the rendering.

### Shapes: Projection Applies to Identity and Tabulation

Of markout's shape vocabulary, projection targets two:

| Shape | Projection | Mechanism |
| --- | --- | --- |
| **Identity** (Fields, FieldList) | `IncludeFields` / `ExcludeFields` | Filter by key name |
| **Tabulation** (Tables) | `IncludeColumns` / `ExcludeColumns` | Filter and reorder by header name |

Other shapes (Trees, Metrics, Breakdowns, Descriptions, Code, Callouts) pass through unchanged. They don't have named sub-elements that a user would want to select individually.

Section filtering remains the coarse-grained control for everything — it operates on the document structure, not individual shapes.

## For LLM and Agent Consumers

Projection makes markout output **token-efficient and parseable** for LLM workflows. Instead of receiving a full document and extracting what you need, you request only the fields and columns that matter.

### Example: SKILL.md Paragraph

```markdown
## Output Projection

Use `--columns` and `--fields` to trim output to exactly what you need:

- **`--columns Name,Version`** — show only these table columns, in this order
- **`--fields Name,License`** — show only these scalar fields
- **`-s Dependencies --columns Name`** — combine with section filtering

Prefer `--columns` over `--oneline` for token-efficient scanning.
Prefer `--fields` over `-v:q` when you need specific values, not less detail.
Both options work with any output format (markdown, oneline, JSON).
```

### Why This Matters for Agents

1. **Predictable output shape.** An agent requesting `--columns Name,Version` knows exactly what columns appear — no parsing surprises, no version-dependent extra columns.

2. **Token efficiency.** A 15-column table trimmed to 2 columns is 7× fewer tokens. For scanning large API surfaces or dependency lists, this adds up fast.

3. **Composable queries.** Section filtering picks the block, column filtering picks the data within it. An agent can progressively narrow: first `-s Dependencies` to find the right section, then `--columns Name` to extract just the names.

4. **Format-independent.** The same projection produces trimmed Markdown (for rendering), trimmed oneline (for grep), or trimmed plain text (for piping). An agent doesn't need different strategies per output format.
