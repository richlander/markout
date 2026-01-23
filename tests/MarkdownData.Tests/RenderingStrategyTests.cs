using MarkdownData;
using Xunit;
using Xunit.Abstractions;

namespace MarkdownData.Tests;

/// <summary>
/// Tests exploring different rendering strategies for List&lt;Group&gt; where Group has List&lt;Item&gt;.
/// The right strategy depends on what insight the reader needs.
/// </summary>

#region Strategy Models

// Base data structure - the problem
public class StrategyDependencyGroup
{
    public string TargetFramework { get; set; } = "";
    public List<StrategyDependency> Packages { get; set; } = new();
}

public class StrategyDependency
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
}

// STRATEGY 1: Pivot Table - Compare across groups
[MdfSerializable(TitleProperty = nameof(PackageName))]
public class PackageWithPivot
{
    [MdfPropertyName("Package")]
    public string PackageName { get; set; } = "";
    
    [MdfSection(Name = "Dependencies")]
    public List<StrategyDependencyVersionMatrix> Dependencies { get; set; } = new();
}

[MdfSerializable]
public class StrategyDependencyVersionMatrix
{
    public string Package { get; set; } = "";
    [MdfPropertyName("net6.0")]
    public string? Net6 { get; set; }
    [MdfPropertyName("net8.0")]
    public string? Net8 { get; set; }
    [MdfPropertyName("netstandard2.0")]
    public string? NetStandard { get; set; }
}

// STRATEGY 2: Multiple Tables - Each group gets its own table
[MdfSerializable(TitleProperty = nameof(PackageName))]
public class PackageWithMultipleTables
{
    [MdfPropertyName("Package")]
    public string PackageName { get; set; } = "";
    
    // Each framework gets its own section with table
    [MdfSection(Name = "Dependencies (net6.0)")]
    public List<SimpleDependency>? Net6Dependencies { get; set; }
    
    [MdfSection(Name = "Dependencies (net8.0)")]
    public List<SimpleDependency>? Net8Dependencies { get; set; }
    
    [MdfSection(Name = "Dependencies (netstandard2.0)")]
    public List<SimpleDependency>? NetStandardDependencies { get; set; }
}

[MdfSerializable]
public class SimpleDependency
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
}

// STRATEGY 3: Multiple Lists - Simple bullet lists per group
[MdfSerializable(TitleProperty = nameof(PackageName))]
public class PackageWithMultipleLists
{
    [MdfPropertyName("Package")]
    public string PackageName { get; set; } = "";
    
    [MdfPropertyName("Dependencies (net6.0)")]
    public List<string>? Net6Dependencies { get; set; }
    
    [MdfPropertyName("Dependencies (net8.0)")]
    public List<string>? Net8Dependencies { get; set; }
    
    [MdfPropertyName("Dependencies (netstandard2.0)")]
    public List<string>? NetStandardDependencies { get; set; }
}

// STRATEGY 4: Multiple Subsections - For complex groups (proposed feature)
[MdfSerializable(TitleProperty = nameof(ProjectName))]
public class ProjectWithSubsections
{
    [MdfPropertyName("Project")]
    public string ProjectName { get; set; } = "";
    
    // Hypothetical: Library could render each item as H3
    [MdfSection(Name = "Build Configurations", Level = 2)]
    public List<BuildConfiguration>? Configurations { get; set; }
}

[MdfSerializable]
public class BuildConfiguration
{
    public string Name { get; set; } = "";  // Would become H3 heading
    public string Platform { get; set; } = "";
    public bool Optimized { get; set; }
    
    [MdfIgnore]  // Proposed: Could render in subsection strategy
    public List<string>? Warnings { get; set; }
    
    [MdfIgnore]  // Proposed: Could become table in subsection
    public List<CompilerFlag>? Flags { get; set; }
}

[MdfSerializable]
public class CompilerFlag
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
}

#endregion

#region Test Context

[MdfContext(typeof(PackageWithPivot))]
[MdfContext(typeof(PackageWithMultipleTables))]
[MdfContext(typeof(PackageWithMultipleLists))]
[MdfContext(typeof(ProjectWithSubsections))]
public partial class RenderingStrategyContext : MdfSerializerContext
{
}

