# MDF Rendering Strategies for Nested Lists

## The Problem

When you have `List<Group>` where each `Group` contains `List<Item>`, Markdown tables can't represent this directly. Instead of seeing it as a limitation, we recognize it as a **design decision**: **which rendering strategy best serves the reader's insight?**

## The Four Strategies

### Strategy 1: Pivot Table 📊
**Transform groups into columns, items into rows**

```markdown
## Dependencies

| Package          | net6.0 | net8.0 | netstandard2.0 |
|------------------|--------|--------|----------------|
| System.Memory    | 4.5.5  | 4.5.5  | 4.5.5          |
| System.Text.Json | 6.0.0  |        | 6.0.0          |
| Microsoft.CSharp | 4.7.0  | 4.7.0  | 4.7.0          |
```

**Reader Insight:** "How do items compare across groups?"

**Best For:**
- Comparing same items across groups
- Version compatibility matrix
- Seeing which items appear in multiple groups
- Finding differences/inconsistencies

**Not Good For:**
- Many columns (>5-6 frameworks)
- Groups have very different items
- Reader cares about one group at a time

**Example Use Cases:**
- NuGet package dependencies across frameworks
- Test results across platforms
- Feature availability by plan/tier

---

### Strategy 2: Multiple Tables 📋
**Each group gets its own section with a table**

```markdown
## Dependencies (net6.0)

| Name             | Version |
|------------------|---------|
| System.Memory    | 4.5.5   |
| System.Text.Json | 6.0.0   |

## Dependencies (net8.0)

| Name             | Version |
|------------------|---------|
| System.Memory    | 4.5.5   |
| Microsoft.CSharp | 4.7.0   |
```

**Reader Insight:** "What does net6.0 need?" "What does net8.0 need?"

**Best For:**
- Reader examines one group at a time
- Groups have different sets of items
- Each group is a complete unit (build config, test suite, etc.)
- Items have multiple properties (2-3 columns)

**Not Good For:**
- Comparing across groups
- Too many groups (>4-5)
- Simple items (just names) - use lists instead

**Example Use Cases:**
- Build projects by configuration
- Test assemblies with their results
- API endpoints by version

---

### Strategy 3: Multiple Lists 📝
**Each group gets a bullet list**

```markdown
Dependencies (net6.0):
- System.Memory 4.5.5
- System.Text.Json 6.0.0
- Microsoft.CSharp 4.7.0

Dependencies (net8.0):
- System.Memory 4.5.5
- Microsoft.CSharp 4.7.0
```

**Reader Insight:** "Quick scan of what each group needs"

**Best For:**
- Simple items (just names or name+version)
- Quick readability
- Compact output
- Items don't need tabular formatting

**Not Good For:**
- Items with multiple properties
- Need to compare/sort items
- Large number of items per group (>10)

**Example Use Cases:**
- Installed packages by category
- Features by edition
- Supported platforms
- File lists

---

### Strategy 4: Multiple Subsections 📑
**Each group item becomes an H3 subsection (proposed feature)**

```markdown
## Build Configurations

### Debug

Platform: Any CPU  
Optimized: no

Warnings:
- CS8600
- CS8603

| Flag            | Value        |
|-----------------|--------------|
| DefineConstants | DEBUG;TRACE  |
| DebugType       | full         |

### Release

Platform: Any CPU  
Optimized: yes

| Flag            | Value   |
|-----------------|---------|
| DefineConstants | TRACE   |
| DebugType       | pdbonly |
```

**Reader Insight:** "Show me complete Debug config" "Show me complete Release config"

**Best For:**
- Groups have complex nested structure
- Each group should be examined independently
- Groups have their own nested lists/tables
- Reader navigates by group (using heading hierarchy)

**Not Good For:**
- Comparing across groups
- Simple groups (overkill)

**Example Use Cases:**
- Build configurations (Debug, Release, etc.)
- Deployment stages with steps
- Test suites with categories and tests
- CI/CD pipeline stages

**Note:** This strategy requires library enhancement to automatically render List<T> items as subsections when T has non-scalar properties.

---

## Decision Tree

```
What does the reader want to know?

├─ "How do items COMPARE across groups?"
│  └─ Use PIVOT TABLE (Strategy 1)
│
├─ "What's IN each group?" (groups are independent)
│  │
│  ├─ Items are SIMPLE (name only or name+version)
│  │  └─ Use MULTIPLE LISTS (Strategy 3)
│  │
│  ├─ Items have 2-3 PROPERTIES, no nesting
│  │  └─ Use MULTIPLE TABLES (Strategy 2)
│  │
│  └─ Items have NESTED structure
│     └─ Use MULTIPLE SUBSECTIONS (Strategy 4)
│
└─ "Groups are FUNDAMENTALLY DIFFERENT"
   └─ Don't use List<Group>, use separate properties
```

