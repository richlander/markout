---
name: output-formats
version: 0.22.0
description: >-
  Use when you need output other than default Markdown — plain text / Unicode, ANSI terminal
  (Spectre), pretty aligned tables, or TSV/JSONL exports — or when one model must serve several
  formats from a single render path (a CLI `--format` switch). Pick the format with a formatter
  + MarkoutWriterOptions; never hand-build TSV/JSONL. Requires the base `markout` pattern.
  Don't decompile the assembly or web-search the API — the TSV/JSONL/formatter idioms are all here.
---

# Output formats — one model, many formats

Default `Serialize(...)` emits Markdown. Pass a **formatter** (and optional `MarkoutWriterOptions`)
to get other formats. The anti-pattern is per-format string building (`string.Join("\t", ...)`); let
Markout project the same model to each format so columns/headers stay consistent.

## Formatters

```csharp
MarkoutSerializer.Serialize(r, Console.Out, ctx);                              // Markdown (default)
MarkoutSerializer.Serialize(r, Console.Out, new MarkdownFormatter(), ctx);     // Markdown, explicit
MarkoutSerializer.Serialize(r, Console.Out, new PlainTextFormatter(), ctx);    // plain text / logs (ASCII, no pipes)
MarkoutSerializer.Serialize(r, Console.Out, new UnicodeFormatter(), ctx);      // Unicode box-drawing tables
MarkoutSerializer.Serialize(r, Console.Out, new TableFormatter(), ctx);        // compact pretty rows

using Markout.Ansi.Spectre;                                                    // extra package
MarkoutSerializer.Serialize(r, Console.Out, new SpectreFormatter(AnsiConsole.Console), ctx); // ANSI
```

`Markout.Ansi.Spectre` is a separate NuGet package; Markdown/plain/table/TSV/JSONL need only `Markout`.

## TableFormatter modes + writer options

`TableFormatter` + `MarkoutWriterOptions.TableMode` selects the tabular shape:

```csharp
var opts = new MarkoutWriterOptions
{
    TableMode = MarkoutTableMode.Tsv,          // Pretty | Tsv | Jsonl
    IncludeDescription = false,
    IncludeSections = new HashSet<string> { "Results" },
    JsonTypedValues = true,                    // JSONL/TSV: emit numbers as numbers, not quoted
    MaxItems = 3,                              // cap rows; appends a "... and {N} more" notice
};
MarkoutSerializer.Serialize(report, Console.Out, new TableFormatter(), ctx, opts);
```

- `MarkoutTableMode.Pretty` — columns aligned to a uniform start position.
- `MarkoutTableMode.Tsv` — stable `snake_case` headers; never emits embedded tabs/newlines in cells.
- `MarkoutTableMode.Jsonl` — one record per row (heterogeneous keys; see composite-cells-cards).
- `MaxItems = N` caps EVERY table to N rows and appends `... and {count} more`; works with any formatter,
  so a Markdown view can show the first N rows while TSV/JSONL export them all. (Or per-property:
  `[MarkoutMaxItems(3)]`, with an optional `EllipsisFormat`.)

## Central multi-format dispatch (the CLI `--format` pattern)

Route one model through one method so every command gets every format for free:

```csharp
static void Render<TView, TJson>(TView view, OutputFormat fmt, MarkoutSerializerContext ctx)
{
    switch (fmt)
    {
        case OutputFormat.Text:  MarkoutSerializer.Serialize(view, Console.Out, new PlainTextFormatter(), ctx); break;
        case OutputFormat.Md:    MarkoutSerializer.Serialize(view, Console.Out, new MarkdownFormatter(), ctx,
                                     new MarkoutWriterOptions { MaxItems = 3 }); break;   // first 3 rows + "... and N more"
        case OutputFormat.Table: MarkoutSerializer.Serialize(view, Console.Out, new TableFormatter(), ctx); break;
        case OutputFormat.Tsv:   MarkoutSerializer.Serialize(view, Console.Out, new TableFormatter(), ctx,
                                     new MarkoutWriterOptions { TableMode = MarkoutTableMode.Tsv }); break;
        case OutputFormat.Jsonl: MarkoutSerializer.Serialize(view, Console.Out, new TableFormatter(), ctx,
                                     new MarkoutWriterOptions { TableMode = MarkoutTableMode.Jsonl, JsonTypedValues = true }); break;
    }
}
```

If a format needs byte-exact output (e.g. a verbatim packet), bypass Markout for that one case and
keep Markout for the structured formats — don't try to force it.

## Format promises (rely on these)

- Markdown table cells normalize `|` to `&#124;` (not `\|`).
- TSV uses stable `snake_case` headers and never embeds tabs/newlines in cells.
- Pretty renders the same projection as TSV, aligned.

## Guardrails

- One model → many formats; never hand-assemble TSV/JSONL rows.
- Reach for `IncludeSections`/`IncludeDescription` to trim exports rather than a second model.
- Spectre/ANSI needs the `Markout.Ansi.Spectre` package; the rest do not.
