# mq — Markdown Query Tool

`mq` is a query tool for markdown pipe tables, analogous to `jq` for JSON.
It uses a hybrid query syntax combining LINQ-style keywords with jq-style
array access.

## Quick start

```bash
# From a file
mq 'where .Type == "LTS"' releases.md

# From stdin
cat releases.md | mq 'count'
```

The tool is published as a native AOT binary (~1.5 MB).

```bash
dotnet publish tools/mq -c Release
```

## Query syntax

Queries are a pipeline of operations separated by `|`.

### LINQ-style operations

| Operation | Example | Description |
| --------- | ------- | ----------- |
| `where` | `where .Type == "LTS"` | Filter rows |
| `select` | `select .Version, .Type` | Project columns |
| `orderby` | `orderby .Version desc` | Sort rows |
| `take` | `take 5` | First N rows |
| `skip` | `skip 2` | Skip N rows |
| `first` | `first` | First row as fields |
| `last` | `last` | Last row as fields |
| `count` | `count` | Row count |
| `distinct` | `distinct` | Unique rows |

### jq-style array access

| Syntax | Example | Description |
| ------ | ------- | ----------- |
| `.[N]` | `.[0]` | Row by index |
| `.[-N]` | `.[-1]` | Row from end |
| `.[S:E]` | `.[0:3]` | Slice |
| `.[].Col` | `.[].Version` | Column extract |
| `.[N].Col` | `.[0].Version` | Scalar cell |

### Comparison operators

`==`, `!=`, `>`, `<`, `>=`, `<=` — with automatic numeric detection.

### Chaining

```bash
mq 'where .Type == "LTS" | select .Version, .Supported | orderby .Version desc'
mq '.[0:5] | select .Version, .Type'
mq 'where .Supported == "✓" | count'
```

### Multi-table documents

For documents with multiple tables under headings, use quoted section names:

```bash
mq '."All Releases" | where .Type == "LTS"'
```

## Architecture

```text
MarkdownTable.Formatting          MarkdownTable.Query              mq (CLI)
├── DocumentReader                ├── Tokenizer                    └── Program.cs
├── LineReader                    ├── QueryParser
├── ByteLineClassifier            ├── QueryEngine
├── MarkdownDocument              └── Operations/
├── FieldParser                       ├── WhereOperation
├── FieldValue                        ├── SelectOperation
└── TableParser                       ├── OrderByOperation
                                      └── TableOperations
```

`DocumentReader` has three entry points:

- `ReadAsync(Stream)` — primary path using `LineReader` for buffered,
  streaming byte-level I/O. Used by the `mq` CLI for both files and stdin.
- `Read(string)` — direct string splitting, fastest when content is already
  in memory as a string. Used by `QueryEngine.Execute(string, string)`.
- `Read(ReadOnlySpan<byte>)` — convenience overload wrapping a MemoryStream.

`LineReader` is ported from MarkdownTable.IO (smooth-markdown-table) with
the prefetch/double-buffering path removed. It retains buffer management,
`SavePosition`/`Rewind` for transactional multi-line lookahead,
`BufferFlipVersion`/`Validate` for span lifetime safety, and
`SearchValues<byte>` for SIMD-accelerated newline search.

`ByteLineClassifier` classifies lines at the byte level into 8 kinds
(Heading, PipeTable, BoldField, Bullet, OneLineFields, Skippable, Empty,
Content) before any UTF-8 → UTF-16 string conversion.

## Performance

All measurements on Apple Silicon, Release build, 100K iterations using
`System.Diagnostics.Stopwatch`.

### Library-level: parse + query (µs/op)

| Operation | mq string | mq MemoryStream | mq FileStream | json |
| --------- | --------: | --------------: | ------------: | ---: |
| Count | 5.04 | 1.90 | 24.65 | 4.95 |
| Filter | 2.83 | 2.40 | 19.01 | 2.61 |
| Scalar | 2.20 | 2.05 | 18.84 | 1.73 |
| Project | 2.53 | 2.28 | 19.52 | 2.44 |

