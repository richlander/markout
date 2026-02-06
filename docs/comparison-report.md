# Markout vs .NET Runtime Source Generators: Comprehensive Comparison Report

## Executive Summary

This report compares Markout—a source-generated Markdown serializer—with the source generators in the .NET runtime repository, primarily System.Text.Json (STJ), but also Configuration.Binder, LoggerMessage, Options Validator, **LibraryImport**, and **Regex**. Additionally, it analyzes **dotnet-inspect**, a real-world Markout consumer, to validate recommendations against actual usage patterns. The goal is to identify patterns Markout can adopt, pitfalls to avoid, and opportunities to leverage its position as a newer, more focused library.

**Key Findings:**
- Markout's architecture is clean and well-designed for its scope, following the Parser → Model → Emitter pattern established by STJ
- STJ's complexity stems from decades of feature accretion; Markout benefits from a constrained, opinionated design
- Several STJ patterns (incremental caching, multi-file emission, dual-path generation) could benefit Markout as it scales
- Markout's closed type system is a feature, not a limitation—but the lack of custom formatters may need reconsideration
- The Configuration.Binder's interceptor pattern and LoggerMessage's dual-strategy approach offer valuable lessons
- **LibraryImport's marshaller shape system** provides a model for type-specific code generation strategies that could inform Markout's handling of different property kinds
- **Regex's sophisticated emission patterns** demonstrate advanced techniques for generating highly-optimized code
- **dotnet-inspect reveals critical gaps**: ~80% of its Markout usage is direct `MarkoutWriter` calls, not source generation—indicating the current generator model covers only a narrow "sweet spot" of static, unconditional rendering

---

## 1. Executive Summary by Serializer

### System.Text.Json Source Generator

The STJ generator is a mature, production-grade source generator targeting JSON read/write. It supports 25+ collection types, polymorphism, custom converters, multiple construction strategies, and three Roslyn version targets. Its complexity is both a strength (comprehensive) and a burden (maintenance, learning curve).

### Configuration.Binder Source Generator

A binding/deserialization generator that maps `IConfiguration` key-value pairs to strongly-typed objects. Notable for its interceptor system and bit-flag generation tracking. Architecturally sophisticated with a clean TypeSpec hierarchy.

### LoggerMessage Source Generator

A focused generator for high-performance structured logging. Features a dual-strategy pattern: optimized `LoggerMessage.Define<>()` for simple cases, custom state structs for complex cases. Simple, effective, well-scoped.

### Options Validator Source Generator

Generates `IValidateOptions<T>` implementations from data annotations. The simplest of the runtime generators, with minimal incremental caching investment. Uses record-based models and synthesizes validators for transitive types.

### Markout

A source-generated Markdown serializer for human/LLM-readable output. Write-only, closed type system, deliberately constrained. Targets CLI output, documentation, and structured reports. At ~0.2.4, it's early-stage but architecturally sound.

---

## 2. Architecture Comparison

### Pipeline Design

| Generator | Pipeline |
|-----------|----------|
| **STJ** | `ForAttributeWithMetadataName` → Parser.ParseContextGenerationSpec() → ContextGenerationSpec → Emitter.Emit() |
| **Config.Binder** | `CreateSyntaxProvider` → Invocation Collection → Parser → SourceGenerationSpec → Emitter |
| **LoggerMessage** | `ForAttributeWithMetadataName` → Parser → LoggerClass/Method models → Emitter |
| **Options Validator** | `ForAttributeWithMetadataName` → Parser.GetValidatorTypes() → ValidatorType[] → Emitter |
| **Markout** | `CreateSyntaxProvider` → TypeParser.ParseContext() → ContextMetadata → SerializerEmitter |

**Markout's Current Approach:**

```csharp
// MarkoutSourceGenerator.cs
var contexts = context.SyntaxProvider
    .CreateSyntaxProvider(
        predicate: static (node, _) => IsClassWithAttributes(node),
        transform: static (ctx, ct) => TypeParser.ParseContext(ctx, ct))
    .Where(static m => m is not null)
    .Select(static (m, _) => m!);
```

**Recommendation:** Markout should migrate to `ForAttributeWithMetadataName` (available in Roslyn 4.4+) for its predicate. This API is more efficient because Roslyn handles the attribute filtering internally rather than visiting all `ClassDeclarationSyntax` nodes. STJ and the newer runtime generators use this pattern:

```csharp
// Recommended pattern from STJ
context.SyntaxProvider
    .ForAttributeWithMetadataName(
        "Markout.MarkoutContextAttribute",
        (node, _) => node is ClassDeclarationSyntax,
        (context, ct) => TypeParser.ParseContext(context, ct))
```

### Incremental Generation

All runtime generators are `IIncrementalGenerator` implementations, designed for keystroke-level responsiveness. The key challenge is ensuring model types have correct structural equality.

**STJ's Approach:**
- All model types are `sealed record` with `required` properties
- Uses custom `ImmutableEquatableArray<T>` for collections (wraps `ImmutableArray` with value equality)
- Uses `TypeRef` instead of `ITypeSymbol` to avoid Roslyn symbol comparison issues
- Every model type has explicit comments warning about equality requirements

**Markout's Current Approach:**
- Uses `sealed class` with manual `IEquatable<T>` implementations
- Equality checks are shallow (`FullTypeName == other.FullTypeName` for `TypeMetadata`)
- Collections are `IReadOnlyList<PropertyMetadata>` without custom equality

```csharp
// Current Markout TypeMetadata
public bool Equals(TypeMetadata? other)
{
    if (other is null) return false;
    if (ReferenceEquals(this, other)) return true;
    return FullTypeName == other.FullTypeName;  // Shallow!
}
```

**Recommendation:** The current equality implementation is incorrect for incremental caching. If properties change but the type name stays the same, the generator won't regenerate. Markout should either:
1. Use C# records (`sealed record TypeMetadata(...)`) for automatic deep equality
2. Implement proper deep equality including `Properties` collection comparison
3. Consider adopting STJ's `ImmutableEquatableArray<T>` pattern

### Model Design

**STJ Model Hierarchy:**

```
ContextGenerationSpec
├── TypeGenerationSpec (30+ properties)
│   ├── PropertyGenerationSpec (20+ properties)
│   ├── ParameterGenerationSpec
│   └── PropertyInitializerGenerationSpec
└── SourceGenerationOptionsSpec
```

**Markout Model Hierarchy:**

```
ContextMetadata
├── TypeMetadata (11 properties)
│   ├── PropertyMetadata (18 properties)
│   └── DiagnosticInfo
└── (no options spec)
```

Markout's model is appropriately simpler—it doesn't need STJ's complexity. However, it could benefit from:
- Separating `DiagnosticInfo` out of `TypeMetadata` (diagnostics shouldn't be part of the model's equality)
- Adding a dedicated `TypeRef` type for cross-references (currently uses `FullTypeName` string)

### Code Organization

**STJ Structure:**

```
gen/
├── JsonSourceGenerator.cs (entry point)
├── JsonSourceGenerator.Parser.cs (partial)
├── JsonSourceGenerator.Emitter.cs (partial)
├── Helpers/
│   ├── KnownTypeSymbols.cs
│   └── RoslynExtensions.cs
└── Model/
    ├── ContextGenerationSpec.cs
    ├── TypeGenerationSpec.cs
    └── PropertyGenerationSpec.cs
```

**Markout Structure:**

```
Markout.SourceGeneration/
├── MarkoutSourceGenerator.cs
├── DiagnosticDescriptors.cs
├── Parser/
│   ├── TypeParser.cs
│   ├── TypeMetadata.cs
│   └── ContextMetadata.cs
└── Emitter/
    └── SerializerEmitter.cs
```

Markout's structure is clean but could benefit from:
- A `Helpers/` directory for `KnownTypeSymbols` pattern (lazy resolution of `ITypeSymbol` references)
- Splitting `TypeMetadata.cs` into separate files for `PropertyMetadata`, `PropertyKind`, etc.

---

## 3. API Design Comparison

### User-Facing Configuration

| Aspect | STJ | Markout |
|--------|-----|---------|
| **Context Attribute** | `[JsonSerializable(typeof(T))]` | `[MarkoutContext(typeof(T))]` |
| **Type Attribute** | `[JsonSerializable]` (optional) | `[MarkoutSerializable]` (optional) |
| **Options Attribute** | `[JsonSourceGenerationOptions(...)]` | None |
| **Property Attributes** | `[JsonPropertyName]`, `[JsonIgnore]`, `[JsonInclude]`, `[JsonConstructor]`, `[JsonConverter]`, `[JsonNumberHandling]`, etc. | `[MarkoutPropertyName]`, `[MarkoutIgnore]`, `[MarkoutIgnoreInTable]`, `[MarkoutSection]`, `[MarkoutBoolFormat]`, `[MarkoutFormat]` |
| **Per-Type Override** | `TypeInfoPropertyName`, `GenerationMode` | None |

