# Compile-Time Error Detection - Implementation Status

## Current State: NOT IMPLEMENTED

**dotnet-inspector compiles without error** even though it has the problematic pattern:

```csharp
[MarkOutSerializable]
public class DependencyGroup {
    string TargetFramework { get; set; }
    List<PackageDependency> Dependencies { get; set; }  // ❌ Currently allowed!
}

public class InspectionResult {
    [MdfSection(Name = "Package Dependencies")]
    List<DependencyGroup> DependencyGroups { get; set; }
}
```

This currently produces:
```markdown
## Package Dependencies

| Target Framework | Dependencies |
|------------------|--------------|
| net6.0 | System.Collections.Generic.List`1[PackageDependency] |  ❌ USELESS!
| net8.0 | System.Collections.Generic.List`1[PackageDependency] |  ❌ USELESS!
```

## What We've Created

The **`CompileTimeErrorTests.cs`** file is a **SPECIFICATION**, not a test of existing functionality. It documents:

1. **What patterns SHOULD be errors** (but aren't yet)
2. **How to detect them** (detection algorithm)
3. **What error messages should say** (helpful guidance)
4. **How to implement it** (source generator changes)

## Implementation Required

To make this work, you need to modify the source generator in `TypeParser.cs`:

### 1. Track Table Context

```csharp
private static TypeMetadata? ParseTypeSymbol(
    INamedTypeSymbol typeSymbol, 
    Compilation compilation)
{
    // NEW: Check if this type is used in List<T>
    bool isInTableContext = IsUsedInList(typeSymbol, compilation);
    
    var properties = new List<PropertyMetadata>();
    foreach (var prop in typeSymbol.GetMembers().OfType<IPropertySymbol>())
    {
        var propMeta = ParseProperty(
            prop, 
            compilation,
            isInTableContext  // Pass context down
        );
        properties.Add(propMeta);
    }
    // ...
}

private static bool IsUsedInList(INamedTypeSymbol type, Compilation compilation)
{
    // Search all types to see if any have List<type> or IEnumerable<type>
    foreach (var otherType in GetAllTypes(compilation))
    {
        foreach (var prop in otherType.GetMembers().OfType<IPropertySymbol>())
        {
            if (IsGenericList(prop.Type, type))
                return true;
        }
    }
    return false;
}
```

### 2. Validate Properties in Table Context

```csharp
private static PropertyMetadata? ParseProperty(
    IPropertySymbol prop, 
    Compilation compilation,
    bool isInTableContext)  // NEW
{
    var isIgnored = HasAttribute(prop, MdfIgnoreAttribute);
    var isSection = HasAttribute(prop, MdfSectionAttribute);
    
    var (kind, _, _) = DeterminePropertyKind(prop.Type, compilation);
    
    // NEW: Validate if in table context
    if (isInTableContext && !isIgnored && !isSection)
    {
        if (!IsScalarKind(kind))
        {
            // EMIT ERROR
            context.ReportDiagnostic(Diagnostic.Create(
                NonScalarInTableRule,
                prop.Locations[0],
                prop.Name,
                prop.ContainingType.Name,
                kind.ToString()
            ));
        }
    }
    
    return new PropertyMetadata(...);
}

private static bool IsScalarKind(PropertyKind kind)
{
    return kind is 
        PropertyKind.String or 
        PropertyKind.Boolean or 
        PropertyKind.Int32 or 
        PropertyKind.Int64 or 
        PropertyKind.Double or 
        PropertyKind.Decimal or 
        PropertyKind.DateTime or 
        PropertyKind.DateTimeOffset;
}
```

### 3. Define Diagnostic Descriptors

```csharp
private static readonly DiagnosticDescriptor NonScalarInTableRule = new(
    id: "MARKOUT001",
    title: "Non-scalar property in table row",
    messageFormat: "Property '{0}' in '{1}' is {2} and cannot be rendered in a table cell. " +
                   "Use [MarkOutIgnore], [MarkOutSection], or transform the data. " +
                   "See: https://docs.mdf.dev/errors/MARKOUT001",
    category: "MarkdownData.Design",
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "List and complex object properties cannot be rendered in table cells and " +
                 "will show as useless ToString() output. You must explicitly choose how to " +
                 "handle them: ignore, separate section, or pivot/flatten the data."
);

private static readonly DiagnosticDescriptor DictionaryPropertyRule = new(
    id: "MARKOUT003",
    title: "Dictionary property not supported",
    messageFormat: "Property '{0}' is Dictionary<TKey, TValue> which is not supported. " +
                   "Convert to List<KeyValueItem> or use separate properties.",
    category: "MarkdownData.Design",
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true
);
```

## Error Codes Defined

| Code   | Title | Severity | Description |
|--------|-------|----------|-------------|
| MARKOUT001 | Non-scalar property in table row | ERROR | List<T> or complex object in table cell |
| MARKOUT002 | Complex object property in table | ERROR | Multi-property class in table cell |
| MARKOUT003 | Dictionary not supported | ERROR | Dictionary<TKey, TValue> property |

## After Implementation

Once implemented, dotnet-inspector would **fail to compile** with:

```
error MARKOUT001: Property 'Dependencies' in 'DependencyGroup' is List<PackageDependency> 
and cannot be rendered in a table cell.

Use [MarkOutIgnore], [MarkOutSection], or transform the data.

Solutions:
  1. PIVOT: Transform to comparison table
     [MarkOutIgnore]
     public List<DependencyGroup> DependencyGroups { get; set; }
     
     [MdfSection(Name = "Dependencies")]
     public List<DependencyVersionMatrix> DependencyMatrix
         => PivotDependencies(DependencyGroups);
  
  2. IGNORE: Exclude from output
     [MarkOutIgnore]
     public List<PackageDependency> Dependencies { get; set; }
     
     public int DependencyCount => Dependencies?.Count ?? 0;
  
  3. SECTION: Separate sections per group
     [MdfSection(Name = "Dependencies (net6.0)")]
     public List<PackageDependency> Net6Dependencies { get; set; }

See: https://docs.mdf.dev/strategies/nested-lists
```

The user would be forced to fix it before the code compiles, resulting in proper output.

## Why This Matters

**Before (current):**
- ✅ Compiles
- ✅ Runs
- ❌ Produces garbage output
- ❌ User doesn't know until they look at the file
- ❌ No guidance on how to fix

**After (with errors):**
- ❌ Fails to compile
- ✅ Immediate feedback
- ✅ Clear error message
- ✅ Multiple solution options
- ✅ Links to documentation
- ✅ Forces explicit, correct choice

This is similar to C# compiler errors like "Cannot implicitly convert List<T> to string" - it forces you to make an explicit transformation rather than silently calling ToString().

## Test Suite

The **CompileTimeErrorTests.cs** (8 tests) serves as:
1. Specification for what should error
2. Documentation of detection logic
3. Examples of error messages
4. Guide for implementation
5. Examples of proper fixes

Once implemented, these tests could be converted to actual compilation tests using Roslyn test infrastructure.
