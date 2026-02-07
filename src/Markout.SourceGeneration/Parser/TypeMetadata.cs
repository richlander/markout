using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Markout.SourceGeneration.Parser;

/// <summary>
/// Stores diagnostic information to be reported during source generation.
/// </summary>
internal sealed class DiagnosticInfo
{
    public DiagnosticDescriptor Descriptor { get; }
    public Location? Location { get; }
    public object[] MessageArgs { get; }

    public DiagnosticInfo(DiagnosticDescriptor descriptor, Location? location, params object[] messageArgs)
    {
        Descriptor = descriptor;
        Location = location;
        MessageArgs = messageArgs;
    }
}

/// <summary>
/// Metadata about a type marked with [MarkoutSerializable].
/// </summary>
internal sealed class TypeMetadata : IEquatable<TypeMetadata>
{
    public string Namespace { get; }
    public string TypeName { get; }
    public string FullTypeName { get; }
    public IReadOnlyList<PropertyMetadata> Properties { get; }
    public bool IsValueType { get; }
    public string? TitleProperty { get; }
    public string? TitleContextProperty { get; }
    public string? DescriptionProperty { get; }
    public bool AutoFields { get; }
    public FieldLayoutKind FieldLayout { get; }
    public IReadOnlyList<DiagnosticInfo> Diagnostics { get; }

    public TypeMetadata(
        string @namespace,
        string typeName,
        string fullTypeName,
        IReadOnlyList<PropertyMetadata> properties,
        bool isValueType,
        string? titleProperty = null,
        string? titleContextProperty = null,
        string? descriptionProperty = null,
        bool autoFields = true,
        FieldLayoutKind fieldLayout = FieldLayoutKind.OneLine,
        IReadOnlyList<DiagnosticInfo>? diagnostics = null)
    {
        Namespace = @namespace;
        TypeName = typeName;
        FullTypeName = fullTypeName;
        Properties = properties;
        IsValueType = isValueType;
        TitleProperty = titleProperty;
        TitleContextProperty = titleContextProperty;
        DescriptionProperty = descriptionProperty;
        AutoFields = autoFields;
        FieldLayout = fieldLayout;
        Diagnostics = diagnostics ?? Array.Empty<DiagnosticInfo>();
    }

    public bool Equals(TypeMetadata? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return FullTypeName == other.FullTypeName &&
               Namespace == other.Namespace &&
               TypeName == other.TypeName &&
               IsValueType == other.IsValueType &&
               TitleProperty == other.TitleProperty &&
               TitleContextProperty == other.TitleContextProperty &&
               DescriptionProperty == other.DescriptionProperty &&
               AutoFields == other.AutoFields &&
               FieldLayout == other.FieldLayout &&
               SequenceEqual(Properties, other.Properties);
    }

    public override bool Equals(object? obj) => Equals(obj as TypeMetadata);
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = FullTypeName.GetHashCode();
            hash = hash * 397 ^ IsValueType.GetHashCode();
            hash = hash * 397 ^ AutoFields.GetHashCode();
            hash = hash * 397 ^ (int)FieldLayout;
            foreach (var prop in Properties)
                hash = hash * 397 ^ prop.GetHashCode();
            return hash;
        }
    }

    private static bool SequenceEqual<T>(IReadOnlyList<T> a, IReadOnlyList<T> b) where T : IEquatable<T>
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            if (!a[i].Equals(b[i])) return false;
        return true;
    }
}

/// <summary>
/// Metadata about a property to serialize.
/// </summary>
internal sealed class PropertyMetadata : IEquatable<PropertyMetadata>
{
    public string Name { get; }
    public string DisplayName { get; }
    public string TypeName { get; }
    public PropertyKind Kind { get; }
    public bool IsIgnored { get; }
    public bool IsIgnoredInTable { get; }
    public bool IsUnsupportedInTable { get; }
    public bool IsSection { get; }
    public int SectionLevel { get; }
    public string? SectionName { get; }
    public string? SectionIgnoreProperty { get; }
    public string? ElementTypeName { get; }
    public IReadOnlyList<PropertyMetadata>? ElementProperties { get; }
    public bool ElementHasNestedContent { get; }
    public string? ElementTitleProperty { get; }
    public string? BoolTrueValue { get; }
    public string? BoolFalseValue { get; }
    public bool IsNullableValueType { get; }
    public bool IsArray { get; }
    public string? CustomFormat { get; }
    public string? JoinSeparator { get; }
    public bool SkipWhenDefault { get; }