#endregion

public class RenderingStrategyTests
{
    private readonly ITestOutputHelper _output;

    public RenderingStrategyTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region Strategy 1: Pivot Table (Compare Across Groups)

    [Fact]
    public void Strategy1_PivotTable_BestForComparison()
    {
        var package = new PackageWithPivot
        {
            PackageName = "Newtonsoft.Json",
            Dependencies = new List<StrategyDependencyVersionMatrix>
            {
                new() { Package = "System.Memory", Net6 = "4.5.5", Net8 = "4.5.5", NetStandard = "4.5.5" },
                new() { Package = "System.Text.Json", Net6 = "6.0.0", Net8 = null, NetStandard = "6.0.0" },
                new() { Package = "System.Runtime", Net6 = null, Net8 = null, NetStandard = "4.3.1" },
                new() { Package = "Microsoft.CSharp", Net6 = "4.7.0", Net8 = "4.7.0", NetStandard = "4.7.0" }
            }
        };

        var mdf = MdfSerializer.Serialize(package, RenderingStrategyContext.Default);
        
        _output.WriteLine("=== STRATEGY 1: PIVOT TABLE ===");
        _output.WriteLine(mdf);
        _output.WriteLine("");
        _output.WriteLine("📊 READER INSIGHT:");
        _output.WriteLine("  \"Which packages are used across different frameworks?\"");
        _output.WriteLine("  \"Do versions differ between frameworks?\"");
        _output.WriteLine("  \"Which packages are framework-specific?\"");
        _output.WriteLine("");
        _output.WriteLine("✅ BEST FOR:");
        _output.WriteLine("  - Comparing same items across groups");
        _output.WriteLine("  - Version compatibility matrix");
        _output.WriteLine("  - Seeing which items appear in multiple groups");
        _output.WriteLine("  - Finding differences/inconsistencies");
        _output.WriteLine("");
        _output.WriteLine("❌ NOT GOOD FOR:");
        _output.WriteLine("  - Many columns (>5-6 frameworks)");
        _output.WriteLine("  - Groups have very different items");
        _output.WriteLine("  - Reader cares about one group at a time");
        _output.WriteLine("");
    }

    #endregion

    #region Strategy 2: Multiple Tables (Focus on Each Group)

    [Fact]
    public void Strategy2_MultipleTables_BestForIndividualGroups()
    {
        var package = new PackageWithMultipleTables
        {
            PackageName = "Newtonsoft.Json",
            Net6Dependencies = new List<SimpleDependency>
            {
                new() { Name = "System.Memory", Version = "4.5.5" },
                new() { Name = "System.Text.Json", Version = "6.0.0" },
                new() { Name = "Microsoft.CSharp", Version = "4.7.0" }
            },
            Net8Dependencies = new List<SimpleDependency>
            {
                new() { Name = "System.Memory", Version = "4.5.5" },
                new() { Name = "Microsoft.CSharp", Version = "4.7.0" }
            },
            NetStandardDependencies = new List<SimpleDependency>
            {
                new() { Name = "System.Memory", Version = "4.5.5" },
                new() { Name = "System.Text.Json", Version = "6.0.0" },
                new() { Name = "System.Runtime", Version = "4.3.1" },
                new() { Name = "Microsoft.CSharp", Version = "4.7.0" }
            }
        };

        var mdf = MdfSerializer.Serialize(package, RenderingStrategyContext.Default);
        
        _output.WriteLine("=== STRATEGY 2: MULTIPLE TABLES ===");
        _output.WriteLine(mdf);
        _output.WriteLine("");
        _output.WriteLine("📊 READER INSIGHT:");
        _output.WriteLine("  \"What does net6.0 need?\"");
        _output.WriteLine("  \"What does net8.0 need?\"");
        _output.WriteLine("  \"Show me each framework's complete dependency list\"");
        _output.WriteLine("");
        _output.WriteLine("✅ BEST FOR:");
        _output.WriteLine("  - Reader examines one group at a time");
        _output.WriteLine("  - Groups have different sets of items");
        _output.WriteLine("  - Each group is a complete unit (build config, test suite, etc.)");
        _output.WriteLine("  - Items have multiple properties to show in table");
        _output.WriteLine("");
        _output.WriteLine("❌ NOT GOOD FOR:");
        _output.WriteLine("  - Comparing across groups");
        _output.WriteLine("  - Too many groups (>4-5)");
        _output.WriteLine("  - Simple items (just names)");
        _output.WriteLine("");
    }

