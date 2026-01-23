# Compile-Time Error Detection

## Overview

MarkOut detects common serialization mistakes at compile time rather than producing useless output at runtime. This prevents issues like nested lists rendering as `ToString()` garbage.

## The Problem

Without compile-time validation, this code would silently produce useless output:

```csharp
[MarkOutSerializable]
public class DependencyGroup
{
    public string Framework { get; set; }
    public List<Dependency> Packages { get; set; }  // ❌ List in a table cell!
}
```

**Output without validation:**
```markdown
| Framework | Packages                                              |
|-----------|-------------------------------------------------------|
| net6.0    | System.Collections.Generic.List`1[Dependency]        |
```

This is useless! The user wanted to see dependency details, not `ToString()` output.

## Design Decision

**Force explicit data transformation at compile time.**

The source generator detects when a type will be rendered in a table (because it's in a `List<T>`) and validates that all properties are scalar (string, int, bool, DateTime, etc.). Non-scalar properties trigger a compile error.

## Error Codes

### MARKOUT001: Non-scalar Property in Table

**Trigger:** Property with complex type (List, custom object) in a type used in `List<T>`

**Message:**
```
error MARKOUT001: Property 'Packages' in type 'DependencyGroup' is an array 
of complex objects and cannot be rendered in a table cell. Use [MarkOutIgnore], 
[MarkOutSection], or transform the data.
```

**Solutions:**
1. Use `[MarkOutIgnore]` to hide the property (but data is lost!)
2. Use `[MarkOutSection]` to render it separately (if appropriate)
3. Transform the data using one of the 4 strategies (see nested-lists-guide.md)

**Example Fix:**
```csharp
// Option 1: Pivot transformation (recommended for dependencies)
var matrix = groups
    .SelectMany(g => g.Packages.Select(p => new { g.Framework, p.Name, p.Version }))
    .GroupBy(x => x.Name)
    .Select(g => new PackageMatrix
    {
        Package = g.Key,
        Net6 = g.FirstOrDefault(x => x.Framework == "net6.0")?.Version,
        Net8 = g.FirstOrDefault(x => x.Framework == "net8.0")?.Version
    })
    .ToList();

// Result: Package × Framework matrix showing version compatibility
```

### MARKOUT002: Complex Object Property (Planned)

Not yet implemented. Would detect single complex objects (not lists) in table cells.

### MARKOUT003: Dictionary Not Supported (Planned)

Not yet implemented. Would detect `Dictionary<TKey, TValue>` and suggest conversion to `List<KeyValuePair>`.

## Implementation

### Detection Algorithm

1. **Find table contexts**: Search all syntax trees for properties of type `List<T>` where `T` is marked `[MarkOutSerializable]`
2. **Validate properties**: For each property of `T`, check if it's scalar
3. **Exempt marked properties**: Properties with `[MarkOutIgnore]` or `[MarkOutSection]` are skipped
4. **Report diagnostics**: Non-scalar properties without exemption → MARKOUT001

### Scalar Types

A type is considered scalar if it's one of:
- Primitive: string, int, long, decimal, float, double, bool, DateTime, DateTimeOffset, TimeSpan, Guid
- Enum
- Nullable<T> where T is scalar
- String array (currently triggers error - debatable)

### Source Generator Integration

The `TypeParser` class performs validation during type metadata collection:
- `IsUsedInList()` determines if a type appears in table context
- `IsScalarKind()` validates PropertyKind
- Diagnostics stored in `TypeMetadata.Diagnostics`
- `MarkOutSourceGenerator` reports diagnostics before code generation

**Key Files:**
- `src/MarkOut.SourceGeneration/DiagnosticDescriptors.cs` - Error definitions
- `src/MarkOut.SourceGeneration/Parser/TypeParser.cs` - Detection logic
- `src/MarkOut.SourceGeneration/Parser/TypeMetadata.cs` - Diagnostic storage
- `src/MarkOut.SourceGeneration/MarkOutSourceGenerator.cs` - Reporting

## Design Rationale

**Why fail at compile time?**

1. **Immediate feedback**: Developer sees the problem right where they write the code
2. **Prevents production issues**: Can't accidentally deploy code that produces garbage
3. **Forces good design**: Developer must think about data transformation
4. **Better error messages**: Can provide specific solutions in compile error

**Why not auto-transform?**

Different use cases need different transformations:
- Dependency report: Pivot table (framework × package matrix)
- Build results: Multiple tables (one per configuration)
- Feature lists: Multiple bullet lists (one per tier)

The library can't know which transformation serves the reader best, so it forces the developer to choose explicitly.

## Related Documentation

- [Nested Lists Guide](../nested-lists-guide.md) - Complete guide to transformation strategies
- [Specification](../specification.md) - Format rules and limitations