    public PropertyMetadata(
        string name,
        string displayName,
        string typeName,
        PropertyKind kind,
        bool isIgnored = false,
        bool isIgnoredInTable = false,
        bool isUnsupportedInTable = false,
        bool isSection = false,
        int sectionLevel = 2,
        string? sectionName = null,
        string? sectionIgnoreProperty = null,
        string? elementTypeName = null,
        IReadOnlyList<PropertyMetadata>? elementProperties = null,
        bool elementHasNestedContent = false,
        string? elementTitleProperty = null,
        string? boolTrueValue = null,
        string? boolFalseValue = null,
        bool isNullableValueType = false,
        bool isArray = false,
        string? customFormat = null,
        string? joinSeparator = null,
        bool skipWhenDefault = false)
    {
        Name = name;
        DisplayName = displayName;
        TypeName = typeName;
        Kind = kind;
        IsIgnored = isIgnored;
        IsIgnoredInTable = isIgnoredInTable;
        IsUnsupportedInTable = isUnsupportedInTable;
        IsSection = isSection;
        SectionLevel = sectionLevel;
        SectionName = sectionName;
        SectionIgnoreProperty = sectionIgnoreProperty;
        ElementTypeName = elementTypeName;
        ElementProperties = elementProperties;
        ElementHasNestedContent = elementHasNestedContent;
        ElementTitleProperty = elementTitleProperty;
        BoolTrueValue = boolTrueValue;
        BoolFalseValue = boolFalseValue;
        IsNullableValueType = isNullableValueType;
        IsArray = isArray;
        CustomFormat = customFormat;
        JoinSeparator = joinSeparator;
        SkipWhenDefault = skipWhenDefault;
    }

    public bool Equals(PropertyMetadata? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Name == other.Name &&
               DisplayName == other.DisplayName &&
               TypeName == other.TypeName &&
               Kind == other.Kind &&
               IsIgnored == other.IsIgnored &&
               IsIgnoredInTable == other.IsIgnoredInTable &&
               IsUnsupportedInTable == other.IsUnsupportedInTable &&
               IsSection == other.IsSection &&
               SectionLevel == other.SectionLevel &&
               SectionName == other.SectionName &&
               SectionIgnoreProperty == other.SectionIgnoreProperty &&
               ElementTypeName == other.ElementTypeName &&
               ElementHasNestedContent == other.ElementHasNestedContent &&
               ElementTitleProperty == other.ElementTitleProperty &&
               BoolTrueValue == other.BoolTrueValue &&
               BoolFalseValue == other.BoolFalseValue &&
               IsNullableValueType == other.IsNullableValueType &&
               IsArray == other.IsArray &&
               CustomFormat == other.CustomFormat &&
               JoinSeparator == other.JoinSeparator &&
               SkipWhenDefault == other.SkipWhenDefault &&
               SequenceEqual(ElementProperties, other.ElementProperties);
    }

    public override bool Equals(object? obj) => Equals(obj as PropertyMetadata);
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = (Name?.GetHashCode() ?? 0) * 397 ^ (TypeName?.GetHashCode() ?? 0);
            hash = hash * 397 ^ (int)Kind;
            hash = hash * 397 ^ (DisplayName?.GetHashCode() ?? 0);
            hash = hash * 397 ^ SkipWhenDefault.GetHashCode();
            hash = hash * 397 ^ (SectionIgnoreProperty?.GetHashCode() ?? 0);
            return hash;
        }
    }

    private static bool SequenceEqual(IReadOnlyList<PropertyMetadata>? a, IReadOnlyList<PropertyMetadata>? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return a is null && b is null;
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            if (!a[i].Equals(b[i])) return false;
        return true;
    }
}

/// <summary>
/// How scalar fields are laid out in generated code.
/// Values mirror the runtime Markout.FieldLayout enum.
/// </summary>
internal enum FieldLayoutKind
{
    OneLine = 0,
    LineBreaks = 1,
    LineBreaksDoubleSpace = 2,
    List = 3
}

/// <summary>
/// The kind of property for serialization purposes.
/// </summary>
internal enum PropertyKind
{
    String,
    Boolean,
    Int32,
    Int64,
    Double,
    Decimal,
    DateTime,
    DateTimeOffset,
    Enum,
    Formattable,
    StringArray,
    ComplexArray,
    NestedObject,
    FieldCollection,
    Tree,
    Other
}