**Markout's Strengths:**
- Simpler attribute surface area (8 attributes vs STJ's 15+)
- Domain-specific attributes (`[MarkoutSection]`, `[MarkoutBoolFormat]`) are clear and purposeful
- `[MarkoutIgnoreInTable]` is a clever design-time constraint that surfaces Markdown limitations

**STJ's Strengths:**
- Per-type generation mode override (`Metadata`, `Serialization`, or both)
- `[JsonSourceGenerationOptions]` captures compile-time options
- `TypeInfoPropertyName` allows custom naming in the generated context

**Recommendation:** Consider adding a `[MarkoutContextOptions]` attribute for compile-time configuration:

```csharp
[MarkoutContextOptions(
    DefaultFieldLayout = FieldLayout.LineBreaks,
    BoldFieldNames = true)]
[MarkoutContext(typeof(Report))]
public partial class ReportContext : MarkoutSerializerContext { }
```

This would bake options into the generated code rather than requiring runtime `MarkoutWriterOptions`.

### Developer Experience

**Markout Advantages:**
1. **Single package**: `Markout` contains both runtime and generator (no separate analyzer package)
2. **Minimal ceremony**: Types don't require `[MarkoutSerializable]` if default behavior is acceptable
3. **Compile-time validation**: MARKOUT001–004 catch problematic patterns early
4. **Schema introspection**: `GetSchemaInfo<T>()` returns rendering metadata at runtime

**STJ Complexity Points Markout Avoids:**
1. No `partial` requirement on types (only context class)
2. No generation mode confusion (`Metadata` vs `Serialization`)
3. No custom converter pipeline complexity
4. No init-only setter issues
5. No multi-Roslyn-version maintenance burden

---

## 4. Type System & Serialization

### Type Support Comparison

| Type Category | STJ | Markout |
|---------------|-----|---------|
| **Primitives** | `string`, `bool`, all numeric (including `Int128`, `Half`), `char` | `string`, `bool`, `int`, `long`, `double`, `decimal` |
| **Date/Time** | `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan` | `DateTime`, `DateTimeOffset` |
| **Collections** | 25+ types (List, Array, Set, Queue, Stack, immutable variants, Memory, etc.) | `List<T>`, `IReadOnlyList<T>`, `IList<T>`, `T[]` |
| **Dictionaries** | Full support | **Not supported** (compile error MARKOUT003) |
| **Nullable** | `Nullable<T>` | `Nullable<T>` for value types |
| **Enums** | Full support with naming options | Not supported |
| **Polymorphism** | `[JsonDerivedType]` | Not supported |
| **Custom Types** | Via `JsonConverter<T>` | Not supported |

### STJ's Collection Resolution Complexity

STJ's `TryResolveCollectionType` is ~170 lines of carefully-ordered type checks:

```csharp
// STJ collection resolution priority
1. Memory<T> / ReadOnlyMemory<T>
2. IAsyncEnumerable<T>
3. T[]
4. KeyedCollection<,>
5. List<T>
6. Dictionary<K,V>
7. Immutable dictionaries
8. IDictionary<K,V>
9. IReadOnlyDictionary<K,V>
// ... 15+ more checks
```

**Markout's Simpler Approach:**

```csharp
// Markout collection detection
if (type is IArrayTypeSymbol) → Array handling
else if (namedType.AllInterfaces.Any(IDictionary)) → Error
else if (IEnumerable<T>) → List handling
```

**Recommendation:** Markout's simplicity is appropriate for its scope. However, consider:
- Adding `ICollection<T>` support for symmetry
- Supporting `ImmutableList<T>` and `ImmutableArray<T>` for functional patterns
- Adding enum support (Markdown can represent enums as strings naturally)

### Unsupported Type Handling

**STJ:**
- `ClassType.UnsupportedType` for known unsupported types (`System.Type`, `IntPtr`)
- `ClassType.TypeUnsupportedBySourceGen` for generator limitations (ref-like, error types)
- Falls back gracefully with diagnostics

**Markout:**
- Dictionary → Compile error (MARKOUT003) with helpful suggestion
- Other unsupported → `PropertyKind.Other`, silently skipped
- Table-incompatible properties → Warning (MARKOUT001, MARKOUT002)

**Recommendation:** Markout's approach of making dictionaries a compile error is correct—it forces users to transform data into a Markdown-friendly shape. Consider applying the same rigor to other unsupported patterns rather than silent skipping.

---

## 5. Generated Code Quality

### STJ Generated Code Patterns

**Per-Type Factory Method:**

```csharp
private JsonTypeInfo<MyPoco> Create_MyPoco(JsonSerializerOptions options)
{
    if (!TryGetTypeInfoForRuntimeCustomConverter<MyPoco>(options, out JsonTypeInfo<MyPoco> jsonTypeInfo))
    {
        // Type-specific metadata creation
    }
    jsonTypeInfo.OriginatingResolver = this;
    return jsonTypeInfo;
}
```

**Fast-Path Serialization:**

```csharp
private void MyPocoSerializeHandler(Utf8JsonWriter writer, MyPoco? value)
{
    if (value is null) { writer.WriteNullValue(); return; }
    writer.WriteStartObject();
    writer.WriteNumber(PropName_Id, value.Id);      // Direct Utf8JsonWriter call
    writer.WriteString(PropName_Name, value.Name);  // No boxing
    writer.WriteEndObject();
}
```

**Pre-encoded Property Names:**

```csharp
private static readonly JsonEncodedText PropName_Id = JsonEncodedText.Encode("id");
```

### Markout Generated Code Patterns

**TypeInfo Serialize Method:**

```csharp
public override void Serialize(MarkoutWriter writer, Package value)
{
    if (value == null) return;
    
    // Scalars use builder for nullable/string filtering
    var __fields = new List<MarkoutField>();
    if (!string.IsNullOrEmpty(value.Name))
        __fields.Add(new MarkoutField("Name", value.Name));
    // ...
    if (__fields.Count > 0)
        writer.WriteCompactFields(__fields);
    
    // Arrays
    if (value.Frameworks != null)
        writer.WriteArray("Frameworks", value.Frameworks);
}
```

**Context GetTypeInfo Dispatch:**

```csharp
public override MarkoutTypeInfo<T>? GetTypeInfo<T>()
{
    if (typeof(T) == typeof(Package))
        return (MarkoutTypeInfo<T>)(object)PackageMarkoutTypeInfo.Instance;
    // ...
    return null;
}
```

### Code Quality Analysis

**Markout Strengths:**
1. **Readable**: Generated code is straightforward and debuggable
2. **Direct writes**: Uses `MarkoutWriter` methods directly, no intermediate DOM
3. **Null handling**: Consistent pattern for nullable fields
4. **Clean structure**: Each type gets a simple `Serialize` method

**Areas for Improvement:**

1. **List allocation in OneLine layout:**

```csharp
// Current: allocates List<MarkoutField> for every call
var __fields = new List<MarkoutField>();
if (!string.IsNullOrEmpty(value.Name))
    __fields.Add(new MarkoutField("Name", value.Name));
```

**Recommendation:** For types where all scalars are non-nullable value types, emit direct `WriteCompactFields(params ReadOnlySpan<MarkoutField>)` calls:

```csharp
// Optimized path for all-non-nullable scalars
writer.WriteCompactFields(
    new MarkoutField("Count", value.Count.ToString(CultureInfo.InvariantCulture)),
    new MarkoutField("Price", value.Price.ToString(CultureInfo.InvariantCulture)));
```

1. **No pre-formatted strings:**
STJ pre-encodes property names as `JsonEncodedText`. Markout could cache display names as `const string`:

```csharp
private const string DisplayName_Count = "Count";
// Use: writer.WriteField(DisplayName_Count, value.Count);
```

1. **Heading level arithmetic in generated code:**

```csharp
sb.AppendLine($"    writer.WriteHeading({effectiveSectionLevel}, ...);");
```

Consider computing heading levels at generation time rather than embedding arithmetic. Note: this is closely related to section hiding/inclusion—when sections are excluded, heading levels of remaining sections may need to be recalculated to avoid gaps in the heading hierarchy.

---

## 6. Diagnostics & Developer Experience

### Diagnostic Coverage Comparison

| ID | STJ | Markout |
|----|-----|---------|
| **Error** | Multiple constructors, multiple extension data, invalid extension data type, unsupported language version | Dictionary property (MARKOUT003) |
| **Warning** | Type not supported, context not partial, inaccessible members, polymorphism in fast-path | Property in table context (MARKOUT001, MARKOUT002), RenderScalars with no content (MARKOUT004) |
| **Count** | 15 diagnostics | 4 diagnostics |

### STJ Diagnostic Patterns

```csharp
// STJ: Diagnostic with fallback location
private void ReportDiagnostic(DiagnosticDescriptor descriptor, ISymbol symbol, params object[] args)
{
    Location location = symbol.Locations.FirstOrDefault() ?? _contextClassLocation;
    // Falls back to context class if type is from external assembly
}
```

### Markout Diagnostic Patterns

```csharp
// Markout: Diagnostic collection in model
diagnostics.Add(new DiagnosticInfo(
    DiagnosticDescriptors.UnsupportedPropertyInTable,
    prop.Locations.FirstOrDefault(),
    prop.Name, prop.ContainingType.Name, GetKindDisplayName(kind)));
```

**Recommendations:**

1. **Add more diagnostics:**
   - Enum properties (currently silently unsupported)
   - Deeply nested structures that will produce unwieldy Markdown
   - Missing `[MarkoutSection]` on collection properties in document context
   - Circular type references (if ever supported)

2. **Location fallback:** Add fallback to context class location for properties from external assemblies

3. **Diagnostic suppression:** Consider adding a `MarkoutDiagnosticSuppressor` for common false positives

---

## 7. Extensibility & Customization

### STJ Extensibility Model

1. **Design-time converters**: `[JsonConverter(typeof(MyConverter))]`
2. **Runtime converters**: `JsonSerializerOptions.Converters`
3. **Custom naming policies**: `JsonNamingPolicy` subclasses
4. **Lifecycle callbacks**: `IJsonOnSerializing`, `IJsonOnSerialized`

### Markout Extensibility Model

1. **View model projection**: The primary pattern—transform data before serialization
2. **MarkoutField collections**: Dynamic key-value data
3. **TreeNode**: Hierarchical structures
4. **MarkoutWriter direct usage**: Complete control for edge cases
5. **Section filtering**: Include/exclude sections at runtime

### Current Limitations

Markout has no equivalent to `JsonConverter<T>`. This is intentional—the closed type system ensures predictable Markdown output. However, some patterns are awkward:

```csharp
// User has a TimeSpan property
public class Report
{
    public TimeSpan Duration { get; set; }  // ← Not supported
}

// Current workaround: view model projection
public class ReportView
{
    [MarkoutFormat("hh\\:mm\\:ss")]
    public string Duration { get; set; }  // ← String with manual formatting
}
```

**Recommendation:** Consider a lightweight formatter interface for custom scalar formatting:

```csharp
// Proposed: IMarkoutFormattable for custom types
public interface IMarkoutFormattable
{
    string FormatForMarkout();
}

// Then TimeSpan wrapper:
public readonly struct MarkoutTimeSpan(TimeSpan value) : IMarkoutFormattable
{
    public string FormatForMarkout() => value.ToString(@"hh\:mm\:ss");
}
```

This would preserve the closed type system while allowing specific extension points.

---

## 8. Performance

### Generator Performance

**STJ Optimizations:**
1. **Incremental caching**: Full model hierarchy with structural equality
2. **Lazy symbol resolution**: `KnownTypeSymbols` with `Option<T>` pattern
3. **BFS type graph walk**: Queue-based traversal, not recursive
4. **Culture invariant emission**: Avoids locale-specific character issues

**Markout Current State:**
1. Incremental caching with shallow equality (may over-regenerate)
2. No `KnownTypeSymbols` pattern (resolves types inline)
3. Recursive property parsing (fine for typical depths)

**Recommendations:**
1. Fix model equality for proper incremental caching
2. Add `KnownTypeSymbols` if Markout grows to handle more BCL types
3. Consider `WithTrackingName()` for pipeline debugging (STJ 4.4 pattern)

### Runtime Serialization Performance

**STJ Fast-Path:**
- Direct `Utf8JsonWriter` calls
- Pre-encoded property names (`JsonEncodedText`)
- Index-based collection iteration
- Instance method delegates for JIT optimization

**Markout Current State:**
- Direct `MarkoutWriter` calls ✓
- `ISpanFormattable.TryFormat()` with `stackalloc` buffer ✓
- `List<MarkoutField>` allocation for nullable scalars ✗
- `foreach` for collections ✓

**Recommendations:**

1. **Eliminate List allocation for simple cases:**

```csharp
// If all scalars are non-nullable non-string, use params overload
writer.WriteCompactFields(
    new MarkoutField("Id", value.Id),
    new MarkoutField("Count", value.Count));
```

1. **Pre-compute display names as constants**

2. **Consider index-based iteration for known collection types**

---

## 9. Key Differentiators

### What Makes Markout Unique

1. **Markdown as serialization target**: Novel approach for structured CLI output
2. **Compile-time design validation**: MARKOUT001–004 catch Markdown-incompatible patterns
3. **View model philosophy**: Actively promotes data transformation over raw dumping
4. **Section filtering**: Built-in verbosity control via include/exclude
5. **Schema introspection**: `GetSchemaInfo<T>()` describes rendering structure
6. **Tree rendering**: First-class support for hierarchical data with box-drawing
7. **Write-only simplicity**: No deserialization complexity

### Advantages of Being Newer

1. **Can target .NET 10 only**: No multi-TFM or multi-Roslyn complexity
2. **Can adopt modern C# features**: `required`, `file`, `ReadOnlySpan<T>` params
3. **Smaller surface area**: Easier to maintain, document, and test
4. **Opinionated design**: Can make breaking decisions without legacy concerns

### Lessons from STJ's Mistakes/Complexity

1. **Don't over-generalize early**: STJ's 25 collection types are a maintenance burden
2. **Generation modes are confusing**: Avoid `Metadata` vs `Serialization` splits
3. **Multi-Roslyn support is expensive**: Stick to recent Roslyn if possible
4. **Init-only setters are problematic**: Markout avoids this by being write-only
5. **Fast-path exceptions are complex**: STJ has many "fast-path disabled" conditions

---

## 10. Concrete Recommendations for Markout

### High Priority

#### 1. Fix Incremental Caching Equality

**Problem:** `TypeMetadata.Equals()` only compares `FullTypeName`, ignoring property changes.

**Action:** Convert to records or implement deep equality:

```csharp
public sealed record TypeMetadata(
    string Namespace,
    string TypeName,
    // ... other properties
    ImmutableEquatableArray<PropertyMetadata> Properties);
```

#### 2. Migrate to ForAttributeWithMetadataName

**Problem:** Current `CreateSyntaxProvider` visits all class declarations.

**Action:**

```csharp
context.SyntaxProvider
    .ForAttributeWithMetadataName(
        "Markout.MarkoutContextAttribute",
        static (node, _) => node is ClassDeclarationSyntax,
        static (context, ct) => TypeParser.ParseContext(context, ct))
```

#### 3. Add Enum Support

**Problem:** Enums are common in view models but unsupported.

**Action:**
- Add `PropertyKind.Enum`
- Generate `.ToString()` calls (or format string support)
- Consider `[MarkoutEnumFormat]` for custom display

### Medium Priority

#### 4. Add Compile-Time Options Attribute

**Pattern from STJ:** `[JsonSourceGenerationOptions]`

**Proposal:**

```csharp
[MarkoutContextOptions(
    DefaultFieldLayout = FieldLayout.LineBreaks,
    BoldFieldNames = true,
    IncludeIcons = true)]
[MarkoutContext(typeof(Report))]
public partial class ReportContext : MarkoutSerializerContext { }
```

This bakes options into generated code, avoiding runtime options objects.

#### 5. Adopt Dual-Strategy Pattern (from LoggerMessage)

**Pattern:** LoggerMessage uses `LoggerMessage.Define<>()` for simple cases, custom structs for complex cases.

**Application to Markout:**
- **Simple types** (all non-nullable scalars, no sections): Emit direct `WriteCompactFields(params ...)` call
- **Complex types** (nullable, sections, nested): Use current List builder pattern

#### 6. Multi-File Emission (from STJ)

**Current:** Markout emits one file per type + one context file.

**STJ Pattern:** Also emits `PropertyNames.g.cs` for shared constants.

**Recommendation:** For large contexts, consider:
- `{Context}.PropertyNames.g.cs` for display name constants
- `{Context}.SectionNames.g.cs` for section constants (already partially done)

#### 7. Add KnownTypeSymbols Pattern

**Pattern from STJ/Config.Binder:**

```csharp
internal sealed class KnownTypeSymbols
{
    private Option<INamedTypeSymbol?> _markoutField;
    public INamedTypeSymbol? MarkoutField => _markoutField.Value ?? 
        (_markoutField = compilation.GetTypeByMetadataName("Markout.MarkoutField")).Value;
}
```

This caches symbol lookups for better generator performance.

### Lower Priority

#### 8. Consider IMarkoutFormattable Interface

For custom scalar formatting without breaking the closed type system:

```csharp
public interface IMarkoutFormattable
{
    string FormatForMarkout();
}
```

Detect at generation time; emit `.FormatForMarkout()` call.

#### 9. Add Diagnostic Suppressor

Pattern from Config.Binder's `ConfigurationBindingGenerator.Suppressor.cs`:
- Suppress NRT warnings in generated code
- Suppress CA warnings for generated patterns

#### 10. Pre-computed Display Names

**Current:**

```csharp
writer.WriteField("Display Name", value.Name);  // String literal each time
```

**Better:**

```csharp
// In generated context
private const string Field_Name = "Display Name";

// In serialize method
writer.WriteField(Field_Name, value.Name);
```

### Architecture Recommendations Summary

| Area | Current | Recommended |
|------|---------|-------------|
| **Syntax Provider** | `CreateSyntaxProvider` | `ForAttributeWithMetadataName` |
| **Model Equality** | Shallow (broken) | Records or deep equality |
| **Symbol Caching** | Inline resolution | `KnownTypeSymbols` pattern |
| **Scalar Emission** | Always List builder | Dual-path (simple vs complex) |
| **Display Names** | String literals | Generated constants |
| **Options** | Runtime only | Compile-time attribute |
| **Enum Support** | None | Add `PropertyKind.Enum` |

### What NOT to Do (Lessons from STJ)

1. **Don't add 25 collection types**: Stick to `List<T>`, `T[]`, `IReadOnlyList<T>`. Users can project to these.

2. **Don't add generation modes**: STJ's `Metadata`/`Serialization` split adds complexity without clear value for Markout.

3. **Don't add polymorphism support**: Markout's view model philosophy is the answer—transform before serialize.

4. **Don't add multi-Roslyn support**: Target Roslyn 4.4+ and avoid the maintenance burden.

5. **Don't add custom converters (yet)**: The closed type system is a feature. If needed, add `IMarkoutFormattable` for targeted extension.

---

## 11. LibraryImport Source Generator Analysis

The LibraryImport source generator (`LibraryImportGenerator`) transforms `[LibraryImport]`-decorated methods into fully-realized P/Invoke stubs with marshalling code. It represents one of the most sophisticated source generators in the .NET runtime, with patterns directly applicable to Markout.

### Architecture

LibraryImport is split into two projects:

| Project | Purpose |
|---------|---------|
| **LibraryImportGenerator** | Generator-specific entry point, diagnostic descriptors, forwarder logic |
| **Microsoft.Interop.SourceGeneration** | Shared infrastructure reused by LibraryImport, ComInterface, and JSImport generators |

The shared infrastructure pattern is notable: three separate generators share a common code generation pipeline. This could inform how Markout might share code with future generators targeting different output formats.

### The Symbol-to-Model Barrier

LibraryImport's most important pattern is its strict **symbol-to-model barrier**. The `CalculateStubInformation` step is the last point that touches `ISymbol`/Compilation; everything after operates on pure record types:

```csharp
record IncrementalStubGenerationContext(
    SignatureContext SignatureContext,
    ContainingSyntaxContext ContainingSyntaxContext,
    ContainingSyntax StubMethodSyntaxTemplate,
    SequenceEqualImmutableArray<AttributeSyntax> ForwardedAttributes,
    LibraryImportData LibraryImportData,
    // ... all value-comparable types, no ISymbol references
);
```

This enables effective incremental caching—if the model hasn't changed, code generation is skipped entirely. Markout's current `TypeMetadata.Equals()` compares only `FullTypeName`, violating this principle.

### ManagedTypeInfo Hierarchy (Type Classification)

LibraryImport classifies managed types into a discriminated-union-style hierarchy:

```
ManagedTypeInfo(FullTypeName, DiagnosticFormattedName)
├── SpecialTypeInfo(SpecialType)       — int, string, bool, void
├── EnumTypeInfo(UnderlyingType)       — enum types
├── PointerTypeInfo(IsFunctionPointer) — T* and delegate*
├── SzArrayType(ElementTypeInfo)       — T[]
├── ValueTypeInfo(IsByRefLike)         — structs
└── ReferenceTypeInfo                  — classes
```

This hierarchy is **ISymbol-free**—it captures just enough information to drive code generation decisions without holding symbol references. Markout's `PropertyKind` enum serves a similar purpose but is less structured.

**Recommendation:** Consider adopting a richer type classification hierarchy in Markout, perhaps:

```csharp
abstract record MarkoutTypeInfo(string FullTypeName)
{
    public record Scalar(string FullTypeName, ScalarKind Kind) : MarkoutTypeInfo(FullTypeName);
    public record Collection(string FullTypeName, MarkoutTypeInfo ElementType) : MarkoutTypeInfo(FullTypeName);
    public record FieldCollection(string FullTypeName) : MarkoutTypeInfo(FullTypeName);
    public record TreeNode(string FullTypeName) : MarkoutTypeInfo(FullTypeName);
    public record ComplexObject(string FullTypeName, ImmutableEquatableArray<PropertyMetadata> Properties) : MarkoutTypeInfo(FullTypeName);
}
```

### MarshallerShape System (Strategy Resolution)

The `MarshallerShape` flags enum describes what operations a custom marshaller supports:

```csharp
[Flags]
enum MarshallerShape
{
    ToUnmanaged               = 0x1,   // Can convert managed → native
    CallerAllocatedBuffer     = 0x2,   // Supports stack-allocated buffer
    ToManaged                 = 0x10,  // Can convert native → managed
    Free                      = 0x40,  // Supports Free()
    // ...
}
```

This is used by `ShapeMemberNames` to define method signatures marshallers must implement. The pattern is:

1. **Analyze capabilities** → What can this type do?
2. **Select strategy** → Based on capabilities, which code generation path?
3. **Emit code** → Generate the appropriate method calls

**Application to Markout:** Consider a `PropertyRenderingShape` that describes what operations a property type supports:

```csharp
[Flags]
enum PropertyRenderingShape
{
    ScalarField           = 0x01,  // Can render as key: value
    TableCell             = 0x02,  // Can render in table column
    TableRows             = 0x04,  // Can render as table (collection of scalars)
    Section               = 0x08,  // Can render as H2+ section
    CompactField          = 0x10,  // Can render in pipe-separated line
    TreeRenderable        = 0x20,  // Can render as tree structure
    DynamicFields         = 0x40,  // Is List<MarkoutField>
}
```

This would formalize the implicit decisions currently scattered through `SerializerEmitter`.

### Stage-Based Code Generation

Code generation is organized into **11 sequential stages** (Setup, Marshal, Pin, Invoke, Unmarshal, etc.). Each `IBoundMarshallingGenerator.Generate()` is called once per stage and returns statements for that stage.

This pattern keeps code manageable—each generator only needs to know about its own stage behavior. The stages are then assembled into the final method body by `ManagedToNativeStubGenerator.GenerateStubBody()`.

**Application to Markout:** Consider organizing emission into explicit stages:

```
Stage 1: NullCheck         → if (value == null) return;
Stage 2: Title             → writer.WriteHeading(1, ...)
Stage 3: Description       → writer.WriteParagraph(...)
Stage 4: CompactFields     → writer.WriteCompactFields(...)
Stage 5: ScalarFields      → writer.WriteField(...) per property
Stage 6: Sections          → writer.WriteHeading(2, ...) per section
Stage 7: Collections       → tables, arrays, trees
Stage 8: NestedObjects     → recursive type emission
```

Each property kind emitter would implement only the stages it participates in.

### Composite Resolver Chain (Chain of Responsibility)

Marshalling generators are resolved through a chain of `IMarshallingGeneratorResolver` implementations:

```
CompositeMarshallingGeneratorResolver
├── BlittableMarshallerResolver
├── MarshalAsMarshallingGeneratorResolver
├── AttributedMarshallingModelGeneratorResolver
├── CharMarshallingGeneratorResolver
└── NotSupportedResolver (fallback)
```

Each resolver returns `ResolvedGenerator.Resolved(generator)` if it handles the type, or `UnresolvedGenerator` to pass to the next resolver. This avoids massive switch statements and is extensible.

**Application to Markout:** Consider a resolver chain for property rendering:

```csharp
interface IPropertyRendererResolver
{
    PropertyRenderer? TryResolve(PropertyMetadata property, RenderContext context);
}

// Chain: ScalarResolver → CollectionResolver → NestedObjectResolver → UnsupportedResolver
```

### Key Patterns for Markout

| LibraryImport Pattern | Markout Application |
|-----------------------|---------------------|
| Symbol-to-model barrier | Fix `TypeMetadata` equality to compare all fields |
| `SequenceEqualImmutableArray<T>` | Adopt for property collections in models |
| ManagedTypeInfo hierarchy | Create `MarkoutTypeInfo` discriminated union |
| MarshallerShape flags | Create `PropertyRenderingShape` flags |
| Stage-based generation | Organize `SerializerEmitter` into explicit stages |
| Composite resolver chain | Replace switch statements with resolver chain |
| `ContainingSyntaxContext` | Directly reusable for namespace/type nesting |
| `DiagnosticOr<T>` | Adopt for clean validation in the pipeline |
| Single concatenated output | Consider consolidating generated files |

---

## 12. Regex Source Generator Analysis

The `RegexGenerator` compiles regex patterns into highly-optimized C# code at compile time. It represents the state-of-the-art in .NET source generation for performance-critical scenarios.

### Architecture & Pipeline

The generator uses a three-stage pipeline:

1. **Syntax Discovery** — `ForAttributeWithMetadataName("GeneratedRegexAttribute")` finds decorated methods/properties
2. **Regex Parsing** — `RegexParser.Parse()` builds a `RegexTree`, then `RegexTreeAnalyzer.Analyze()` computes structural properties
3. **Code Generation** — `EmitRegexDerivedTypeRunnerFactory()` generates optimized matching code

The generated output is a `Regex`-derived class with a `Runner` that overrides `Scan(ReadOnlySpan<char>)`.

### Code Emission Strategy: Direct Recursive Generation

Unlike a state machine approach, RegexGenerator directly walks the `RegexNode` tree and emits imperative C# code for each node. The core `EmitNode()` method is a recursive dispatcher that handles every `RegexNodeKind` and delegates to specialized emitters.

This produces **readable, debuggable C#** that the JIT can optimize aggressively. The generated code is essentially what a skilled developer would write by hand.

**Application to Markout:** The `SerializerEmitter.EmitSerializeMethod()` already follows this pattern. Consider making it more explicit with specialized emitters:

```csharp
void EmitPropertyValue(PropertyMetadata property, StringBuilder sb)
{
    switch (property.Kind)
    {
        case PropertyKind.String: EmitStringProperty(property, sb); break;
        case PropertyKind.Numeric: EmitNumericProperty(property, sb); break;
        case PropertyKind.Boolean: EmitBooleanProperty(property, sb); break;
        case PropertyKind.Collection: EmitCollectionProperty(property, sb); break;
        // ...
    }
}
```

### Static Position Tracking

A critical optimization is the `sliceStaticPos` variable. For fixed-length constructs, the generator tracks position at compile time instead of repeatedly incrementing a runtime variable:

```csharp
// Instead of:
if (slice[pos] != 'a') goto NoMatch; pos++;
if (slice[pos] != 'b') goto NoMatch; pos++;

// Generated:
if (slice[0] != 'a') goto NoMatch;
if (slice[1] != 'b') goto NoMatch;
// Transfer static position to runtime variable only when needed
```

**Application to Markout:** While Markout doesn't have position tracking per se, the principle applies: compute as much as possible at generation time. For example, heading levels could be pre-computed rather than using runtime arithmetic.

### Multi-Level Character Class Optimization

`MatchCharacterClass()` applies optimizations in priority order:

1. Built-in shortcuts (`\d` → `char.IsDigit()`)
2. Single range (`[a-z]` → `char.IsBetween()`)
3. Unicode categories → `char.GetUnicodeCategory()` switch
4. 2-3 character sets → direct comparison with bit tricks
5. Narrow range ≤32 → branchless uint bitmap
6. Narrow range ≤64 → branchless ulong bitmap
7. ASCII lookup → 128-bit lookup table
8. General fallback

**Lesson for Markout:** When emitting code for different property types, consider a cascade of specializations. For example, boolean formatting:

```csharp
// Tier 1: [MarkoutBoolFormat] with custom strings
// Tier 2: Default yes/no with optimized literal
// Tier 3: General IFormattable path
```

### Span-Based APIs and IndexOf Family

The generator aggressively converts character searches into `Span<char>.IndexOf` family calls:

- `IndexOf(char)` for single characters
- `IndexOfAny(SearchValues<char>)` for larger sets (uses SIMD internally)
- `IndexOfAnyExcept(char)` for complement sets

**Application to Markout:** The `MarkoutWriter` already uses `stackalloc` for numeric formatting. Consider extending this pattern:

```csharp
// For known-length output, use stackalloc + direct writing
Span<char> buffer = stackalloc char[256];
int written = 0;
"Name: ".AsSpan().CopyTo(buffer[written..]);
written += 6;
value.Name.AsSpan().CopyTo(buffer[written..]);
// Write entire span at once
```

### Helper Method Deduplication

The generator uses a `Dictionary<string, string[]> requiredHelpers` pattern: each time a helper is needed, it checks if already registered and adds if not. Helpers are emitted once in a shared `file static class Utilities`.

**Application to Markout:** Consider emitting shared helpers for common patterns:

```csharp
// Generated once in context file:
file static class MarkoutHelpers
{
    public static void WriteNonEmptyField(MarkoutWriter writer, string name, string? value)
    {
        if (!string.IsNullOrEmpty(value))
            writer.WriteField(name, value);
    }
}
```

### Local Functions as Emitters

The massive `EmitTryMatchAtCurrentPosition` method uses C# local functions as emitters, closing over shared state (`doneLabel`, `sliceStaticPos`, `writer`). This avoids passing dozens of parameters while keeping code organized.

**Application to Markout:** The current `SerializerEmitter` uses methods with many parameters. Consider local functions for complex emission scenarios.

### Additional Declarations Pattern

Emitters discover needed variable declarations partway through generation. Rather than pre-declaring everything, a `HashSet<string> additionalDeclarations` collects needs, then `InsertAdditionalDeclarations()` patches them in.

**Application to Markout:** Useful if Markout ever needs conditional local variables (e.g., only declare `__fields` if nullable properties exist).

### Fallback Strategy

When full code generation isn't possible (e.g., case-insensitive backreferences), the generator emits a simple caching wrapper:

```csharp
internal static readonly Regex Instance = new Regex(pattern, options);
```

This graceful degradation maintains functionality while reporting an informational diagnostic (SYSLIB1044).

**Application to Markout:** Consider a fallback for types that can't be fully source-generated:

```csharp
// MARKOUT005: Type has conditional rendering that cannot be source-generated
// Generated fallback delegates to MarkoutWriter directly
public override void Serialize(MarkoutWriter writer, ComplexType value)
{
    // Call a user-provided method
    value.WriteMarkout(writer);
}
```

### Key Patterns for Markout

| Regex Pattern | Markout Application |
|---------------|---------------------|
| Direct recursive emission | Already used; formalize with specialized emitters |
| Multi-level optimization cascade | Apply to boolean formatting, collection rendering |
| Span-based processing | Extend `MarkoutWriter` span usage |
| Helper deduplication | Emit shared `file static` helpers |
| Local functions for complex emission | Use in `SerializerEmitter` |
| Additional declarations pattern | For conditional local variables |
| Graceful fallback with diagnostic | For types that need imperative rendering |
| Golden output tests | Add snapshot tests for generated code |

---

## 13. dotnet-inspect — Real-World Markout Consumer

**dotnet-inspect** is a .NET CLI tool for inspecting assemblies and NuGet packages. It's the original consumer Markout was built for, making it invaluable for validating design decisions against real-world usage.

### Overview

The tool uses Markout in two distinct modes:

| Mode | Usage | Commands |
|------|-------|----------|
| **Source Generation** | `MarkoutSerializerContext` with 11 registered types | package, type, file tree |
| **Direct MarkoutWriter** | Imperative `MarkoutWriter` calls | api, diff, find, implements, extensions, platform, samples, assembly audit |

**Critical finding:** Roughly **80% of Markout usage is direct `MarkoutWriter`**, only **20% is source-generated**. This ratio reveals the current generator model covers only a narrow "sweet spot" of static, unconditional rendering.

### MarkoutContext Definition

```csharp
// src/dotnet-inspect/MarkoutContext.cs
[MarkoutContext(typeof(InspectionResult))]
[MarkoutContext(typeof(AssemblyAudit))]
[MarkoutContext(typeof(AssemblyInfo))]
[MarkoutContext(typeof(ApiSurface))]
[MarkoutContext(typeof(ApiType))]
[MarkoutContext(typeof(ApiMember))]
// ... 11 types total
public partial class MarkoutContext : MarkoutSerializerContext { }
```

However, several registered types (`ApiSurface`, `ApiType`, `ApiMember`) are **never serialized through the context**—they're rendered imperatively. Their Markout attributes are aspirational, showing what the output *would* look like if source generation could handle their conditional rendering needs.

### View Model Patterns

**InspectionResult** (the primary view model, ~470 lines) demonstrates sophisticated patterns:

#### 1. Dual-Representation Properties

```csharp
// Data property (for JSON)
public long? TotalDownloads { get; set; }

// Display property (for Markout)
[MarkoutPropertyName("Downloads")]
[JsonIgnore]
public string? DownloadsDisplay => TotalDownloads.HasValue 
    ? FormatDownloads(TotalDownloads.Value) : null;
```

This pattern repeats **12 times** for: Published, TotalDownloads, VersionDownloads, PackageSize, Owners, TargetFrameworks, SupportedRids, Vulnerabilities, PackageTypes, ContentDirectories, ToolCommands, NativeFiles.

**Pain point:** A `[MarkoutFormat]` attribute supporting custom formatting would eliminate most of these.

#### 2. Collection-to-Summary Properties

```csharp
[MarkoutIgnoreInTable]
public List<string>? TargetFrameworks { get; set; }

[MarkoutPropertyName("Target Frameworks")]
[JsonIgnore]
public string? TargetFrameworksSummary => TargetFrameworks is { Count: > 0 }
    ? string.Join(", ", TargetFrameworks) : null;
```

**Pain point:** A `[MarkoutJoin(", ")]` attribute for `List<string>` would eliminate ~8 companion properties.

#### 3. List<MarkoutField> for Dynamic Field Groups

```csharp
[MarkoutSection(Name = "Metadata")]
[JsonIgnore]
public List<MarkoutField> Metadata => GetMetadataFields();

private List<MarkoutField> GetMetadataFields()
{
    var fields = new List<MarkoutField>();
    if (!string.IsNullOrWhiteSpace(Authors))
        fields.Add(new("Authors", Authors));
    if (!string.IsNullOrWhiteSpace(License))
        fields.Add(new("License", License));
    // ... 30+ conditional field additions
    return fields;
}
```

This is the view model's way of expressing **conditional rendering** while still using source generation. It works but is verbose.

**Pain point:** A `[MarkoutSkipWhenNull]` attribute or automatic null-skipping would reduce boilerplate.

#### 4. RenderScalars = false

```csharp
[MarkoutSerializable(
    TitleProperty = nameof(PackageName), 
    DescriptionProperty = nameof(Description),
    RenderScalars = false)]  // ← Suppresses all scalar properties
public class InspectionResult { ... }
```

The view model uses `RenderScalars = false` because scalar properties exist for JSON but are consumed by `GetMetadataFields()` for Markout. Without this, the serializer would render duplicates.

**Pain point:** This is a workaround, not intentional design. Consider a more explicit approach.

### Type Usage Patterns

| Type Category | Examples | Notes |
|---------------|----------|-------|
| `string` / `string?` | PackageName, Description, Authors | Most common — ubiquitous |
| `int` / `int?` | AssemblyCount, VersionCount | Counts, sometimes optional |
| `long?` | TotalDownloads, PackageSize | Large numbers, need formatting |
| `bool` / `bool?` | IsSigned, IsVerified | Flags; tri-state for unknown |
| `DateTimeOffset?` | Published | **Manually formatted to string** |
| `List<string>` | TargetFrameworks, Owners | **Manually joined** |
| `List<T>` | DependencyGroups, AssemblyAudits | Complex collections → sections |
| `List<MarkoutField>` | Summary, Metadata, Statistics | Dynamic field collections |
| `List<TreeNode>` | Members, Files | Tree structures |

**Missing type support visible in usage:**
- **Enum**: `Verbosity` enum used in options but no enum properties in view models (would need ToString())
- **TimeSpan**: Not used, but would be awkward without formatting support
- **Custom formatting**: Dates, numbers, and collections all require manual formatting

### Why Commands Use MarkoutWriter Directly

The `OutputFormatter.RenderAssemblyMarkdown()` method (~150 lines) is instructive. `AssemblyAudit` **IS registered** in the `MarkoutContext` but is rendered manually because:

1. **Conditional table rows**: Add "Builder" row only if non-null
2. **Conditional sections**: PDB section, Source Coverage section
3. **Complex computed values**: SourceLink status with explanation
4. **Conditional notes**: Windows PDB warnings
5. **Mixed layouts**: Field + table + paragraph within sections

```csharp
// From OutputFormatter.cs - conditional row example
var auditRows = new List<string[]>
{
    new[] { "Deterministic", audit.IsDeterministic ? "✓" : "✗" },
    new[] { "Reproducible Flag", audit.HasReproducibleFlag ? "✓" : "✗" },
    new[] { "SourceLink", audit.SourceLinkStatus ?? "Unknown" }
};
if (!string.IsNullOrEmpty(audit.Builder))
{
    auditRows.Add(new[] { "Builder", audit.Builder });  // ← Conditional row
}
```

This pattern cannot be expressed with current Markout attributes.

### MarkoutWriter API Surface Used

| Method | Usage Count | Description |
|--------|-------------|-------------|
| `WriteHeading(level, text)` | ~40 calls | Section headers |
| `WriteTable(headers, rows)` | ~25 calls | Tables with string[][] |
| `WriteField(name, value)` | ~35 calls | Key-value pairs |
| `WriteParagraph(text)` | ~25 calls | Notes, explanations |
| `WriteListItem(text)` | ~15 calls | Bullet items |
| `WriteArray(label, items)` | ~5 calls | Lists of strings |
| `WriteTree(nodes)` | 1 call | Assembly reference tree |
| `WriteCompactFields(fields)` | 1 call | Pipe-separated display |
| `WriteCodeBlockStart/End` | 2 calls | Code samples |

The most common operations are conditional (`if (value != null)`) writes of fields and table rows—precisely what the source generator cannot express.

### Section Filtering in Practice

The package command actively uses section filtering for verbosity control:

```csharp
var context = new MarkoutContext(new MarkoutWriterOptions
{
    IncludeSections = options.IncludeSections,
    ExcludeSections = GetExcludeSections(options),
    IncludeDescription = options.Verbosity != Verbosity.Quiet
});
```

Verbosity maps to section exclusions:
- **Quiet**: Title + compact line only
- **Minimal**: Title + description + compact line
- **Normal**: Metadata section only
- **Detailed**: All sections

This works well. The `--discover` flag lists available sections for programmatic use.

### TreeNode Usage

`TreeNode` is used effectively in 4 contexts:

1. Assembly reference trees
2. File trees (package command)
3. Type shape output (type command)
4. Assembly reference trees with icons

```csharp
var icon = node.ResolvedFrom switch
{
    "local" => "📁",
    "platform" => "🚢",
    _ => "❓"
};
result.Add(new TreeNode($"{node.Name} {node.Version}{suffix}", icon));
```

The icon support (`new TreeNode(label, icon)`) is used and works well.

### What Works Well

| Feature | Verdict |
|---------|---------|
| `[MarkoutSerializable]` with TitleProperty/DescriptionProperty | ✅ Clean, declarative |
| `[MarkoutSection]` for organizing output | ✅ Used for verbosity control |
| `[MarkoutBoolFormat]` for boolean display | ✅ Works well on `AssemblyAudit` |
| `[MarkoutPropertyName]` for renaming | ✅ Ubiquitous |
| `[MarkoutIgnore]` / `[MarkoutIgnoreInTable]` | ✅ Essential for dual-property pattern |
| Section filtering | ✅ Core to verbosity model |
| `List<MarkoutField>` for dynamic fields | ⚠️ Works but verbose |
| `TreeNode` for hierarchical data | ✅ Used in 4 contexts |
| `MarkoutWriter` as escape hatch | ⚠️ Too often needed |

### What's Missing (Pain Points from Real Usage)

| Gap | Impact | Evidence |
|-----|--------|----------|
| **Format strings on properties** | 12 dual-property patterns | `Published` → `PublishedDisplay`, etc. |
| **Collection joining** | 8 companion properties | `TargetFrameworks` → `TargetFrameworksSummary` |
| **Null/empty suppression** | 40+ conditional field builds | `GetMetadataFields()` is 40 lines of conditionals |
| **Conditional table rows** | Manual AssemblyAudit render | 150 lines of MarkoutWriter code |
| **Enum support** | No enums in view models | `Verbosity` enum avoided in serializable types |
| **Verbosity-aware properties** | Section filtering workaround | Properties exist at all verbosity levels |

### Recommendations Validated by dotnet-inspect

Based on real-world usage, the following recommendations are **elevated in priority**:

1. **High Priority (validated as blocking)**:
   - Add `[MarkoutFormat("format")]` for dates and numbers → eliminates 12 dual-property patterns
   - Add `[MarkoutJoin(", ")]` for `List<string>` → eliminates 8 companion properties
   - Add automatic null/empty skipping for fields → eliminates `List<MarkoutField>` workaround

2. **Medium Priority (would help)**:
   - Add enum support → currently avoided in view models
   - Add conditional field/row expressions → would allow source-generating `AssemblyAudit`

3. **Lower Priority (nice to have)**:
   - Add verbosity-aware property visibility
   - Add `IMarkoutRenderable` interface for custom types that provide their own rendering

### Implications for Markout Development

The 80/20 split (MarkoutWriter vs source generation) is concerning. It suggests:

1. **The current generator model is too rigid** — it handles only "static" output shapes where all properties always render.

2. **Conditional rendering is the missing feature** — the majority of MarkoutWriter usage is conditional (`if (x != null)`).

3. **The view model philosophy is correct but needs tooling** — dotnet-inspect actively uses view models, but building them requires excessive boilerplate.

4. **The `List<MarkoutField>` pattern is a success** — it's the current way to express conditional fields, and it works. Consider embracing this pattern more fully rather than trying to make every property declarative.

---

## 14. Revised Recommendations

Based on the combined analysis of STJ, Configuration.Binder, LoggerMessage, Options Validator, LibraryImport, Regex, and the dotnet-inspect real-world consumer, the following recommendations are organized by priority and validation status.

### Critical Priority (Blocking Real-World Usage)

| # | Recommendation | Source | Validated By | Action |
|---|----------------|--------|--------------|--------|
| 1 | **Fix Incremental Caching Equality** | STJ, LibraryImport | Architectural | Convert models to records or implement deep equality using `SequenceEqualImmutableArray<T>` |
| 2 | **Add Format Strings** | dotnet-inspect | 12 dual-property patterns | Add `[MarkoutFormat("format")]` supporting .NET format strings for dates, numbers |
| 3 | **Add Collection Joining** | dotnet-inspect | 8 companion properties | Add `[MarkoutJoin(", ")]` for `List<string>` automatic joining |
| 4 | **Add Null/Empty Suppression** | dotnet-inspect | 40+ conditional builds | Add `[MarkoutSkipWhenNull]` or automatic null-skipping for fields |

### High Priority (Validated and Impactful)

| # | Recommendation | Source | Validated By | Action |
|---|----------------|--------|--------------|--------|
| 5 | **Migrate to ForAttributeWithMetadataName** | STJ, Regex, LibraryImport | Best practice | Replace `CreateSyntaxProvider` predicate |
| 6 | **Add Enum Support** | STJ, dotnet-inspect | Enum avoidance | Add `PropertyKind.Enum` with `.ToString()` generation |
| 7 | **Adopt Symbol-to-Model Barrier** | LibraryImport | Architectural | Ensure no `ISymbol` references leak into model types |
| 8 | **Add KnownTypeSymbols Pattern** | STJ, Config.Binder | Performance | Cache symbol lookups for BCL types |

### Medium Priority (Valuable Improvements)

| # | Recommendation | Source | Validated By | Action |
|---|----------------|--------|--------------|--------|
| 9 | **Add Compile-Time Options Attribute** | STJ | Pattern | Add `[MarkoutContextOptions]` for baked-in configuration |
| 10 | **Adopt Dual-Strategy Pattern** | LoggerMessage, Regex | Performance | Simple types → direct emit; complex → List builder |
| 11 | **Type Classification Hierarchy** | LibraryImport | Architecture | Create `MarkoutTypeInfo` discriminated union |
| 12 | **Stage-Based Emission** | LibraryImport | Organization | Organize `SerializerEmitter` into explicit stages |
| 13 | **Add Conditional Row Support** | dotnet-inspect | AssemblyAudit render | Enable conditional table rows in source generation |

### Lower Priority (Nice to Have)

| # | Recommendation | Source | Validated By | Action |
|---|----------------|--------|--------------|--------|
| 14 | **Pre-computed Display Names** | STJ | Performance | Generate constants for display names |
| 15 | **Multi-File Emission** | STJ | Organization | Separate PropertyNames.g.cs for large contexts |
| 16 | **IMarkoutFormattable Interface** | STJ (JsonConverter) | Extensibility | Allow custom types to format themselves |
| 17 | **Diagnostic Suppressor** | Config.Binder | DX | Suppress NRT warnings in generated code |
| 18 | **Graceful Fallback** | Regex | Resilience | Emit fallback for types needing imperative rendering |
| 19 | **Helper Method Deduplication** | Regex | Code size | Emit shared `file static` helpers |
| 20 | **Golden Output Tests** | Regex | Testing | Add snapshot tests for generated code |

### What NOT to Do (Reaffirmed)

1. **Don't add 25 collection types** — dotnet-inspect uses only `List<T>`, `T[]`, `List<MarkoutField>`, `List<TreeNode>`
2. **Don't add polymorphism** — view model projection is the answer
3. **Don't add generation modes** — STJ's complexity without benefit
4. **Don't add multi-Roslyn support** — target Roslyn 4.4+ only
5. **Don't add bidirectional serialization** — write-only is a feature

### New Patterns to Adopt

| Pattern | Source | Application |
|---------|--------|-------------|
| Composite Resolver Chain | LibraryImport | Replace switch statements for property kind resolution |
| MarshallerShape Flags | LibraryImport | Create `PropertyRenderingShape` flags enum |
| `DiagnosticOr<T>` | LibraryImport | Clean validation in the pipeline |
| Multi-level Optimization | Regex | Apply to boolean formatting, collection rendering |
| Helper Deduplication | Regex | Emit shared `file static` helpers |
| Additional Declarations | Regex | For conditional local variables in generated code |
| Local Functions | Regex | For complex emission scenarios |

---

## 15. Custom Formatters and Markdown Context Safety

A critical design question for custom formatters: **how do we ensure they emit valid Markdown, both generally and within the rendering context where they're invoked?** The canonical example is a code fence emitted inside a code fence, which breaks Markdown structure.

### The Problem Space

Markdown has context-sensitive syntax. Certain constructs cannot nest:

| Outer Context | Problematic Inner Content |
|---------------|--------------------------|
| Code fence (`` ``` ``) | Code fence (`` ``` ``) — creates premature closure |
| Table cell (`\| ... \|`) | Pipe characters, newlines, block-level elements |
| Inline code (`` ` ``) | Backticks of same length |
| Heading (`#`) | Block-level elements (tables, code blocks) |
| List item | Improperly indented block elements |

Today, `MarkoutWriter` handles some of this ad hoc:
- `EscapeTableCell()` escapes `|` and replaces newlines in table cells
- `_inTable` state prevents invalid nesting (throws on missing `WriteTableStart`)
- `_sectionExcluded` suppresses output in excluded sections

But there is **no `_inCodeBlock` tracking**, and no mechanism for a custom formatter to know what context it's writing into.

### Current State in MarkoutWriter

```csharp
// State tracked today:
private bool _inTable;          // ✓ Table context
private bool _sectionExcluded;  // ✓ Section filtering
private bool _needsBlankLine;   // ✓ Spacing

// State NOT tracked:
// _inCodeBlock               // ✗ Code fence context
// _inListItem                // ✗ List context (indentation)
// _nestingDepth              // ✗ General nesting
```

### Design Approaches

#### Approach 1: Context-Aware Writer (Recommended)

Track rendering context in the writer and expose it to formatters. The formatter receives context and can adapt:

```csharp
// Writer tracks its rendering context
public enum MarkoutRenderContext
{
    Block,       // Top-level or section body — anything is valid
    Table,       // Inside a table — no block elements, pipes escaped
    CodeBlock,   // Inside a code fence — content is literal, no Markdown
    InlineCode,  // Inside backticks — content is literal
    ListItem,    // Inside a list item — block elements need indentation
}

public sealed class MarkoutWriter
{
    public MarkoutRenderContext CurrentContext { get; private set; }

    public void WriteCodeBlockStart(string? language = null)
    {
        if (CurrentContext == MarkoutRenderContext.CodeBlock)
            throw new InvalidOperationException("Cannot nest code fences.");
        // ...
        CurrentContext = MarkoutRenderContext.CodeBlock;
    }
}
```

A custom formatter interface receives the writer (and thus context):

```csharp
public interface IMarkoutFormattable
{
    void FormatMarkout(MarkoutWriter writer);
}
```

Because the formatter writes *through* the writer rather than returning a raw string, the writer can enforce invariants. If a formatter tries to open a code fence inside a code fence, the writer throws. If inside a table, the writer can auto-escape or reject block-level calls.

**This is the same pattern as `System.Text.Json`'s `Utf8JsonWriter`** — the writer is a state machine that enforces structural validity. `JsonConverter<T>.Write(Utf8JsonWriter writer, ...)` writes through the writer, which rejects invalid JSON structure (e.g., writing a property name outside an object).

#### Approach 2: Capability-Based Constraints

Instead of runtime errors, give formatters a constrained view of what they can do:

```csharp
// Different interfaces for different contexts
public interface IMarkoutBlockFormattable
{
    // Can emit any block-level Markdown
    void FormatMarkout(MarkoutWriter writer);
}

public interface IMarkoutInlineFormattable
{
    // Can only emit inline Markdown (no code blocks, tables, headings)
    string FormatMarkoutInline();
}
```

The source generator knows at compile time whether a property appears in a table column (inline context) vs. a field value (block context) and emits the appropriate call. This moves validation to **compile time** via the source generator's diagnostics:

```csharp
// Source generator emits:
// In table context → calls IMarkoutInlineFormattable.FormatMarkoutInline()
// In field context → calls IMarkoutBlockFormattable.FormatMarkout(writer)

// Diagnostic MARKOUT005: Type 'Foo' implements IMarkoutBlockFormattable but is
// used in a table column. Implement IMarkoutInlineFormattable for table contexts.
```

#### Approach 3: Escape Hatch with Validation

For the code-fence-in-code-fence problem specifically, the writer could use longer fence sequences (Markdown supports ``````, etc.):

```csharp
public void WriteCodeBlockStart(string? language = null)
{
    // If already in a code block, use a longer fence
    var fence = CurrentContext == MarkoutRenderContext.CodeBlock
        ? "````" : "```";
    // ...
}
```

However, this only solves one specific case and doesn't generalize.

### Recommendation

**Combine Approaches 1 and 2:**

1. **Track context in `MarkoutWriter`** — add `CurrentContext` property and `_inCodeBlock` state. This is a small, backwards-compatible change that immediately prevents invalid nesting for all callers (not just custom formatters).

2. **Design `IMarkoutFormattable` to write through the writer** — not return strings. This is the key insight from STJ's `Utf8JsonWriter`/`JsonConverter` design: the writer is the enforcement boundary.

3. **Use the source generator to emit compile-time diagnostics** when types with block-level formatting are used in inline contexts (table columns). This catches the most common mistakes before runtime.

4. **For the specific nested code fence problem**, support longer fence sequences as a runtime fallback rather than throwing.

### What This Means for the Closed Type System

This design **preserves** the closed type system while adding targeted extensibility:
- The closed types (string, int, bool, DateTime, etc.) are always safe — the writer knows how to render them in any context
- `IMarkoutFormattable` is the single escape hatch, and it writes through the writer's state machine, so it can't break structure
- The source generator validates at compile time that formattable types are used in compatible contexts

This is analogous to how STJ's closed set of built-in converters are always safe, while custom `JsonConverter<T>` implementations write through `Utf8JsonWriter` which enforces valid JSON.

---

## 16. Terminology Clarification: Fields, Scalars, and Layouts

Markout grew organically, and its terminology conflates two distinct concepts: the **data shape** (what a value is) and the **rendering strategy** (how a collection of values is displayed). Clarifying this separation is foundational for future API evolution.

### The Conflation

Today, "scalar" does double duty:

1. **Data concept**: A `MarkoutField` is a key-value pair (KVP). The *value* is a scalar — a single atomic value (string, int, bool, DateTime, etc.). This is the type classification concern: "is this property a scalar, a collection, or a complex object?"

2. **Rendering concept**: `RenderScalars`, `IsScalarKind()`, `EmitScalarsWithLayout()`, `scalarProps` — these all use "scalar" to mean "properties that get grouped together and rendered via a field layout." But the layout itself is about rendering a **list of KVPs**, not about scalars.

The `FieldLayout` enum makes this visible — it describes four ways to render a list of fields:

```
OneLine:              "Version: 10.0 | Security: true | Updated: 2026-01"
LineBreaks:           "Version: 10.0\nSecurity: true\nUpdated: 2026-01"
LineBreaksDoubleSpace: "Version: 10.0  \nSecurity: true  \nUpdated: 2026-01  "
List:                 "- Version: 10.0\n- Security: true\n- Updated: 2026-01"
```

These are not four kinds of scalars. They are four renderings of the **same data**: a vector of `MarkoutField` KVPs. There may be more variants in the future (e.g., definition lists, two-column tables, indented blocks).

### Proposed Conceptual Model

Separate the concerns cleanly:

| Concept | Name | Responsibility |
|---------|------|----------------|
| **Value type** | "Scalar" or "PropertyKind" | Type classification: string, bool, int, DateTime, etc. Determines how a single value is converted to a string. |
| **Data shape** | "Field" | A key-value pair (`MarkoutField`). The key is a display name, the value is a scalar. This is the data unit. |
| **Rendering** | "FieldLayout" | How a collection of fields is rendered. This is purely a presentation concern. |

Under this model:
- `MarkoutField` stays as-is — it's a KVP, and "field" correctly suggests that
- `PropertyKind` stays as the type discriminator (String, Boolean, Int32, etc.)
- `IsScalarKind()` is fine — it answers "is this property's value a scalar type?"
- `RenderScalars` should conceptually be "RenderFields" or "AutoRenderFields" — it controls whether scalar properties are *automatically collected into a field list and rendered*
- `EmitScalarsWithLayout` should conceptually be "EmitFieldList" — it emits code to render a list of fields using the specified layout
- `scalarProps` / `nonScalarProps` in the emitter are fine as local variable names (they partition by type kind)

### What to Rename (if anything)

The internal naming (`scalarProps`, `IsScalarKind`) is acceptable — it correctly refers to type classification. The user-facing concern is `RenderScalars` on `[MarkoutSerializable]`, which conflates "collect scalar properties into fields" with the rendering. Options:

| Current | Possible Rename | Rationale |
|---------|----------------|-----------|
| `RenderScalars` | `AutoFields` | Clearer: "automatically render scalar properties as fields" |
| `FieldLayout` | (keep) | Already correct — it describes how a field list is laid out |
| `MarkoutField` | (keep) | Good name — suggests KVP |
| `EmitScalarsWithLayout` | `EmitFieldListWithLayout` | Internal, but clearer intent |
| `WriteCompactFields` | (keep) | Describes the `OneLine` layout rendering |
| `WriteField` | (keep) | Describes the `LineBreaksDoubleSpace` layout (one field at a time) |
| `WriteFieldNoBreak` | (keep) | Describes the `LineBreaks` layout |

### Why This Matters

This separation matters for three reasons:

1. **Future layouts**: New rendering strategies (definition lists, two-column tables, accordion-style collapsible sections) should be addable by extending `FieldLayout`, not by changing the type system or adding new `Write*` methods for each combination.

2. **Custom formatters**: When designing `IMarkoutFormattable` (§15), the interface needs to understand that a formatter produces a **scalar value** (a string representation), and the *caller* decides how to render it within a field list. The formatter shouldn't need to know whether its output ends up in a compact pipe-separated line or a bullet list.

3. **Source generator clarity**: The emitter's job is clearer when separated: (a) classify each property by `PropertyKind`, (b) collect scalar properties into a field list, (c) render the field list using the chosen `FieldLayout`. Today steps (a) and (b) are interleaved with (c) in `EmitPropertySerializations`.

---

## Conclusion

Markout is well-positioned as a focused, opinionated alternative to general-purpose serializers. Its architecture follows established patterns from the .NET runtime generators while avoiding their complexity traps.

**Immediate actions** should focus on fixing incremental caching, migrating to `ForAttributeWithMetadataName`, and addressing the critical gaps revealed by dotnet-inspect: format strings, collection joining, and null suppression. These three additions would eliminate the majority of boilerplate currently required in view models.

**Medium-term improvements** like enum support, compile-time options, stage-based emission, and the type classification hierarchy from LibraryImport will improve architecture and developer experience without adding significant complexity.

The key insight from this expanded comparison is that **Markout's constraints are features, but its rigidity is a problem**. The closed type system, write-only design, and Markdown-specific diagnostics create a tool that does one thing well. However, the dotnet-inspect analysis reveals that the current source generator model covers only ~20% of real-world usage—the remaining 80% falls back to `MarkoutWriter` due to conditional rendering needs.

The path forward is clear:
1. **Make declarative rendering more expressive** — format strings, collection joining, null suppression
2. **Adopt architectural patterns from LibraryImport and Regex** — symbol-to-model barrier, stage-based emission, type hierarchies
3. **Embrace the `List<MarkoutField>` pattern** as the sanctioned approach for dynamic fields rather than fighting it
4. **Preserve the view model philosophy** but reduce the boilerplate required to create view models
5. **Design custom formatters around context safety** — write through the writer (like STJ's Utf8JsonWriter), track rendering context, and use the source generator to catch context mismatches at compile time

Future development should preserve Markout's focus while selectively adopting patterns proven by the runtime generators and validated by real-world consumer needs.