### Additional Considerations

**Number of Groups:**
- 2-4 groups: Any strategy works
- 5-10 groups: Avoid pivot (too many columns)
- 10+ groups: Use subsections or reconsider grouping

**Items per Group:**
- 1-5 items: Lists work great
- 5-20 items: Tables or pivot
- 20+ items: Tables, consider splitting

---

## Real-World Examples

### Package Dependencies (dotnet-inspector)
**Scenario:** NuGet package dependencies by target framework

**Reader Questions:**
- "Does this package work with my framework?"
- "Are there version inconsistencies?"
- "Which dependencies are shared?"

**Recommended:** **PIVOT TABLE** ✅
- Easy cross-framework comparison
- Version differences visible
- Compact representation

---

### Build Configurations
**Scenario:** MSBuild project with Debug/Release configs

**Reader Questions:**
- "What compiler flags are in Debug?"
- "What's the complete Release config?"

**Recommended:** **MULTIPLE SUBSECTIONS** ✅
- Each config is self-contained
- Complex nested data (flags, warnings, defines)
- Reader examines one config at a time

---

### Test Results by Assembly
**Scenario:** Test suite results grouped by assembly

**Reader Questions:**
- "How many tests passed in Core.Tests?"
- "Which assembly has failures?"

**Recommended:** **MULTIPLE TABLES** ✅
- Each assembly is independent
- Test results have multiple properties (status, duration, message)
- Tables easier to scan than pivot

---

### Installed Packages by Category
**Scenario:** Show packages grouped by usage category

**Reader Questions:**
- "What dev tools are installed?"
- "Quick overview of all packages"

**Recommended:** **MULTIPLE LISTS** ✅
- Simple items (package names + versions)
- Quick readability
- Compact output

---

## Implementation Guide

### For Strategy 1 (Pivot Table)

```csharp
class Package {
    // Keep original for code
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
    [MdfPropertyName("net6.0")]
    public string? Net6Version { get; set; }
    [MdfPropertyName("net8.0")]
    public string? Net8Version { get; set; }
}
```

### For Strategy 2 (Multiple Tables)

```csharp
class Package {
    [MdfSection(Name = "Dependencies (net6.0)")]
    public List<Dependency>? Net6Dependencies { get; set; }
    
    [MdfSection(Name = "Dependencies (net8.0)")]
    public List<Dependency>? Net8Dependencies { get; set; }
}

class Dependency {
    public string Name { get; set; }
    public string Version { get; set; }
}
```

### For Strategy 3 (Multiple Lists)

```csharp
class Package {
    [MdfPropertyName("Dependencies (net6.0)")]
    public List<string>? Net6Dependencies { get; set; }
    
    [MdfPropertyName("Dependencies (net8.0)")]
    public List<string>? Net8Dependencies { get; set; }
}
```

### For Strategy 4 (Multiple Subsections - Proposed)

```csharp
class Project {
    // Proposed: Library detects non-scalar properties and uses subsections
    [MdfSection(Name = "Build Configurations", Level = 2)]
    public List<BuildConfiguration>? Configurations { get; set; }
}

class BuildConfiguration {
    public string Name { get; set; }  // Becomes H3 heading
    public string Platform { get; set; }
    public bool Optimized { get; set; }
    public List<CompilerFlag>? Flags { get; set; }  // Becomes table in subsection
}
```

---

## Conclusion

The "limitation" of nested lists in MDF is actually an opportunity to **choose the right representation for your readers**. Just like Excel users pivot data based on what they want to see, MDF users should transform nested data based on what insight they want to provide.

**Key Principle:** The format doesn't dictate the structure - the reader's questions do.

---

## Test Coverage

This analysis is backed by **70 comprehensive tests** covering:
- All four strategies with examples
- Decision matrix validation
- Real-world scenarios
- Edge cases and trade-offs
- Visual output demonstrations

See test files:
- `RenderingStrategyTests.cs` - All four strategies with decision matrix
- `PivotTableTests.cs` - Pivot table deep dive
- `ListRenderingStrategyTests.cs` - Alternative approaches
- `NestedStructureTests.cs` - Core nesting patterns
- `BuildResultsTests.cs` - Real-world examples
- `FormatExamplesTests.cs` - Visual demonstrations