    #endregion

    #region Strategy 3: Multiple Lists (Simplest Items)

    [Fact]
    public void Strategy3_MultipleLists_BestForSimpleItems()
    {
        var package = new PackageWithMultipleLists
        {
            PackageName = "Newtonsoft.Json",
            Net6Dependencies = new List<string>
            {
                "System.Memory 4.5.5",
                "System.Text.Json 6.0.0",
                "Microsoft.CSharp 4.7.0"
            },
            Net8Dependencies = new List<string>
            {
                "System.Memory 4.5.5",
                "Microsoft.CSharp 4.7.0"
            },
            NetStandardDependencies = new List<string>
            {
                "System.Memory 4.5.5",
                "System.Text.Json 6.0.0",
                "System.Runtime 4.3.1",
                "Microsoft.CSharp 4.7.0"
            }
        };

        var mdf = MdfSerializer.Serialize(package, RenderingStrategyContext.Default);
        
        _output.WriteLine("=== STRATEGY 3: MULTIPLE LISTS ===");
        _output.WriteLine(mdf);
        _output.WriteLine("");
        _output.WriteLine("📊 READER INSIGHT:");
        _output.WriteLine("  \"Quick scan of what each framework needs\"");
        _output.WriteLine("  \"Simple, compact overview\"");
        _output.WriteLine("");
        _output.WriteLine("✅ BEST FOR:");
        _output.WriteLine("  - Simple items (just names or name+version)");
        _output.WriteLine("  - Quick readability");
        _output.WriteLine("  - Compact output");
        _output.WriteLine("  - Items don't need tabular formatting");
        _output.WriteLine("");
        _output.WriteLine("❌ NOT GOOD FOR:");
        _output.WriteLine("  - Items with multiple properties");
        _output.WriteLine("  - Need to compare/sort items");
        _output.WriteLine("  - Large number of items per group");
        _output.WriteLine("");
    }

    #endregion

    #region Strategy 4: Multiple Subsections (Proposed Feature)

