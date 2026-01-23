using MarkOut;
using Xunit;
using Xunit.Abstractions;

namespace MarkOut.Tests;

/// <summary>
/// Tests documenting patterns that SHOULD produce compile-time errors.
/// These patterns result in useless ToString() output and should be caught by the source generator.
/// </summary>
public class CompileTimeErrorTests
{
    private readonly ITestOutputHelper _output;

    public CompileTimeErrorTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Pattern1_ListPropertyInTable_ShouldError()
    {
        _output.WriteLine("=== PATTERN THAT SHOULD BE COMPILE ERROR #1 ===");
        _output.WriteLine("");
        _output.WriteLine("CODE:");
        _output.WriteLine("  [MarkOutSerializable]");
        _output.WriteLine("  class DependencyGroup {");
        _output.WriteLine("      string TargetFramework { get; set; }");
        _output.WriteLine("      List<Dependency> Packages { get; set; }  // ❌ ERROR");
        _output.WriteLine("  }");
        _output.WriteLine("");
        _output.WriteLine("  class Package {");
        _output.WriteLine("      List<DependencyGroup> Groups { get; set; }");
        _output.WriteLine("  }");
        _output.WriteLine("");
        _output.WriteLine("PROBLEM:");
        _output.WriteLine("  DependencyGroup will be rendered as a table row.");
        _output.WriteLine("  The Packages property would show as:");
        _output.WriteLine("    System.Collections.Generic.List`1[Dependency]");
        _output.WriteLine("  This is USELESS output!");
        _output.WriteLine("");
        _output.WriteLine("ERROR MESSAGE (proposed):");
        _output.WriteLine("  MARKOUT001: Property 'Packages' in 'DependencyGroup' is a List<T>.");
        _output.WriteLine("  List<T> properties cannot be rendered in table cells.");
        _output.WriteLine("  ");
        _output.WriteLine("  Choose one of these strategies:");
        _output.WriteLine("  1. [MarkOutIgnore] - Exclude this property from serialization");
        _output.WriteLine("  2. Provide aggregate data (e.g., 'PackageCount')");
        _output.WriteLine("  3. Transform to pivot table (see docs)");
        _output.WriteLine("  4. Use separate sections with [MarkOutSection] attribute");
        _output.WriteLine("  ");
        _output.WriteLine("  See: https://docs.mdf.dev/strategies/nested-lists");
        _output.WriteLine("");
        _output.WriteLine("DETECTION LOGIC:");
        _output.WriteLine("  - Type has [MarkOutSerializable] attribute");
        _output.WriteLine("  - Type is used in List<T>");
        _output.WriteLine("  - Type has property that is List<U> or IEnumerable<U>");
        _output.WriteLine("  - Property does NOT have [MarkOutIgnore]");
        _output.WriteLine("  - Property does NOT have [MarkOutSection]");
        _output.WriteLine("  → EMIT COMPILE ERROR");
        _output.WriteLine("");
    }

    [Fact]
    public void Pattern2_ComplexObjectPropertyInTable_ShouldError()
    {
        _output.WriteLine("=== PATTERN THAT SHOULD BE COMPILE ERROR #2 ===");
        _output.WriteLine("");
        _output.WriteLine("CODE:");
        _output.WriteLine("  [MarkOutSerializable]");
        _output.WriteLine("  class BuildResult {");
        _output.WriteLine("      string ProjectName { get; set; }");
        _output.WriteLine("      BuildMetadata Metadata { get; set; }  // ❌ ERROR");
        _output.WriteLine("  }");
        _output.WriteLine("");
        _output.WriteLine("  [MarkOutSerializable]");
        _output.WriteLine("  class BuildMetadata {");
        _output.WriteLine("      string Commit { get; set; }");
        _output.WriteLine("      DateTime BuildTime { get; set; }");
        _output.WriteLine("      List<string> Tags { get; set; }");
        _output.WriteLine("  }");
        _output.WriteLine("");
        _output.WriteLine("  class BuildSummary {");
        _output.WriteLine("      List<BuildResult> Builds { get; set; }");
        _output.WriteLine("  }");
        _output.WriteLine("");
        _output.WriteLine("PROBLEM:");
        _output.WriteLine("  BuildResult will be rendered as table row.");
        _output.WriteLine("  The Metadata property would show as:");
        _output.WriteLine("    BuildMetadata (ToString)");
        _output.WriteLine("  This is USELESS output!");
        _output.WriteLine("");
        _output.WriteLine("ERROR MESSAGE (proposed):");
        _output.WriteLine("  MARKOUT002: Property 'Metadata' in 'BuildResult' is a complex object.");
        _output.WriteLine("  Complex objects cannot be rendered in table cells.");
        _output.WriteLine("  ");
        _output.WriteLine("  Choose one of these strategies:");
        _output.WriteLine("  1. [MarkOutIgnore] - Exclude this property");
        _output.WriteLine("  2. Flatten - Move Metadata properties to BuildResult");
        _output.WriteLine("  3. Provide summary (e.g., 'CommitHash' as string)");
        _output.WriteLine("  ");
        _output.WriteLine("  See: https://docs.mdf.dev/strategies/complex-properties");
        _output.WriteLine("");
        _output.WriteLine("DETECTION LOGIC:");
        _output.WriteLine("  - Type T has [MarkOutSerializable]");
        _output.WriteLine("  - T is used in List<T>");
        _output.WriteLine("  - T has property P of type U");
        _output.WriteLine("  - U is a class/struct (not scalar)");
        _output.WriteLine("  - U has multiple public properties");
        _output.WriteLine("  - P does NOT have [MarkOutIgnore]");
        _output.WriteLine("  → EMIT COMPILE ERROR");
        _output.WriteLine("");
    }

