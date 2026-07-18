---
name: multi-view-verbosity
version: 0.22.0
description: >-
  Use when one data model must feed several verbosity levels or summaries from a single
  source — a quiet/normal/detail (`-v`) switch, a compact summary vs a full report, or
  collecting only the data the requested sections actually need ("verbosity backpressure").
  Markout has no "verbosity" attribute; you implement it by composing section gating +
  IncludeSections + field-layout choices. Requires the base `markout` pattern and pairs with
  conditional-composition. Don't decompile the assembly or web-search the API — the verbosity/gating idioms are here.
---

# Multi-view & verbosity — summary/detail from one model

Real CLIs expose `-v`/`--detail`/`--quiet` and section selectors. The anti-pattern is a separate
render path (or DTO) per level. With Markout you keep **one model** and select what renders — and,
importantly, drive **what you collect** from the requested verbosity so you don't over-scan.

## Compact summary via field layout

`AutoFields` renders scalar properties as fields; tune the summary without new types:

```csharp
[MarkoutSerializable(
    TitleProperty = nameof(Name),
    FieldLayout = FieldLayout.Inline)]   // Table (default) | Inline | Bulleted | Numbered | Plain
public class View
{
    public string Name { get; set; } = "";
    [MarkoutSkipNull] public string? Summary { get; set; }   // drops out of the terse view when null
}
```

`FieldLayout.Inline` gives a pipe-separated one-liner header; `Table` gives a two-column field block.
Choose the terse shape for quiet mode, the fuller sections for detail.

## Verbosity as section selection

Verbosity is a caller concept. Express it as the set of sections to render:

```csharp
static readonly string[] QuietSections  = { "Summary" };
static readonly string[] DetailSections = { "Summary", "Members", "Diagnostics", "Sources" };

var sections = verbose ? DetailSections : QuietSections;
var options = new MarkoutWriterOptions { IncludeSections = new HashSet<string>(sections) };
MarkoutSerializer.Serialize(view, Console.Out, new MarkdownFormatter(), ctx, options);
```

Combine with same-name section variants (see conditional-composition) when a level needs a *different
projection* of a section rather than just its presence.

## Verbosity backpressure — collect only what's rendered

The higher-value workflow: let the requested sections **promote** how much data you gather, so a
narrow request stays cheap and a `--section Diagnostics` request pulls exactly the data it needs.
Verbosity is **your** concept — `MarkoutWriterOptions` has no `Verbosity` property. Keep the policy
in your collection code; Markout only ever sees the populated model + `IncludeSections`.

```csharp
enum Verbosity { Quiet, Normal, Detailed }

// Resolve requested sections first, then raise your own verbosity to satisfy them, THEN collect.
Verbosity required = GetRequiredVerbosity(requestedSections);          // e.g. Diagnostics => Detailed
var verbosity = (Verbosity)Math.Max((int)cliVerbosity, (int)required);
var model = Collect(verbosity);                                        // scan only as deep as needed

var options = new MarkoutWriterOptions { IncludeSections = new HashSet<string>(sections) };
MarkoutSerializer.Serialize(model, Console.Out, new MarkdownFormatter(), ctx, options);
```

Markout renders whatever the model carries; the verbosity policy is expressed to it purely through
which properties you populate + `IncludeSections`. This is the pattern behind tools that map
`-D`/`-S` flags to a single reusable view.

## Guardrails

- One model, many levels — do not fork a `RenderQuiet` / `RenderFull` pair of string builders.
- Keep the section-name constants in one place; reuse them for both collection and `IncludeSections`.
- Prefer computing the terse/detail predicates on the model so rendering stays declarative.