The **MemoryStream path** (LineReader) is the fastest for in-memory data,
**2.6x faster** than `JsonDocument` on Count. It uses `SearchValues<byte>`
for SIMD newline search and `ByteLineClassifier` for byte-level line
classification, deferring UTF-8 decode to only the lines that carry data.

The **string path** uses direct `string.Split` and is competitive with JSON.
The **FileStream path** includes real I/O (file open + kernel read per call)
and shows the ~20µs overhead of actual disk access.

### Library-level: pre-parsed document (µs/op)

| Operation | mq (pre-parsed) | json (pre-parsed) | Ratio |
| --------- | --------------: | -----------------: | ----: |
| Count | 0.05 | 0.01 | 5.0x |
| Filter | 0.43 | 0.40 | 1.1x |
| Scalar | 0.16 | 0.03 | 5.3x |
| Project | 0.62 | 0.69 | 0.90x |

When the document is already parsed, both are sub-microsecond. JSON's
pre-parsed advantage on Count and Scalar comes from `JsonElement`'s direct
array indexing versus mq's list-based table model. Filter and Project are
at parity because both iterate all rows.

### CLI-level: native AOT mq vs native jq (ms/op)

| Operation | mq | jq | Ratio |
| --------- | -: | -: | ----: |
| Count | 17.2 | 17.1 | 1.01x |
| Scalar | 20.3 | 14.9 | 1.36x |
| Filter | 17.6 | 18.2 | 0.97x |

Both tools are dominated by process startup (~15 ms). At this scale, they are
effectively equivalent. The native AOT binary starts as fast as jq's C binary.

### Data size

| Format | Bytes | Ratio |
| ------ | ----: | ----: |
| JSON | 719 | 1.00x |
| Markdown | 465 | 0.65x |

Markdown is **35% smaller** because it avoids repeated key names, braces,
brackets, and quotes. Column names appear once in the header row.

## Design decisions

### Why not Markdig?

Markdig is a full CommonMark AST parser (~200 KB package). It has no concept
of fields (key-value pairs) and requires walking an AST to extract table
cells. `DocumentReader` is purpose-built for Markout's data format: fields
as first-class citizens, zero external dependencies, and 10–20% faster than
JSON for the target use case.

### Why LINQ + jq hybrid?

- LINQ keywords are unambiguous: `select` means project, `where` means filter
  (unlike jq where `select` means filter)
- jq's array access (`.[0]`, `.[].Col`) is concise and well-understood
- The combination gives the best of both: readable keywords and terse navigation
- Natural fit for the .NET audience

### Why native AOT?

The mq tool is a CLI utility invoked per-query, so startup time matters.
Native AOT eliminates the ~80 ms JIT startup penalty, bringing `mq` to
parity with `jq`'s native C binary (~17 ms).

### Why LineReader?

`DocumentReader.ReadAsync(Stream)` uses `LineReader`, ported from
MarkdownTable.IO (smooth-markdown-table), for buffered byte-level I/O.
The design provides:

- **`SearchValues<byte>`** — SIMD-accelerated newline search on raw UTF-8
- **Buffer management** — compact unprocessed bytes, read more from stream
- **`SavePosition`/`Rewind`** — transactional multi-line lookahead for table
  header identification (header + separator must be validated atomically)
- **`BufferFlipVersion`/`Validate`** — span lifetime safety across buffer
  operations; ensures spans obtained before a buffer flip are not used after
- **Streaming-ready** — processes data as it arrives; never requires the
  full document in memory

The prefetch/double-buffering path was removed (unnecessary for current
workloads). `ByteLineClassifier` classifies lines at the byte level into
8 Markout-specific kinds, deferring `Encoding.UTF8.GetString` to only the
lines that carry data. The MemoryStream path is 2.6x faster than
`JsonDocument` on parse-heavy operations.

## Running the benchmark

```bash
# Build mq as native AOT
dotnet publish tools/mq -c Release

# Run the benchmark harness
dotnet run --project tests/mq-bench -c Release
```

The benchmark compares mq and JSON at two levels:

1. **Library**: `QueryEngine.Execute()` vs `JsonDocument.Parse()` + property
   lookups, measured with `Stopwatch` at 100K iterations
2. **CLI**: native `mq` binary vs native `jq` binary, measured by spawning
   processes at 500 iterations
