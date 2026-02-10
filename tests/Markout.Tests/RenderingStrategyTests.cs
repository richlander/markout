using Markout;

namespace Markout.Tests;

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
[MarkoutSerializable(TitleProperty = nameof(PackageName))]
public class PackageWithPivot
{
    [MarkoutPropertyName("Package")]
    public string PackageName { get; set; } = "";
    
    [MarkoutSection(Name = "Dependencies")]
    public List<StrategyDependencyVersionMatrix> Dependencies { get; set; } = new();
}

[MarkoutSerializable]
public class StrategyDependencyVersionMatrix
{
    public string Package { get; set; } = "";
    [MarkoutPropertyName("net6.0")]
    public string? Net6 { get; set; }
    [MarkoutPropertyName("net8.0")]
    public string? Net8 { get; set; }
    [MarkoutPropertyName("netstandard2.0")]
    public string? NetStandard { get; set; }
}

// STRATEGY 2: Multiple Tables - Each group gets its own table
[MarkoutSerializable(TitleProperty = nameof(PackageName))]
public class PackageWithMultipleTables
{
    [MarkoutPropertyName("Package")]
    public string PackageName { get; set; } = "";
    
    // Each framework gets its own section with table
    [MarkoutSection(Name = "Dependencies (net6.0)")]
    public List<SimpleDependency>? Net6Dependencies { get; set; }
    
    [MarkoutSection(Name = "Dependencies (net8.0)")]
    public List<SimpleDependency>? Net8Dependencies { get; set; }
    
    [MarkoutSection(Name = "Dependencies (netstandard2.0)")]
    public List<SimpleDependency>? NetStandardDependencies { get; set; }
}

[MarkoutSerializable]
public class SimpleDependency
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
}

// STRATEGY 3: Multiple Lists - Simple bullet lists per group
[MarkoutSerializable(TitleProperty = nameof(PackageName))]
public class PackageWithMultipleLists
{
    [MarkoutPropertyName("Package")]
    public string PackageName { get; set; } = "";
    
    [MarkoutPropertyName("Dependencies (net6.0)")]
    [MarkoutIgnoreInTable]
    public List<string>? Net6Dependencies { get; set; }
    
    [MarkoutPropertyName("Dependencies (net8.0)")]
    [MarkoutIgnoreInTable]
    public List<string>? Net8Dependencies { get; set; }
    
    [MarkoutPropertyName("Dependencies (netstandard2.0)")]
    [MarkoutIgnoreInTable]
    public List<string>? NetStandardDependencies { get; set; }
}

// STRATEGY 4: Multiple Subsections - For complex groups (proposed feature)
[MarkoutSerializable(TitleProperty = nameof(ProjectName))]
public class ProjectWithSubsections
{
    [MarkoutPropertyName("Project")]
    public string ProjectName { get; set; } = "";
    
    // Hypothetical: Library could render each item as H3
    [MarkoutSection(Name = "Build Configurations", Level = 2)]
    public List<BuildConfiguration>? Configurations { get; set; }
}

[MarkoutSerializable]
public class BuildConfiguration
{
    public string Name { get; set; } = "";  // Would become H3 heading
    public string Platform { get; set; } = "";
    public bool Optimized { get; set; }
    
    [MarkoutIgnore]  // Proposed: Could render in subsection strategy
    public List<string>? Warnings { get; set; }
    
    [MarkoutIgnore]  // Proposed: Could become table in subsection
    public List<CompilerFlag>? Flags { get; set; }
}

[MarkoutSerializable]
public class CompilerFlag
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
}

#endregion

#region Test Context

[MarkoutContext(typeof(PackageWithPivot))]
[MarkoutContext(typeof(PackageWithMultipleTables))]
[MarkoutContext(typeof(PackageWithMultipleLists))]
[MarkoutContext(typeof(ProjectWithSubsections))]
public partial class RenderingStrategyContext : MarkoutSerializerContext
{
}

#endregion

public class RenderingStrategyTests
{
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

        var mdf = MarkoutSerializer.Serialize(package, RenderingStrategyContext.Default);
        
