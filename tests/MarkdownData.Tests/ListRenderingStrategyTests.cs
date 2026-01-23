using MarkdownData;
using Xunit;
using Xunit.Abstractions;

namespace MarkdownData.Tests;

#region Test Models for Different Strategies

// Strategy 2: Section-per-item (works but verbose)
[MdfSerializable(TitleProperty = nameof(PackageName))]
public class PackageWithSectionsPerGroup
{
    [MdfPropertyName("Package")]
    public string PackageName { get; set; } = "";
    public string Version { get; set; } = "";
    
    [MdfSection(Name = "Dependencies (net6.0)")]
    public List<Dependency>? Net6Dependencies { get; set; }
    
    [MdfSection(Name = "Dependencies (net8.0)")]
    public List<Dependency>? Net8Dependencies { get; set; }
    
    [MdfSection(Name = "Dependencies (netstandard2.0)")]
    public List<Dependency>? NetStandard2Dependencies { get; set; }
}

// Strategy 3: Flatten the structure
[MdfSerializable]
public class FlatDependency
{
    [MdfPropertyName("Target Framework")]
    public string TargetFramework { get; set; } = "";
    
    [MdfPropertyName("Package Name")]
    public string PackageName { get; set; } = "";
    
    [MdfPropertyName("Package Version")]
    public string PackageVersion { get; set; } = "";
}

[MdfSerializable(TitleProperty = nameof(PackageName))]
public class PackageWithFlatDependencies
{
    [MdfPropertyName("Package")]
    public string PackageName { get; set; } = "";
    public string Version { get; set; } = "";
    
    [MdfSection(Name = "Dependencies")]
    public List<FlatDependency>? Dependencies { get; set; }
}

// Strategy 4: Use subsections for list items (hypothetical - would require new feature)
[MdfSerializable(TitleProperty = nameof(Name))]
public class ProjectWithSubsectionGroups
{
    public string Name { get; set; } = "";
    
    // Hypothetical: Could render each DependencyGroup as H3 subsection
    [MdfSection(Name = "Dependencies", Level = 2)]
    public List<DependencyGroupAsSubsection>? DependencyGroups { get; set; }
}

[MdfSerializable]
public class DependencyGroupAsSubsection
{
    [MdfPropertyName("Target Framework")]
    public string TargetFramework { get; set; } = "";
    
    [MdfIgnore]  // Proposed: Could become table in subsection (Strategy 4)
    public List<Dependency>? Packages { get; set; }
}

#endregion

#region Test Context

[MdfContext(typeof(PackageWithSectionsPerGroup))]
[MdfContext(typeof(PackageWithFlatDependencies))]
[MdfContext(typeof(ProjectWithSubsectionGroups))]
public partial class StrategyTestContext : MdfSerializerContext
{
}

#endregion

/// <summary>
/// Tests exploring alternative rendering strategies for List&lt;T&gt; where T contains non-scalar properties.
/// This addresses the question: what should we do when List&lt;T&gt; items have nested lists or complex objects?
/// </summary>
public class ListRenderingStrategyTests
{
    private readonly ITestOutputHelper _output;

    public ListRenderingStrategyTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Problem_NonScalarInListTable_NowPreventedAtCompileTime()
    {
        // This test previously demonstrated the PROBLEM of ToString() in table cells
        // NOW: The source generator prevents this at compile-time with MDF001 error
        // The DependencyGroup.Packages property has [MdfIgnore] to satisfy the compiler
        
        var package = new PackageInspection
        {
            PackageName = "Newtonsoft.Json",
            Version = "13.0.3",
            Dependencies = new List<DependencyGroup>
            {
                new DependencyGroup
                {
                    TargetFramework = "net6.0",
                    Packages = new List<Dependency>  // This property is now [MdfIgnore]
                    {
                        new Dependency { Name = "System.Memory", Version = "4.5.5" },
                        new Dependency { Name = "System.Text.Json", Version = "6.0.0" }
                    }
                },
                new DependencyGroup
                {
                    TargetFramework = "net8.0",
                    Packages = new List<Dependency>
                    {
                        new Dependency { Name = "System.Memory", Version = "4.5.5" }
                    }
                }
            }
        };

        var mdf = MdfSerializer.Serialize(package, NestedTestContext.Default);
        
        _output.WriteLine("=== PROBLEM PREVENTED ===");
        _output.WriteLine(mdf);
        _output.WriteLine("");
        _output.WriteLine("✅ The 'Packages' column is no longer in the table");
        _output.WriteLine("   because it has [MdfIgnore] attribute.");
        _output.WriteLine("");
        _output.WriteLine("💡 Without [MdfIgnore], you would get:");
        _output.WriteLine("   error MDF001: Property 'Packages' in type 'DependencyGroup'");
        _output.WriteLine("   is an array of complex objects and cannot be rendered in a table cell.");
        _output.WriteLine("");
        _output.WriteLine("This prevents the useless ToString() output!");
        
        // Verify that Packages column is NOT present
        Assert.DoesNotContain("Packages", mdf);
        Assert.DoesNotContain("System.Collections.Generic.List", mdf);
        
        // Verify table still has the Target Framework column
        Assert.Contains("| Target Framework |", mdf);
        Assert.Contains("| net6.0 |", mdf);
        Assert.Contains("| net8.0 |", mdf);
    }

