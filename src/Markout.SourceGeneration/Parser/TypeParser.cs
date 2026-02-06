using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Markout.SourceGeneration.Parser;

/// <summary>
/// Parses types and contexts marked with [MarkoutContext].
/// Types do not require [MarkoutSerializable] — it is optional for customizing behavior.
/// </summary>
internal static class TypeParser
{
    private const string MarkoutSerializableAttribute = "Markout.MarkoutSerializableAttribute";
    private const string MarkoutContextAttribute = "Markout.MarkoutContextAttribute";
    private const string MarkoutPropertyNameAttribute = "Markout.MarkoutPropertyNameAttribute";
    private const string MarkoutIgnoreAttribute = "Markout.MarkoutIgnoreAttribute";
    private const string MarkoutIgnoreInTableAttribute = "Markout.MarkoutIgnoreInTableAttribute";
    private const string MarkoutSectionAttribute = "Markout.MarkoutSectionAttribute";
    private const string MarkoutBoolFormatAttribute = "Markout.MarkoutBoolFormatAttribute";

    public static ContextMetadata? ParseContext(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(classDecl, cancellationToken);

        if (symbol is not INamedTypeSymbol classSymbol)
            return null;

        var contextAttributes = classSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.ToDisplayString() == MarkoutContextAttribute)
            .ToList();

        if (contextAttributes.Count == 0)
            return null;

        var types = new List<TypeMetadata>();
        foreach (var attr in contextAttributes)
        {
            if (attr.ConstructorArguments.Length > 0 &&
                attr.ConstructorArguments[0].Value is INamedTypeSymbol typeArg)
            {
                // Pass the generator context for diagnostics
                var typeMeta = ParseTypeSymbol(typeArg, context.SemanticModel.Compilation, null, null, null, null);
                if (typeMeta != null)
                    types.Add(typeMeta);
            }
        }

        var ns = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : classSymbol.ContainingNamespace.ToDisplayString();

