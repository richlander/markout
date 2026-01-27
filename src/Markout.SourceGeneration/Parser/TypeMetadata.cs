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
    public string? DescriptionProperty { get; }
    public IReadOnlyList<DiagnosticInfo> Diagnostics { get; }

    public TypeMetadata(
        string @namespace,
        string typeName,
        string fullTypeName,
        IReadOnlyList<PropertyMetadata> properties,
        bool isValueType,
        string? titleProperty = null,
        string? descriptionProperty = null,
        IReadOnlyList<DiagnosticInfo>? diagnostics = null)
    {
        Namespace = @namespace;
        TypeName = typeName;
        FullTypeName = fullTypeName;
        Properties = properties;
        IsValueType = isValueType;
        TitleProperty = titleProperty;
        DescriptionProperty = descriptionProperty;
        Diagnostics = diagnostics ?? Array.Empty<DiagnosticInfo>();
    }

    public bool Equals(TypeMetadata? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return FullTypeName == other.FullTypeName;
    }

    public override bool Equals(object? obj) => Equals(obj as TypeMetadata);
    public override int GetHashCode() => FullTypeName.GetHashCode();
}

/// <summary>
/// Metadata about a property to serialize.
/// </summary>
internal sealed class PropertyMetadata : IEquatable<PropertyMetadata>
{
    public string Name { get; }
    public string MdfName { get; }
    public string TypeName { get; }
    public PropertyKind Kind { get; }
    public bool IsIgnored { get; }
    public bool IsIgnoredInTable { get; }
    public bool IsUnsupportedInTable { get; }
    public bool IsSection { get; }
    public int SectionLevel { get; }
    public string? SectionName { get; }
    public string? ElementTypeName { get; }
    public IReadOnlyList<PropertyMetadata>? ElementProperties { get; }
    public bool ElementHasNestedContent { get; }
    public string? ElementTitleProperty { get; }

    public PropertyMetadata(
        string name,
        string mdfName,
        string typeName,
        PropertyKind kind,
        bool isIgnored = false,
        bool isIgnoredInTable = false,
        bool isUnsupportedInTable = false,
        bool isSection = false,
        int sectionLevel = 2,
        string? sectionName = null,
        string? elementTypeName = null,
        IReadOnlyList<PropertyMetadata>? elementProperties = null,
        bool elementHasNestedContent = false,
        string? elementTitleProperty = null)
    {
        Name = name;
        MdfName = mdfName;
        TypeName = typeName;
        Kind = kind;
        IsIgnored = isIgnored;
        IsIgnoredInTable = isIgnoredInTable;
        IsUnsupportedInTable = isUnsupportedInTable;
        IsSection = isSection;
        SectionLevel = sectionLevel;
        SectionName = sectionName;
        ElementTypeName = elementTypeName;
        ElementProperties = elementProperties;
        ElementHasNestedContent = elementHasNestedContent;
        ElementTitleProperty = elementTitleProperty;
    }

    public bool Equals(PropertyMetadata? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Name == other.Name && TypeName == other.TypeName;
    }

    public override bool Equals(object? obj) => Equals(obj as PropertyMetadata);
    public override int GetHashCode()
    {
        unchecked
        {
            return (Name?.GetHashCode() ?? 0) * 397 ^ (TypeName?.GetHashCode() ?? 0);
        }
    }
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
    StringArray,
    ComplexArray,
    NestedObject,
    Other
}
