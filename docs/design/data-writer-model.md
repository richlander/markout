# Markout Data and Writer Model

This document defines the formal model for Markout's data shapes, writer architecture, and rendering pipeline.

## Core Principles

1. **Shapes represent data relationships, not visual forms.** A shape describes the semantic structure of data (key-value pairs, hierarchies, tabular rows), not how it looks when rendered.

2. **Layout is a presentation concern.** The same data shape can be rendered in multiple layouts. Layout decisions belong to either the caller (via method choice) or the writer (via its rendering strategy).

3. **Writers declare capabilities, not restrictions.** A writer specifies what shapes it supports. Unsupported shapes produce diagnostics and are skipped.

4. **Data flows through a unified type system.** `MarkoutField` is the canonical representation for key-value data regardless of how it's rendered.

## Data Shapes

The `MarkoutShape` enum defines the vocabulary of data relationships:

| Shape | Data Relationship | Examples |
|-------|-------------------|----------|
| **Headings** | Identity/naming | Document title, section headers |
| **Paragraphs** | Prose content | Descriptions, explanations |
| **Fields** | Key-value pairs | Properties, metadata, attributes |
| **Tables** | Uniform columnar rows | Lists of records, comparison data |
| **Lists** | Ordered/unordered items | Features, steps, options |
| **Trees** | Hierarchical parent-child | Org charts, file structures |
| **Graphs** | Directed relationships between deduplicated nodes | Call graphs, dependency graphs |
| **TextDiffs** | Correspondence between ordered text sequences | Source, instruction, configuration diffs |
| **Descriptions** | Term with explanation | Glossaries, definitions |
| **Metrics** | Labeled measurements | Test counts, performance data |
| **Breakdowns** | Proportional composition | Category distributions |
| **Code** | Fenced source regions | Code samples, configurations |
| **Quotation** | Attributed prose | Block quotes |
| **Callouts** | Attention blocks | Notes, warnings, tips |

### Fields: The Unified Key-Value Shape

`Fields` is the single shape for all key-value data. The underlying data type is:

```csharp
public readonly record struct MarkoutField(string Key, string? Value);
```

**Layout is orthogonal to the shape.** The same field data can be rendered as:

| Layout | Visual Form | Method |
|--------|-------------|--------|
| Vertical | One field per line | `WriteField()` or `WriteFields()` |
| Inline | Pipe-separated on one line | `WriteFieldsInline()` |
| Tabular | Two-column Field/Value table | `WriteFieldsTable()` |
| Bulleted | Bullet list items | `WriteFieldsBulleted()` |
| Numbered | Numbered list items | `WriteFieldsNumbered()` |

All these methods operate on `Fields` shape data. The choice of method determines layout, not the underlying data relationship.

### Removed: FieldList Shape

Previously, `MarkoutShape.FieldList` was a separate shape for inline field rendering. This was incorrect - it conflated presentation with data structure. `FieldList` has been removed from the enum.

`WriteFieldsInline()` now operates under the `Fields` shape, as it should - it's just an inline layout for field data.

## Writer Architecture

### Base Class: MarkoutWriter

All writers inherit from `MarkoutWriter` and declare their capabilities:

```csharp
public abstract class MarkoutWriter
{
    public virtual MarkoutShape SupportedShapes => MarkoutShape.All;
}
```

### Writer Implementations

| Writer | Purpose | Supported Shapes |
|--------|---------|------------------|
| **MarkoutWriter** | Plain text baseline | All |
| **MarkdownFormatter** | GitHub-flavored markdown | All |
| **TableFormatter** | Compact table or TSV output | Tables, Lists, Fields |
| **UnicodeFormatter** | Box-drawing characters | All |
| **SpectreFormatter** | Terminal with color | All |
| **DiagramFormatter** | Visualization specialist | Headings, Trees, Metrics |

### Shape Support and Fallback

When a writer encounters an unsupported shape:

1. Check `SupportedShapes` - if supported, render normally
2. If unsupported but in `SuppressedShapes`, skip silently
3. Otherwise, emit a warning to stderr (once per shape) and skip

Writers may implement **shape adaptation** - rendering one shape's data using another shape's format. For example, `TableFormatter` can render `Fields` as compact tabular output.

## FieldLayout Enum

The `FieldLayout` enum controls how source-generated code emits field data:

```csharp
public enum FieldLayout
{
    Vertical,   // WriteFields() - each on own line
    Inline,     // WriteFieldsInline() - pipe-separated
    Bulleted,   // WriteFieldsBulleted() - bullet list
    Numbered    // WriteFieldsNumbered() - numbered list
}
```

This is a **source generation concern**, not a shape distinction. The generated code chooses which write method to call based on the layout, but all layouts emit `Fields` shape data.

## Data Flow

```text
Source Data (object properties)
    ↓
Source Generator (applies FieldLayout)
    ↓
Writer Method (WriteField, WriteFields, WriteFieldsInline, etc.)
    ↓
Shape Check (SupportedShapes)
    ↓
Writer-Specific Rendering
    ↓
Output (markdown, plain text, ANSI, etc.)
```

## TableFormatter Behavior

`TableFormatter` produces compact table output suitable for CLI tools. It supports:

- **Tables** - Rendered with space-padded columns or normalized TSV via `MarkoutTableMode`
- **Lists** - Rendered as plain lines
- **Fields** - Rendered as a two-column table (via shape adaptation)

In JSONL mode, section boundaries are presentation metadata and do not add records or
blank lines. Rows from adjacent sections therefore form one uninterrupted JSONL stream.

When `WriteField()` or `WriteFields()` is called, `TableFormatter` adapts field-compatible data to compact tabular output.

## Projection System

`MarkoutProjection` provides subtractive filtering at render time:

- `IncludeFields` / `ExcludeFields` - Filter which fields appear
- `IncludeColumns` / `ExcludeColumns` - Filter table columns
- `IncludeSections` - Filter by H2 heading

Projection is applied within writer methods, not at the data layer.

## Migration Notes

### Breaking Changes

1. `MarkoutShape.FieldList` removed from enum
2. Code checking for `FieldList` shape should use `Fields` instead
3. `SuppressedShapes = MarkoutShape.FieldList` should become `MarkoutShape.Fields`

### Writer Updates

Writers that previously rejected `FieldList` but accepted `Tables` should now:
1. Accept `Fields` in `SupportedShapes`
2. Implement appropriate rendering for field data (adaptation to tables if needed)

## Summary

The Markout model cleanly separates:

- **Data shapes** - Semantic relationships (Fields, Tables, Trees, etc.)
- **Layout** - Presentation choices (inline, multiline, tabular)
- **Writers** - Output format (markdown, ANSI, plain text)

`MarkoutField` is the universal key-value container. `Fields` is the single shape for all key-value data. Layout is determined by method choice and writer behavior, not by separate shapes.
