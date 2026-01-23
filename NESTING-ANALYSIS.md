# MDF Format: Nesting Analysis and Limitations

This document summarizes the findings from comprehensive testing of nested data structures in the Markdown Data Format (MDF).

## Test Coverage

We created **61 tests** across six test files:

1. **NestedStructureTests.cs** - Tests various nesting patterns and edge cases
2. **BuildResultsTests.cs** - Tests real-world build system output patterns  
3. **FormatExamplesTests.cs** - Visual examples showing actual MDF output
4. **ListRenderingStrategyTests.cs** - Alternative strategies for List<T> with non-scalars
5. **PivotTableTests.cs** - Pivot table solutions for nested lists ⭐ **Key insight**
6. **MarkOutWriterTests.cs** - Low-level writer tests
7. **SerializerTests.cs** - Basic serialization tests

## What Works Well

### 1. Nested Objects (Scalar Fields)
✅ **Pattern:** Object → Nested Object (with scalar fields only)
```
# Person

Age: 30

## Contact Info

Email: alice@example.com
Phone: 555-1234
City: Seattle
```
**Status:** Works perfectly. Nested objects become H2 subsections.

### 2. Lists of Objects → Tables
✅ **Pattern:** Object → List\<Object\>
```
## Projects

| Name | Status | Duration |
|------|--------|----------|
| API  | Success| 2.5s     |
| Web  | Success| 3.2s     |
```
**Status:** Works perfectly. Lists of objects become markdown tables.

### 3. Object with Multiple Lists at Same Level
✅ **Pattern:** Object → List\<A\>, List\<B\>, List\<C\>
```
## Services
[table of services]

## Dependencies
[table of dependencies]

Features:
- Feature 1
- Feature 2
```
**Status:** Works perfectly. Each list becomes its own section.

### 4. String Arrays
✅ **Pattern:** Object → List\<string\>
```
Frameworks:
- netstandard2.0
- net6.0
- net8.0
```
**Status:** Works perfectly. String lists use markdown bullet syntax.

### 5. Nested Object with List
✅ **Pattern:** Object → Nested Object → List\<Object\>
```
## Team Info

Lead: Alice
Size: 5

## Contributors

| Name | Commits |
|------|---------|
| Bob  | 42      |
```
**Status:** Works well. The nested object's list becomes a table at the same heading level.

## Critical Limitations

### 1. ❌ Lists of Objects Where Each Object Contains Lists

**Pattern:** Object → List\<A\> where A contains List\<B\>

**Example from testing:**
```csharp
class PackageInspection {
    List<DependencyGroup> Dependencies;  // Each has List<Dependency>
}
```

**Current Output:**
```
## Dependencies

| Target Framework | Packages |
|------------------|----------|
| net6.0 | System.Collections.Generic.List`1[Dependency] |
| net8.0 | System.Collections.Generic.List`1[Dependency] |
```

**Problem:** The inner `Packages` list cannot be represented in a table cell. It gets serialized as `ToString()` which is useless.

**Why:** Markdown tables cannot contain nested lists or tables in cells.

**This is Actually a Pivot Table Problem! 🎯**

The nested structure is asking to be pivoted. Instead of:
- Rows = Groups (net6.0, net8.0)
- Column with nested list = Packages

Pivot to:
- Rows = Packages (System.Memory, System.Text.Json, etc.)
- Columns = Frameworks (net6.0, net8.0, netstandard2.0)
- Values = Versions (4.5.5, 6.0.0, etc.)

**Pivoted Output:**
```
## Dependencies

| Package          | net6.0 | net8.0 | netstandard2.0 |
|------------------|--------|--------|----------------|
| System.Memory    | 4.5.5  | 4.5.5  | 4.5.5          |
| System.Text.Json | 6.0.0  |        | 6.0.0          |
| System.Runtime   |        |        | 4.3.1          |
| Microsoft.CSharp | 4.7.0  | 4.7.0  | 4.7.0          |
```

✅ **This works perfectly with current MDF!** The user just needs to pivot their data before serializing.

**Real-World Impact:** This is the pattern used in dotnet-inspector for showing dependencies grouped by target framework. The solution is to provide a computed property that returns the pivoted view.

### 2. ❌ Three or More Levels of Nesting
**Pattern:** Object → List\<A\> where A contains List\<B\> where B contains List\<C\>

**Example:**
```csharp
class Organization {
    List<Department> Departments;  // Each has List<Team>
                                   // Each Team has List<string> Projects
}
```

**Problem:** Only the first two levels can be represented. The third level (Projects within Teams within Departments) is lost.

## Recommended Patterns

### ✅ Use This: Pivot Nested Lists into Matrix

When you have `List<Group>` where each `Group` has `List<Item>`:

**Instead of:**
```csharp
class Package {
    List<DependencyGroup> Groups;  // Each has List<Dependency>
}
```

