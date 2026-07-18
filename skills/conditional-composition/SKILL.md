---
name: conditional-composition
version: 0.22.0
description: >-
  Use when a Markout report must adapt to its data — show or hide whole sections, drop table
  columns that are empty/uniform, filter output to a chosen set of sections, or render one
  model into several shapes ("one model, many views"). This is Markout's highest-value idiom:
  declare the conditions with attributes instead of hand-writing if/else + StringBuilder.
  Requires the base `markout` pattern (model + context + Serialize). Don't decompile the
  assembly or web-search the API — the conditional idioms are here.
---

# Conditional composition — one model, many views

The trap this skill prevents: hand-rolling `if (hasErrors) sb.AppendLine("## Errors")` and manual
column arithmetic. In Markout you **declare** the condition on the model; the generator renders the
right shape. This keeps one source of truth and makes the same model serve quiet/detail/export modes.

## Conditional sections

```csharp
[MarkoutSerializable(TitleProperty = nameof(Name))]
public class Inspection
{
    public string Name { get; set; } = "";

    // Section renders ONLY when HasFailures is true. Compute the predicate on the model.
    [MarkoutSection(Name = "Failures", ShowWhenProperty = nameof(HasFailures))]
    public List<FailureRow>? Failures { get; set; }
    public bool HasFailures => Failures is { Count: > 0 };

    // EmptyText shows a fallback paragraph when the list is non-null but empty; null omits the section.
    [MarkoutSection(Name = "Warnings", EmptyText = "No warnings.")]
    public List<WarnRow>? Warnings { get; set; }
}
```

- `ShowWhenProperty = nameof(Bool)` gates a **section** on a bool property.
- `[MarkoutShowWhen(nameof(Bool))]` gates a **scalar field** the same way.
- `[MarkoutSkipNull]` / `[MarkoutSkipDefault]` drop individual fields when null/default.

## Adaptive columns — hide what carries no information

```csharp
// Drop the "Pattern" column when it's uniform/empty across the rows (keeps tables compact).
[MarkoutIgnoreColumnWhen(nameof(HidePattern))]
public string? Pattern { get; set; }

// On the section: IgnoreProperty hides named columns unconditionally.
[MarkoutSection(Name = "Matches", IgnoreProperty = "InternalId,Debug")]
public List<MatchRow>? Matches { get; set; }
```

`[MarkoutIgnoreColumnWhen(...)]` is the declarative replacement for "compute distinct values, then
rebuild headers and rows." Columns hidden this way are also dropped from TSV/JSONL decomposition.

## Filter to specific sections at render time

```csharp
var options = new MarkoutWriterOptions { IncludeSections = new HashSet<string> { "Failures" } };
MarkoutSerializer.Serialize(report, Console.Out, new MarkdownFormatter(), ctx, options);
```

`IncludeSections` renders only the named sections — the caller-side lever for "just show me X"
without a second model. (See **multi-view-verbosity** for driving this from a verbosity level.)

## One model, many shapes: same-name section variants

When a mode needs a different projection of the same logical section (e.g. terse vs with-docs),
declare multiple properties with the **same** `Name` and gate them so exactly one renders:

```csharp
[MarkoutSection(Name = "Members", ShowWhenProperty = nameof(Terse))]
public List<MemberRow>? MembersTerse { get; set; }

[MarkoutSection(Name = "Members", ShowWhenProperty = nameof(WithDocs))]
public List<MemberDocRow>? MembersWithDocs { get; set; }
```

This "same-name polymorphic section" is how a single report serves `--docs`, quiet, and select
modes without branching in the writer. Set the gating bools when you build the model.

## Guardrails

- Prefer declaration over imperative assembly: no `if`+`AppendLine`, no manual header/row rebuilding.
- Keep predicates (`Has*`, `Terse`) as computed properties on the model, next to the data.
- Empty vs absent matters: `null` list omits a section; empty list + `EmptyText` shows the fallback.