    [Fact]
    public void Strategy4_MultipleSubsections_BestForComplexGroups()
    {
        var project = new ProjectWithSubsections
        {
            ProjectName = "MyApp",
            Configurations = new List<BuildConfiguration>
            {
                new()
                {
                    Name = "Debug",
                    Platform = "Any CPU",
                    Optimized = false,
                    Warnings = new List<string> { "CS8600", "CS8603" },
                    Flags = new List<CompilerFlag>
                    {
                        new() { Name = "DefineConstants", Value = "DEBUG;TRACE" },
                        new() { Name = "DebugType", Value = "full" }
                    }
                },
                new()
                {
                    Name = "Release",
                    Platform = "Any CPU",
                    Optimized = true,
                    Warnings = new List<string>(),
                    Flags = new List<CompilerFlag>
                    {
                        new() { Name = "DefineConstants", Value = "TRACE" },
                        new() { Name = "DebugType", Value = "pdbonly" }
                    }
                }
            }
        };

        var mdf = MdfSerializer.Serialize(project, RenderingStrategyContext.Default);
        
        _output.WriteLine("=== STRATEGY 4: MULTIPLE SUBSECTIONS (PROPOSED) ===");
        _output.WriteLine("");
        _output.WriteLine("CURRENT OUTPUT:");
        _output.WriteLine(mdf);
        _output.WriteLine("");
        _output.WriteLine("DESIRED OUTPUT:");
        _output.WriteLine("# MyApp");
        _output.WriteLine("");
        _output.WriteLine("## Build Configurations");
        _output.WriteLine("");
        _output.WriteLine("### Debug");
        _output.WriteLine("");
        _output.WriteLine("Platform: Any CPU");
        _output.WriteLine("Optimized: no");
        _output.WriteLine("");
        _output.WriteLine("Warnings:");
        _output.WriteLine("- CS8600");
        _output.WriteLine("- CS8603");
        _output.WriteLine("");
        _output.WriteLine("| Name | Value |");
        _output.WriteLine("|------|-------|");
        _output.WriteLine("| DefineConstants | DEBUG;TRACE |");
        _output.WriteLine("| DebugType | full |");
        _output.WriteLine("");
        _output.WriteLine("### Release");
        _output.WriteLine("");
        _output.WriteLine("Platform: Any CPU");
        _output.WriteLine("Optimized: yes");
        _output.WriteLine("");
        _output.WriteLine("| Name | Value |");
        _output.WriteLine("|------|-------|");
        _output.WriteLine("| DefineConstants | TRACE |");
        _output.WriteLine("| DebugType | pdbonly |");
        _output.WriteLine("");
        _output.WriteLine("📊 READER INSIGHT:");
        _output.WriteLine("  \"What's in the Debug configuration?\"");
        _output.WriteLine("  \"What's in the Release configuration?\"");
        _output.WriteLine("  \"Each group is a complete, self-contained unit\"");
        _output.WriteLine("");
        _output.WriteLine("✅ BEST FOR:");
        _output.WriteLine("  - Groups have complex nested structure");
        _output.WriteLine("  - Each group should be examined independently");
        _output.WriteLine("  - Groups have their own nested lists/tables");
        _output.WriteLine("  - Reader navigates by group (using heading hierarchy)");
        _output.WriteLine("");
        _output.WriteLine("❌ NOT GOOD FOR:");
        _output.WriteLine("  - Comparing across groups");
        _output.WriteLine("  - Simple groups (overkill)");
        _output.WriteLine("");
        _output.WriteLine("IMPLEMENTATION:");
        _output.WriteLine("  - Detect List<T> where T has non-scalar properties");
        _output.WriteLine("  - Use first string property or TitleProperty as H3 heading");
        _output.WriteLine("  - Render each T's properties within that H3 section");
        _output.WriteLine("  - Nested lists become tables at H3 level");
        _output.WriteLine("");
    }

    #endregion

    #region Decision Matrix

    [Fact]
    public void DecisionMatrix_WhichStrategyToUse()
    {
        _output.WriteLine("=== DECISION MATRIX: CHOOSING THE RIGHT STRATEGY ===");
        _output.WriteLine("");
        _output.WriteLine("Question 1: What does the reader want to know?");
        _output.WriteLine("");
        _output.WriteLine("┌─ \"How do items compare across groups?\"");
        _output.WriteLine("│  → PIVOT TABLE (Strategy 1)");
        _output.WriteLine("│  Examples: dependency versions across frameworks,");
        _output.WriteLine("│             test results across platforms");
        _output.WriteLine("│");
        _output.WriteLine("├─ \"What's in each group?\" (groups are independent)");
        _output.WriteLine("│  │");
        _output.WriteLine("│  ├─ Items are simple (name only or name+version)");
        _output.WriteLine("│  │  → MULTIPLE LISTS (Strategy 3)");
        _output.WriteLine("│  │  Examples: feature lists, installed packages");
        _output.WriteLine("│  │");
        _output.WriteLine("│  ├─ Items have 2-3 properties, no nesting");
        _output.WriteLine("│  │  → MULTIPLE TABLES (Strategy 2)");
        _output.WriteLine("│  │  Examples: build projects, test assemblies");
        _output.WriteLine("│  │");
        _output.WriteLine("│  └─ Items have nested structure");
        _output.WriteLine("│     → MULTIPLE SUBSECTIONS (Strategy 4)");
        _output.WriteLine("│     Examples: build configs, deployment stages");
        _output.WriteLine("│");
        _output.WriteLine("└─ \"Groups are fundamentally different\"");
        _output.WriteLine("   → Don't use List<Group>, use separate properties");
        _output.WriteLine("");
        _output.WriteLine("Question 2: How many groups?");
        _output.WriteLine("  - 2-4 groups: Any strategy works");
        _output.WriteLine("  - 5-10 groups: Avoid pivot table (too many columns)");
        _output.WriteLine("  - 10+ groups: Use subsections or flatten differently");
        _output.WriteLine("");
        _output.WriteLine("Question 3: How many items per group?");
        _output.WriteLine("  - 1-5 items: Lists work great");
        _output.WriteLine("  - 5-20 items: Tables or pivot");
        _output.WriteLine("  - 20+ items: Tables, consider pagination/filtering");
        _output.WriteLine("");
    }

