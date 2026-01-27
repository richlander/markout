# MarkOut View System Design

## Philosophy

The MarkOut view system is designed for LLM consumption. Our core insight is that **it's more efficient to offer more sections and make them filterable** than to create multiple bespoke views for different use cases.

Traditional CLI output forces a choice between verbosity (comprehensive but token-heavy) and terseness (efficient but incomplete). The MarkOut approach resolves this tension by:

1. **Generating all sections** at the appropriate verbosity level
2. **Letting consumers filter** to exactly what they need
3. **Using indices** to make the query system self-evident

## The Height × Width Model

Output verbosity is a 2D space:

- **Height** = Which sections are included (summary, tables, summaries)
- **Width** = Which columns appear in each section (compact vs full)

Verbosity levels move diagonally through this space:

```
        Narrow              Wide
        (compact cols)      (full cols)
        ────────────────────────────────
Short   │ QUIET            │           │
        │ Summary only     │           │
        ├──────────────────┼───────────┤
Medium  │ MINIMAL          │           │
        │ +Errors,Warnings │           │
        │ File|Line|Code   │           │
        ├──────────────────┼───────────┤
Tall    │                  │ NORMAL    │
        │                  │ +Projects │
        │                  │ +Message  │
        ├──────────────────┼───────────┤
Extra   │                  │ DETAILED  │
        │                  │ +Summary  │
        │                  │ tables    │
        └──────────────────┴───────────┘
```

Users pick a verbosity level to set the baseline, then filter sections as needed.

## Section-Based Architecture

### Principles

1. **Sections are H2 headers** - Each section starts with `## SectionName`, making them grep-able and addressable
2. **Sections are numbered** - 1-indexed for human ergonomics
3. **Sections are composable** - Each section renderer is independent
4. **Empty sections are omitted** - No empty tables or headers

### Implementation Pattern

```csharp
// Define section renderers (each self-contained)
private string SummaryLine() => "...";
private string ProjectsTable() => "...";
private string ErrorsCompact() => "...";
private string ErrorsFull() => "...";
private string WarningsCompact() => "...";
private string WarningsFull() => "...";
private string ErrorTypesSummary() => "...";

// Compose sections based on verbosity
private string[] GetSectionsForVerbosity() => _verbosity switch
{
    Quiet => [SummaryLine()],
    Minimal => [ErrorsCompact(), WarningsCompact()],
    Normal => [H1() + SummaryLine(), ProjectsTable(), ErrorsFull(), WarningsFull()],
    Detailed => [H1() + SummaryLine(), ProjectsTable(), ErrorsFull(), WarningsFull(), ErrorTypesSummary()],
};

// Apply section filtering
private string RenderMarkOut()
{
    var sections = GetSectionsForVerbosity();
    var filtered = sections
        .Where((_, idx) => IsSectionIncluded(idx + 1))
        .Where(s => !string.IsNullOrEmpty(s));
    return string.Join("\n", filtered);
}

private bool IsSectionIncluded(int index)
{
    if (_includeSections?.Count > 0 && !_includeSections.Contains(index))
        return false;
    if (_excludeSections?.Contains(index) == true)
        return false;
    return true;
}
```

### Index-Based Filtering

We use indices rather than names because:

1. **Self-documenting** - Section order in output matches indices
2. **Terse** - `-s 1,3` vs `-s summary,errors`
3. **Language-agnostic** - No localization concerns
4. **Discoverable** - Run once, count the sections

The mapping is documented in help text and `llms.txt`:
```
1=Summary  2=Projects  3=Errors  4=Warnings  5=ErrorTypes
```

## Configuration Interface

### Command-Line Options

```bash
# Verbosity (coarse-grain)
dotnet build --mo -v:q     # quiet
dotnet build --mo -v:m     # minimal (default)
dotnet build --mo -v:n     # normal
dotnet build --mo -v:d     # detailed

# Section include (fine-grain)
dotnet build --mo -s 1,3   # Only sections 1 and 3

# Section exclude (fine-grain)
dotnet build --mo -x 4     # All except section 4
```

### Parameter Format

For MSBuild loggers, parameters are passed as semicolon-delimited key=value pairs:

```
-logger:LoggerType,Assembly;format=markout;verbosity=minimal;sections=1,3;exclude=4
```

## Design Decisions

### Why Indices Over Names?

Names require:
- String parsing and normalization
- Case handling
- Potential localization
- Longer command lines

Indices are:
- Unambiguous
- Position-based (self-evident from output)
- Compact
- Universal

### Why Filter Instead of Multiple Endpoints?

Alternative: Provide multiple views (e.g., `--errors-only`, `--summary-only`)

Problems with alternatives:
- Combinatorial explosion of options
- Each new section requires new flags
- Consumers can't compose arbitrary combinations

Filtering advantages:
- Single mechanism for all combinations
- New sections automatically filterable
- No new CLI options needed

### Why Default to Minimal?

- Success case is terse (single line)
- Failure case shows actionable info (file/line/code)
- Messages are often redundant (error codes are searchable)
- LLMs can request more detail if needed

## Extending the System

### Adding a New Section

1. Create the section renderer:
```csharp
private string NewSection()
{
    if (noData) return string.Empty;
    
    var sb = new StringBuilder();
    sb.AppendLine();
    sb.AppendLine("## New Section");
    sb.AppendLine();
    sb.AppendLine("| Col1 | Col2 |");
    sb.AppendLine("|------|------|");
    // ... rows
    return sb.ToString().TrimEnd();
}
```

2. Add to appropriate verbosity levels in `GetSectionsForVerbosity()`

3. Update the section index documentation

### Adding a New Verbosity Level

Verbosity levels should follow the diagonal pattern - adding both height and width as you move up. Consider whether the new level:

- Adds new sections (height)
- Adds new columns to existing sections (width)
- Both

## Integration with llms.txt

The `dotnet llms` command outputs a concise guide for LLMs. Keep it updated when:

- Adding new commands that support `--mo`
- Changing section indices
- Adding new verbosity levels

The llms.txt should be minimal - LLMs can experiment to learn the details.