    [Fact]
    public void Pattern3_DictionaryProperty_ShouldError()
    {
        _output.WriteLine("=== PATTERN THAT SHOULD BE COMPILE ERROR #3 ===");
        _output.WriteLine("");
        _output.WriteLine("CODE:");
        _output.WriteLine("  [MarkOutSerializable]");
        _output.WriteLine("  class Configuration {");
        _output.WriteLine("      string Name { get; set; }");
        _output.WriteLine("      Dictionary<string, string> Settings { get; set; }  // ❌ ERROR");
        _output.WriteLine("  }");
        _output.WriteLine("");
        _output.WriteLine("PROBLEM:");
        _output.WriteLine("  Dictionary<TKey, TValue> cannot be rendered in MDF.");
        _output.WriteLine("  Would show as:");
        _output.WriteLine("    System.Collections.Generic.Dictionary`2[System.String,System.String]");
        _output.WriteLine("");
        _output.WriteLine("ERROR MESSAGE (proposed):");
        _output.WriteLine("  MARKOUT003: Property 'Settings' is a Dictionary<TKey, TValue>.");
        _output.WriteLine("  Dictionaries are not supported in MDF.");
        _output.WriteLine("  ");
        _output.WriteLine("  Choose one of these strategies:");
        _output.WriteLine("  1. Convert to List<KeyValuePair> or List<Setting>");
        _output.WriteLine("  2. Use [MarkOutIgnore] if not needed in output");
        _output.WriteLine("  3. Provide as separate scalar properties if keys are known");
        _output.WriteLine("  ");
        _output.WriteLine("  Example:");
        _output.WriteLine("    class Setting {");
        _output.WriteLine("        string Key { get; set; }");
        _output.WriteLine("        string Value { get; set; }");
        _output.WriteLine("    }");
        _output.WriteLine("    List<Setting> Settings { get; set; }");
        _output.WriteLine("");
    }