    #endregion

    #region Real-World Examples

    [Fact]
    public void RealWorld_PackageDependencies()
    {
        _output.WriteLine("=== REAL WORLD: Package Dependencies ===");
        _output.WriteLine("");
        _output.WriteLine("Scenario: NuGet package with dependencies per framework");
        _output.WriteLine("");
        _output.WriteLine("Reader Questions:");
        _output.WriteLine("  - \"Does this package work with my framework?\"");
        _output.WriteLine("  - \"What versions are compatible?\"");
        _output.WriteLine("  - \"Are there version inconsistencies?\"");
        _output.WriteLine("");
        _output.WriteLine("RECOMMENDATION: PIVOT TABLE (Strategy 1)");
        _output.WriteLine("  - Easy to scan for your framework");
        _output.WriteLine("  - See version differences at a glance");
        _output.WriteLine("  - Compact representation");
        _output.WriteLine("");
    }

    [Fact]
    public void RealWorld_BuildConfigurations()
    {
        _output.WriteLine("=== REAL WORLD: Build Configurations ===");
        _output.WriteLine("");
        _output.WriteLine("Scenario: MSBuild project with Debug/Release/etc configs");
        _output.WriteLine("");
        _output.WriteLine("Reader Questions:");
        _output.WriteLine("  - \"What compiler flags are in Debug mode?\"");
        _output.WriteLine("  - \"What's different between Debug and Release?\"");
        _output.WriteLine("  - \"Show me complete Release config\"");
        _output.WriteLine("");
        _output.WriteLine("RECOMMENDATION: MULTIPLE SUBSECTIONS (Strategy 4)");
        _output.WriteLine("  - Each config is self-contained");
        _output.WriteLine("  - Complex nested data (flags, warnings, etc.)");
        _output.WriteLine("  - Reader examines one config at a time");
        _output.WriteLine("");
    }

    [Fact]
    public void RealWorld_TestResults()
    {
        _output.WriteLine("=== REAL WORLD: Test Results by Assembly ===");
        _output.WriteLine("");
        _output.WriteLine("Scenario: Test suite with results per test assembly");
        _output.WriteLine("");
        _output.WriteLine("Reader Questions:");
        _output.WriteLine("  - \"How many tests passed in each assembly?\"");
        _output.WriteLine("  - \"Which assembly has failures?\"");
        _output.WriteLine("  - \"Show me summary per assembly\"");
        _output.WriteLine("");
        _output.WriteLine("RECOMMENDATION: MULTIPLE TABLES (Strategy 2)");
        _output.WriteLine("  - Each assembly is independent");
        _output.WriteLine("  - Items (test results) have multiple properties");
        _output.WriteLine("  - Tables are easier to scan than pivot");
        _output.WriteLine("");
    }

    [Fact]
    public void RealWorld_InstalledPackages()
    {
        _output.WriteLine("=== REAL WORLD: Installed Packages by Category ===");
        _output.WriteLine("");
        _output.WriteLine("Scenario: Show installed packages grouped by category");
        _output.WriteLine("  Categories: Development, Testing, Deployment, etc.");
        _output.WriteLine("");
        _output.WriteLine("Reader Questions:");
        _output.WriteLine("  - \"What dev tools are installed?\"");
        _output.WriteLine("  - \"Quick scan of all packages\"");
        _output.WriteLine("");
        _output.WriteLine("RECOMMENDATION: MULTIPLE LISTS (Strategy 3)");
        _output.WriteLine("  - Simple items (package names + versions)");
        _output.WriteLine("  - Quick readability");
        _output.WriteLine("  - Compact output");
        _output.WriteLine("");
    }

    #endregion
}
