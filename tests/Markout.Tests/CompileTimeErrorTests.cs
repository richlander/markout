using Markout;

namespace Markout.Tests;

/// <summary>
/// Tests documenting patterns that SHOULD produce compile-time errors.
/// These patterns result in useless ToString() output and should be caught by the source generator.
/// </summary>
public class CompileTimeErrorTests
{
    [Fact]
    public void Pattern1_ListPropertyInTable_ShouldError()
    {
        TestContext.Current.TestOutputHelper!.WriteLine("=== PATTERN THAT SHOULD BE COMPILE ERROR #1 ===");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("CODE:");
        TestContext.Current.TestOutputHelper!.WriteLine("  [MarkoutSerializable]");
        TestContext.Current.TestOutputHelper!.WriteLine("  class DependencyGroup {");
        TestContext.Current.TestOutputHelper!.WriteLine("      string TargetFramework { get; set; }");
        TestContext.Current.TestOutputHelper!.WriteLine("      List<Dependency> Packages { get; set; }  // ❌ ERROR");
        TestContext.Current.TestOutputHelper!.WriteLine("  }");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("  class Package {");
        TestContext.Current.TestOutputHelper!.WriteLine("      List<DependencyGroup> Groups { get; set; }");
        TestContext.Current.TestOutputHelper!.WriteLine("  }");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("PROBLEM:");
        TestContext.Current.TestOutputHelper!.WriteLine("  DependencyGroup will be rendered as a table row.");
        TestContext.Current.TestOutputHelper!.WriteLine("  The Packages property would show as:");
        TestContext.Current.TestOutputHelper!.WriteLine("    System.Collections.Generic.List`1[Dependency]");
        TestContext.Current.TestOutputHelper!.WriteLine("  This is USELESS output!");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("ERROR MESSAGE (proposed):");
        TestContext.Current.TestOutputHelper!.WriteLine("  MARKOUT001: Property 'Packages' in 'DependencyGroup' is a List<T>.");
        TestContext.Current.TestOutputHelper!.WriteLine("  List<T> properties cannot be rendered in table cells.");
        TestContext.Current.TestOutputHelper!.WriteLine("  ");
        TestContext.Current.TestOutputHelper!.WriteLine("  Choose one of these strategies:");
        TestContext.Current.TestOutputHelper!.WriteLine("  1. [MarkoutIgnore] - Exclude this property from serialization");
        TestContext.Current.TestOutputHelper!.WriteLine("  2. Provide aggregate data (e.g., 'PackageCount')");
        TestContext.Current.TestOutputHelper!.WriteLine("  3. Transform to pivot table (see docs)");
        TestContext.Current.TestOutputHelper!.WriteLine("  4. Use separate sections with [MarkoutSection] attribute");
        TestContext.Current.TestOutputHelper!.WriteLine("  ");
        TestContext.Current.TestOutputHelper!.WriteLine("  See: https://docs.mdf.dev/strategies/nested-lists");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("DETECTION LOGIC:");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Type has [MarkoutSerializable] attribute");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Type is used in List<T>");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Type has property that is List<U> or IEnumerable<U>");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Property does NOT have [MarkoutIgnore]");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Property does NOT have [MarkoutSection]");
        TestContext.Current.TestOutputHelper!.WriteLine("  → EMIT COMPILE ERROR");
        TestContext.Current.TestOutputHelper!.WriteLine("");
    }

    [Fact]
    public void Pattern2_ComplexObjectPropertyInTable_ShouldError()
    {
        TestContext.Current.TestOutputHelper!.WriteLine("=== PATTERN THAT SHOULD BE COMPILE ERROR #2 ===");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("CODE:");
        TestContext.Current.TestOutputHelper!.WriteLine("  [MarkoutSerializable]");
        TestContext.Current.TestOutputHelper!.WriteLine("  class BuildResult {");
        TestContext.Current.TestOutputHelper!.WriteLine("      string ProjectName { get; set; }");
        TestContext.Current.TestOutputHelper!.WriteLine("      BuildMetadata Metadata { get; set; }  // ❌ ERROR");
        TestContext.Current.TestOutputHelper!.WriteLine("  }");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("  [MarkoutSerializable]");
        TestContext.Current.TestOutputHelper!.WriteLine("  class BuildMetadata {");
        TestContext.Current.TestOutputHelper!.WriteLine("      string Commit { get; set; }");
        TestContext.Current.TestOutputHelper!.WriteLine("      DateTime BuildTime { get; set; }");
        TestContext.Current.TestOutputHelper!.WriteLine("      List<string> Tags { get; set; }");
        TestContext.Current.TestOutputHelper!.WriteLine("  }");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("  class BuildSummary {");
        TestContext.Current.TestOutputHelper!.WriteLine("      List<BuildResult> Builds { get; set; }");
        TestContext.Current.TestOutputHelper!.WriteLine("  }");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("PROBLEM:");
        TestContext.Current.TestOutputHelper!.WriteLine("  BuildResult will be rendered as table row.");
        TestContext.Current.TestOutputHelper!.WriteLine("  The Metadata property would show as:");
        TestContext.Current.TestOutputHelper!.WriteLine("    BuildMetadata (ToString)");
        TestContext.Current.TestOutputHelper!.WriteLine("  This is USELESS output!");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("ERROR MESSAGE (proposed):");
        TestContext.Current.TestOutputHelper!.WriteLine("  MARKOUT002: Property 'Metadata' in 'BuildResult' is a complex object.");
        TestContext.Current.TestOutputHelper!.WriteLine("  Complex objects cannot be rendered in table cells.");
        TestContext.Current.TestOutputHelper!.WriteLine("  ");
        TestContext.Current.TestOutputHelper!.WriteLine("  Choose one of these strategies:");
        TestContext.Current.TestOutputHelper!.WriteLine("  1. [MarkoutIgnore] - Exclude this property");
        TestContext.Current.TestOutputHelper!.WriteLine("  2. Flatten - Move Metadata properties to BuildResult");
        TestContext.Current.TestOutputHelper!.WriteLine("  3. Provide summary (e.g., 'CommitHash' as string)");
        TestContext.Current.TestOutputHelper!.WriteLine("  ");
        TestContext.Current.TestOutputHelper!.WriteLine("  See: https://docs.mdf.dev/strategies/complex-properties");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("DETECTION LOGIC:");
        TestContext.Current.TestOutputHelper!.WriteLine("  - Type T has [MarkoutSerializable]");
        TestContext.Current.TestOutputHelper!.WriteLine("  - T is used in List<T>");
        TestContext.Current.TestOutputHelper!.WriteLine("  - T has property P of type U");
        TestContext.Current.TestOutputHelper!.WriteLine("  - U is a class/struct (not scalar)");
        TestContext.Current.TestOutputHelper!.WriteLine("  - U has multiple public properties");
        TestContext.Current.TestOutputHelper!.WriteLine("  - P does NOT have [MarkoutIgnore]");
        TestContext.Current.TestOutputHelper!.WriteLine("  → EMIT COMPILE ERROR");
        TestContext.Current.TestOutputHelper!.WriteLine("");
    }

    [Fact]
    public void Pattern3_DictionaryProperty_ShouldError()
    {
        TestContext.Current.TestOutputHelper!.WriteLine("=== PATTERN THAT SHOULD BE COMPILE ERROR #3 ===");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("CODE:");
        TestContext.Current.TestOutputHelper!.WriteLine("  [MarkoutSerializable]");
        TestContext.Current.TestOutputHelper!.WriteLine("  class Configuration {");
        TestContext.Current.TestOutputHelper!.WriteLine("      string Name { get; set; }");
        TestContext.Current.TestOutputHelper!.WriteLine("      Dictionary<string, string> Settings { get; set; }  // ❌ ERROR");
        TestContext.Current.TestOutputHelper!.WriteLine("  }");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("PROBLEM:");
        TestContext.Current.TestOutputHelper!.WriteLine("  Dictionary<TKey, TValue> cannot be rendered in Markout.");
        TestContext.Current.TestOutputHelper!.WriteLine("  Would show as:");
        TestContext.Current.TestOutputHelper!.WriteLine("    System.Collections.Generic.Dictionary`2[System.String,System.String]");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("ERROR MESSAGE (proposed):");
        TestContext.Current.TestOutputHelper!.WriteLine("  MARKOUT003: Property 'Settings' is a Dictionary<TKey, TValue>.");
        TestContext.Current.TestOutputHelper!.WriteLine("  Dictionaries are not supported in Markout.");
        TestContext.Current.TestOutputHelper!.WriteLine("  ");
        TestContext.Current.TestOutputHelper!.WriteLine("  Choose one of these strategies:");
        TestContext.Current.TestOutputHelper!.WriteLine("  1. Convert to List<KeyValuePair> or List<Setting>");
        TestContext.Current.TestOutputHelper!.WriteLine("  2. Use [MarkoutIgnore] if not needed in output");
        TestContext.Current.TestOutputHelper!.WriteLine("  3. Provide as separate scalar properties if keys are known");
        TestContext.Current.TestOutputHelper!.WriteLine("  ");
        TestContext.Current.TestOutputHelper!.WriteLine("  Example:");
        TestContext.Current.TestOutputHelper!.WriteLine("    class Setting {");
        TestContext.Current.TestOutputHelper!.WriteLine("        string Key { get; set; }");
        TestContext.Current.TestOutputHelper!.WriteLine("        string Value { get; set; }");
        TestContext.Current.TestOutputHelper!.WriteLine("    }");
        TestContext.Current.TestOutputHelper!.WriteLine("    List<Setting> Settings { get; set; }");
        TestContext.Current.TestOutputHelper!.WriteLine("");
    }

    [Fact]
    public void DetectionLogic_ScalarVsNonScalar()
    {
        TestContext.Current.TestOutputHelper!.WriteLine("=== DETECTION LOGIC: SCALAR VS NON-SCALAR ===");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("SCALAR TYPES (safe for table cells):");
        TestContext.Current.TestOutputHelper!.WriteLine("  ✅ string");
        TestContext.Current.TestOutputHelper!.WriteLine("  ✅ bool");
        TestContext.Current.TestOutputHelper!.WriteLine("  ✅ int, long, double, decimal, byte, short, etc.");
        TestContext.Current.TestOutputHelper!.WriteLine("  ✅ DateTime, DateTimeOffset, TimeSpan");
        TestContext.Current.TestOutputHelper!.WriteLine("  ✅ Guid");
        TestContext.Current.TestOutputHelper!.WriteLine("  ✅ enum");
        TestContext.Current.TestOutputHelper!.WriteLine("  ✅ Nullable<T> where T is scalar");
        TestContext.Current.TestOutputHelper!.WriteLine("  ✅ Uri (has good ToString)");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("NON-SCALAR TYPES (ERROR in table cells):");
        TestContext.Current.TestOutputHelper!.WriteLine("  ❌ List<T>, IEnumerable<T>, T[]");
        TestContext.Current.TestOutputHelper!.WriteLine("  ❌ Dictionary<TKey, TValue>");
        TestContext.Current.TestOutputHelper!.WriteLine("  ❌ class/struct with multiple properties");
        TestContext.Current.TestOutputHelper!.WriteLine("  ❌ object");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("DETECTION ALGORITHM:");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("  foreach (Type T with [MarkoutSerializable]) {");
        TestContext.Current.TestOutputHelper!.WriteLine("      if (IsUsedInList(T)) {");
        TestContext.Current.TestOutputHelper!.WriteLine("          foreach (Property P in T.Properties) {");
        TestContext.Current.TestOutputHelper!.WriteLine("              if (P.HasAttribute<MarkoutIgnore>()) continue;");
        TestContext.Current.TestOutputHelper!.WriteLine("              if (P.HasAttribute<MarkoutSection>()) continue;");
        TestContext.Current.TestOutputHelper!.WriteLine("              ");
        TestContext.Current.TestOutputHelper!.WriteLine("              if (!IsScalar(P.Type)) {");
        TestContext.Current.TestOutputHelper!.WriteLine("                  EmitError(");
        TestContext.Current.TestOutputHelper!.WriteLine("                      $\"MARKOUT001: Property '{P.Name}' in '{T.Name}' \");");
        TestContext.Current.TestOutputHelper!.WriteLine("                      $\"is not scalar and will not render correctly.\");");
        TestContext.Current.TestOutputHelper!.WriteLine("              }");
        TestContext.Current.TestOutputHelper!.WriteLine("          }");
        TestContext.Current.TestOutputHelper!.WriteLine("      }");
        TestContext.Current.TestOutputHelper!.WriteLine("  }");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("  bool IsScalar(Type type) {");
        TestContext.Current.TestOutputHelper!.WriteLine("      // Handle nullable");
        TestContext.Current.TestOutputHelper!.WriteLine("      if (type is Nullable<T>) type = T;");
        TestContext.Current.TestOutputHelper!.WriteLine("      ");
        TestContext.Current.TestOutputHelper!.WriteLine("      // Primitive types");
        TestContext.Current.TestOutputHelper!.WriteLine("      if (type.IsPrimitive) return true;");
        TestContext.Current.TestOutputHelper!.WriteLine("      ");
        TestContext.Current.TestOutputHelper!.WriteLine("      // Known scalar types");
        TestContext.Current.TestOutputHelper!.WriteLine("      if (type == typeof(string)) return true;");
        TestContext.Current.TestOutputHelper!.WriteLine("      if (type == typeof(DateTime)) return true;");
        TestContext.Current.TestOutputHelper!.WriteLine("      if (type == typeof(DateTimeOffset)) return true;");
        TestContext.Current.TestOutputHelper!.WriteLine("      if (type == typeof(Guid)) return true;");
        TestContext.Current.TestOutputHelper!.WriteLine("      if (type == typeof(Uri)) return true;");
        TestContext.Current.TestOutputHelper!.WriteLine("      if (type.IsEnum) return true;");
        TestContext.Current.TestOutputHelper!.WriteLine("      ");
        TestContext.Current.TestOutputHelper!.WriteLine("      return false;");
        TestContext.Current.TestOutputHelper!.WriteLine("  }");
        TestContext.Current.TestOutputHelper!.WriteLine("");
    }

    [Fact]
    public void GoodPatterns_NoErrors()
    {
        TestContext.Current.TestOutputHelper!.WriteLine("=== PATTERNS THAT SHOULD NOT ERROR ===");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("✅ PATTERN 1: All scalar properties");
        TestContext.Current.TestOutputHelper!.WriteLine("  [MarkoutSerializable]");
        TestContext.Current.TestOutputHelper!.WriteLine("  class TestResult {");
        TestContext.Current.TestOutputHelper!.WriteLine("      string Name { get; set; }");
        TestContext.Current.TestOutputHelper!.WriteLine("      bool Passed { get; set; }");
        TestContext.Current.TestOutputHelper!.WriteLine("      int Duration { get; set; }");
        TestContext.Current.TestOutputHelper!.WriteLine("  }");
        TestContext.Current.TestOutputHelper!.WriteLine("  → Renders as table perfectly");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("✅ PATTERN 2: Non-scalar with [MarkoutIgnore]");
        TestContext.Current.TestOutputHelper!.WriteLine("  [MarkoutSerializable]");
        TestContext.Current.TestOutputHelper!.WriteLine("  class BuildResult {");
        TestContext.Current.TestOutputHelper!.WriteLine("      string Name { get; set; }");
        TestContext.Current.TestOutputHelper!.WriteLine("      ");
        TestContext.Current.TestOutputHelper!.WriteLine("      [MarkoutIgnore]");
        TestContext.Current.TestOutputHelper!.WriteLine("      List<string> Warnings { get; set; }  // OK - ignored");
        TestContext.Current.TestOutputHelper!.WriteLine("      ");
        TestContext.Current.TestOutputHelper!.WriteLine("      int WarningCount { get; set; }  // Provide aggregate instead");
        TestContext.Current.TestOutputHelper!.WriteLine("  }");
        TestContext.Current.TestOutputHelper!.WriteLine("  → User explicitly chose to ignore");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("✅ PATTERN 3: Non-scalar with [MarkoutSection]");
        TestContext.Current.TestOutputHelper!.WriteLine("  [MarkoutSerializable]");
        TestContext.Current.TestOutputHelper!.WriteLine("  class BuildResult {");
        TestContext.Current.TestOutputHelper!.WriteLine("      string Name { get; set; }");
        TestContext.Current.TestOutputHelper!.WriteLine("      ");
        TestContext.Current.TestOutputHelper!.WriteLine("      [MarkoutSection(Name = \"Warnings\")]");
        TestContext.Current.TestOutputHelper!.WriteLine("      List<string> Warnings { get; set; }  // OK - separate section");
        TestContext.Current.TestOutputHelper!.WriteLine("  }");
        TestContext.Current.TestOutputHelper!.WriteLine("  → User explicitly chose section rendering");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("✅ PATTERN 4: Top-level list (not in table)");
        TestContext.Current.TestOutputHelper!.WriteLine("  [MarkoutSerializable]");
        TestContext.Current.TestOutputHelper!.WriteLine("  class Package {");
        TestContext.Current.TestOutputHelper!.WriteLine("      string Name { get; set; }");
        TestContext.Current.TestOutputHelper!.WriteLine("      ");
        TestContext.Current.TestOutputHelper!.WriteLine("      List<DependencyGroup> Groups { get; set; }  // OK - top level");
        TestContext.Current.TestOutputHelper!.WriteLine("  }");
        TestContext.Current.TestOutputHelper!.WriteLine("  → Not used in a list itself, so no table");
        TestContext.Current.TestOutputHelper!.WriteLine("");
    }

    [Fact]
    public void ErrorMessages_ShouldBeHelpful()
    {
        TestContext.Current.TestOutputHelper!.WriteLine("=== ERROR MESSAGE DESIGN PRINCIPLES ===");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("Good error messages should:");
        TestContext.Current.TestOutputHelper!.WriteLine("  1. Explain WHY it's a problem");
        TestContext.Current.TestOutputHelper!.WriteLine("  2. Show what the output would look like");
        TestContext.Current.TestOutputHelper!.WriteLine("  3. Provide 2-3 concrete solutions");
        TestContext.Current.TestOutputHelper!.WriteLine("  4. Link to documentation");
        TestContext.Current.TestOutputHelper!.WriteLine("  5. Show example code");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("EXAMPLE ERROR MESSAGE:");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("  ╔════════════════════════════════════════════════════════════╗");
        TestContext.Current.TestOutputHelper!.WriteLine("  ║ ERROR MARKOUT001: Non-scalar property in table row            ║");
        TestContext.Current.TestOutputHelper!.WriteLine("  ╚════════════════════════════════════════════════════════════╝");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("  Property 'Packages' in type 'DependencyGroup' is a List<T>.");
        TestContext.Current.TestOutputHelper!.WriteLine("  ");
        TestContext.Current.TestOutputHelper!.WriteLine("  When DependencyGroup is rendered in a table (because it's in a");
        TestContext.Current.TestOutputHelper!.WriteLine("  List<DependencyGroup>), the Packages property will show as:");
        TestContext.Current.TestOutputHelper!.WriteLine("  ");
        TestContext.Current.TestOutputHelper!.WriteLine("    System.Collections.Generic.List`1[Dependency]");
        TestContext.Current.TestOutputHelper!.WriteLine("  ");
        TestContext.Current.TestOutputHelper!.WriteLine("  This is not useful output!");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("  ──────────────────────────────────────────────────────────");
        TestContext.Current.TestOutputHelper!.WriteLine("  Solutions:");
        TestContext.Current.TestOutputHelper!.WriteLine("  ──────────────────────────────────────────────────────────");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("  1. PIVOT: Transform to comparison table");
        TestContext.Current.TestOutputHelper!.WriteLine("     ");
        TestContext.Current.TestOutputHelper!.WriteLine("     [MarkoutIgnore]");
        TestContext.Current.TestOutputHelper!.WriteLine("     public List<DependencyGroup> Groups { get; set; }");
        TestContext.Current.TestOutputHelper!.WriteLine("     ");
        TestContext.Current.TestOutputHelper!.WriteLine("     [MarkoutSection(Name = \"Dependencies\")]");
        TestContext.Current.TestOutputHelper!.WriteLine("     public List<DependencyMatrix> Matrix");
        TestContext.Current.TestOutputHelper!.WriteLine("         => PivotGroups(Groups);");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("  2. IGNORE: Exclude from output");
        TestContext.Current.TestOutputHelper!.WriteLine("     ");
        TestContext.Current.TestOutputHelper!.WriteLine("     [MarkoutIgnore]");
        TestContext.Current.TestOutputHelper!.WriteLine("     public List<Dependency> Packages { get; set; }");
        TestContext.Current.TestOutputHelper!.WriteLine("     ");
        TestContext.Current.TestOutputHelper!.WriteLine("     public int PackageCount { get; set; }  // Aggregate");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("  3. SECTION: Separate sections per group");
        TestContext.Current.TestOutputHelper!.WriteLine("     ");
        TestContext.Current.TestOutputHelper!.WriteLine("     [MarkoutSection(Name = \"Dependencies (net6.0)\")]");
        TestContext.Current.TestOutputHelper!.WriteLine("     public List<Dependency> Net6Packages { get; set; }");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("  ──────────────────────────────────────────────────────────");
        TestContext.Current.TestOutputHelper!.WriteLine("  Learn more:");
        TestContext.Current.TestOutputHelper!.WriteLine("    https://docs.mdf.dev/strategies/nested-lists");
        TestContext.Current.TestOutputHelper!.WriteLine("  ──────────────────────────────────────────────────────────");
        TestContext.Current.TestOutputHelper!.WriteLine("");
    }

    [Fact]
    public void ImplementationStrategy_SourceGenerator()
    {
        TestContext.Current.TestOutputHelper!.WriteLine("=== SOURCE GENERATOR IMPLEMENTATION ===");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("In TypeParser.cs:");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("  private static PropertyMetadata? ParseProperty(");
        TestContext.Current.TestOutputHelper!.WriteLine("      IPropertySymbol prop, ");
        TestContext.Current.TestOutputHelper!.WriteLine("      Compilation compilation,");
        TestContext.Current.TestOutputHelper!.WriteLine("      bool isInTableContext)  // NEW PARAMETER");
        TestContext.Current.TestOutputHelper!.WriteLine("  {");
        TestContext.Current.TestOutputHelper!.WriteLine("      var isIgnored = HasAttribute(prop, MarkoutIgnoreAttribute);");
        TestContext.Current.TestOutputHelper!.WriteLine("      var isSection = HasAttribute(prop, MarkoutSectionAttribute);");
        TestContext.Current.TestOutputHelper!.WriteLine("      ");
        TestContext.Current.TestOutputHelper!.WriteLine("      var (kind, elementTypeName, elementProperties) = ");
        TestContext.Current.TestOutputHelper!.WriteLine("          DeterminePropertyKind(prop.Type, compilation);");
        TestContext.Current.TestOutputHelper!.WriteLine("      ");
        TestContext.Current.TestOutputHelper!.WriteLine("      // NEW: Validate if in table context");
        TestContext.Current.TestOutputHelper!.WriteLine("      if (isInTableContext && !isIgnored && !isSection) {");
        TestContext.Current.TestOutputHelper!.WriteLine("          if (!IsScalarKind(kind)) {");
        TestContext.Current.TestOutputHelper!.WriteLine("              ReportError(");
        TestContext.Current.TestOutputHelper!.WriteLine("                  \"MARKOUT001\",");
        TestContext.Current.TestOutputHelper!.WriteLine("                  $\"Property '{prop.Name}' is not scalar\",");
        TestContext.Current.TestOutputHelper!.WriteLine("                  prop.Locations.FirstOrDefault(),");
        TestContext.Current.TestOutputHelper!.WriteLine("                  prop.Type.ToDisplayString()");
        TestContext.Current.TestOutputHelper!.WriteLine("              );");
        TestContext.Current.TestOutputHelper!.WriteLine("          }");
        TestContext.Current.TestOutputHelper!.WriteLine("      }");
        TestContext.Current.TestOutputHelper!.WriteLine("      ");
        TestContext.Current.TestOutputHelper!.WriteLine("      return new PropertyMetadata(...);");
        TestContext.Current.TestOutputHelper!.WriteLine("  }");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("  private static bool IsScalarKind(PropertyKind kind) {");
        TestContext.Current.TestOutputHelper!.WriteLine("      return kind switch {");
        TestContext.Current.TestOutputHelper!.WriteLine("          PropertyKind.String => true,");
        TestContext.Current.TestOutputHelper!.WriteLine("          PropertyKind.Boolean => true,");
        TestContext.Current.TestOutputHelper!.WriteLine("          PropertyKind.Int32 => true,");
        TestContext.Current.TestOutputHelper!.WriteLine("          PropertyKind.Int64 => true,");
        TestContext.Current.TestOutputHelper!.WriteLine("          PropertyKind.Double => true,");
        TestContext.Current.TestOutputHelper!.WriteLine("          PropertyKind.Decimal => true,");
        TestContext.Current.TestOutputHelper!.WriteLine("          PropertyKind.DateTime => true,");
        TestContext.Current.TestOutputHelper!.WriteLine("          PropertyKind.DateTimeOffset => true,");
        TestContext.Current.TestOutputHelper!.WriteLine("          _ => false");
        TestContext.Current.TestOutputHelper!.WriteLine("      };");
        TestContext.Current.TestOutputHelper!.WriteLine("  }");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("When analyzing types:");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("  var properties = new List<PropertyMetadata>();");
        TestContext.Current.TestOutputHelper!.WriteLine("  ");
        TestContext.Current.TestOutputHelper!.WriteLine("  // Check if this type is used in List<T>");
        TestContext.Current.TestOutputHelper!.WriteLine("  bool isInTableContext = IsTypeUsedInList(typeSymbol, compilation);");
        TestContext.Current.TestOutputHelper!.WriteLine("  ");
        TestContext.Current.TestOutputHelper!.WriteLine("  foreach (var member in typeSymbol.GetMembers()) {");
        TestContext.Current.TestOutputHelper!.WriteLine("      if (member is IPropertySymbol prop) {");
        TestContext.Current.TestOutputHelper!.WriteLine("          var propMeta = ParseProperty(");
        TestContext.Current.TestOutputHelper!.WriteLine("              prop, ");
        TestContext.Current.TestOutputHelper!.WriteLine("              compilation,");
        TestContext.Current.TestOutputHelper!.WriteLine("              isInTableContext  // Pass context");
        TestContext.Current.TestOutputHelper!.WriteLine("          );");
        TestContext.Current.TestOutputHelper!.WriteLine("          if (propMeta != null)");
        TestContext.Current.TestOutputHelper!.WriteLine("              properties.Add(propMeta);");
        TestContext.Current.TestOutputHelper!.WriteLine("      }");
        TestContext.Current.TestOutputHelper!.WriteLine("  }");
        TestContext.Current.TestOutputHelper!.WriteLine("");
    }

    [Fact]
    public void WarningLevels_Severity()
    {
        TestContext.Current.TestOutputHelper!.WriteLine("=== ERROR SEVERITY LEVELS ===");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("MARKOUT001: Non-scalar property in table");
        TestContext.Current.TestOutputHelper!.WriteLine("  Severity: ERROR");
        TestContext.Current.TestOutputHelper!.WriteLine("  Reason: Will produce useless output");
        TestContext.Current.TestOutputHelper!.WriteLine("  Fix: Required before compilation");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("MARKOUT002: Complex object property in table");
        TestContext.Current.TestOutputHelper!.WriteLine("  Severity: ERROR");
        TestContext.Current.TestOutputHelper!.WriteLine("  Reason: Will produce useless output");
        TestContext.Current.TestOutputHelper!.WriteLine("  Fix: Required before compilation");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("MARKOUT003: Dictionary<TKey, TValue> property");
        TestContext.Current.TestOutputHelper!.WriteLine("  Severity: ERROR");
        TestContext.Current.TestOutputHelper!.WriteLine("  Reason: Not supported, will produce useless output");
        TestContext.Current.TestOutputHelper!.WriteLine("  Fix: Required before compilation");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("MARKOUT101: Property has no getter (INFO)");
        TestContext.Current.TestOutputHelper!.WriteLine("  Severity: INFO");
        TestContext.Current.TestOutputHelper!.WriteLine("  Reason: Will be skipped, might be unintentional");
        TestContext.Current.TestOutputHelper!.WriteLine("  Fix: Optional");
        TestContext.Current.TestOutputHelper!.WriteLine("");
        TestContext.Current.TestOutputHelper!.WriteLine("MARKOUT102: Deep nesting (>2 levels) (WARNING)");
        TestContext.Current.TestOutputHelper!.WriteLine("  Severity: WARNING");
        TestContext.Current.TestOutputHelper!.WriteLine("  Reason: Might not render as expected");
        TestContext.Current.TestOutputHelper!.WriteLine("  Fix: Consider flattening");
        TestContext.Current.TestOutputHelper!.WriteLine("");
    }
}