        TestContext.Current.TestOutputHelper!.WriteLine("=== STRATEGY 1: PIVOT TABLE ===");
        TestContext.Current.TestOutputHelper!.WriteLine(mdf);
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("📊 READER INSIGHT:");
        TestContext.Current.TestOutputHelper!.WriteLine("  \"Which packages are used across different frameworks?\"");
        TestContext.Current.TestOutputHelper!.WriteLine("  \"Do versions differ between frameworks?\"");
        TestContext.Current.TestOutputHelper!.WriteLine("  \"Which packages are framework-specific?\"");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("✅ BEST FOR:");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Comparing same items across groups");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Version compatibility matrix");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Seeing which items appear in multiple groups");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Finding differences/inconsistencies");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("❌ NOT GOOD FOR:");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Many columns (>5-6 frameworks)");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Groups have very different items");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Reader cares about one group at a time");
        TestContext.Current.TestOutputHelper!.WriteLine("");
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

        var mdf = MarkoutSerializer.Serialize(package, RenderingStrategyContext.Default);
        
        TestContext.Current.TestOutputHelper!.WriteLine("=== STRATEGY 2: MULTIPLE TABLES ===");
        TestContext.Current.TestOutputHelper!.WriteLine(mdf);
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("📊 READER INSIGHT:");
        TestContext.Current.TestOutputHelper!.WriteLine("  \"What does net6.0 need?\"");
        TestContext.Current.TestOutputHelper!.WriteLine("  \"What does net8.0 need?\"");
        TestContext.Current.TestOutputHelper!.WriteLine("  \"Show me each framework's complete dependency list\"");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("✅ BEST FOR:");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Reader examines one group at a time");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Groups have different sets of items");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Each group is a complete unit (build config, test suite, etc.)");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Items have multiple properties to show in table");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("❌ NOT GOOD FOR:");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Comparing across groups");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Too many groups (>4-5)");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Simple items (just names)");
        TestContext.Current.TestOutputHelper!.WriteLine("");
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

        var mdf = MarkoutSerializer.Serialize(package, RenderingStrategyContext.Default);
        
        TestContext.Current.TestOutputHelper!.WriteLine("=== STRATEGY 3: MULTIPLE LISTS ===");
        TestContext.Current.TestOutputHelper!.WriteLine(mdf);
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("📊 READER INSIGHT:");
        TestContext.Current.TestOutputHelper!.WriteLine("  \"Quick scan of what each framework needs\"");
        TestContext.Current.TestOutputHelper!.WriteLine("  \"Simple, compact overview\"");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("✅ BEST FOR:");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Simple items (just names or name+version)");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Quick readability");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Compact output");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Items don't need tabular formatting");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("❌ NOT GOOD FOR:");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Items with multiple properties");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Need to compare/sort items");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Large number of items per group");
        TestContext.Current.TestOutputHelper!.WriteLine("");
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

        var mdf = MarkoutSerializer.Serialize(project, RenderingStrategyContext.Default);
        
        TestContext.Current.TestOutputHelper!.WriteLine("=== STRATEGY 4: MULTIPLE SUBSECTIONS (PROPOSED) ===");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("CURRENT OUTPUT:");
        TestContext.Current.TestOutputHelper!.WriteLine(mdf);
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("DESIRED OUTPUT:");
        TestContext.Current.TestOutputHelper!.WriteLine("# MyApp");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("## Build Configurations");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("### Debug");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("Platform: Any CPU");
        TestContext.Current.TestOutputHelper!.WriteLine("Optimized: no");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("Warnings:");
        TestContext.Current.TestOutputHelper!.WriteLine("- CS8600");
        TestContext.Current.TestOutputHelper!.WriteLine("- CS8603");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("| Name | Value |");
        TestContext.Current.TestOutputHelper!.WriteLine("|------|-------|");
        TestContext.Current.TestOutputHelper!.WriteLine("| DefineConstants | DEBUG;TRACE |");
        TestContext.Current.TestOutputHelper!.WriteLine("| DebugType | full |");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("### Release");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("Platform: Any CPU");
        TestContext.Current.TestOutputHelper!.WriteLine("Optimized: yes");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("| Name | Value |");
        TestContext.Current.TestOutputHelper!.WriteLine("|------|-------|");
        TestContext.Current.TestOutputHelper!.WriteLine("| DefineConstants | TRACE |");
        TestContext.Current.TestOutputHelper!.WriteLine("| DebugType | pdbonly |");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("📊 READER INSIGHT:");
        TestContext.Current.TestOutputHelper!.WriteLine("  \"What's in the Debug configuration?\"");
        TestContext.Current.TestOutputHelper!.WriteLine("  \"What's in the Release configuration?\"");
        TestContext.Current.TestOutputHelper!.WriteLine("  \"Each group is a complete, self-contained unit\"");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("✅ BEST FOR:");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Groups have complex nested structure");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Each group should be examined independently");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Groups have their own nested lists/tables");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Reader navigates by group (using heading hierarchy)");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("❌ NOT GOOD FOR:");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Comparing across groups");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Simple groups (overkill)");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("IMPLEMENTATION:");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Detect List<T> where T has non-scalar properties");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Use first string property or TitleProperty as H3 heading");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Render each T's properties within that H3 section");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Nested lists become tables at H3 level");
        TestContext.Current.TestOutputHelper!.WriteLine("");
    }

    #endregion

    #region Decision Matrix

    [Fact]
    public void DecisionMatrix_WhichStrategyToUse()
    {
        TestContext.Current.TestOutputHelper!.WriteLine("=== DECISION MATRIX: CHOOSING THE RIGHT STRATEGY ===");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("Question 1: What does the reader want to know?");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("┌─ \"How do items compare across groups?\"");
        TestContext.Current.TestOutputHelper!.WriteLine("│  → PIVOT TABLE (Strategy 1)");
        TestContext.Current.TestOutputHelper!.WriteLine("│  Examples: dependency versions across frameworks,");
        TestContext.Current.TestOutputHelper!.WriteLine("│             test results across platforms");
        TestContext.Current.TestOutputHelper!.WriteLine("│");
        TestContext.Current.TestOutputHelper!.WriteLine("├─ \"What's in each group?\" (groups are independent)");
        TestContext.Current.TestOutputHelper!.WriteLine("│  │");
        TestContext.Current.TestOutputHelper!.WriteLine("│  ├─ Items are simple (name only or name+version)");
        TestContext.Current.TestOutputHelper!.WriteLine("│  │  → MULTIPLE LISTS (Strategy 3)");
        TestContext.Current.TestOutputHelper!.WriteLine("│  │  Examples: feature lists, installed packages");
        TestContext.Current.TestOutputHelper!.WriteLine("│  │");
        TestContext.Current.TestOutputHelper!.WriteLine("│  ├─ Items have 2-3 properties, no nesting");
        TestContext.Current.TestOutputHelper!.WriteLine("│  │  → MULTIPLE TABLES (Strategy 2)");
        TestContext.Current.TestOutputHelper!.WriteLine("│  │  Examples: build projects, test assemblies");
        TestContext.Current.TestOutputHelper!.WriteLine("│  │");
        TestContext.Current.TestOutputHelper!.WriteLine("│  └─ Items have nested structure");
        TestContext.Current.TestOutputHelper!.WriteLine("│     → MULTIPLE SUBSECTIONS (Strategy 4)");
        TestContext.Current.TestOutputHelper!.WriteLine("│     Examples: build configs, deployment stages");
        TestContext.Current.TestOutputHelper!.WriteLine("│");
        TestContext.Current.TestOutputHelper!.WriteLine("└─ \"Groups are fundamentally different\"");
        TestContext.Current.TestOutputHelper!.WriteLine("   → Don't use List<Group>, use separate properties");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("Question 2: How many groups?");
        TestContext.Current.TestOutputHelper!.WriteLine("  - 2-4 groups: Any strategy works");
        TestContext.Current.TestOutputHelper!.WriteLine("  - 5-10 groups: Avoid pivot table (too many columns)");
        TestContext.Current.TestOutputHelper!.WriteLine("  - 10+ groups: Use subsections or flatten differently");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("Question 3: How many items per group?");
        TestContext.Current.TestOutputHelper!.WriteLine("  - 1-5 items: Lists work great");
        TestContext.Current.TestOutputHelper!.WriteLine("  - 5-20 items: Tables or pivot");
        TestContext.Current.TestOutputHelper!.WriteLine("  - 20+ items: Tables, consider pagination/filtering");
        TestContext.Current.TestOutputHelper!.WriteLine("");
    }

    #endregion

    #region Real-World Examples

    [Fact]
    public void RealWorld_PackageDependencies()
    {
        TestContext.Current.TestOutputHelper!.WriteLine("=== REAL WORLD: Package Dependencies ===");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("Scenario: NuGet package with dependencies per framework");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("Reader Questions:");
        TestContext.Current.TestOutputHelper!.WriteLine("  - \"Does this package work with my framework?\"");
        TestContext.Current.TestOutputHelper!.WriteLine("  - \"What versions are compatible?\"");
        TestContext.Current.TestOutputHelper!.WriteLine("  - \"Are there version inconsistencies?\"");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("RECOMMENDATION: PIVOT TABLE (Strategy 1)");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Easy to scan for your framework");
        TestContext.Current.TestOutputHelper!.WriteLine("  - See version differences at a glance");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Compact representation");
        TestContext.Current.TestOutputHelper!.WriteLine("");
    }

    [Fact]
    public void RealWorld_BuildConfigurations()
    {
        TestContext.Current.TestOutputHelper!.WriteLine("=== REAL WORLD: Build Configurations ===");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("Scenario: MSBuild project with Debug/Release/etc configs");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("Reader Questions:");
        TestContext.Current.TestOutputHelper!.WriteLine("  - \"What compiler flags are in Debug mode?\"");
        TestContext.Current.TestOutputHelper!.WriteLine("  - \"What's different between Debug and Release?\"");
        TestContext.Current.TestOutputHelper!.WriteLine("  - \"Show me complete Release config\"");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("RECOMMENDATION: MULTIPLE SUBSECTIONS (Strategy 4)");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Each config is self-contained");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Complex nested data (flags, warnings, etc.)");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Reader examines one config at a time");
        TestContext.Current.TestOutputHelper!.WriteLine("");
    }

    [Fact]
    public void RealWorld_TestResults()
    {
        TestContext.Current.TestOutputHelper!.WriteLine("=== REAL WORLD: Test Results by Assembly ===");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("Scenario: Test suite with results per test assembly");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("Reader Questions:");
        TestContext.Current.TestOutputHelper!.WriteLine("  - \"How many tests passed in each assembly?\"");
        TestContext.Current.TestOutputHelper!.WriteLine("  - \"Which assembly has failures?\"");
        TestContext.Current.TestOutputHelper!.WriteLine("  - \"Show me summary per assembly\"");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("RECOMMENDATION: MULTIPLE TABLES (Strategy 2)");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Each assembly is independent");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Items (test results) have multiple properties");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Tables are easier to scan than pivot");
        TestContext.Current.TestOutputHelper!.WriteLine("");
    }

    [Fact]
    public void RealWorld_InstalledPackages()
    {
        TestContext.Current.TestOutputHelper!.WriteLine("=== REAL WORLD: Installed Packages by Category ===");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("Scenario: Show installed packages grouped by category");
        TestContext.Current.TestOutputHelper!.WriteLine("  Categories: Development, Testing, Deployment, etc.");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("Reader Questions:");
        TestContext.Current.TestOutputHelper!.WriteLine("  - \"What dev tools are installed?\"");
        TestContext.Current.TestOutputHelper!.WriteLine("  - \"Quick scan of all packages\"");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("RECOMMENDATION: MULTIPLE LISTS (Strategy 3)");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Simple items (package names + versions)");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Quick readability");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Compact output");
        TestContext.Current.TestOutputHelper!.WriteLine("");
    }

    #endregion
}
