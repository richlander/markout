using Markout;
using Xunit;
using Xunit.Abstractions;

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
    private readonly ITestOutputHelper _output;

    public PivotTableTests(ITestOutputHelper output)
    {
        _output = output;
    }

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
        
        _output.WriteLine("=== ORIGINAL NESTED STRUCTURE ===");
        _output.WriteLine(mdf);
        _output.WriteLine("");
        _output.WriteLine("PROBLEM: The nested Packages lists show as ToString()");
        _output.WriteLine("");
        _output.WriteLine("This data SHOULD be a pivot table!");
        _output.WriteLine("");
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
        
        _output.WriteLine("=== PIVOT STRATEGY 1: Frameworks as Columns (Version Matrix) ===");
        _output.WriteLine(mdf);
        _output.WriteLine("");
        _output.WriteLine("Expected table:");
        _output.WriteLine("| Package          | net6.0 | net8.0 | netstandard2.0 |");
        _output.WriteLine("|------------------|--------|--------|----------------|");
        _output.WriteLine("| System.Memory    | 4.5.5  | 4.5.5  | 4.5.5          |");
        _output.WriteLine("| System.Text.Json | 6.0.0  |        | 6.0.0          |");
        _output.WriteLine("| System.Runtime   |        |        | 4.3.1          |");
        _output.WriteLine("| Microsoft.CSharp | 4.7.0  | 4.7.0  | 4.7.0          |");
        _output.WriteLine("");
        _output.WriteLine("✅ Pros:");
        _output.WriteLine("  - Single table, easy to scan");
        _output.WriteLine("  - Shows version differences across frameworks");
        _output.WriteLine("  - Natural Excel/pivot table representation");
        _output.WriteLine("  - Works with current Markout implementation!");
        _output.WriteLine("");
        _output.WriteLine("❌ Cons:");
        _output.WriteLine("  - Requires knowing frameworks at design time");
        _output.WriteLine("  - User must manually pivot before serializing");
        _output.WriteLine("  - Many columns if many frameworks");
        _output.WriteLine("  - Null cells for unused dependencies");
        _output.WriteLine("");

        Assert.Contains("| Package |", mdf);
        Assert.Contains("| net6.0 |", mdf);
        Assert.Contains("| System.Memory |", mdf);
        Assert.Contains("| 4.5.5 |", mdf);
    }

    [Fact]
    public void PivotExample_ShowsManualTransformation()
    {
        _output.WriteLine("=== MANUAL PIVOT TRANSFORMATION ===");
        _output.WriteLine("");
        _output.WriteLine("FROM (nested):");
        _output.WriteLine("  DependencyGroups: [");
        _output.WriteLine("    { TargetFramework: \"net6.0\", Packages: [");
        _output.WriteLine("        { Name: \"System.Memory\", Version: \"4.5.5\" },");
        _output.WriteLine("        { Name: \"System.Text.Json\", Version: \"6.0.0\" }");
        _output.WriteLine("    ]},");
        _output.WriteLine("    { TargetFramework: \"net8.0\", Packages: [");
        _output.WriteLine("        { Name: \"System.Memory\", Version: \"4.5.5\" }");
        _output.WriteLine("    ]}");
        _output.WriteLine("  ]");
        _output.WriteLine("");
        _output.WriteLine("TO (pivoted):");
        _output.WriteLine("  Dependencies: [");
        _output.WriteLine("    { Package: \"System.Memory\", net6.0: \"4.5.5\", net8.0: \"4.5.5\" },");
        _output.WriteLine("    { Package: \"System.Text.Json\", net6.0: \"6.0.0\", net8.0: null }");
        _output.WriteLine("  ]");
        _output.WriteLine("");
        _output.WriteLine("C# Code:");
        _output.WriteLine("  var pivoted = groups");
        _output.WriteLine("      .SelectMany(g => g.Packages.Select(p => new { g.TargetFramework, p.Name, p.Version }))");
        _output.WriteLine("      .GroupBy(x => x.Name)");
        _output.WriteLine("      .Select(g => new DependencyVersionMatrix {");
        _output.WriteLine("          PackageName = g.Key,");
        _output.WriteLine("          Net6Version = g.FirstOrDefault(x => x.TargetFramework == \"net6.0\")?.Version,");
        _output.WriteLine("          Net8Version = g.FirstOrDefault(x => x.TargetFramework == \"net8.0\")?.Version");
        _output.WriteLine("      })");
        _output.WriteLine("      .ToList();");
        _output.WriteLine("");
    }

    [Fact]
    public void PivotStrategy2_PresenceMatrix_Simpler()
    {
        _output.WriteLine("=== PIVOT STRATEGY 2: Presence Matrix (Simpler) ===");
        _output.WriteLine("");
        _output.WriteLine("If you don't care about versions, just show presence:");
        _output.WriteLine("");
        _output.WriteLine("| Package          | net6.0 | net8.0 | netstandard2.0 |");
        _output.WriteLine("|------------------|--------|--------|----------------|");
        _output.WriteLine("| System.Memory    | yes    | yes    | yes            |");
        _output.WriteLine("| System.Text.Json | yes    | no     | yes            |");
        _output.WriteLine("| System.Runtime   | no     | no     | yes            |");
        _output.WriteLine("| Microsoft.CSharp | yes    | yes    | yes            |");
        _output.WriteLine("");
        _output.WriteLine("Model:");
        _output.WriteLine("  class DependencyPresenceMatrix {");
        _output.WriteLine("      string Package;");
        _output.WriteLine("      bool Net6;");
        _output.WriteLine("      bool Net8;");
        _output.WriteLine("      bool NetStandard2;");
        _output.WriteLine("  }");
        _output.WriteLine("");
        _output.WriteLine("✅ Pros:");
        _output.WriteLine("  - Even simpler than version matrix");
        _output.WriteLine("  - Good for 'at a glance' compatibility check");
        _output.WriteLine("  - Boolean columns render nicely as yes/no");
        _output.WriteLine("");
        _output.WriteLine("❌ Cons:");
        _output.WriteLine("  - Loses version information");
        _output.WriteLine("  - Still requires knowing frameworks at design time");
        _output.WriteLine("");
    }

    [Fact]
    public void DynamicPivot_RequiresHelperMethod()
    {
        _output.WriteLine("=== DYNAMIC PIVOT (Frameworks Unknown at Design Time) ===");
        _output.WriteLine("");
        _output.WriteLine("PROBLEM: What if frameworks are dynamic?");
        _output.WriteLine("  - NuGet package might target any frameworks");
        _output.WriteLine("  - Can't hardcode Net6Version, Net8Version properties");
        _output.WriteLine("");
        _output.WriteLine("SOLUTION OPTIONS:");
        _output.WriteLine("");
        _output.WriteLine("1. Use Dictionary<string, string> (not supported in Markout tables)");
        _output.WriteLine("");
        _output.WriteLine("2. Helper method to generate flat class:");
        _output.WriteLine("   DependencyVersionMatrix CreateMatrix(List<string> frameworks) {");
        _output.WriteLine("       // Generate properties dynamically");
        _output.WriteLine("   }");
        _output.WriteLine("");
        _output.WriteLine("3. Fall back to non-table format for dynamic case:");
        _output.WriteLine("   System.Memory:");
        _output.WriteLine("     net6.0: 4.5.5");
        _output.WriteLine("     net8.0: 4.5.5");
        _output.WriteLine("     netstandard2.0: 4.5.5");
        _output.WriteLine("");
        _output.WriteLine("4. Transpose: Frameworks as rows, packages as columns");
        _output.WriteLine("   | Framework      | System.Memory | System.Text.Json | System.Runtime |");
        _output.WriteLine("   |----------------|---------------|------------------|----------------|");
        _output.WriteLine("   | net6.0         | 4.5.5         | 6.0.0            | -              |");
        _output.WriteLine("   | net8.0         | 4.5.5         | -                | -              |");
        _output.WriteLine("   (Only works if few packages)");
        _output.WriteLine("");
    }

    [Fact]
    public void LibrarySupport_PivotHelper()
    {
        _output.WriteLine("=== COULD THE LIBRARY HELP WITH PIVOTING? ===");
        _output.WriteLine("");
        _output.WriteLine("Option 1: Detection + Warning");
        _output.WriteLine("  - Source generator detects List<Group> where Group has List<Item>");
        _output.WriteLine("  - Emits compile warning: \"Consider pivoting this data\"");
        _output.WriteLine("  - Provides documentation link");
        _output.WriteLine("");
        _output.WriteLine("Option 2: Pivot Attribute");
        _output.WriteLine("  [MdfPivot(GroupKey = nameof(TargetFramework), ItemKey = nameof(Name), Value = nameof(Version))]");
        _output.WriteLine("  public List<DependencyGroup>? DependencyGroups { get; set; }");
        _output.WriteLine("  ");
        _output.WriteLine("  Source generator creates pivot logic automatically");
        _output.WriteLine("");
        _output.WriteLine("Option 3: Runtime Pivot API");
        _output.WriteLine("  var pivoted = MdfPivot.Create(");
        _output.WriteLine("      groups,");
        _output.WriteLine("      g => g.TargetFramework,  // Column key");
        _output.WriteLine("      g => g.Packages,          // Items");
        _output.WriteLine("      p => p.Name,              // Row key");
        _output.WriteLine("      p => p.Version            // Cell value");
        _output.WriteLine("  );");
        _output.WriteLine("");
        _output.WriteLine("Option 4: Extension Method");
        _output.WriteLine("  var matrix = dependencyGroups.PivotForMdf(");
        _output.WriteLine("      g => g.TargetFramework,");
        _output.WriteLine("      g => g.Packages,");
        _output.WriteLine("      p => p.Name,");
        _output.WriteLine("      p => p.Version");
        _output.WriteLine("  );");
        _output.WriteLine("  // Returns List<dynamic> with dynamic columns");
        _output.WriteLine("");
    }

    [Fact]
    public void RealWorld_DotnetInspectorSolution()
    {
        _output.WriteLine("=== REAL SOLUTION FOR DOTNET-INSPECTOR ===");
        _output.WriteLine("");
        _output.WriteLine("Current structure:");
        _output.WriteLine("  class InspectionResult {");
        _output.WriteLine("      List<DependencyGroup> DependencyGroups;");
        _output.WriteLine("  }");
        _output.WriteLine("  class DependencyGroup {");
        _output.WriteLine("      string TargetFramework;");
        _output.WriteLine("      List<PackageDependency> Dependencies;");
        _output.WriteLine("  }");
        _output.WriteLine("");
        _output.WriteLine("RECOMMENDATION: Add helper method to create pivot view");
        _output.WriteLine("");
        _output.WriteLine("  public class InspectionResult {");
        _output.WriteLine("      // Original structure (kept for programmatic access)");
        _output.WriteLine("      [MarkoutIgnore]");
        _output.WriteLine("      public List<DependencyGroup> DependencyGroups;");
        _output.WriteLine("");
        _output.WriteLine("      // Pivoted view for Markout serialization");
        _output.WriteLine("      [MarkoutSection(Name = \"Dependencies\")]");
        _output.WriteLine("      public List<DependencyVersionMatrix>? DependencyMatrix");
        _output.WriteLine("      {");
        _output.WriteLine("          get => PivotDependencies(DependencyGroups);");
        _output.WriteLine("          set { } // Not used for deserialization");
        _output.WriteLine("      }");
        _output.WriteLine("  }");
        _output.WriteLine("");
        _output.WriteLine("This gives you:");
        _output.WriteLine("  ✅ Original nested structure for code");
        _output.WriteLine("  ✅ Pivoted table for Markout output");
        _output.WriteLine("  ✅ Clear, readable output");
        _output.WriteLine("");
    }
}