**Do this:**
```csharp
class Package {
    // Keep original for programmatic access
    [MarkOutIgnore]
    public List<DependencyGroup> DependencyGroups { get; set; }
    
    // Provide pivoted view for MDF
    [MdfSection(Name = "Dependencies")]
    public List<DependencyVersionMatrix> DependencyMatrix
    {
        get => PivotDependencies(DependencyGroups);
        set { }
    }
}

class DependencyVersionMatrix {
    public string Package { get; set; }
    public string? Net6Version { get; set; }
    public string? Net8Version { get; set; }
    public string? NetStandard2Version { get; set; }
}
```

This gives you the Excel pivot table view that works perfectly in Markdown!

### ✅ Use This: Flat Parallel Lists
```csharp
class BuildResult {
    List<Project> Projects;      // → Section with table
    BuildSummary Summary;        // → Section with scalars
}
```

### ✅ Use This: Nested Object with Final List
```csharp
class Assembly {
    AssemblyInfo Metadata;       // → Section with scalars
    ApiSurface Api;              // → Section with scalars + counts
}
```

### ❌ Avoid This: Lists in Lists

```csharp
class Package {
    List<DependencyGroup> Groups;  // Each has List<Dependency>
}
```

**Solution:** Pivot the data! See "Pivot Nested Lists" pattern above.

### 🔧 Alternative Solutions for Nested Lists

**Option 1: Pivot (Recommended)**
Transform `List<Group>` → `List<Matrix>` where groups become columns

**Option 2: Flatten**
Transform nested structure into single flat list with repeated group keys

**Option 3: Section-per-Item** (if items known at design time)
```csharp
[MdfSection(Name = "Dependencies (net6.0)")]
List<Dependency> Net6Dependencies;

[MdfSection(Name = "Dependencies (net8.0)")]  
List<Dependency> Net8Dependencies;
```

**Option 4: Subsections** (would require library enhancement)
Render each group item as H3 subsection with its nested list as a table

## Real-World Usage: dotnet-inspector

The dotnet-inspector tool successfully uses MDF because it follows these patterns:

### Works Well
```csharp
class InspectionResult {
    // Scalar fields
    string PackageName, Version, Description;
    
    // String arrays
    List<string> TargetFrameworks, SupportedRids;
    
    // Object lists (become tables)
    List<RidPackageReference> RuntimeIdentifierPackages;
    List<AssemblyAudit> AssemblyAudits;
    
    // Nested objects with scalars
    AuditSummary AuditSummary;
}
```

### Limitation Hit
```csharp
class InspectionResult {
    // This pattern is problematic:
    List<DependencyGroup> DependencyGroups;  // Each has List<PackageDependency>
}
```

**Current Solution:** The `DependencyGroups` uses `[MarkOutIgnore]` on the nested `Dependencies` property, or flattens the structure in dotnet-inspector's output.

## Design Implications

### Format Constraint
MDF is fundamentally constrained by Markdown's capabilities:
- Tables cannot contain lists or nested tables
- Heading hierarchy limits nesting depth (H1-H6)
- Complex nested structures require multiple heading levels

### Target Use Cases
MDF works best for:
1. **Configuration data** - Mostly scalars with some lists
2. **Build/test results** - Parallel lists at same level
3. **Package metadata** - Shallow hierarchies
4. **API documentation** - Object with property lists

MDF struggles with:
1. **Deeply nested documents** (3+ levels)
2. **Graphs with cross-references**
3. **Lists of objects with their own lists**
4. **Tree structures with variable depth**

### Recommendations for Library Evolution

1. **Document these limitations clearly** in mdf-spec.md
2. **Consider adding detection** - source generator could warn when it detects problematic patterns
3. **Add explicit support for common workarounds:**
   - Helper attributes for section-per-item patterns
   - Built-in flattening strategies
   - Better ToString() handling for nested collections
4. **Consider alternative representations:**
   - Nested lists could use indented markdown lists instead of tables
   - Could generate multiple H2 sections for list items (verbose but complete)
   - Could support YAML-like nested structures in code blocks

## Test Statistics

- **Total Tests:** 61
- **Passing:** 61 (100%)
- **Test Files:** 6
- **Test Classes:** 8
- **Example Outputs:** 7 visual examples with commentary
- **Pivot Strategies:** 3 different approaches tested

## Conclusions

1. **MDF is excellent for 1-2 levels of nesting** with the current approach
2. **The List<Group> with List<Item> pattern is a PIVOT TABLE problem** - easily solved by pivoting data before serialization
3. **Real-world usage (dotnet-inspector) can use computed properties** to provide pivoted views
4. **The format hits fundamental Markdown limitations** at 3+ levels of true nesting
5. **Users need clear guidance** on what patterns work and which to pivot
6. **The format should not try to be JSON** - accept the trade-offs for human readability

**Key Breakthrough:** Recognizing the "Excel pivot table problem" transforms the main limitation into a solvable data transformation challenge. Users can keep their nested structure for code and provide a pivoted view for MDF serialization.

The test suite now provides concrete examples of what works and what doesn't, helping guide both library evolution and user expectations.