    [Fact]
    public void DetectionLogic_ScalarVsNonScalar()
    {
        _output.WriteLine("=== DETECTION LOGIC: SCALAR VS NON-SCALAR ===");
        _output.WriteLine("");
        _output.WriteLine("SCALAR TYPES (safe for table cells):");
        _output.WriteLine("  ✅ string");
        _output.WriteLine("  ✅ bool");
        _output.WriteLine("  ✅ int, long, double, decimal, byte, short, etc.");
        _output.WriteLine("  ✅ DateTime, DateTimeOffset, TimeSpan");
        _output.WriteLine("  ✅ Guid");
        _output.WriteLine("  ✅ enum");
        _output.WriteLine("  ✅ Nullable<T> where T is scalar");
        _output.WriteLine("  ✅ Uri (has good ToString)");
        _output.WriteLine("");
        _output.WriteLine("NON-SCALAR TYPES (ERROR in table cells):");
        _output.WriteLine("  ❌ List<T>, IEnumerable<T>, T[]");
        _output.WriteLine("  ❌ Dictionary<TKey, TValue>");
        _output.WriteLine("  ❌ class/struct with multiple properties");
        _output.WriteLine("  ❌ object");
        _output.WriteLine("");
        _output.WriteLine("DETECTION ALGORITHM:");
        _output.WriteLine("");
        _output.WriteLine("  foreach (Type T with [MarkOutSerializable]) {");
        _output.WriteLine("      if (IsUsedInList(T)) {");
        _output.WriteLine("          foreach (Property P in T.Properties) {");
        _output.WriteLine("              if (P.HasAttribute<MarkOutIgnore>()) continue;");
        _output.WriteLine("              if (P.HasAttribute<MarkOutSection>()) continue;");
        _output.WriteLine("              ");
        _output.WriteLine("              if (!IsScalar(P.Type)) {");
        _output.WriteLine("                  EmitError(");
        _output.WriteLine("                      $\"MARKOUT001: Property '{P.Name}' in '{T.Name}' \");");
        _output.WriteLine("                      $\"is not scalar and will not render correctly.\");");
        _output.WriteLine("              }");
        _output.WriteLine("          }");
        _output.WriteLine("      }");
        _output.WriteLine("  }");
        _output.WriteLine("");
        _output.WriteLine("  bool IsScalar(Type type) {");
        _output.WriteLine("      // Handle nullable");
        _output.WriteLine("      if (type is Nullable<T>) type = T;");
        _output.WriteLine("      ");
        _output.WriteLine("      // Primitive types");
        _output.WriteLine("      if (type.IsPrimitive) return true;");
        _output.WriteLine("      ");
        _output.WriteLine("      // Known scalar types");
        _output.WriteLine("      if (type == typeof(string)) return true;");
        _output.WriteLine("      if (type == typeof(DateTime)) return true;");
        _output.WriteLine("      if (type == typeof(DateTimeOffset)) return true;");
        _output.WriteLine("      if (type == typeof(Guid)) return true;");
        _output.WriteLine("      if (type == typeof(Uri)) return true;");
        _output.WriteLine("      if (type.IsEnum) return true;");
        _output.WriteLine("      ");
        _output.WriteLine("      return false;");
        _output.WriteLine("  }");
        _output.WriteLine("");
    }

    [Fact]
    public void GoodPatterns_NoErrors()
    {
        _output.WriteLine("=== PATTERNS THAT SHOULD NOT ERROR ===");
        _output.WriteLine("");
        _output.WriteLine("✅ PATTERN 1: All scalar properties");
        _output.WriteLine("  [MarkOutSerializable]");
        _output.WriteLine("  class TestResult {");
        _output.WriteLine("      string Name { get; set; }");
        _output.WriteLine("      bool Passed { get; set; }");
        _output.WriteLine("      int Duration { get; set; }");
        _output.WriteLine("  }");
        _output.WriteLine("  → Renders as table perfectly");
        _output.WriteLine("");
        _output.WriteLine("✅ PATTERN 2: Non-scalar with [MarkOutIgnore]");
        _output.WriteLine("  [MarkOutSerializable]");
        _output.WriteLine("  class BuildResult {");
        _output.WriteLine("      string Name { get; set; }");
        _output.WriteLine("      ");
        _output.WriteLine("      [MarkOutIgnore]");
        _output.WriteLine("      List<string> Warnings { get; set; }  // OK - ignored");
        _output.WriteLine("      ");
        _output.WriteLine("      int WarningCount { get; set; }  // Provide aggregate instead");
        _output.WriteLine("  }");
        _output.WriteLine("  → User explicitly chose to ignore");
        _output.WriteLine("");
        _output.WriteLine("✅ PATTERN 3: Non-scalar with [MarkOutSection]");
        _output.WriteLine("  [MarkOutSerializable]");
        _output.WriteLine("  class BuildResult {");
        _output.WriteLine("      string Name { get; set; }");
        _output.WriteLine("      ");
        _output.WriteLine("      [MarkOutSection(Name = \"Warnings\")]");
        _output.WriteLine("      List<string> Warnings { get; set; }  // OK - separate section");
        _output.WriteLine("  }");
        _output.WriteLine("  → User explicitly chose section rendering");
        _output.WriteLine("");
        _output.WriteLine("✅ PATTERN 4: Top-level list (not in table)");
        _output.WriteLine("  [MarkOutSerializable]");
        _output.WriteLine("  class Package {");
        _output.WriteLine("      string Name { get; set; }");
        _output.WriteLine("      ");
        _output.WriteLine("      List<DependencyGroup> Groups { get; set; }  // OK - top level");
        _output.WriteLine("  }");
        _output.WriteLine("  → Not used in a list itself, so no table");
        _output.WriteLine("");
    }

