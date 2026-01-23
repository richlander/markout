using Microsoft.CodeAnalysis;

namespace MarkdownData.SourceGeneration;

/// <summary>
/// Diagnostic descriptors for MDF source generator errors and warnings.
/// </summary>
internal static class DiagnosticDescriptors
{
    private const string Category = "MarkdownData.Design";

    public static readonly DiagnosticDescriptor NonScalarPropertyInTable = new(
        id: "MDF001",
        title: "Non-scalar property in table row",
        messageFormat: "Property '{0}' in type '{1}' is {2} and cannot be rendered in a table cell. " +
                       "Use [MdfIgnore], [MdfSection], or transform the data. " +
                       "See: https://github.com/your-repo/mdf/docs/errors/MDF001",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Properties that are collections or complex objects cannot be rendered in Markdown table cells. " +
                     "They will show as useless ToString() output. You must explicitly choose how to handle them: " +
                     "use [MdfIgnore] to exclude them, use [MdfSection] to render in a separate section, " +
                     "or transform your data (e.g., pivot table, flatten, or provide aggregate values).",
        helpLinkUri: "https://github.com/your-repo/mdf/docs/strategies/nested-lists"
    );

    public static readonly DiagnosticDescriptor ComplexObjectPropertyInTable = new(
        id: "MDF002",
        title: "Complex object property in table row",
        messageFormat: "Property '{0}' in type '{1}' is a complex object and cannot be rendered in a table cell. " +
                       "Use [MdfIgnore], flatten the properties, or provide a summary value.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Complex objects with multiple properties cannot be meaningfully rendered in table cells. " +
                     "Consider flattening the structure by moving properties to the parent type, " +
                     "or excluding the property with [MdfIgnore]."
    );

    public static readonly DiagnosticDescriptor DictionaryProperty = new(
        id: "MDF003",
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
