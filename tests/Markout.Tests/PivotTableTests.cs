using Markout;
using Xunit;

namespace Markout.Tests;

/// <summary>
/// Tests exploring pivot table strategies for List&lt;Group&gt; where Group has List&lt;Item&gt;.
/// This is the "Excel pivot table problem" - how to represent grouped data in a 2D table.
/// </summary>

#region Pivot Models

// Original nested structure (the problem)
[MarkoutSerializable(TitleProperty = nameof(PackageName))]
public class PackageWithNestedDependencies
{
    [MarkoutPropertyName("Package")]
    public string PackageName { get; set; } = "";
    public string Version { get; set; } = "";
    
    // This creates the problem: table can't contain lists
    [MarkoutSection(Name = "Dependencies by Framework")]
    public List<DependencyGroup>? DependencyGroups { get; set; }
}

// Pivot Strategy 1: Frameworks as Columns (Version Matrix)
[MarkoutSerializable(TitleProperty = nameof(PackageName))]
public class PackageWithPivotedDependencies
{
    [MarkoutPropertyName("Package")]
    public string PackageName { get; set; } = "";
    public string Version { get; set; } = "";
    
    // Pivoted: Each row is a dependency, each column is a framework
    [MarkoutSection(Name = "Dependencies")]
    public List<DependencyVersionMatrix>? Dependencies { get; set; }
}

[MarkoutSerializable]
public class DependencyVersionMatrix
{
    [MarkoutPropertyName("Package")]
    public string PackageName { get; set; } = "";
    
    [MarkoutPropertyName("net6.0")]
    public string? Net6Version { get; set; }
    
    [MarkoutPropertyName("net8.0")]
    public string? Net8Version { get; set; }
    
    [MarkoutPropertyName("netstandard2.0")]
    public string? NetStandard2Version { get; set; }
}

// Pivot Strategy 2: Frameworks as Columns (Presence/Absence)
[MarkoutSerializable]
public class DependencyPresenceMatrix
{
    [MarkoutPropertyName("Package")]
    public string PackageName { get; set; } = "";
    
    [MarkoutPropertyName("net6.0")]
    public bool Net6 { get; set; }
    
    [MarkoutPropertyName("net8.0")]
    public bool Net8 { get; set; }
    
    [MarkoutPropertyName("netstandard2.0")]
    public bool NetStandard2 { get; set; }
}

// Pivot Strategy 3: Dynamic columns (for unknown frameworks at design time)
// This would require dynamic property generation - not currently supported
// but could be achieved with a helper method that generates the flat structure

#endregion

#region Pivot Context

[MarkoutContext(typeof(PackageWithNestedDependencies))]
[MarkoutContext(typeof(PackageWithPivotedDependencies))]
public partial class PivotTestContext : MarkoutSerializerContext
{
}

#endregion

public class PivotTableTests
{
    [Fact]
    public void OriginalStructure_ShowsProblem()
    {
        var package = new PackageWithNestedDependencies
        {
            PackageName = "Newtonsoft.Json",
            Version = "13.0.3",
            DependencyGroups = new List<DependencyGroup>
            {
                new DependencyGroup
                {
                    TargetFramework = "net6.0",
                    Packages = new List<Dependency>
                    {
                        new Dependency { Name = "System.Memory", Version = "4.5.5" },
                        new Dependency { Name = "System.Text.Json", Version = "6.0.0" },
                        new Dependency { Name = "Microsoft.CSharp", Version = "4.7.0" }
                    }
                },
                new DependencyGroup
                {
                    TargetFramework = "net8.0",
                    Packages = new List<Dependency>
                    {
                        new Dependency { Name = "System.Memory", Version = "4.5.5" },
                        new Dependency { Name = "Microsoft.CSharp", Version = "4.7.0" }
                    }
                },
                new DependencyGroup
                {
                    TargetFramework = "netstandard2.0",
                    Packages = new List<Dependency>
                    {
                        new Dependency { Name = "System.Memory", Version = "4.5.5" },
                        new Dependency { Name = "System.Text.Json", Version = "6.0.0" },
                        new Dependency { Name = "System.Runtime", Version = "4.3.1" },
                        new Dependency { Name = "Microsoft.CSharp", Version = "4.7.0" }
                    }
                }
            }
        };

        var mdf = MarkoutSerializer.Serialize(package, PivotTestContext.Default);
        
        TestContext.Current.TestOutputHelper!.WriteLine("=== ORIGINAL NESTED STRUCTURE ===");
        TestContext.Current.TestOutputHelper!.WriteLine(mdf);
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("PROBLEM: The nested Packages lists show as ToString()");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("This data SHOULD be a pivot table!");
        TestContext.Current.TestOutputHelper!.WriteLine("");
    }