    [Fact]
    public void ErrorMessages_ShouldBeHelpful()
    {
        _output.WriteLine("=== ERROR MESSAGE DESIGN PRINCIPLES ===");
        _output.WriteLine("");
        _output.WriteLine("Good error messages should:");
        _output.WriteLine("  1. Explain WHY it's a problem");
        _output.WriteLine("  2. Show what the output would look like");
        _output.WriteLine("  3. Provide 2-3 concrete solutions");
        _output.WriteLine("  4. Link to documentation");
        _output.WriteLine("  5. Show example code");
        _output.WriteLine("");
        _output.WriteLine("EXAMPLE ERROR MESSAGE:");
        _output.WriteLine("");
        _output.WriteLine("  ╔════════════════════════════════════════════════════════════╗");
        _output.WriteLine("  ║ ERROR MARKOUT001: Non-scalar property in table row            ║");
        _output.WriteLine("  ╚════════════════════════════════════════════════════════════╝");
        _output.WriteLine("");
        _output.WriteLine("  Property 'Packages' in type 'DependencyGroup' is a List<T>.");
        _output.WriteLine("  ");
        _output.WriteLine("  When DependencyGroup is rendered in a table (because it's in a");
        _output.WriteLine("  List<DependencyGroup>), the Packages property will show as:");
        _output.WriteLine("  ");
        _output.WriteLine("    System.Collections.Generic.List`1[Dependency]");
        _output.WriteLine("  ");
        _output.WriteLine("  This is not useful output!");
        _output.WriteLine("");
        _output.WriteLine("  ──────────────────────────────────────────────────────────");
        _output.WriteLine("  Solutions:");
        _output.WriteLine("  ──────────────────────────────────────────────────────────");
        _output.WriteLine("");
        _output.WriteLine("  1. PIVOT: Transform to comparison table");
        _output.WriteLine("     ");
        _output.WriteLine("     [MarkOutIgnore]");
        _output.WriteLine("     public List<DependencyGroup> Groups { get; set; }");
        _output.WriteLine("     ");
        _output.WriteLine("     [MarkOutSection(Name = \"Dependencies\")]");
        _output.WriteLine("     public List<DependencyMatrix> Matrix");
        _output.WriteLine("         => PivotGroups(Groups);");
        _output.WriteLine("");
        _output.WriteLine("  2. IGNORE: Exclude from output");
        _output.WriteLine("     ");
        _output.WriteLine("     [MarkOutIgnore]");
        _output.WriteLine("     public List<Dependency> Packages { get; set; }");
        _output.WriteLine("     ");
        _output.WriteLine("     public int PackageCount { get; set; }  // Aggregate");
        _output.WriteLine("");
        _output.WriteLine("  3. SECTION: Separate sections per group");
        _output.WriteLine("     ");
        _output.WriteLine("     [MarkOutSection(Name = \"Dependencies (net6.0)\")]");
        _output.WriteLine("     public List<Dependency> Net6Packages { get; set; }");
        _output.WriteLine("");
        _output.WriteLine("  ──────────────────────────────────────────────────────────");
        _output.WriteLine("  Learn more:");
        _output.WriteLine("    https://docs.mdf.dev/strategies/nested-lists");
        _output.WriteLine("  ──────────────────────────────────────────────────────────");
        _output.WriteLine("");
    }

