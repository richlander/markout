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

        var serializableAttr = typeSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == MdfSerializableAttribute);

        if (serializableAttr == null)
            return null;

        string? titleProperty = null;
        string? descriptionProperty = null;
        foreach (var named in serializableAttr.NamedArguments)
        {
            if (named.Key == "TitleProperty" && named.Value.Value is string tp)
                titleProperty = tp;
            else if (named.Key == "DescriptionProperty" && named.Value.Value is string dp)
                descriptionProperty = dp;
        }

        return ParseTypeSymbol(typeSymbol, context.SemanticModel.Compilation, null, titleProperty, descriptionProperty);
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
                // Pass the generator context for diagnostics
                var typeMeta = ParseTypeSymbol(typeArg, context.SemanticModel.Compilation, null, null);
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
        string? descriptionProperty = null)
    {
        // If titleProperty/descriptionProperty not passed, try to get them from the type's [MdfSerializable] attribute
        if (titleProperty == null || descriptionProperty == null)
        {
            var serializableAttr = typeSymbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == MdfSerializableAttribute);
            if (serializableAttr != null)
            {
                foreach (var named in serializableAttr.NamedArguments)
                {
                    if (named.Key == "TitleProperty" && named.Value.Value is string tp)
                        titleProperty ??= tp;
                    else if (named.Key == "DescriptionProperty" && named.Value.Value is string dp)
                        descriptionProperty ??= dp;
                }
            }
        }

        // Check if this type is used in a List<T> (table context)
        bool isInTableContext = IsUsedInList(typeSymbol, compilation);

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

            var propMeta = ParseProperty(prop, compilation, isInTableContext, diagnostics);
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
            typeSymbol.IsValueType,
            titleProperty,
            descriptionProperty,
            diagnostics);
    }

    private static PropertyMetadata? ParseProperty(
        IPropertySymbol prop, 
        Compilation compilation,
        bool isInTableContext,
        List<DiagnosticInfo> diagnostics)
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

        var (kind, elementTypeName, elementProperties) = DeterminePropertyKind(prop.Type, compilation, diagnostics);

        // Validate if in table context and collect diagnostics
        if (isInTableContext && !isIgnored && !isSection)
        {
            if (!IsScalarKind(kind))
            {
                diagnostics.Add(new DiagnosticInfo(
                    DiagnosticDescriptors.NonScalarPropertyInTable,
                    prop.Locations.FirstOrDefault(),
                    prop.Name,
                    prop.ContainingType.Name,
                    GetKindDisplayName(kind)
                ));
            }
        }

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
        DeterminePropertyKind(ITypeSymbol type, Compilation compilation, List<DiagnosticInfo>? diagnostics = null)
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
            _ => DetermineComplexPropertyKind(type, compilation, diagnostics)
        };
    }

    private static (PropertyKind Kind, string? ElementTypeName, IReadOnlyList<PropertyMetadata>? ElementProperties)
        DetermineComplexPropertyKind(ITypeSymbol type, Compilation compilation, List<DiagnosticInfo>? diagnostics = null)
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

            var elementProps = GetTypeProperties(elementType, compilation, true, diagnostics);
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

                    var elementProps = GetTypeProperties(elementType, compilation, true, diagnostics);
                    return (PropertyKind.ComplexArray, elementType.ToDisplayString(), elementProps);
                }
            }
        }

        // Nested object
        if (type.TypeKind == TypeKind.Class || type.TypeKind == TypeKind.Struct)
        {
            var props = GetTypeProperties(type, compilation, false, diagnostics);
            if (props.Count > 0)
                return (PropertyKind.NestedObject, null, props);
        }

        return (PropertyKind.Other, null, null);
    }

    private static IReadOnlyList<PropertyMetadata> GetTypeProperties(
        ITypeSymbol type, 
        Compilation compilation,
        bool isInTableContext = false,
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

            var propMeta = ParseProperty(prop, compilation, isInTableContext, diagnostics);
            if (propMeta != null)
                properties.Add(propMeta);
        }

        return properties;
    }

    private static bool IsUsedInList(INamedTypeSymbol type, Compilation compilation)
    {
        // Search through all syntax trees to find property declarations
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot();
            
            foreach (var node in root.DescendantNodes())
            {
                if (node is PropertyDeclarationSyntax propDecl)
                {
                    var propSymbol = semanticModel.GetDeclaredSymbol(propDecl) as IPropertySymbol;
                    if (propSymbol != null && IsGenericListOf(propSymbol.Type, type))
                    {
                        return true;
                    }
                }
            }
        }
        
        return false;
    }

    private static bool IsGenericListOf(ITypeSymbol propertyType, INamedTypeSymbol targetType)
    {
        if (propertyType is not INamedTypeSymbol namedType)
            return false;

        // Check if it's a generic type
        if (!namedType.IsGenericType && namedType.TypeArguments.Length == 0)
            return false;

        // Check direct type arguments (List<TargetType>, IEnumerable<TargetType>, etc.)
        if (namedType.TypeArguments.Length > 0)
        {
            var firstArg = namedType.TypeArguments[0];
            if (SymbolEqualityComparer.Default.Equals(firstArg, targetType))
                return true;
        }

        // Check through all interfaces for IEnumerable<TargetType>
        foreach (var iface in namedType.AllInterfaces)
        {
            if (iface.TypeArguments.Length > 0 &&
                SymbolEqualityComparer.Default.Equals(iface.TypeArguments[0], targetType))
            {
                var ifaceTypeName = iface.OriginalDefinition.ToDisplayString();
                if (ifaceTypeName == "System.Collections.Generic.IEnumerable<T>" ||
                    ifaceTypeName == "System.Collections.Generic.ICollection<T>" ||
                    ifaceTypeName == "System.Collections.Generic.IList<T>")
                {
                    return true;
                }
            }
        }

        return false;
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
