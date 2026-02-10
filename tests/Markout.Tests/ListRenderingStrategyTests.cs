using Markout;

namespace Markout.Tests;

#region Test Models for Different Strategies

// Strategy 2: Section-per-item (works but verbose)
[MarkoutSerializable(TitleProperty = nameof(PackageName))]
public class PackageWithSectionsPerGroup
{
    [MarkoutPropertyName("Package")]
    public string PackageName { get; set; } = "";
    public string Version { get; set; } = "";
    
    [MarkoutSection(Name = "Dependencies (net6.0)")]
    public List<Dependency>? Net6Dependencies { get; set; }
    
    [MarkoutSection(Name = "Dependencies (net8.0)")]
    public List<Dependency>? Net8Dependencies { get; set; }
    
    [MarkoutSection(Name = "Dependencies (netstandard2.0)")]
    public List<Dependency>? NetStandard2Dependencies { get; set; }
}

// Strategy 3: Flatten the structure
[MarkoutSerializable]
public class FlatDependency
{
    [MarkoutPropertyName("Target Framework")]
    public string TargetFramework { get; set; } = "";
    
    [MarkoutPropertyName("Package Name")]
    public string PackageName { get; set; } = "";
    
    [MarkoutPropertyName("Package Version")]
    public string PackageVersion { get; set; } = "";
}

[MarkoutSerializable(TitleProperty = nameof(PackageName))]
public class PackageWithFlatDependencies
{
    [MarkoutPropertyName("Package")]
    public string PackageName { get; set; } = "";
    public string Version { get; set; } = "";
    
    [MarkoutSection(Name = "Dependencies")]
    public List<FlatDependency>? Dependencies { get; set; }
}

// Strategy 4: Use subsections for list items (hypothetical - would require new feature)
[MarkoutSerializable(TitleProperty = nameof(Name))]
public class ProjectWithSubsectionGroups
{
    public string Name { get; set; } = "";
    
    // Hypothetical: Could render each DependencyGroup as H3 subsection
    [MarkoutSection(Name = "Dependencies", Level = 2)]
    public List<DependencyGroupAsSubsection>? DependencyGroups { get; set; }
}

[MarkoutSerializable]
public class DependencyGroupAsSubsection
{
    [MarkoutPropertyName("Target Framework")]
    public string TargetFramework { get; set; } = "";
    
    [MarkoutIgnore]  // Proposed: Could become table in subsection (Strategy 4)
    public List<Dependency>? Packages { get; set; }
}

#endregion

#region Test Context

[MarkoutContext(typeof(PackageWithSectionsPerGroup))]
[MarkoutContext(typeof(PackageWithFlatDependencies))]
[MarkoutContext(typeof(ProjectWithSubsectionGroups))]
public partial class StrategyTestContext : MarkoutSerializerContext
{
}

#endregion

/// <summary>
/// Tests exploring alternative rendering strategies for List&lt;T&gt; where T contains non-scalar properties.
/// This addresses the question: what should we do when List&lt;T&gt; items have nested lists or complex objects?
/// </summary>
public class ListRenderingStrategyTests
{
    [Fact]
    public void Problem_NonScalarInListTable_NowPreventedAtCompileTime()
    {
        // This test previously demonstrated the PROBLEM of ToString() in table cells
        // NOW: The source generator prevents this at compile-time with MARKOUT001 error
        // The DependencyGroup.Packages property has [MarkoutIgnore] to satisfy the compiler
        
        var package = new PackageInspection
        {
            PackageName = "Newtonsoft.Json",
            Version = "13.0.3",
            Dependencies = new List<DependencyGroup>
            {
                new DependencyGroup
                {
                    TargetFramework = "net6.0",
                    Packages = new List<Dependency>  // This property is now [MarkoutIgnore]
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

        var mdf = MarkoutSerializer.Serialize(package, NestedTestContext.Default);
        
        TestContext.Current.TestOutputHelper!.WriteLine("=== PROBLEM PREVENTED ===");
        TestContext.Current.TestOutputHelper!.WriteLine(mdf);
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("✅ The 'Packages' column is no longer in the table");
        TestContext.Current.TestOutputHelper!.WriteLine("   because it has [MarkoutIgnore] attribute.");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("💡 Without [MarkoutIgnore], you would get:");
        TestContext.Current.TestOutputHelper!.WriteLine("   error MARKOUT001: Property 'Packages' in type 'DependencyGroup'");
        TestContext.Current.TestOutputHelper!.WriteLine("   is an array of complex objects and cannot be rendered in a table cell.");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("This prevents the useless ToString() output!");
        
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

        var mdf = MarkoutSerializer.Serialize(package, StrategyTestContext.Default);
        
        TestContext.Current.TestOutputHelper!.WriteLine("=== STRATEGY 2: Separate Section Per Item ===");
        TestContext.Current.TestOutputHelper!.WriteLine(mdf);
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("✅ Pros:");
        TestContext.Current.TestOutputHelper!.WriteLine("  - All data preserved");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Each framework gets its own section with proper table");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Works with current implementation");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("❌ Cons:");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Only works if you know items ahead of time (net6.0, net8.0, etc.)");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Can't handle dynamic list (what if there are 20 frameworks?)");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Verbose model definition");
        TestContext.Current.TestOutputHelper!.WriteLine("");

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

        var mdf = MarkoutSerializer.Serialize(package, StrategyTestContext.Default);
        
        TestContext.Current.TestOutputHelper!.WriteLine("=== STRATEGY 3: Flatten Structure ===");
        TestContext.Current.TestOutputHelper!.WriteLine(mdf);
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("✅ Pros:");
        TestContext.Current.TestOutputHelper!.WriteLine("  - All data in single table");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Works with current implementation");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Good for sorting/filtering across groups");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("❌ Cons:");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Loses semantic grouping (groups become repeated column)");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Repetitive data (TargetFramework appears multiple times)");
        TestContext.Current.TestOutputHelper!.WriteLine("  - User must manually flatten before serializing");
        TestContext.Current.TestOutputHelper!.WriteLine("");

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