    [Fact]
    public void ImplementationStrategy_SourceGenerator()
    {
        _output.WriteLine("=== SOURCE GENERATOR IMPLEMENTATION ===");
        _output.WriteLine("");
        _output.WriteLine("In TypeParser.cs:");
        _output.WriteLine("");
        _output.WriteLine("  private static PropertyMetadata? ParseProperty(");
        _output.WriteLine("      IPropertySymbol prop, ");
        _output.WriteLine("      Compilation compilation,");
        _output.WriteLine("      bool isInTableContext)  // NEW PARAMETER");
        _output.WriteLine("  {");
        _output.WriteLine("      var isIgnored = HasAttribute(prop, MarkOutIgnoreAttribute);");
        _output.WriteLine("      var isSection = HasAttribute(prop, MarkOutSectionAttribute);");
        _output.WriteLine("      ");
        _output.WriteLine("      var (kind, elementTypeName, elementProperties) = ");
        _output.WriteLine("          DeterminePropertyKind(prop.Type, compilation);");
        _output.WriteLine("      ");
        _output.WriteLine("      // NEW: Validate if in table context");
        _output.WriteLine("      if (isInTableContext && !isIgnored && !isSection) {");
        _output.WriteLine("          if (!IsScalarKind(kind)) {");
        _output.WriteLine("              ReportError(");
        _output.WriteLine("                  \"MARKOUT001\",");
        _output.WriteLine("                  $\"Property '{prop.Name}' is not scalar\",");
        _output.WriteLine("                  prop.Locations.FirstOrDefault(),");
        _output.WriteLine("                  prop.Type.ToDisplayString()");
        _output.WriteLine("              );");
        _output.WriteLine("          }");
        _output.WriteLine("      }");
        _output.WriteLine("      ");
        _output.WriteLine("      return new PropertyMetadata(...);");
        _output.WriteLine("  }");
        _output.WriteLine("");
        _output.WriteLine("  private static bool IsScalarKind(PropertyKind kind) {");
        _output.WriteLine("      return kind switch {");
        _output.WriteLine("          PropertyKind.String => true,");
        _output.WriteLine("          PropertyKind.Boolean => true,");
        _output.WriteLine("          PropertyKind.Int32 => true,");
        _output.WriteLine("          PropertyKind.Int64 => true,");
        _output.WriteLine("          PropertyKind.Double => true,");
        _output.WriteLine("          PropertyKind.Decimal => true,");
        _output.WriteLine("          PropertyKind.DateTime => true,");
        _output.WriteLine("          PropertyKind.DateTimeOffset => true,");
        _output.WriteLine("          _ => false");
        _output.WriteLine("      };");
        _output.WriteLine("  }");
        _output.WriteLine("");
        _output.WriteLine("When analyzing types:");
        _output.WriteLine("");
        _output.WriteLine("  var properties = new List<PropertyMetadata>();");
        _output.WriteLine("  ");
        _output.WriteLine("  // Check if this type is used in List<T>");
        _output.WriteLine("  bool isInTableContext = IsTypeUsedInList(typeSymbol, compilation);");
        _output.WriteLine("  ");
        _output.WriteLine("  foreach (var member in typeSymbol.GetMembers()) {");
        _output.WriteLine("      if (member is IPropertySymbol prop) {");
        _output.WriteLine("          var propMeta = ParseProperty(");
        _output.WriteLine("              prop, ");
        _output.WriteLine("              compilation,");
        _output.WriteLine("              isInTableContext  // Pass context");
        _output.WriteLine("          );");
        _output.WriteLine("          if (propMeta != null)");
        _output.WriteLine("              properties.Add(propMeta);");
        _output.WriteLine("      }");
        _output.WriteLine("  }");
        _output.WriteLine("");
    }

    [Fact]
    public void WarningLevels_Severity()
    {
        _output.WriteLine("=== ERROR SEVERITY LEVELS ===");
        _output.WriteLine("");
        _output.WriteLine("MARKOUT001: Non-scalar property in table");
        _output.WriteLine("  Severity: ERROR");
        _output.WriteLine("  Reason: Will produce useless output");
        _output.WriteLine("  Fix: Required before compilation");
        _output.WriteLine("");
        _output.WriteLine("MARKOUT002: Complex object property in table");
        _output.WriteLine("  Severity: ERROR");
        _output.WriteLine("  Reason: Will produce useless output");
        _output.WriteLine("  Fix: Required before compilation");
        _output.WriteLine("");
        _output.WriteLine("MARKOUT003: Dictionary<TKey, TValue> property");
        _output.WriteLine("  Severity: ERROR");
        _output.WriteLine("  Reason: Not supported, will produce useless output");
        _output.WriteLine("  Fix: Required before compilation");
        _output.WriteLine("");
        _output.WriteLine("MDF101: Property has no getter (INFO)");
        _output.WriteLine("  Severity: INFO");
        _output.WriteLine("  Reason: Will be skipped, might be unintentional");
        _output.WriteLine("  Fix: Optional");
        _output.WriteLine("");
        _output.WriteLine("MDF102: Deep nesting (>2 levels) (WARNING)");
        _output.WriteLine("  Severity: WARNING");
        _output.WriteLine("  Reason: Might not render as expected");
        _output.WriteLine("  Fix: Consider flattening");
        _output.WriteLine("");
    }
}
