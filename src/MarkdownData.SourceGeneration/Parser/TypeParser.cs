using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MarkdownData.SourceGeneration.Parser;

/// <summary>
/// Parses types marked with [MdfSerializable] and contexts marked with [MdfContext].
/// </summary>
internal static class TypeParser
{
    private const string MdfSerializableAttribute = "MarkdownData.MdfSerializableAttribute";
    private const string MdfContextAttribute = "MarkdownData.MdfContextAttribute";
    private const string MdfPropertyNameAttribute = "MarkdownData.MdfPropertyNameAttribute";
    private const string MdfIgnoreAttribute = "MarkdownData.MdfIgnoreAttribute";
    private const string MdfSectionAttribute = "MarkdownData.MdfSectionAttribute";

    public static TypeMetadata? ParseSerializableType(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var typeDecl = (TypeDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);

        if (symbol is not INamedTypeSymbol typeSymbol)
            return null;

        if (!HasAttribute(typeSymbol, MdfSerializableAttribute))
            return null;

        return ParseTypeSymbol(typeSymbol, context.SemanticModel.Compilation);
    }

    public static ContextMetadata? ParseContext(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(classDecl, cancellationToken);

        if (symbol is not INamedTypeSymbol classSymbol)
            return null;

        var contextAttributes = classSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.ToDisplayString() == MdfContextAttribute)
            .ToList();

        if (contextAttributes.Count == 0)
            return null;

        var types = new List<TypeMetadata>();
        foreach (var attr in contextAttributes)
        {
            if (attr.ConstructorArguments.Length > 0 &&
                attr.ConstructorArguments[0].Value is INamedTypeSymbol typeArg)
            {
                var typeMeta = ParseTypeSymbol(typeArg, context.SemanticModel.Compilation);
                if (typeMeta != null)
                    types.Add(typeMeta);
            }
        }

        var ns = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : classSymbol.ContainingNamespace.ToDisplayString();

        return new ContextMetadata(ns, classSymbol.Name, types);
    }

    private static TypeMetadata? ParseTypeSymbol(INamedTypeSymbol typeSymbol, Compilation compilation)
    {
        var properties = new List<PropertyMetadata>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IPropertySymbol prop)
                continue;

            if (prop.DeclaredAccessibility != Accessibility.Public)
                continue;

            if (prop.GetMethod == null)
                continue;

            var propMeta = ParseProperty(prop, compilation);
            if (propMeta != null)
                properties.Add(propMeta);
        }

        var ns = typeSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : typeSymbol.ContainingNamespace.ToDisplayString();

        return new TypeMetadata(
            ns,
            typeSymbol.Name,
            typeSymbol.ToDisplayString(),
            properties,
            typeSymbol.IsValueType);
    }

    private static PropertyMetadata? ParseProperty(IPropertySymbol prop, Compilation compilation)
    {
        var isIgnored = HasAttribute(prop, MdfIgnoreAttribute);
        var isSection = HasAttribute(prop, MdfSectionAttribute);
        var sectionLevel = 2;
        string? sectionName = null;

        if (isSection)
        {
            var sectionAttr = prop.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == MdfSectionAttribute);
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

        var mdfName = prop.Name;
        var nameAttr = prop.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == MdfPropertyNameAttribute);
        if (nameAttr?.ConstructorArguments.Length > 0 &&
            nameAttr.ConstructorArguments[0].Value is string customName)
        {
            mdfName = customName;
        }

        var (kind, elementTypeName, elementProperties) = DeterminePropertyKind(prop.Type, compilation);

        return new PropertyMetadata(
            prop.Name,
            mdfName,
            prop.Type.ToDisplayString(),
            kind,
            isIgnored,
            isSection,
            sectionLevel,
            sectionName,
            elementTypeName,
            elementProperties);
    }

    private static (PropertyKind Kind, string? ElementTypeName, IReadOnlyList<PropertyMetadata>? ElementProperties)
        DeterminePropertyKind(ITypeSymbol type, Compilation compilation)
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
            SpecialType.System_String => (PropertyKind.String, null, null),
            SpecialType.System_Boolean => (PropertyKind.Boolean, null, null),
            SpecialType.System_Int32 => (PropertyKind.Int32, null, null),
            SpecialType.System_Int64 => (PropertyKind.Int64, null, null),
            SpecialType.System_Double => (PropertyKind.Double, null, null),
            SpecialType.System_Decimal => (PropertyKind.Decimal, null, null),
            _ => DetermineComplexPropertyKind(type, compilation)
        };
    }

    private static (PropertyKind Kind, string? ElementTypeName, IReadOnlyList<PropertyMetadata>? ElementProperties)
        DetermineComplexPropertyKind(ITypeSymbol type, Compilation compilation)
    {
        var typeName = type.ToDisplayString();

        // DateTime types
        if (typeName == "System.DateTime")
            return (PropertyKind.DateTime, null, null);
        if (typeName == "System.DateTimeOffset")
            return (PropertyKind.DateTimeOffset, null, null);

        // Check for arrays
        if (type is IArrayTypeSymbol arrayType)
        {
            var elementType = arrayType.ElementType;
            if (elementType.SpecialType == SpecialType.System_String)
                return (PropertyKind.StringArray, null, null);

            var elementProps = GetTypeProperties(elementType, compilation);
            return (PropertyKind.ComplexArray, elementType.ToDisplayString(), elementProps);
        }

        // Check for IEnumerable<T> / List<T> / etc.
        if (type is INamedTypeSymbol namedType)
        {
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
                    if (elementType.SpecialType == SpecialType.System_String)
                        return (PropertyKind.StringArray, null, null);

                    var elementProps = GetTypeProperties(elementType, compilation);
                    return (PropertyKind.ComplexArray, elementType.ToDisplayString(), elementProps);
                }
            }
        }

        // Nested object
        if (type.TypeKind == TypeKind.Class || type.TypeKind == TypeKind.Struct)
        {
            var props = GetTypeProperties(type, compilation);
            if (props.Count > 0)
                return (PropertyKind.NestedObject, null, props);
        }

        return (PropertyKind.Other, null, null);
    }

    private static IReadOnlyList<PropertyMetadata> GetTypeProperties(ITypeSymbol type, Compilation compilation)
    {
        var properties = new List<PropertyMetadata>();

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

            var propMeta = ParseProperty(prop, compilation);
            if (propMeta != null)
                properties.Add(propMeta);
        }

        return properties;
    }

    private static bool HasAttribute(ISymbol symbol, string attributeName)
    {
        return symbol.GetAttributes()
            .Any(a => a.AttributeClass?.ToDisplayString() == attributeName);
    }
}
