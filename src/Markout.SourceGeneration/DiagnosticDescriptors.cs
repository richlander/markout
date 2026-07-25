using Microsoft.CodeAnalysis;

namespace Markout.SourceGeneration;

/// <summary>
/// Diagnostic descriptors for Markout source generator errors and warnings.
/// </summary>
internal static class DiagnosticDescriptors
{
    private const string Category = "Markout.Design";

    public static readonly DiagnosticDescriptor UnsupportedPropertyInTable = new(
        id: "MARKOUT001",
        title: "Unsupported property in table context",
        messageFormat: "Property '{0}' in type '{1}' is {2} and will be skipped in table context. " +
                       "Add [MarkoutIgnoreInTable] to silence this warning.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Properties that are collections or complex objects cannot be rendered in Markdown table cells. " +
                     "They will be skipped when the type is rendered as a table row. " +
                     "Add [MarkoutIgnoreInTable] to acknowledge this and silence the warning, " +
                     "or use [MarkoutSection] to render in a separate section."
    );

    public static readonly DiagnosticDescriptor ComplexObjectPropertyInTable = new(
        id: "MARKOUT002",
        title: "Complex object property in table row",
        messageFormat: "Property '{0}' in type '{1}' is a complex object and cannot be rendered in a table cell. " +
                       "Use [MarkoutIgnore], flatten the properties, or provide a summary value.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Complex objects with multiple properties cannot be meaningfully rendered in table cells. " +
                     "Consider flattening the structure by moving properties to the parent type, " +
                     "or excluding the property with [MarkoutIgnore]."
    );

    public static readonly DiagnosticDescriptor DictionaryProperty = new(
        id: "MARKOUT003",
        title: "Dictionary property not supported",
        messageFormat: "Property '{0}' is Dictionary<TKey, TValue> which is not supported in Markout. " +
                       "Convert to List<KeyValueItem> or use separate properties.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Dictionary types cannot be serialized in Markout format. " +
                     "Convert to a List<T> where T has Key and Value properties, " +
                     "or if keys are known at design time, use separate scalar properties."
    );

    public static readonly DiagnosticDescriptor AutoFieldsNoContent = new(
        id: "MARKOUT004",
        title: "AutoFields=false with no sections",
        messageFormat: "Type '{0}' has AutoFields=false but no [MarkoutSection] or FieldCollection properties. " +
                       "Output will be empty or contain only the title.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When AutoFields is false, only properties with [MarkoutSection] or FieldCollection types are rendered. " +
                     "Without any such properties, the serialized output will be empty or contain only the title/description. " +
                     "Either add [MarkoutSection] to collection properties, or remove AutoFields=false."
    );

    public static readonly DiagnosticDescriptor MetricChangeNonScalarType = new(
        id: "MARKOUT005",
        title: "MetricChange<T> requires a numeric scalar type argument",
        messageFormat: "Property '{0}' uses MetricChange<{1}>, but T must be a numeric scalar type " +
                       "(int, long, double, decimal, etc.). Composite shapes (e.g. Segments, Share) are not " +
                       "supported here; keep them as ordinary rows or a plain metric.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "MetricChange<T> renders Before/After/Target as scalar values. A non-numeric type argument " +
                     "renders incorrectly (e.g. a target prints the raw struct), so it is rejected at compile time."
    );

    public static readonly DiagnosticDescriptor TableRowNoVisibleColumns = new(
        id: "MARKOUT006",
        title: "Table row type has no visible columns",
        messageFormat: "Property '{0}' renders '{1}' as a table, but that row type has no visible columns " +
                       "(every property is [MarkoutIgnore]'d, section-ignored, or a [MarkoutChild] flag). " +
                       "Add at least one scalar column, or render it as sections instead.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A table is emitted with a fixed header row before any data rows, so a row type with no " +
                     "renderable columns throws at runtime (\"At least one header is required\"). This is rejected " +
                     "at compile time. A [MarkoutChild] flag is a nesting marker, not a column, so a row whose only " +
                     "property is the child flag is degenerate; give the row at least one scalar value column."
    );

}
