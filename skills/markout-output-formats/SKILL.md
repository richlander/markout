---
name: markout-output-formats
version: 0.36.0
description: >-
  Use when you need output other than default Markdown — plain text / Unicode, ANSI terminal
  (Spectre), pretty aligned tables, or TSV/JSONL exports — or when one model must serve several
  formats from a single render path (a CLI `--format` switch). Also covers semantic inline code
  that renders as Markdown code spans but decodes to plain text in TSV/JSONL. Pick the format with
  a formatter + MarkoutWriterOptions; never hand-build TSV/JSONL.
  Don't decompile the assembly or web-search the API — the multi-format idioms are all here.
---

# Output formats — one model, many formats

Default `Serialize(...)` emits Markdown. Pass a **formatter** (and optional `MarkoutWriterOptions`)
to get other formats. The anti-pattern is per-format string building (`string.Join("\t", ...)`); let
Markout project the same model to each format so columns/headers stay consistent.

## Required setup

Markout has **no reflection fallback**. Every report needs a partial context registering each model
type and a `Serialize` call that passes it. `[MarkoutSerializable]` is optional customization, not
a prerequisite for registration.

```csharp
using Markout;

[MarkoutSerializable(TitleProperty = nameof(Title))]
public class Report
{
    public string Title { get; set; } = "";
    [MarkoutSection(Name = "Rows")] public List<Row>? Rows { get; set; }
}

[MarkoutSerializable]
public class Row { public string Name { get; set; } = ""; public int Count { get; set; } }

[MarkoutContext(typeof(Report))]
[MarkoutContext(typeof(Row))]          // every user element type of a List<T>, too
public partial class ReportContext : MarkoutSerializerContext { }

var ctx = ReportContext.Default;       // the `ctx` passed in the examples below
```

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

`Graph` sections become Markdown edge tables by default. To embed the same
graph as Mermaid without rebuilding it, select the Markdown graph mode:

```csharp
MarkoutSerializer.Serialize(
    report,
    Console.Out,
    new MarkdownFormatter(MarkdownGraphMode.Mermaid),
    ctx);
```

## TableFormatter modes + writer options

`TableFormatter` + `MarkoutWriterOptions.TableMode` selects the tabular shape:

```csharp
var opts = new MarkoutWriterOptions
{
    TableMode = MarkoutTableMode.Tsv,          // Pretty | Tsv | Jsonl
    IncludeDescription = false,
    IncludeSections = new HashSet<string> { "Results" },
    JsonTypedValues = true,                    // JSONL: emit numbers and booleans as JSON values
    MaxItems = 3,                              // cap rows; JSONL stays record-only when rows are skipped
};
MarkoutSerializer.Serialize(report, Console.Out, new TableFormatter(), ctx, opts);
```

- `MarkoutTableMode.Pretty` — columns aligned to a uniform start position.
- `MarkoutTableMode.Tsv` — stable `snake_case` headers; never emits embedded tabs/newlines in cells.
- `MarkoutTableMode.Jsonl` — one record per row, carrying only that row's keys (heterogeneous).
- `MaxItems = N` caps every table to N rows. Presentation and TSV output append
  `... and {count} more`; JSONL omits the notice because every line must remain a data record.
  Apply the cap only to presentation options when TSV/JSONL exports must remain complete.
  (Or cap a property with `[MarkoutMaxItems(3)]` and an optional `EllipsisFormat`.)

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

## Format-neutral inline code

Store code-like values with semantic `<code>...</code>` tags. XML-escape angle brackets inside
the tags:

```csharp
row.Operation = "<code>dotnet test</code>";
row.Signature = "<code>Result&lt;T&gt; Parse&lt;T&gt;(string value)</code>";
```

Markdown renders code spans such as `` `Result<T> Parse<T>(string value)` ``. Pretty, TSV, and
JSONL remove the tags, decode the entities, and emit plain text. Do not store raw backticks or use
``[MarkoutDisplayFormat("`{0}`")]``; that hard-codes Markdown into the model.

JSONL must still go through Markout:

```csharp
var jsonl = new MarkoutWriterOptions
{
    TableMode = MarkoutTableMode.Jsonl,
    JsonTypedValues = true,
};
MarkoutSerializer.Serialize(report, Console.Out, new TableFormatter(), ctx, jsonl);
```

Do not substitute `System.Text.Json`: it does not apply Markout's table projection, stable property
names, or semantic-tag decoding.

## Guardrails

- One model → many formats; never hand-assemble TSV/JSONL rows.
- Reach for `IncludeSections`/`IncludeDescription` to trim exports rather than a second model.
- Spectre/ANSI needs the `Markout.Ansi.Spectre` package; the rest do not.
