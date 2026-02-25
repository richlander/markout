using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

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
    private const string MarkoutFormatAttribute = "Markout.MarkoutFormatAttribute";
    private const string MarkoutJoinAttribute = "Markout.MarkoutJoinAttribute";
    private const string MarkoutSkipDefaultAttribute = "Markout.MarkoutSkipDefaultAttribute";
    private const string MarkoutSkipNullAttribute = "Markout.MarkoutSkipNullAttribute";
    private const string MarkoutDisplayFormatAttribute = "Markout.MarkoutDisplayFormatAttribute";
    private const string MarkoutMaxItemsAttribute = "Markout.MarkoutMaxItemsAttribute";
    private const string MarkoutTableDisplayAttribute = "Markout.MarkoutTableDisplayAttribute";
    private const string MarkoutValueFormatterAttribute = "Markout.MarkoutValueFormatterAttribute";
    private const string MarkoutShowWhenAttribute = "Markout.MarkoutShowWhenAttribute";
    private const string MarkoutLinkAttribute = "Markout.MarkoutLinkAttribute";
    private const string MarkoutValueMapAttribute = "Markout.MarkoutValueMapAttribute";
    private const string MarkoutUnwrapAttribute = "Markout.MarkoutUnwrapAttribute";
    private const string MarkoutIgnoreColumnWhenAttribute = "Markout.MarkoutIgnoreColumnWhenAttribute";
    private const string MarkoutIgnoreFieldsAttribute = "Markout.MarkoutIgnoreFieldsAttribute";

    private const string MarkoutContextOptionsAttribute = "Markout.MarkoutContextOptionsAttribute";

    public static ContextMetadata? ParseContext(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
            return null;

        var contextAttributes = context.Attributes;

        if (contextAttributes.Length == 0)
            return null;

        var knownTypes = new KnownTypeSymbols(context.SemanticModel.Compilation);

        // Parse [MarkoutContextOptions] attribute (before processing types, as options affect type parsing)
        bool? boldFieldNames = null;
        bool? includeBadges = null;
        bool? includeDescription = null;
        bool suppressTableWarnings = false;
        var optionsAttr = classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == MarkoutContextOptionsAttribute);
        if (optionsAttr != null)
        {
            foreach (var named in optionsAttr.NamedArguments)
            {
                if (named.Key == "BoldFieldNames" && named.Value.Value is bool bf)
                    boldFieldNames = bf;
                else if (named.Key == "IncludeBadges" && named.Value.Value is bool ii)
                    includeBadges = ii;
                else if (named.Key == "IncludeDescription" && named.Value.Value is bool id)
                    includeDescription = id;
                else if (named.Key == "SuppressTableWarnings" && named.Value.Value is bool stw)
                    suppressTableWarnings = stw;
            }
        }

        var types = new List<TypeMetadata>();
        foreach (var attr in contextAttributes)
        {
            if (attr.ConstructorArguments.Length > 0 &&
                attr.ConstructorArguments[0].Value is INamedTypeSymbol typeArg)
            {
                var typeMeta = ParseTypeSymbol(typeArg, context.SemanticModel.Compilation, knownTypes, null, null, null, null, suppressTableWarnings: suppressTableWarnings);
                if (typeMeta != null)
                    types.Add(typeMeta);
            }
        }

        var ns = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : classSymbol.ContainingNamespace.ToDisplayString();

        return new ContextMetadata(ns, classSymbol.Name, classSymbol.DeclaredAccessibility == Accessibility.Public, types, boldFieldNames, includeBadges, includeDescription, suppressTableWarnings);
    }

    private static TypeMetadata? ParseTypeSymbol(
        INamedTypeSymbol typeSymbol,
        Compilation compilation,
        KnownTypeSymbols knownTypes,
        string? titleProperty = null,
        string? titleContextProperty = null,
        string? descriptionProperty = null,
        bool? autoFields = null,
        int? autoFieldsCount = null,
        FieldLayoutKind? fieldLayout = null,
        NamingPolicyKind? namingPolicy = null,
        bool suppressTableWarnings = false)
    {
        // If titleProperty/descriptionProperty not passed, try to get them from the type's [MarkoutSerializable] attribute
        if (titleProperty == null || titleContextProperty == null || descriptionProperty == null || autoFields == null || fieldLayout == null)
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
                    else if (named.Key == "AutoFields" && named.Value.Value is bool af)
                        autoFields ??= af;
                    else if (named.Key == "AutoFieldsCount" && named.Value.Value is int afc)
                        autoFieldsCount ??= afc;
                    else if (named.Key == "FieldLayout" && named.Value.Value is int fl)
                        fieldLayout ??= (FieldLayoutKind)fl;
                    else if (named.Key == "NamingPolicy" && named.Value.Value is int np)
                        namingPolicy ??= (NamingPolicyKind)np;
                }
            }
        }

        // AutoFieldsCount > 0 implies AutoFields = true
        if (autoFieldsCount is > 0)
            autoFields = true;

        // Default to true if not specified
        autoFields ??= true;
        // Default to 0 (unlimited)
        autoFieldsCount ??= 0;
        // Default to OneLine
        fieldLayout ??= FieldLayoutKind.OneLine;
        // Default naming policy
        namingPolicy ??= NamingPolicyKind.Default;

        // Check for type-level [MarkoutSkipNull]
        bool skipNullByDefault = typeSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.ToDisplayString() == MarkoutSkipNullAttribute);

        // Parse [MarkoutIgnoreFields] attributes (AllowMultiple)
        var ignoreFieldsWriters = typeSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.ToDisplayString() == MarkoutIgnoreFieldsAttribute)
            .Select(a => a.ConstructorArguments.Length > 0 && a.ConstructorArguments[0].Value is string s ? s : null)
            .Where(s => s != null)
            .Cast<string>()
            .ToList();

        var properties = new List<PropertyMetadata>();
        var diagnostics = new List<DiagnosticInfo>();

        // Seed visited set with the root type for cycle protection.
        // This prevents infinite recursion when a property's type graph
        // leads back to the root (direct or mutual). The root type stays
        // in the set for the entire parse; nested types use add/remove.
        var visitedTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IPropertySymbol prop)
                continue;

            if (prop.DeclaredAccessibility != Accessibility.Public)
                continue;

            if (prop.GetMethod == null)
                continue;

            var propMeta = ParseProperty(prop, compilation, knownTypes, diagnostics, visitedTypes, namingPolicy.Value, skipNullByDefault);
            if (propMeta != null)
                properties.Add(propMeta);
        }

        // Warn if AutoFields=false but no sections or field collections exist
        if (autoFields == false)
        {
            bool hasSectionOrFieldCollection = properties.Any(p => 
                !p.IsIgnored && (p.IsSection || p.Kind == PropertyKind.FieldCollection));
            
            if (!hasSectionOrFieldCollection)
            {
                diagnostics.Add(new DiagnosticInfo(
                    DiagnosticDescriptors.AutoFieldsNoContent,
                    typeSymbol.Locations.FirstOrDefault(),
                    typeSymbol.Name));
            }
        }

        var ns = typeSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : typeSymbol.ContainingNamespace.ToDisplayString();

        // Filter out MARKOUT001 warnings when SuppressTableWarnings is enabled
        var filteredDiagnostics = suppressTableWarnings
            ? diagnostics.Where(d => d.Descriptor.Id != "MARKOUT001").ToList()
            : diagnostics;

        return new TypeMetadata(
            ns,
            typeSymbol.Name,
            typeSymbol.ToDisplayString(),
            properties,
            typeSymbol.IsValueType,
            titleProperty,
            titleContextProperty,
            descriptionProperty,
            autoFields.Value,
            autoFieldsCount.Value,
            fieldLayout.Value,
            namingPolicy.Value,
            skipNullByDefault,
            ignoreFieldsWriters.Count > 0 ? ignoreFieldsWriters : null,
            filteredDiagnostics);
    }

    private static PropertyMetadata? ParseProperty(
        IPropertySymbol prop,
        Compilation compilation,
        KnownTypeSymbols knownTypes,
        List<DiagnosticInfo> diagnostics,
        HashSet<ITypeSymbol>? visitedTypes = null,
        NamingPolicyKind namingPolicy = NamingPolicyKind.Default,
        bool skipNullByDefault = false)
    {
        var isIgnored = HasAttribute(prop, MarkoutIgnoreAttribute);
        var isIgnoredInTable = HasAttribute(prop, MarkoutIgnoreInTableAttribute);
        var isSection = HasAttribute(prop, MarkoutSectionAttribute);
        var isUnwrapped = HasAttribute(prop, MarkoutUnwrapAttribute);
        var sectionLevel = 2;
        string? sectionName = null;
        string? sectionIgnoreProperty = null;
        string? sectionFormatProperty = null;
        string? sectionFormatterTypeName = null;
        string? sectionColumnName = null;
        string? sectionShowWhenProperty = null;
        string? sectionGroupByProperty = null;
        List<(string MethodName, string ColumnName)>? sectionIgnoreColumnWhen = null;

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
                    else if (named.Key == "IgnoreProperty" && named.Value.Value is string ip)
                        sectionIgnoreProperty = ip;
                    else if (named.Key == "FormatProperty" && named.Value.Value is string fp)
                        sectionFormatProperty = fp;
                    else if (named.Key == "Formatter" && named.Value.Value is INamedTypeSymbol formatterType)
                        sectionFormatterTypeName = formatterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    else if (named.Key == "ColumnName" && named.Value.Value is string cn)
                        sectionColumnName = cn;
                    else if (named.Key == "ShowWhenProperty" && named.Value.Value is string swp)
                        sectionShowWhenProperty = swp;
                    else if (named.Key == "GroupBy" && named.Value.Value is string gb)
                        sectionGroupByProperty = gb;
                }
            }
        }

        // Parse [MarkoutIgnoreColumnWhen] attributes (AllowMultiple)
        foreach (var icwAttr in prop.GetAttributes()
            .Where(a => a.AttributeClass?.ToDisplayString() == MarkoutIgnoreColumnWhenAttribute))
        {
            if (icwAttr.ConstructorArguments.Length >= 2 &&
                icwAttr.ConstructorArguments[0].Value is string methodName &&
                icwAttr.ConstructorArguments[1].Value is string columnName)
            {
                sectionIgnoreColumnWhen ??= new List<(string, string)>();
                sectionIgnoreColumnWhen.Add((methodName, columnName));
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
        else if (namingPolicy == NamingPolicyKind.PascalCaseWords)
        {
            displayName = SplitPascalCase(displayName);
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

        // Parse [MarkoutFormat] attribute
        string? customFormat = null;
        var formatAttr = prop.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == MarkoutFormatAttribute);
        if (formatAttr?.ConstructorArguments.Length > 0 &&
            formatAttr.ConstructorArguments[0].Value is string fmt)
        {
            customFormat = fmt;
        }

        // Parse [MarkoutValueFormatter] attribute
        string? valueFormatterTypeName = null;
        var valueFormatterAttr = prop.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == MarkoutValueFormatterAttribute);
        if (valueFormatterAttr?.ConstructorArguments.Length > 0 &&
            valueFormatterAttr.ConstructorArguments[0].Value is INamedTypeSymbol valueFormatterType)
        {
            valueFormatterTypeName = valueFormatterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        // Parse [MarkoutJoin] attribute
        string? joinSeparator = null;
        var joinAttr = prop.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == MarkoutJoinAttribute);
        if (joinAttr?.ConstructorArguments.Length > 0 &&
            joinAttr.ConstructorArguments[0].Value is string sep)
        {
            joinSeparator = sep;
        }

        // Parse [MarkoutSkipDefault] attribute
        bool skipWhenDefault = prop.GetAttributes()
            .Any(a => a.AttributeClass?.ToDisplayString() == MarkoutSkipDefaultAttribute);

        // Parse [MarkoutSkipNull] attribute (property-level or inherited from type-level)
        bool skipWhenNull = skipNullByDefault || prop.GetAttributes()
            .Any(a => a.AttributeClass?.ToDisplayString() == MarkoutSkipNullAttribute);

        // Parse [MarkoutDisplayFormat] attribute
        string? displayFormat = null;
        var displayFormatAttr = prop.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == MarkoutDisplayFormatAttribute);
        if (displayFormatAttr?.ConstructorArguments.Length > 0 &&
            displayFormatAttr.ConstructorArguments[0].Value is string df)
        {
            displayFormat = df;
        }

        // Parse [MarkoutMaxItems] attribute
        int? maxItems = null;
        string? maxItemsEllipsisFormat = null;
        var maxItemsAttr = prop.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == MarkoutMaxItemsAttribute);
        if (maxItemsAttr?.ConstructorArguments.Length > 0 &&
            maxItemsAttr.ConstructorArguments[0].Value is int mi)
        {
            maxItems = mi;
            foreach (var named in maxItemsAttr.NamedArguments)
            {
                if (named.Key == "EllipsisFormat" && named.Value.Value is string ef)
                    maxItemsEllipsisFormat = ef;
            }
        }

        // Parse [MarkoutTableDisplay] attribute
        string? tableDisplayFormat = null;
        var tableDisplayAttr = prop.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == MarkoutTableDisplayAttribute);
        if (tableDisplayAttr?.ConstructorArguments.Length > 0 &&
            tableDisplayAttr.ConstructorArguments[0].Value is string tdf)
        {
            tableDisplayFormat = tdf;
        }

        // Parse [MarkoutShowWhen] attribute
        string? showWhenProperty = null;
        var showWhenAttr = prop.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == MarkoutShowWhenAttribute);
        if (showWhenAttr?.ConstructorArguments.Length > 0 &&
            showWhenAttr.ConstructorArguments[0].Value is string swpField)
        {
            showWhenProperty = swpField;
        }

        // Parse [MarkoutLink] attribute
        bool isLink = false;
        string? linkTextProperty = null;
        var linkAttr = prop.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == MarkoutLinkAttribute);
        if (linkAttr != null)
        {
            isLink = true;
            foreach (var named in linkAttr.NamedArguments)
            {
                if (named.Key == "TextProperty" && named.Value.Value is string ltp)
                    linkTextProperty = ltp;
            }
        }

        // Parse [MarkoutValueMap] attribute
        IReadOnlyList<(string Key, string Value)>? valueMap = null;
        var valueMapAttr = prop.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == MarkoutValueMapAttribute);
        if (valueMapAttr != null && valueMapAttr.ConstructorArguments.Length > 0)
        {
            var entries = new List<(string Key, string Value)>();
            var arg = valueMapAttr.ConstructorArguments[0];
            if (arg.Kind == TypedConstantKind.Array)
            {
                foreach (var item in arg.Values)
                {
                    if (item.Value is string entry)
                    {
                        var eqIdx = entry.IndexOf('=');
                        if (eqIdx > 0)
                            entries.Add((entry.Substring(0, eqIdx), entry.Substring(eqIdx + 1)));
                    }
                }
            }
            if (entries.Count > 0)
                valueMap = entries;
        }

        // Detect nullable value types before determining property kind
        bool isNullableValueType = false;
        if (prop.Type is INamedTypeSymbol nullableCheck &&
            nullableCheck.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            isNullableValueType = true;
        }

        var (kind, elementTypeName, elementProperties, hasNestedContent, elementTitleProperty, elementTitleContextProperty, elementAutoFields, elementFieldLayout, isArray) = DeterminePropertyKind(prop.Type, compilation, knownTypes, diagnostics, prop.Name, prop.Locations.FirstOrDefault(), visitedTypes);

        // Determine if property is unsupported in table context
        // Joined string arrays are treated as scalars, so they're fine in tables
        bool isJoinedArray = kind == PropertyKind.StringArray && joinSeparator != null;
        bool isUnsupportedInTable = !isIgnored && !isSection && !isUnwrapped && !IsScalarKind(kind) && !isJoinedArray && kind != PropertyKind.Formattable;

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
            sectionIgnoreProperty,
            sectionFormatProperty,
            sectionFormatterTypeName,
            sectionColumnName,
            sectionShowWhenProperty,
            sectionGroupByProperty,
            sectionIgnoreColumnWhen,
            elementTypeName,
            elementProperties,
            hasNestedContent,
            elementTitleProperty,
            elementTitleContextProperty,
            elementAutoFields,
            elementFieldLayout,
            boolTrueValue,
            boolFalseValue,
            isNullableValueType,
            isArray,
            customFormat,
            joinSeparator,
            skipWhenDefault,
            valueFormatterTypeName,
            skipWhenNull,
            displayFormat,
            maxItems,
            maxItemsEllipsisFormat,
            tableDisplayFormat,
            showWhenProperty,
            isLink,
            linkTextProperty,
            valueMap,
            isUnwrapped);
    }

    private static (PropertyKind Kind, string? ElementTypeName, IReadOnlyList<PropertyMetadata>? ElementProperties, bool HasNestedContent, string? ElementTitleProperty, string? ElementTitleContextProperty, bool ElementAutoFields, FieldLayoutKind ElementFieldLayout, bool IsArray)
        DeterminePropertyKind(ITypeSymbol type, Compilation compilation, KnownTypeSymbols knownTypes, List<DiagnosticInfo>? diagnostics = null, string? propertyName = null, Location? propertyLocation = null, HashSet<ITypeSymbol>? visitedTypes = null)
    {
        // Check for nullable value types
        if (type is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            type = namedType.TypeArguments[0];
        }

        // Primitives
        return type.SpecialType switch
        {
            SpecialType.System_String => (PropertyKind.String, null, null, false, null, null, true, FieldLayoutKind.OneLine, false),
            SpecialType.System_Boolean => (PropertyKind.Boolean, null, null, false, null, null, true, FieldLayoutKind.OneLine, false),
            SpecialType.System_Int32 => (PropertyKind.Int32, null, null, false, null, null, true, FieldLayoutKind.OneLine, false),
            SpecialType.System_Int64 => (PropertyKind.Int64, null, null, false, null, null, true, FieldLayoutKind.OneLine, false),
            SpecialType.System_Double => (PropertyKind.Double, null, null, false, null, null, true, FieldLayoutKind.OneLine, false),
            SpecialType.System_Decimal => (PropertyKind.Decimal, null, null, false, null, null, true, FieldLayoutKind.OneLine, false),
            _ => DetermineComplexPropertyKind(type, compilation, knownTypes, diagnostics, propertyName, propertyLocation, visitedTypes)
        };
    }

    private static (PropertyKind Kind, string? ElementTypeName, IReadOnlyList<PropertyMetadata>? ElementProperties, bool HasNestedContent, string? ElementTitleProperty, string? ElementTitleContextProperty, bool ElementAutoFields, FieldLayoutKind ElementFieldLayout, bool IsArray)
        DetermineComplexPropertyKind(ITypeSymbol type, Compilation compilation, KnownTypeSymbols knownTypes, List<DiagnosticInfo>? diagnostics = null, string? propertyName = null, Location? propertyLocation = null, HashSet<ITypeSymbol>? visitedTypes = null)
    {
        // DateTime types
        if (SymbolEqualityComparer.Default.Equals(type, knownTypes.DateTime))
            return (PropertyKind.DateTime, null, null, false, null, null, true, FieldLayoutKind.OneLine, false);
        if (SymbolEqualityComparer.Default.Equals(type, knownTypes.DateTimeOffset))
            return (PropertyKind.DateTimeOffset, null, null, false, null, null, true, FieldLayoutKind.OneLine, false);

        // CodeSection type - renders as code
        if (SymbolEqualityComparer.Default.Equals(type, knownTypes.CodeSection))
            return (PropertyKind.CodeSection, null, null, false, null, null, true, FieldLayoutKind.OneLine, false);

        // Callout type - renders as admonition block
        if (SymbolEqualityComparer.Default.Equals(type, knownTypes.Callout))
            return (PropertyKind.Callout, null, null, false, null, null, true, FieldLayoutKind.OneLine, false);

        // Enum types
        if (type.TypeKind == TypeKind.Enum)
            return (PropertyKind.Enum, null, null, false, null, null, true, FieldLayoutKind.OneLine, false);

        // Check for arrays
        if (type is IArrayTypeSymbol arrayType)
        {
            var elementType = arrayType.ElementType;

            // Check for MarkoutField[] - renders as compact line or field table
            if (SymbolEqualityComparer.Default.Equals(elementType, knownTypes.MarkoutField))
                return (PropertyKind.FieldCollection, null, null, false, null, null, true, FieldLayoutKind.OneLine, true);

            if (elementType.SpecialType == SpecialType.System_String)
                return (PropertyKind.StringArray, null, null, false, null, null, true, FieldLayoutKind.OneLine, true);

            var elementSettings = GetElementTypeSettings(elementType);
            var elementProps = GetTypeProperties(elementType, compilation, knownTypes, diagnostics, visitedTypes, elementSettings.NamingPolicy, elementSettings.SkipNullByDefault);
            var hasNested = HasNestedContent(elementProps);
            return (PropertyKind.ComplexArray, elementType.ToDisplayString(), elementProps, hasNested, elementSettings.TitleProperty, elementSettings.TitleContextProperty, elementSettings.AutoFields, elementSettings.FieldLayout, true);
        }

        // Check for IEnumerable<T> / List<T> / etc.
        if (type is INamedTypeSymbol namedType)
        {
            // Detect Dictionary<TKey, TValue> before IEnumerable<T> since dictionaries implement IEnumerable<KeyValuePair>
            var isDictionary = knownTypes.IDictionary != null && namedType.AllInterfaces.Any(i =>
                SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, knownTypes.IDictionary));

            if (isDictionary)
            {
                if (diagnostics != null && propertyName != null)
                {
                    diagnostics.Add(new DiagnosticInfo(
                        DiagnosticDescriptors.DictionaryProperty,
                        propertyLocation,
                        propertyName));
                }
                return (PropertyKind.Other, null, null, false, null, null, true, FieldLayoutKind.OneLine, false);
            }

            var enumerableInterface = knownTypes.IEnumerable != null
                ? namedType.AllInterfaces.FirstOrDefault(i =>
                    SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, knownTypes.IEnumerable))
                : null;

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
                    if (SymbolEqualityComparer.Default.Equals(elementType, knownTypes.MarkoutField))
                    {
                        var typeDisplayString = namedType.OriginalDefinition.ToDisplayString();
                        if (typeDisplayString == "System.Collections.Generic.List<T>" ||
                            typeDisplayString == "System.Collections.Generic.IReadOnlyList<T>" ||
                            typeDisplayString == "System.Collections.Generic.IList<T>")
                        {
                            return (PropertyKind.FieldCollection, null, null, false, null, null, true, FieldLayoutKind.OneLine, false);
                        }
                        // IEnumerable<MarkoutField> without materialization is not supported
                        // User should use List<MarkoutField> or IReadOnlyList<MarkoutField>
                    }

                    // Check for List<TreeNode> - renders as tree structure
                    if (SymbolEqualityComparer.Default.Equals(elementType, knownTypes.TreeNode))
                    {
                        var typeDisplayString = namedType.OriginalDefinition.ToDisplayString();
                        if (typeDisplayString == "System.Collections.Generic.List<T>")
                        {
                            return (PropertyKind.Tree, null, null, false, null, null, true, FieldLayoutKind.OneLine, false);
                        }
                    }

                    // Check for IReadOnlyList<Metric> / List<Metric> - renders as bar chart
                    if (SymbolEqualityComparer.Default.Equals(elementType, knownTypes.Metric))
                    {
                        var typeDisplayString = namedType.OriginalDefinition.ToDisplayString();
                        if (typeDisplayString == "System.Collections.Generic.List<T>" ||
                            typeDisplayString == "System.Collections.Generic.IReadOnlyList<T>" ||
                            typeDisplayString == "System.Collections.Generic.IList<T>")
                        {
                            return (PropertyKind.Metric, null, null, false, null, null, true, FieldLayoutKind.OneLine, false);
                        }
                    }

                    // Check for IReadOnlyList<Description> / List<Description> - renders as labeled list
                    if (SymbolEqualityComparer.Default.Equals(elementType, knownTypes.Description))
                    {
                        var typeDisplayString = namedType.OriginalDefinition.ToDisplayString();
                        if (typeDisplayString == "System.Collections.Generic.List<T>" ||
                            typeDisplayString == "System.Collections.Generic.IReadOnlyList<T>" ||
                            typeDisplayString == "System.Collections.Generic.IList<T>")
                        {
                            return (PropertyKind.Description, null, null, false, null, null, true, FieldLayoutKind.OneLine, false);
                        }
                    }

                    // Check for IReadOnlyList<Breakdown> / List<Breakdown> - renders as distribution chart
                    if (SymbolEqualityComparer.Default.Equals(elementType, knownTypes.Breakdown))
                    {
                        var typeDisplayString = namedType.OriginalDefinition.ToDisplayString();
                        if (typeDisplayString == "System.Collections.Generic.List<T>" ||
                            typeDisplayString == "System.Collections.Generic.IReadOnlyList<T>" ||
                            typeDisplayString == "System.Collections.Generic.IList<T>")
                        {
                            return (PropertyKind.Breakdown, null, null, false, null, null, true, FieldLayoutKind.OneLine, false);
                        }
                    }

                    if (elementType.SpecialType == SpecialType.System_String)
                        return (PropertyKind.StringArray, null, null, false, null, null, true, FieldLayoutKind.OneLine, false);

                    var elementSettings = GetElementTypeSettings(elementType);
                    var elementProps = GetTypeProperties(elementType, compilation, knownTypes, diagnostics, visitedTypes, elementSettings.NamingPolicy, elementSettings.SkipNullByDefault);
                    var hasNested = HasNestedContent(elementProps);
                    return (PropertyKind.ComplexArray, elementType.ToDisplayString(), elementProps, hasNested, elementSettings.TitleProperty, elementSettings.TitleContextProperty, elementSettings.AutoFields, elementSettings.FieldLayout, false);
                }
            }
        }

        // IMarkoutFormattable: custom formatting via interface
        if (knownTypes.IMarkoutFormattable != null &&
            type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, knownTypes.IMarkoutFormattable)))
        {
            return (PropertyKind.Formattable, null, null, false, null, null, true, FieldLayoutKind.OneLine, false);
        }

        // Nested object
        if (type.TypeKind == TypeKind.Class || type.TypeKind == TypeKind.Struct)
        {
            var nestedSettings = GetElementTypeSettings(type);
            var props = GetTypeProperties(type, compilation, knownTypes, diagnostics, visitedTypes, nestedSettings.NamingPolicy, nestedSettings.SkipNullByDefault);
            if (props.Count > 0)
                return (PropertyKind.NestedObject, null, props, false, null, null, nestedSettings.AutoFields, nestedSettings.FieldLayout, false);
        }

        return (PropertyKind.Other, null, null, false, null, null, true, FieldLayoutKind.OneLine, false);
    }

    private static bool HasNestedContent(IReadOnlyList<PropertyMetadata>? props)
    {
        if (props == null) return false;
        return props.Any(p => !p.IsIgnored &&
            (p.IsSection || p.Kind == PropertyKind.Callout ||
             p.Kind == PropertyKind.NestedObject || p.Kind == PropertyKind.ComplexArray ||
             p.Kind == PropertyKind.FieldCollection || p.Kind == PropertyKind.Tree ||
             p.Kind == PropertyKind.Description || p.Kind == PropertyKind.Metric ||
             p.Kind == PropertyKind.CodeSection || p.Kind == PropertyKind.Breakdown ||
             (p.Kind == PropertyKind.StringArray && p.JoinSeparator == null)));
    }

    private static (string? TitleProperty, string? TitleContextProperty, bool AutoFields, FieldLayoutKind FieldLayout, NamingPolicyKind NamingPolicy, bool SkipNullByDefault) GetElementTypeSettings(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType)
            return (null, null, true, FieldLayoutKind.OneLine, NamingPolicyKind.Default, false);

        string? titleProperty = null;
        string? titleContextProperty = null;
        bool autoFields = true;
        FieldLayoutKind fieldLayout = FieldLayoutKind.OneLine;
        NamingPolicyKind namingPolicy = NamingPolicyKind.Default;

        var serializableAttr = namedType.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == MarkoutSerializableAttribute);

        if (serializableAttr != null)
        {
            foreach (var named in serializableAttr.NamedArguments)
            {
                if (named.Key == "TitleProperty" && named.Value.Value is string tp)
                    titleProperty = tp;
                else if (named.Key == "TitleContextProperty" && named.Value.Value is string tcp)
                    titleContextProperty = tcp;
                else if (named.Key == "AutoFields" && named.Value.Value is bool af)
                    autoFields = af;
                else if (named.Key == "FieldLayout" && named.Value.Value is int fl)
                    fieldLayout = (FieldLayoutKind)fl;
                else if (named.Key == "NamingPolicy" && named.Value.Value is int np)
                    namingPolicy = (NamingPolicyKind)np;
            }
        }

        bool skipNullByDefault = namedType.GetAttributes()
            .Any(a => a.AttributeClass?.ToDisplayString() == MarkoutSkipNullAttribute);

        return (titleProperty, titleContextProperty, autoFields, fieldLayout, namingPolicy, skipNullByDefault);
    }

    private static IReadOnlyList<PropertyMetadata> GetTypeProperties(
        ITypeSymbol type,
        Compilation compilation,
        KnownTypeSymbols? knownTypes = null,
        List<DiagnosticInfo>? diagnostics = null,
        HashSet<ITypeSymbol>? visitedTypes = null,
        NamingPolicyKind namingPolicy = NamingPolicyKind.Default,
        bool skipNullByDefault = false)
    {
        var properties = new List<PropertyMetadata>();
        diagnostics ??= new List<DiagnosticInfo>();
        knownTypes ??= new KnownTypeSymbols(compilation);

        if (type is not INamedTypeSymbol namedType)
            return properties;

        // Cycle protection: track types being parsed
        visitedTypes ??= new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        if (!visitedTypes.Add(type))
            return properties;

        try
        {
            foreach (var member in namedType.GetMembers())
            {
                if (member is not IPropertySymbol prop)
                    continue;

                if (prop.DeclaredAccessibility != Accessibility.Public)
                    continue;

                if (prop.GetMethod == null)
                    continue;

                var propMeta = ParseProperty(prop, compilation, knownTypes, diagnostics, visitedTypes, namingPolicy, skipNullByDefault);
                if (propMeta != null)
                    properties.Add(propMeta);
            }
        }
        finally
        {
            visitedTypes.Remove(type);
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
            PropertyKind.DateTimeOffset or
            PropertyKind.Enum;
    }

    private static string GetKindDisplayName(PropertyKind kind)
    {
        return kind switch
        {
            PropertyKind.StringArray => "a string array",
            PropertyKind.ComplexArray => "an array of complex objects",
            PropertyKind.NestedObject => "a complex object",
            PropertyKind.Formattable => "formattable",
            PropertyKind.Other => "a non-scalar type",
            _ => kind.ToString().ToLowerInvariant()
        };
    }

    private static bool HasAttribute(ISymbol symbol, string attributeName)
    {
        return symbol.GetAttributes()
            .Any(a => a.AttributeClass?.ToDisplayString() == attributeName);
    }

    /// <summary>
    /// Splits a PascalCase identifier into space-separated words.
    /// Example: "InformationalVersion" → "Informational Version", "PublicKeyToken" → "Public Key Token".
    /// </summary>
    private static string SplitPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var sb = new System.Text.StringBuilder(name.Length + 4);
        sb.Append(name[0]);

        for (int i = 1; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
            {
                sb.Append(' ');
            }
            else if (char.IsUpper(name[i]) && i + 1 < name.Length && !char.IsUpper(name[i + 1]))
            {
                // Handle acronym boundaries: "XMLParser" → "XML Parser"
                sb.Append(' ');
            }
            sb.Append(name[i]);
        }

        return sb.ToString();
    }
}
