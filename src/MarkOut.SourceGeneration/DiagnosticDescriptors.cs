using Microsoft.CodeAnalysis;

namespace MarkOut.SourceGeneration;

/// <summary>
/// Diagnostic descriptors for MDF source generator errors and warnings.
/// </summary>
internal static class DiagnosticDescriptors
{
    private const string Category = "MarkOut.Design";

    public static readonly DiagnosticDescriptor UnsupportedPropertyInTable = new(
        id: "MARKOUT001",
        title: "Unsupported property in table context",
        messageFormat: "Property '{0}' in type '{1}' is {2} and will be skipped in table context. " +
                       "Add [MarkOutIgnoreInTable] to silence this warning.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Properties that are collections or complex objects cannot be rendered in Markdown table cells. " +
                     "They will be skipped when the type is rendered as a table row. " +
                     "Add [MarkOutIgnoreInTable] to acknowledge this and silence the warning, " +
                     "or use [MarkOutSection] to render in a separate section."
    );

    public static readonly DiagnosticDescriptor ComplexObjectPropertyInTable = new(
        id: "MARKOUT002",
        title: "Complex object property in table row",
        messageFormat: "Property '{0}' in type '{1}' is a complex object and cannot be rendered in a table cell. " +
                       "Use [MarkOutIgnore], flatten the properties, or provide a summary value.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Complex objects with multiple properties cannot be meaningfully rendered in table cells. " +
                     "Consider flattening the structure by moving properties to the parent type, " +
                     "or excluding the property with [MarkOutIgnore]."
    );

    public static readonly DiagnosticDescriptor DictionaryProperty = new(
        id: "MARKOUT003",
        title: "Dictionary property not supported",
        messageFormat: "Property '{0}' is Dictionary<TKey, TValue> which is not supported in MDF. " +
                       "Convert to List<KeyValueItem> or use separate properties.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Dictionary types cannot be serialized in MDF format. " +
                     "Convert to a List<T> where T has Key and Value properties, " +
                     "or if keys are known at design time, use separate scalar properties."
    );
}