        var mdf = MarkoutSerializer.Serialize(project, StrategyTestContext.Default);
        
        TestContext.Current.TestOutputHelper!.WriteLine("=== STRATEGY 4: Subsections for List Items (PROPOSED) ===");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("CURRENT BEHAVIOR:");
        TestContext.Current.TestOutputHelper!.WriteLine(mdf);
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("DESIRED BEHAVIOR:");
        TestContext.Current.TestOutputHelper!.WriteLine("# MyLibrary");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("## Dependencies");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("### net6.0");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("| Name | Version |");
        TestContext.Current.TestOutputHelper!.WriteLine("|------|---------|");
        TestContext.Current.TestOutputHelper!.WriteLine("| System.Memory | 4.5.5 |");
        TestContext.Current.TestOutputHelper!.WriteLine("| System.Text.Json | 6.0.0 |");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("### net8.0");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("| Name | Version |");
        TestContext.Current.TestOutputHelper!.WriteLine("|------|---------|");
        TestContext.Current.TestOutputHelper!.WriteLine("| System.Memory | 4.5.5 |");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("✅ Pros:");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Preserves ALL data");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Preserves grouping semantics");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Scales to any number of groups");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Readable, follows markdown hierarchy");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Handles dynamic lists");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("❌ Cons:");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Requires implementation change");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Uses more heading levels (limits max depth)");
        TestContext.Current.TestOutputHelper!.WriteLine("  - More verbose than a single table");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("IMPLEMENTATION:");
        TestContext.Current.TestOutputHelper!.WriteLine("  1. Detect when List<T> where T has non-scalar properties");
        TestContext.Current.TestOutputHelper!.WriteLine("  2. Instead of rendering as table, render each T as H(n+1) subsection");
        TestContext.Current.TestOutputHelper!.WriteLine("  3. Use a property value (like TargetFramework) as subsection heading");
        TestContext.Current.TestOutputHelper!.WriteLine("  4. Render T's nested lists as tables within that subsection");
        TestContext.Current.TestOutputHelper!.WriteLine("");
    }

    [Fact]
    public void DetectionLogic_WhenToUseSubsections()
    {
        TestContext.Current.TestOutputHelper!.WriteLine("=== DETECTION LOGIC FOR CHOOSING RENDERING STRATEGY ===");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("When serializing List<T>, check T's properties:");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("✅ ALL properties are scalar → Use TABLE (current behavior)");
        TestContext.Current.TestOutputHelper!.WriteLine("   Scalars: string, int, bool, DateTime, enum, etc.");
        TestContext.Current.TestOutputHelper!.WriteLine("   Example: List<Member> { Name, Role, Active }");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("⚠️ T has List<U> property → Use SUBSECTIONS (Strategy 4)");
        TestContext.Current.TestOutputHelper!.WriteLine("   Example: List<DependencyGroup> { TargetFramework, List<Dependency> }");
        TestContext.Current.TestOutputHelper!.WriteLine("   Render: Each DependencyGroup as H3, its Packages as table");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("⚠️ T has complex object property → Use SUBSECTIONS");
        TestContext.Current.TestOutputHelper!.WriteLine("   Example: List<Project> { Name, TeamInfo { Lead, List<Member> } }");
        TestContext.Current.TestOutputHelper!.WriteLine("   Render: Each Project as H3, nested content as normal");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("HEADING CONTEXT:");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Property marked [MarkoutSection(Level=2)] → list items use H3");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Property not in section → list items use H2");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Need to track current heading level to avoid H7+");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("TITLE/NAME FOR SUBSECTION:");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Check for [TitleProperty] on T");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Or use first string property");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Or use index if no suitable property");
        TestContext.Current.TestOutputHelper!.WriteLine("");
    }
}