    [Fact]
    public void Strategy2_SectionPerItem_WorksButLimited()
    {
        // This works IF you know the target frameworks ahead of time
        var package = new PackageWithSectionsPerGroup
        {
            PackageName = "Newtonsoft.Json",
            Version = "13.0.3",
            Net6Dependencies = new List<Dependency>
            {
                new Dependency { Name = "System.Memory", Version = "4.5.5" },
                new Dependency { Name = "System.Text.Json", Version = "6.0.0" }
            },
            Net8Dependencies = new List<Dependency>
            {
                new Dependency { Name = "System.Memory", Version = "4.5.5" }
            }
        };

        var mdf = MdfSerializer.Serialize(package, StrategyTestContext.Default);
        
        _output.WriteLine("=== STRATEGY 2: Separate Section Per Item ===");
        _output.WriteLine(mdf);
        _output.WriteLine("");
        _output.WriteLine("✅ Pros:");
        _output.WriteLine("  - All data preserved");
        _output.WriteLine("  - Each framework gets its own section with proper table");
        _output.WriteLine("  - Works with current implementation");
        _output.WriteLine("");
        _output.WriteLine("❌ Cons:");
        _output.WriteLine("  - Only works if you know items ahead of time (net6.0, net8.0, etc.)");
        _output.WriteLine("  - Can't handle dynamic list (what if there are 20 frameworks?)");
        _output.WriteLine("  - Verbose model definition");
        _output.WriteLine("");

        Assert.Contains("## Dependencies (net6.0)", mdf);
        Assert.Contains("## Dependencies (net8.0)", mdf);
        Assert.Contains("| System.Memory |", mdf);
    }

    [Fact]
    public void Strategy3_Flatten_LosesGroupingSemantics()
    {
        var package = new PackageWithFlatDependencies
        {
            PackageName = "Newtonsoft.Json",
            Version = "13.0.3",
            Dependencies = new List<FlatDependency>
            {
                new FlatDependency { TargetFramework = "net6.0", PackageName = "System.Memory", PackageVersion = "4.5.5" },
                new FlatDependency { TargetFramework = "net6.0", PackageName = "System.Text.Json", PackageVersion = "6.0.0" },
                new FlatDependency { TargetFramework = "net8.0", PackageName = "System.Memory", PackageVersion = "4.5.5" }
            }
        };

        var mdf = MdfSerializer.Serialize(package, StrategyTestContext.Default);
        
        _output.WriteLine("=== STRATEGY 3: Flatten Structure ===");
        _output.WriteLine(mdf);
        _output.WriteLine("");
        _output.WriteLine("✅ Pros:");
        _output.WriteLine("  - All data in single table");
        _output.WriteLine("  - Works with current implementation");
        _output.WriteLine("  - Good for sorting/filtering across groups");
        _output.WriteLine("");
        _output.WriteLine("❌ Cons:");
        _output.WriteLine("  - Loses semantic grouping (groups become repeated column)");
        _output.WriteLine("  - Repetitive data (TargetFramework appears multiple times)");
        _output.WriteLine("  - User must manually flatten before serializing");
        _output.WriteLine("");

        Assert.Contains("| Target Framework | Package Name | Package Version |", mdf);
        Assert.Contains("| net6.0 | System.Memory |", mdf);
        Assert.Contains("| net8.0 | System.Memory |", mdf);
    }

