# Markout overview

Markout is a source-generated .NET serializer for projecting object graphs into readable documents and compact tabular output. Models describe data relationships with attributes; generated code writes through formatter capability interfaces.

## Core architecture

- `src/Markout/` contains runtime types, writer options, formatters, attributes, and capability interfaces.
- `src/Markout.SourceGeneration/` parses model metadata and emits type/context serializers.
- `src/MarkdownTable.Formatting/` formats Markdown pipe tables and table-like documents.
- `src/Markout.Ansi.Spectre/` contains the terminal renderer.
- `src/Markout.Templates/` binds Markdown templates to model data.
- `samples/` demonstrates file-based apps and renderer choices.
- `tests/` contains executable xUnit v3 test projects.

## Output contract

- Markdown is the primary readable document format.
- Markdown renders `Graph` as an edge table by default. Pass
  `MarkdownGraphMode.Mermaid` to `MarkdownFormatter` to embed the same graph as
  a fenced Mermaid diagram.
- `TableFormatter` renders compact table projections. `MarkoutTableMode.Pretty` uses display headers and space-padded columns. `MarkoutTableMode.Tsv` emits normalized TSV with stable snake_case headers.
- Semantic result selection and presentation ownership are separated in
  [Selection ownership](design/selection-ownership.md).
- Markdown table cell pipes are normalized to `&#124;`, not escaped as `\|`.
- TSV cells never contain embedded tabs or newlines.

## Validation

Use executable xUnit projects:

```bash
dotnet build Markout.sln -c Release
dotnet run --project tests/Markout.Tests -c Release
dotnet run --project tests/MarkdownTable.Tests -c Release
dotnet run --project tests/Markout.Templates.Tests -c Release
```
