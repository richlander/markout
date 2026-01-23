# MarkOut

**Human-readable structured data serialization to Markdown**

MarkOut serializes .NET objects to clean, readable Markdown format. Perfect for logs, reports, documentation, and any output that humans need to read.

## Features

- **Tables** - `List<T>` serializes to Markdown tables
- **Sections** - Nested objects become H2 sections
- **Type-safe** - Source generator provides compile-time validation
- **Zero allocation** - Direct string writing, no intermediate objects
- **Compile-time errors** - Prevents common mistakes like nested lists in tables

## Quick Start

```csharp
using MarkOut;

[MarkOutSerializable]
public class BuildResult
{
    public string Project { get; set; }
    public bool Success { get; set; }
    public int Duration { get; set; }
}

[MarkOutContext(typeof(BuildResult))]
public partial class MyContext : MarkOutSerializerContext { }

// Serialize
var result = new BuildResult 
{ 
    Project = "MyApp", 
    Success = true, 
    Duration = 1234 
};

var markdown = MyContext.Default.Serialize(result);
```

**Output:**
```markdown
Project: MyApp
Success: yes
Duration: 1234
```

## Installation

```bash
dotnet add package MarkOut
dotnet add package MarkOut.SourceGeneration
```

## Common Patterns

### List as Table

```csharp
[MarkOutSerializable]
public class TestResult
{
    public string Name { get; set; }
    public bool Passed { get; set; }
    public int Duration { get; set; }
}

var results = new List<TestResult> { ... };
var markdown = MarkOutSerializer.Serialize(results, MyContext.Default);
```

**Output:**
```markdown
| Name           | Passed | Duration |
|----------------|--------|----------|
| Login_Works    | yes    | 145      |
| Logout_Works   | yes    | 89       |
| Invalid_Fails  | no     | 234      |
```

### Nested Objects as Sections

```csharp
[MarkOutSerializable(TitleProperty = nameof(Name))]
public class Project
{
    public string Name { get; set; }
    public string Version { get; set; }
    
    [MarkOutSection(Name = "Dependencies")]
    public List<Dependency> Dependencies { get; set; }
}

[MarkOutSerializable]
public class Dependency
{
    public string Package { get; set; }
    public string Version { get; set; }
}
```

**Output:**
```markdown
# MyApp 1.0.0

Name: MyApp
Version: 1.0.0

## Dependencies

| Package         | Version |
|-----------------|---------|
| Newtonsoft.Json | 13.0.3  |
| Serilog         | 3.1.1   |
```

## Attributes

- **`[MarkOutSerializable]`** - Marks a type for serialization
- **`[MarkOutPropertyName("...")]`** - Custom property display name
- **`[MarkOutIgnore]`** - Excludes a property from output
- **`[MarkOutSection(Name = "...")]`** - Renders property as H2 section
- **`[MarkOutContext(typeof(...))]`** - Registers types for source generation

## Nested Lists

If you have `List<Group>` where `Group` contains `List<Item>`, you'll get a compile error:

```
error MARKOUT001: Property 'Items' in type 'Group' is an array of complex 
objects and cannot be rendered in a table cell.
```

This is intentional! Markdown tables can't contain lists. Choose a transformation strategy:

1. **Pivot Table** - Compare items across groups
2. **Multiple Tables** - One table per group
3. **Multiple Lists** - Simple bullet lists per group
4. **Flatten** - Single table with group as a column

📖 **See [Nested Lists Guide](docs/nested-lists-guide.md)** for complete examples and code

## Real-World Usage

MarkOut was created for [dotnet-inspector](https://github.com/user/dotnet-inspector) to generate readable inspection reports. It excels at:

- Build/test results
- Dependency reports
- API inspection output
- Configuration summaries
- Error reports

## Documentation

- **[Nested Lists Guide](docs/nested-lists-guide.md)** - Handling nested data structures
- **[Specification](docs/specification.md)** - Complete format specification
- **[Design Docs](docs/design/)** - Implementation details

## License

MIT