    [Fact]
    public void Strategy4_SubsectionsForListItems_Proposed()
    {
        // This shows what COULD work if implemented
        var project = new ProjectWithSubsectionGroups
        {
            Name = "MyLibrary",
            DependencyGroups = new List<DependencyGroupAsSubsection>
            {
                new DependencyGroupAsSubsection
                {
                    TargetFramework = "net6.0",
                    Packages = new List<Dependency>
                    {
                        new Dependency { Name = "System.Memory", Version = "4.5.5" },
                        new Dependency { Name = "System.Text.Json", Version = "6.0.0" }
                    }
                },
                new DependencyGroupAsSubsection
                {
                    TargetFramework = "net8.0",
                    Packages = new List<Dependency>
                    {
                        new Dependency { Name = "System.Memory", Version = "4.5.5" }
                    }
                }
            }
        };

        var mdf = MdfSerializer.Serialize(project, StrategyTestContext.Default);
        
        _output.WriteLine("=== STRATEGY 4: Subsections for List Items (PROPOSED) ===");
        _output.WriteLine("");
        _output.WriteLine("CURRENT BEHAVIOR:");
        _output.WriteLine(mdf);
        _output.WriteLine("");
        _output.WriteLine("DESIRED BEHAVIOR:");
        _output.WriteLine("# MyLibrary");
        _output.WriteLine("");
        _output.WriteLine("## Dependencies");
        _output.WriteLine("");
        _output.WriteLine("### net6.0");
        _output.WriteLine("");
        _output.WriteLine("| Name | Version |");
        _output.WriteLine("|------|---------|");
        _output.WriteLine("| System.Memory | 4.5.5 |");
        _output.WriteLine("| System.Text.Json | 6.0.0 |");
        _output.WriteLine("");
        _output.WriteLine("### net8.0");
        _output.WriteLine("");
        _output.WriteLine("| Name | Version |");
        _output.WriteLine("|------|---------|");
        _output.WriteLine("| System.Memory | 4.5.5 |");
        _output.WriteLine("");
        _output.WriteLine("✅ Pros:");
        _output.WriteLine("  - Preserves ALL data");
        _output.WriteLine("  - Preserves grouping semantics");
        _output.WriteLine("  - Scales to any number of groups");
        _output.WriteLine("  - Readable, follows markdown hierarchy");
        _output.WriteLine("  - Handles dynamic lists");
        _output.WriteLine("");
        _output.WriteLine("❌ Cons:");
        _output.WriteLine("  - Requires implementation change");
        _output.WriteLine("  - Uses more heading levels (limits max depth)");
        _output.WriteLine("  - More verbose than a single table");
        _output.WriteLine("");
        _output.WriteLine("IMPLEMENTATION:");
        _output.WriteLine("  1. Detect when List<T> where T has non-scalar properties");
        _output.WriteLine("  2. Instead of rendering as table, render each T as H(n+1) subsection");
        _output.WriteLine("  3. Use a property value (like TargetFramework) as subsection heading");
        _output.WriteLine("  4. Render T's nested lists as tables within that subsection");
        _output.WriteLine("");
    }

    [Fact]
    public void DetectionLogic_WhenToUseSubsections()
    {
        _output.WriteLine("=== DETECTION LOGIC FOR CHOOSING RENDERING STRATEGY ===");
        _output.WriteLine("");
        _output.WriteLine("When serializing List<T>, check T's properties:");
        _output.WriteLine("");
        _output.WriteLine("✅ ALL properties are scalar → Use TABLE (current behavior)");
        _output.WriteLine("   Scalars: string, int, bool, DateTime, enum, etc.");
        _output.WriteLine("   Example: List<Member> { Name, Role, Active }");
        _output.WriteLine("");
        _output.WriteLine("⚠️ T has List<U> property → Use SUBSECTIONS (Strategy 4)");
        _output.WriteLine("   Example: List<DependencyGroup> { TargetFramework, List<Dependency> }");
        _output.WriteLine("   Render: Each DependencyGroup as H3, its Packages as table");
        _output.WriteLine("");
        _output.WriteLine("⚠️ T has complex object property → Use SUBSECTIONS");
        _output.WriteLine("   Example: List<Project> { Name, TeamInfo { Lead, List<Member> } }");
        _output.WriteLine("   Render: Each Project as H3, nested content as normal");
        _output.WriteLine("");
        _output.WriteLine("HEADING CONTEXT:");
        _output.WriteLine("  - Property marked [MdfSection(Level=2)] → list items use H3");
        _output.WriteLine("  - Property not in section → list items use H2");
        _output.WriteLine("  - Need to track current heading level to avoid H7+");
        _output.WriteLine("");
        _output.WriteLine("TITLE/NAME FOR SUBSECTION:");
        _output.WriteLine("  - Check for [TitleProperty] on T");
        _output.WriteLine("  - Or use first string property");
        _output.WriteLine("  - Or use index if no suitable property");
        _output.WriteLine("");
    }
}