        return new ContextMetadata(ns, classSymbol.Name, types);
    }

    private static TypeMetadata? ParseTypeSymbol(
        INamedTypeSymbol typeSymbol,
        Compilation compilation,
        GeneratorSyntaxContext? generatorContext,
        string? titleProperty = null,
        string? titleContextProperty = null,
        string? descriptionProperty = null,
        bool? renderScalars = null,
        FieldLayoutKind? fieldLayout = null)
    {
        // If titleProperty/descriptionProperty not passed, try to get them from the type's [MarkoutSerializable] attribute
        if (titleProperty == null || titleContextProperty == null || descriptionProperty == null || renderScalars == null || fieldLayout == null)
        {
            var serializableAttr = typeSymbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == MarkoutSerializableAttribute);
            if (serializableAttr != null)
            {
                foreach (var named in serializableAttr.NamedArguments)
                {
                    if (named.Key == "TitleProperty" && named.Value.Value is string tp)
                        titleProperty ??= tp;
                    else if (named.Key == "TitleContextProperty" && named.Value.Value is string tcp)
                        titleContextProperty ??= tcp;
                    else if (named.Key == "DescriptionProperty" && named.Value.Value is string dp)
                        descriptionProperty ??= dp;
                    else if (named.Key == "RenderScalars" && named.Value.Value is bool rs)
                        renderScalars ??= rs;
                    else if (named.Key == "FieldLayout" && named.Value.Value is int fl)
                        fieldLayout ??= (FieldLayoutKind)fl;
                }
            }
        }

        // Default to true if not specified
        renderScalars ??= true;
        // Default to OneLine
        fieldLayout ??= FieldLayoutKind.OneLine;

        var properties = new List<PropertyMetadata>();
        var diagnostics = new List<DiagnosticInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IPropertySymbol prop)
                continue;

            if (prop.DeclaredAccessibility != Accessibility.Public)
                continue;

            if (prop.GetMethod == null)
                continue;

            var propMeta = ParseProperty(prop, compilation, diagnostics);
            if (propMeta != null)
                properties.Add(propMeta);
        }

        // Warn if RenderScalars=false but no sections or field collections exist
        if (renderScalars == false)
        {
            bool hasSectionOrFieldCollection = properties.Any(p => 
                !p.IsIgnored && (p.IsSection || p.Kind == PropertyKind.FieldCollection));
            
            if (!hasSectionOrFieldCollection)
            {
                diagnostics.Add(new DiagnosticInfo(
                    DiagnosticDescriptors.RenderScalarsNoContent,
                    typeSymbol.Locations.FirstOrDefault(),
                    typeSymbol.Name));
            }
        }

        var ns = typeSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : typeSymbol.ContainingNamespace.ToDisplayString();

        return new TypeMetadata(
            ns,
            typeSymbol.Name,
            typeSymbol.ToDisplayString(),
            properties,
            typeSymbol.IsValueType,
            titleProperty,
            titleContextProperty,
            descriptionProperty,
            renderScalars.Value,
            fieldLayout.Value,
            diagnostics);
    }

    private static PropertyMetadata? ParseProperty(
        IPropertySymbol prop,
        Compilation compilation,
        List<DiagnosticInfo> diagnostics)
    {
        var isIgnored = HasAttribute(prop, MarkoutIgnoreAttribute);
        var isIgnoredInTable = HasAttribute(prop, MarkoutIgnoreInTableAttribute);
        var isSection = HasAttribute(prop, MarkoutSectionAttribute);
        var sectionLevel = 2;
        string? sectionName = null;

        if (isSection)
        {
            var sectionAttr = prop.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == MarkoutSectionAttribute);
            if (sectionAttr != null)
            {
                foreach (var named in sectionAttr.NamedArguments)
                {
                    if (named.Key == "Level" && named.Value.Value is int level)
                        sectionLevel = level;
                    else if (named.Key == "Name" && named.Value.Value is string name)
                        sectionName = name;
                }
            }
        }

        var displayName = prop.Name;
        var nameAttr = prop.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == MarkoutPropertyNameAttribute);
        if (nameAttr?.ConstructorArguments.Length > 0 &&
            nameAttr.ConstructorArguments[0].Value is string customName)
        {
            displayName = customName;
        }

        // Parse [MarkoutBoolFormat] attribute
        string? boolTrueValue = null;
        string? boolFalseValue = null;
        var boolFormatAttr = prop.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == MarkoutBoolFormatAttribute);
        if (boolFormatAttr?.ConstructorArguments.Length >= 2)
        {
            if (boolFormatAttr.ConstructorArguments[0].Value is string tv)
                boolTrueValue = tv;
            if (boolFormatAttr.ConstructorArguments[1].Value is string fv)
                boolFalseValue = fv;
        }

        // Detect nullable value types before determining property kind
        bool isNullableValueType = false;
        if (prop.Type is INamedTypeSymbol nullableCheck &&
            nullableCheck.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            isNullableValueType = true;
        }

        var (kind, elementTypeName, elementProperties, hasNestedContent, elementTitleProperty, isArray) = DeterminePropertyKind(prop.Type, compilation, diagnostics, prop.Name, prop.Locations.FirstOrDefault());

        // Determine if property is unsupported in table context
        bool isUnsupportedInTable = !isIgnored && !isSection && !IsScalarKind(kind);

        // Emit warning for unsupported properties without [MarkoutIgnoreInTable]
        if (isUnsupportedInTable && !isIgnoredInTable)
        {
            diagnostics.Add(new DiagnosticInfo(
                DiagnosticDescriptors.UnsupportedPropertyInTable,
                prop.Locations.FirstOrDefault(),
                prop.Name,
                prop.ContainingType.Name,
                GetKindDisplayName(kind)
            ));
        }

        return new PropertyMetadata(
            prop.Name,
            displayName,
            prop.Type.ToDisplayString(),
            kind,
            isIgnored,
            isIgnoredInTable,
            isUnsupportedInTable,
            isSection,
            sectionLevel,
            sectionName,
            elementTypeName,
            elementProperties,
            hasNestedContent,
            elementTitleProperty,
            boolTrueValue,
            boolFalseValue,
            isNullableValueType,
            isArray);
    }

    private static (PropertyKind Kind, string? ElementTypeName, IReadOnlyList<PropertyMetadata>? ElementProperties, bool HasNestedContent, string? ElementTitleProperty, bool IsArray)
        DeterminePropertyKind(ITypeSymbol type, Compilation compilation, List<DiagnosticInfo>? diagnostics = null, string? propertyName = null, Location? propertyLocation = null)
    {
        var typeName = type.ToDisplayString();

        // Check for nullable value types
        if (type is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            type = namedType.TypeArguments[0];
            typeName = type.ToDisplayString();
        }

        // Primitives
        return type.SpecialType switch
        {
            SpecialType.System_String => (PropertyKind.String, null, null, false, null, false),
            SpecialType.System_Boolean => (PropertyKind.Boolean, null, null, false, null, false),
            SpecialType.System_Int32 => (PropertyKind.Int32, null, null, false, null, false),
            SpecialType.System_Int64 => (PropertyKind.Int64, null, null, false, null, false),
            SpecialType.System_Double => (PropertyKind.Double, null, null, false, null, false),
            SpecialType.System_Decimal => (PropertyKind.Decimal, null, null, false, null, false),
            _ => DetermineComplexPropertyKind(type, compilation, diagnostics, propertyName, propertyLocation)
        };
    }

    private static (PropertyKind Kind, string? ElementTypeName, IReadOnlyList<PropertyMetadata>? ElementProperties, bool HasNestedContent, string? ElementTitleProperty, bool IsArray)
        DetermineComplexPropertyKind(ITypeSymbol type, Compilation compilation, List<DiagnosticInfo>? diagnostics = null, string? propertyName = null, Location? propertyLocation = null)
    {
        var typeName = type.ToDisplayString();

        // DateTime types
        if (typeName == "System.DateTime")
            return (PropertyKind.DateTime, null, null, false, null, false);
        if (typeName == "System.DateTimeOffset")
            return (PropertyKind.DateTimeOffset, null, null, false, null, false);

        // Check for arrays
        if (type is IArrayTypeSymbol arrayType)
        {
            var elementType = arrayType.ElementType;

            // Check for MarkoutField[] - renders as compact line or field table
            if (elementType.ToDisplayString() == "Markout.MarkoutField")
                return (PropertyKind.FieldCollection, null, null, false, null, true);

            if (elementType.SpecialType == SpecialType.System_String)
                return (PropertyKind.StringArray, null, null, false, null, true);

            var elementProps = GetTypeProperties(elementType, compilation, diagnostics);
            var hasNested = HasNestedContent(elementProps);
            var titleProp = GetTitleProperty(elementType);
            return (PropertyKind.ComplexArray, elementType.ToDisplayString(), elementProps, hasNested, titleProp, true);
        }

        // Check for IEnumerable<T> / List<T> / etc.
        if (type is INamedTypeSymbol namedType)
        {
            // Detect Dictionary<TKey, TValue> before IEnumerable<T> since dictionaries implement IEnumerable<KeyValuePair>
            var isDictionary = namedType.AllInterfaces.Any(i =>
                i.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IDictionary<TKey, TValue>");

            if (isDictionary)
            {
                if (diagnostics != null && propertyName != null)
                {
                    diagnostics.Add(new DiagnosticInfo(
                        DiagnosticDescriptors.DictionaryProperty,
                        propertyLocation,
                        propertyName));
                }
                return (PropertyKind.Other, null, null, false, null, false);
            }

            var enumerableInterface = namedType.AllInterfaces
                .FirstOrDefault(i => i.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>");

            if (enumerableInterface != null ||
                (namedType.OriginalDefinition.ToDisplayString().StartsWith("System.Collections.Generic.") &&
                 namedType.TypeArguments.Length == 1))
            {
                ITypeSymbol? elementType = null;

                if (enumerableInterface != null && enumerableInterface.TypeArguments.Length > 0)
                {
                    elementType = enumerableInterface.TypeArguments[0];
                }
                else if (namedType.TypeArguments.Length > 0)
                {
                    elementType = namedType.TypeArguments[0];
                }

                if (elementType != null)
                {
                    // Check for List<MarkoutField> or IReadOnlyList<MarkoutField> - renders as compact line or field table
                    // Requires materialized collection to avoid double-enumeration issues
                    if (elementType.ToDisplayString() == "Markout.MarkoutField")
                    {
                        var typeDisplayString = namedType.OriginalDefinition.ToDisplayString();
                        if (typeDisplayString == "System.Collections.Generic.List<T>" ||
                            typeDisplayString == "System.Collections.Generic.IReadOnlyList<T>" ||
                            typeDisplayString == "System.Collections.Generic.IList<T>")
                        {
                            return (PropertyKind.FieldCollection, null, null, false, null, false);
                        }
                        // IEnumerable<MarkoutField> without materialization is not supported
                        // User should use List<MarkoutField> or IReadOnlyList<MarkoutField>
                    }

                    // Check for List<TreeNode> - renders as tree structure
                    if (elementType.ToDisplayString() == "Markout.TreeNode")
                    {
                        var typeDisplayString = namedType.OriginalDefinition.ToDisplayString();
                        if (typeDisplayString == "System.Collections.Generic.List<T>")
                        {
                            return (PropertyKind.Tree, null, null, false, null, false);
                        }
                    }

                    if (elementType.SpecialType == SpecialType.System_String)
                        return (PropertyKind.StringArray, null, null, false, null, false);

                    var elementProps = GetTypeProperties(elementType, compilation, diagnostics);
                    var hasNested = HasNestedContent(elementProps);
                    var titleProp = GetTitleProperty(elementType);
                    return (PropertyKind.ComplexArray, elementType.ToDisplayString(), elementProps, hasNested, titleProp, false);
                }
            }
        }

        // Nested object
        if (type.TypeKind == TypeKind.Class || type.TypeKind == TypeKind.Struct)
        {
            var props = GetTypeProperties(type, compilation, diagnostics);
            if (props.Count > 0)
                return (PropertyKind.NestedObject, null, props, false, null, false);
        }

        return (PropertyKind.Other, null, null, false, null, false);
    }

    private static bool HasNestedContent(IReadOnlyList<PropertyMetadata>? props)
    {
        if (props == null) return false;
        return props.Any(p => !p.IsIgnored && 
            (p.Kind == PropertyKind.NestedObject || p.Kind == PropertyKind.ComplexArray));
    }

    private static string? GetTitleProperty(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType) return null;
        
        var serializableAttr = namedType.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == MarkoutSerializableAttribute);
        
        if (serializableAttr != null)
        {
            foreach (var named in serializableAttr.NamedArguments)
            {
                if (named.Key == "TitleProperty" && named.Value.Value is string tp)
                    return tp;
            }
        }
        
        return null;
    }

    private static IReadOnlyList<PropertyMetadata> GetTypeProperties(
        ITypeSymbol type,
        Compilation compilation,
        List<DiagnosticInfo>? diagnostics = null)
    {
        var properties = new List<PropertyMetadata>();
        diagnostics ??= new List<DiagnosticInfo>();

        if (type is not INamedTypeSymbol namedType)
            return properties;

        foreach (var member in namedType.GetMembers())
        {
            if (member is not IPropertySymbol prop)
                continue;

            if (prop.DeclaredAccessibility != Accessibility.Public)
                continue;

            if (prop.GetMethod == null)
                continue;

            var propMeta = ParseProperty(prop, compilation, diagnostics);
            if (propMeta != null)
                properties.Add(propMeta);
        }

        return properties;
    }

    private static bool IsScalarKind(PropertyKind kind)
    {
        return kind is 
            PropertyKind.String or 
            PropertyKind.Boolean or 
            PropertyKind.Int32 or 
            PropertyKind.Int64 or 
            PropertyKind.Double or 
            PropertyKind.Decimal or 
            PropertyKind.DateTime or 
            PropertyKind.DateTimeOffset;
    }

    private static string GetKindDisplayName(PropertyKind kind)
    {
        return kind switch
        {
            PropertyKind.StringArray => "a string array",
            PropertyKind.ComplexArray => "an array of complex objects",
            PropertyKind.NestedObject => "a complex object",
            PropertyKind.Other => "a non-scalar type",
            _ => kind.ToString().ToLowerInvariant()
        };
    }

    private static bool HasAttribute(ISymbol symbol, string attributeName)
    {
        return symbol.GetAttributes()
            .Any(a => a.AttributeClass?.ToDisplayString() == attributeName);
    }
}