    [Fact]
    public void PivotStrategy1_FrameworksAsColumns_VersionMatrix()
    {
        // User manually pivots the data before serializing
        var package = new PackageWithPivotedDependencies
        {
            PackageName = "Newtonsoft.Json",
            Version = "13.0.3",
            Dependencies = new List<DependencyVersionMatrix>
            {
                new DependencyVersionMatrix
                {
                    PackageName = "System.Memory",
                    Net6Version = "4.5.5",
                    Net8Version = "4.5.5",
                    NetStandard2Version = "4.5.5"
                },
                new DependencyVersionMatrix
                {
                    PackageName = "System.Text.Json",
                    Net6Version = "6.0.0",
                    Net8Version = null,  // Not used in net8.0
                    NetStandard2Version = "6.0.0"
                },
                new DependencyVersionMatrix
                {
                    PackageName = "System.Runtime",
                    Net6Version = null,
                    Net8Version = null,
                    NetStandard2Version = "4.3.1"
                },
                new DependencyVersionMatrix
                {
                    PackageName = "Microsoft.CSharp",
                    Net6Version = "4.7.0",
                    Net8Version = "4.7.0",
                    NetStandard2Version = "4.7.0"
                }
            }
        };

        var mdf = MarkoutSerializer.Serialize(package, PivotTestContext.Default);
        
        TestContext.Current.TestOutputHelper!.WriteLine("=== PIVOT STRATEGY 1: Frameworks as Columns (Version Matrix) ===");
        TestContext.Current.TestOutputHelper!.WriteLine(mdf);
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("Expected table:");
        TestContext.Current.TestOutputHelper!.WriteLine("| Package          | net6.0 | net8.0 | netstandard2.0 |");
        TestContext.Current.TestOutputHelper!.WriteLine("|------------------|--------|--------|----------------|");
        TestContext.Current.TestOutputHelper!.WriteLine("| System.Memory    | 4.5.5  | 4.5.5  | 4.5.5          |");
        TestContext.Current.TestOutputHelper!.WriteLine("| System.Text.Json | 6.0.0  |        | 6.0.0          |");
        TestContext.Current.TestOutputHelper!.WriteLine("| System.Runtime   |        |        | 4.3.1          |");
        TestContext.Current.TestOutputHelper!.WriteLine("| Microsoft.CSharp | 4.7.0  | 4.7.0  | 4.7.0          |");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("✅ Pros:");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Single table, easy to scan");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Shows version differences across frameworks");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Natural Excel/pivot table representation");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Works with current Markout implementation!");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("❌ Cons:");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Requires knowing frameworks at design time");
        TestContext.Current.TestOutputHelper!.WriteLine("  - User must manually pivot before serializing");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Many columns if many frameworks");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Null cells for unused dependencies");
        TestContext.Current.TestOutputHelper!.WriteLine("");

        Assert.Contains("| Package |", mdf);
        Assert.Contains("| net6.0 |", mdf);
        Assert.Contains("| System.Memory |", mdf);
        Assert.Contains("| 4.5.5 |", mdf);
    }

    [Fact]
    public void PivotExample_ShowsManualTransformation()
    {
        TestContext.Current.TestOutputHelper!.WriteLine("=== MANUAL PIVOT TRANSFORMATION ===");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("FROM (nested):");
        TestContext.Current.TestOutputHelper!.WriteLine("  DependencyGroups: [");
        TestContext.Current.TestOutputHelper!.WriteLine("    { TargetFramework: \"net6.0\", Packages: [");
        TestContext.Current.TestOutputHelper!.WriteLine("        { Name: \"System.Memory\", Version: \"4.5.5\" },");
        TestContext.Current.TestOutputHelper!.WriteLine("        { Name: \"System.Text.Json\", Version: \"6.0.0\" }");
        TestContext.Current.TestOutputHelper!.WriteLine("    ]},");
        TestContext.Current.TestOutputHelper!.WriteLine("    { TargetFramework: \"net8.0\", Packages: [");
        TestContext.Current.TestOutputHelper!.WriteLine("        { Name: \"System.Memory\", Version: \"4.5.5\" }");
        TestContext.Current.TestOutputHelper!.WriteLine("    ]}");
        TestContext.Current.TestOutputHelper!.WriteLine("  ]");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("TO (pivoted):");
        TestContext.Current.TestOutputHelper!.WriteLine("  Dependencies: [");
        TestContext.Current.TestOutputHelper!.WriteLine("    { Package: \"System.Memory\", net6.0: \"4.5.5\", net8.0: \"4.5.5\" },");
        TestContext.Current.TestOutputHelper!.WriteLine("    { Package: \"System.Text.Json\", net6.0: \"6.0.0\", net8.0: null }");
        TestContext.Current.TestOutputHelper!.WriteLine("  ]");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("C# Code:");
        TestContext.Current.TestOutputHelper!.WriteLine("  var pivoted = groups");
        TestContext.Current.TestOutputHelper!.WriteLine("      .SelectMany(g => g.Packages.Select(p => new { g.TargetFramework, p.Name, p.Version }))");
        TestContext.Current.TestOutputHelper!.WriteLine("      .GroupBy(x => x.Name)");
        TestContext.Current.TestOutputHelper!.WriteLine("      .Select(g => new DependencyVersionMatrix {");
        TestContext.Current.TestOutputHelper!.WriteLine("          PackageName = g.Key,");
        TestContext.Current.TestOutputHelper!.WriteLine("          Net6Version = g.FirstOrDefault(x => x.TargetFramework == \"net6.0\")?.Version,");
        TestContext.Current.TestOutputHelper!.WriteLine("          Net8Version = g.FirstOrDefault(x => x.TargetFramework == \"net8.0\")?.Version");
        TestContext.Current.TestOutputHelper!.WriteLine("      })");
        TestContext.Current.TestOutputHelper!.WriteLine("      .ToList();");
        TestContext.Current.TestOutputHelper!.WriteLine("");
    }

    [Fact]
    public void PivotStrategy2_PresenceMatrix_Simpler()
    {
        TestContext.Current.TestOutputHelper!.WriteLine("=== PIVOT STRATEGY 2: Presence Matrix (Simpler) ===");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("If you don't care about versions, just show presence:");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("| Package          | net6.0 | net8.0 | netstandard2.0 |");
        TestContext.Current.TestOutputHelper!.WriteLine("|------------------|--------|--------|----------------|");
        TestContext.Current.TestOutputHelper!.WriteLine("| System.Memory    | yes    | yes    | yes            |");
        TestContext.Current.TestOutputHelper!.WriteLine("| System.Text.Json | yes    | no     | yes            |");
        TestContext.Current.TestOutputHelper!.WriteLine("| System.Runtime   | no     | no     | yes            |");
        TestContext.Current.TestOutputHelper!.WriteLine("| Microsoft.CSharp | yes    | yes    | yes            |");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("Model:");
        TestContext.Current.TestOutputHelper!.WriteLine("  class DependencyPresenceMatrix {");
        TestContext.Current.TestOutputHelper!.WriteLine("      string Package;");
        TestContext.Current.TestOutputHelper!.WriteLine("      bool Net6;");
        TestContext.Current.TestOutputHelper!.WriteLine("      bool Net8;");
        TestContext.Current.TestOutputHelper!.WriteLine("      bool NetStandard2;");
        TestContext.Current.TestOutputHelper!.WriteLine("  }");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("✅ Pros:");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Even simpler than version matrix");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Good for 'at a glance' compatibility check");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Boolean columns render nicely as yes/no");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("❌ Cons:");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Loses version information");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Still requires knowing frameworks at design time");
        TestContext.Current.TestOutputHelper!.WriteLine("");
    }

    [Fact]
    public void DynamicPivot_RequiresHelperMethod()
    {
        TestContext.Current.TestOutputHelper!.WriteLine("=== DYNAMIC PIVOT (Frameworks Unknown at Design Time) ===");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("PROBLEM: What if frameworks are dynamic?");
        TestContext.Current.TestOutputHelper!.WriteLine("  - NuGet package might target any frameworks");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Can't hardcode Net6Version, Net8Version properties");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("SOLUTION OPTIONS:");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("1. Use Dictionary<string, string> (not supported in Markout tables)");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("2. Helper method to generate flat class:");
        TestContext.Current.TestOutputHelper!.WriteLine("   DependencyVersionMatrix CreateMatrix(List<string> frameworks) {");
        TestContext.Current.TestOutputHelper!.WriteLine("       // Generate properties dynamically");
        TestContext.Current.TestOutputHelper!.WriteLine("   }");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("3. Fall back to non-table format for dynamic case:");
        TestContext.Current.TestOutputHelper!.WriteLine("   System.Memory:");
        TestContext.Current.TestOutputHelper!.WriteLine("     net6.0: 4.5.5");
        TestContext.Current.TestOutputHelper!.WriteLine("     net8.0: 4.5.5");
        TestContext.Current.TestOutputHelper!.WriteLine("     netstandard2.0: 4.5.5");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("4. Transpose: Frameworks as rows, packages as columns");
        TestContext.Current.TestOutputHelper!.WriteLine("   | Framework      | System.Memory | System.Text.Json | System.Runtime |");
        TestContext.Current.TestOutputHelper!.WriteLine("   |----------------|---------------|------------------|----------------|");
        TestContext.Current.TestOutputHelper!.WriteLine("   | net6.0         | 4.5.5         | 6.0.0            | -              |");
        TestContext.Current.TestOutputHelper!.WriteLine("   | net8.0         | 4.5.5         | -                | -              |");
        TestContext.Current.TestOutputHelper!.WriteLine("   (Only works if few packages)");
        TestContext.Current.TestOutputHelper!.WriteLine("");
    }

    [Fact]
    public void LibrarySupport_PivotHelper()
    {
        TestContext.Current.TestOutputHelper!.WriteLine("=== COULD THE LIBRARY HELP WITH PIVOTING? ===");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("Option 1: Detection + Warning");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Source generator detects List<Group> where Group has List<Item>");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Emits compile warning: \"Consider pivoting this data\"");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Provides documentation link");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("Option 2: Pivot Attribute");
        TestContext.Current.TestOutputHelper!.WriteLine("  [MdfPivot(GroupKey = nameof(TargetFramework), ItemKey = nameof(Name), Value = nameof(Version))]");
        TestContext.Current.TestOutputHelper!.WriteLine("  public List<DependencyGroup>? DependencyGroups { get; set; }");
        TestContext.Current.TestOutputHelper!.WriteLine("  ");
        TestContext.Current.TestOutputHelper!.WriteLine("  Source generator creates pivot logic automatically");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("Option 3: Runtime Pivot API");
        TestContext.Current.TestOutputHelper!.WriteLine("  var pivoted = MdfPivot.Create(");
        TestContext.Current.TestOutputHelper!.WriteLine("      groups,");
        TestContext.Current.TestOutputHelper!.WriteLine("      g => g.TargetFramework,  // Column key");
        TestContext.Current.TestOutputHelper!.WriteLine("      g => g.Packages,          // Items");
        TestContext.Current.TestOutputHelper!.WriteLine("      p => p.Name,              // Row key");
        TestContext.Current.TestOutputHelper!.WriteLine("      p => p.Version            // Cell value");
        TestContext.Current.TestOutputHelper!.WriteLine("  );");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("Option 4: Extension Method");
        TestContext.Current.TestOutputHelper!.WriteLine("  var matrix = dependencyGroups.PivotForMdf(");
        TestContext.Current.TestOutputHelper!.WriteLine("      g => g.TargetFramework,");
        TestContext.Current.TestOutputHelper!.WriteLine("      g => g.Packages,");
        TestContext.Current.TestOutputHelper!.WriteLine("      p => p.Name,");
        TestContext.Current.TestOutputHelper!.WriteLine("      p => p.Version");
        TestContext.Current.TestOutputHelper!.WriteLine("  );");
        TestContext.Current.TestOutputHelper!.WriteLine("  // Returns List<dynamic> with dynamic columns");
        TestContext.Current.TestOutputHelper!.WriteLine("");
    }

    [Fact]
    public void RealWorld_DotnetInspectorSolution()
    {
        TestContext.Current.TestOutputHelper!.WriteLine("=== REAL SOLUTION FOR DOTNET-INSPECTOR ===");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("Current structure:");
        TestContext.Current.TestOutputHelper!.WriteLine("  class InspectionResult {");
        TestContext.Current.TestOutputHelper!.WriteLine("      List<DependencyGroup> DependencyGroups;");
        TestContext.Current.TestOutputHelper!.WriteLine("  }");
        TestContext.Current.TestOutputHelper!.WriteLine("  class DependencyGroup {");
        TestContext.Current.TestOutputHelper!.WriteLine("      string TargetFramework;");
        TestContext.Current.TestOutputHelper!.WriteLine("      List<PackageDependency> Dependencies;");
        TestContext.Current.TestOutputHelper!.WriteLine("  }");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("RECOMMENDATION: Add helper method to create pivot view");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("  public class InspectionResult {");
        TestContext.Current.TestOutputHelper!.WriteLine("      // Original structure (kept for programmatic access)");
        TestContext.Current.TestOutputHelper!.WriteLine("      [MarkoutIgnore]");
        TestContext.Current.TestOutputHelper!.WriteLine("      public List<DependencyGroup> DependencyGroups;");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("      // Pivoted view for Markout serialization");
        TestContext.Current.TestOutputHelper!.WriteLine("      [MarkoutSection(Name = \"Dependencies\")]");
        TestContext.Current.TestOutputHelper!.WriteLine("      public List<DependencyVersionMatrix>? DependencyMatrix");
        TestContext.Current.TestOutputHelper!.WriteLine("      {");
        TestContext.Current.TestOutputHelper!.WriteLine("          get => PivotDependencies(DependencyGroups);");
        TestContext.Current.TestOutputHelper!.WriteLine("          set { } // Not used for deserialization");
        TestContext.Current.TestOutputHelper!.WriteLine("      }");
        TestContext.Current.TestOutputHelper!.WriteLine("  }");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("This gives you:");
        TestContext.Current.TestOutputHelper!.WriteLine("  ✅ Original nested structure for code");
        TestContext.Current.TestOutputHelper!.WriteLine("  ✅ Pivoted table for Markout output");
        TestContext.Current.TestOutputHelper!.WriteLine("  ✅ Clear, readable output");
        TestContext.Current.TestOutputHelper!.WriteLine("");
    }
}
