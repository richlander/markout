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
    public int AutoFieldsCount { get; }
    public FieldLayoutKind FieldLayout { get; }
    public NamingPolicyKind NamingPolicy { get; }
    public DocumentLayoutKind Layout { get; }
    public bool SkipNullByDefault { get; }
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
        int autoFieldsCount = 0,
        FieldLayoutKind fieldLayout = FieldLayoutKind.Table,
        NamingPolicyKind namingPolicy = NamingPolicyKind.PascalCaseWords,
        DocumentLayoutKind layout = DocumentLayoutKind.Document,
        bool skipNullByDefault = false,
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
        AutoFieldsCount = autoFieldsCount;
        FieldLayout = fieldLayout;
        NamingPolicy = namingPolicy;
        Layout = layout;
        SkipNullByDefault = skipNullByDefault;
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
               AutoFieldsCount == other.AutoFieldsCount &&
               FieldLayout == other.FieldLayout &&
               NamingPolicy == other.NamingPolicy &&
               SkipNullByDefault == other.SkipNullByDefault &&
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
            hash = hash * 397 ^ AutoFieldsCount;
            hash = hash * 397 ^ (int)FieldLayout;
            hash = hash * 397 ^ (int)NamingPolicy;
            hash = hash * 397 ^ SkipNullByDefault.GetHashCode();
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

    private static bool StringSequenceEqual(IReadOnlyList<string> a, IReadOnlyList<string> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            if (a[i] != b[i]) return false;
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
    public string? SectionFormatProperty { get; }
    public string? SectionFormatterTypeName { get; }
    public string? SectionColumnName { get; }
    public string? SectionShowWhenProperty { get; }
    public string? SectionGroupByProperty { get; }
    public bool SectionHeadless { get; }
    public bool SectionIncludeInStructuredRows { get; }
    public MarkoutFieldOrderKind SectionFieldOrder { get; }
    public IReadOnlyList<(string MethodName, string ColumnName)>? SectionIgnoreColumnWhen { get; }
    public string? ElementTypeName { get; }
    public IReadOnlyList<PropertyMetadata>? ElementProperties { get; }
    public bool ElementHasNestedContent { get; }
    public string? ElementTitleProperty { get; }
    public string? ElementTitleContextProperty { get; }
    public bool ElementAutoFields { get; }
    public FieldLayoutKind ElementFieldLayout { get; }
    public string? BoolTrueValue { get; }
    public string? BoolFalseValue { get; }
    public bool IsNullableValueType { get; }
    public bool IsArray { get; }
    // A lone Metric/Breakdown property (not a List<T>) that still renders as its bar shape.
    public bool IsScalarShape { get; }
    public string? CustomFormat { get; }
    public string? JoinSeparator { get; }
    public bool SkipWhenDefault { get; }
    public string? ValueFormatterTypeName { get; }
    public bool SkipWhenNull { get; }
    public string? DisplayFormat { get; }
    public int? MaxItems { get; }
    public string? MaxItemsEllipsisFormat { get; }
    public string? TableDisplayFormat { get; }
    public string? ShowWhenProperty { get; }
    public bool IsLink { get; }
    public string? LinkTextProperty { get; }
    public IReadOnlyList<(string Key, string Value)>? ValueMap { get; }
    public bool IsUnwrapped { get; }
    public string? SectionEmptyText { get; }
    public MarkoutDeltaKind DeltaMode { get; }
    public string? Unit { get; }
    public MarkoutGoalKind Goal { get; }
    public double Noise { get; }
    public string? DeltaNoun { get; }
    public bool IsReferenceTypeCell { get; }
    public string? MultiSourceLabelHeader { get; }

    /// <summary>True when this property carries <c>[MarkoutChild]</c>: a bool flag marking its row as
    /// a nested child of the preceding row. Excluded from table columns; drives the child glyph.</summary>
    public bool IsChildFlag { get; }

    /// <summary>The <c>[MarkoutNumberFormat]</c> numeric format string (e.g. <c>"N0"</c>) applied to a
    /// numeric <c>Change&lt;&gt;</c> cell's operands and derived delta; <c>null</c> when unset.</summary>
    public string? NumberFormat { get; }

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
        string? sectionFormatProperty = null,
        string? sectionFormatterTypeName = null,
        string? sectionColumnName = null,
        string? sectionShowWhenProperty = null,
        string? sectionGroupByProperty = null,
        bool sectionHeadless = false,
        bool sectionIncludeInStructuredRows = false,
        MarkoutFieldOrderKind sectionFieldOrder = MarkoutFieldOrderKind.Input,
        IReadOnlyList<(string MethodName, string ColumnName)>? sectionIgnoreColumnWhen = null,
        string? elementTypeName = null,
        IReadOnlyList<PropertyMetadata>? elementProperties = null,
        bool elementHasNestedContent = false,
        string? elementTitleProperty = null,
        string? elementTitleContextProperty = null,
        bool elementAutoFields = true,
        FieldLayoutKind elementFieldLayout = FieldLayoutKind.Table,
        string? boolTrueValue = null,
        string? boolFalseValue = null,
        bool isNullableValueType = false,
        bool isArray = false,
        string? customFormat = null,
        string? joinSeparator = null,
        bool skipWhenDefault = false,
        string? valueFormatterTypeName = null,
        bool skipWhenNull = false,
        string? displayFormat = null,
        int? maxItems = null,
        string? maxItemsEllipsisFormat = null,
        string? tableDisplayFormat = null,
        string? showWhenProperty = null,
        bool isLink = false,
        string? linkTextProperty = null,
        IReadOnlyList<(string Key, string Value)>? valueMap = null,
        bool isUnwrapped = false,
        string? sectionEmptyText = null,
        MarkoutDeltaKind deltaMode = MarkoutDeltaKind.None,
        string? unit = null,
        bool isReferenceTypeCell = false,
        string? multiSourceLabelHeader = null,
        MarkoutGoalKind goal = MarkoutGoalKind.Context,
        double noise = 0,
        string? deltaNoun = null,
        bool isScalarShape = false,
        bool isChildFlag = false,
        string? numberFormat = null)
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
        SectionFormatProperty = sectionFormatProperty;
        SectionFormatterTypeName = sectionFormatterTypeName;
        SectionColumnName = sectionColumnName;
        SectionShowWhenProperty = sectionShowWhenProperty;
        SectionGroupByProperty = sectionGroupByProperty;
        SectionHeadless = sectionHeadless;
        SectionIncludeInStructuredRows = sectionIncludeInStructuredRows;
        SectionFieldOrder = sectionFieldOrder;
        SectionIgnoreColumnWhen = sectionIgnoreColumnWhen;
        ElementTypeName = elementTypeName;
        ElementProperties = elementProperties;
        ElementHasNestedContent = elementHasNestedContent;
        ElementTitleProperty = elementTitleProperty;
        ElementTitleContextProperty = elementTitleContextProperty;
        ElementAutoFields = elementAutoFields;
        ElementFieldLayout = elementFieldLayout;
        BoolTrueValue = boolTrueValue;
        BoolFalseValue = boolFalseValue;
        IsNullableValueType = isNullableValueType;
        IsArray = isArray;
        IsScalarShape = isScalarShape;
        CustomFormat = customFormat;
        JoinSeparator = joinSeparator;
        SkipWhenDefault = skipWhenDefault;
        ValueFormatterTypeName = valueFormatterTypeName;
        SkipWhenNull = skipWhenNull;
        DisplayFormat = displayFormat;
        MaxItems = maxItems;
        MaxItemsEllipsisFormat = maxItemsEllipsisFormat;
        TableDisplayFormat = tableDisplayFormat;
        ShowWhenProperty = showWhenProperty;
        IsLink = isLink;
        LinkTextProperty = linkTextProperty;
        ValueMap = valueMap;
        IsUnwrapped = isUnwrapped;
        SectionEmptyText = sectionEmptyText;
        DeltaMode = deltaMode;
        Unit = unit;
        Goal = goal;
        Noise = noise;
        DeltaNoun = deltaNoun;
        IsReferenceTypeCell = isReferenceTypeCell;
        MultiSourceLabelHeader = multiSourceLabelHeader;
        IsChildFlag = isChildFlag;
        NumberFormat = numberFormat;
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
               SectionFormatProperty == other.SectionFormatProperty &&
               SectionFormatterTypeName == other.SectionFormatterTypeName &&
               SectionColumnName == other.SectionColumnName &&
               SectionShowWhenProperty == other.SectionShowWhenProperty &&
               SectionGroupByProperty == other.SectionGroupByProperty &&
               SectionHeadless == other.SectionHeadless &&
               SectionIncludeInStructuredRows == other.SectionIncludeInStructuredRows &&
               SectionFieldOrder == other.SectionFieldOrder &&
               IgnoreColumnWhenEqual(SectionIgnoreColumnWhen, other.SectionIgnoreColumnWhen) &&
               ElementTypeName == other.ElementTypeName &&
               ElementHasNestedContent == other.ElementHasNestedContent &&
               ElementTitleProperty == other.ElementTitleProperty &&
               ElementTitleContextProperty == other.ElementTitleContextProperty &&
               ElementAutoFields == other.ElementAutoFields &&
               ElementFieldLayout == other.ElementFieldLayout &&
               BoolTrueValue == other.BoolTrueValue &&
               BoolFalseValue == other.BoolFalseValue &&
               IsNullableValueType == other.IsNullableValueType &&
               IsArray == other.IsArray &&
               IsScalarShape == other.IsScalarShape &&
               CustomFormat == other.CustomFormat &&
               JoinSeparator == other.JoinSeparator &&
               SkipWhenDefault == other.SkipWhenDefault &&
               ValueFormatterTypeName == other.ValueFormatterTypeName &&
               SkipWhenNull == other.SkipWhenNull &&
               DisplayFormat == other.DisplayFormat &&
               MaxItems == other.MaxItems &&
               MaxItemsEllipsisFormat == other.MaxItemsEllipsisFormat &&
               TableDisplayFormat == other.TableDisplayFormat &&
               ShowWhenProperty == other.ShowWhenProperty &&
               IsLink == other.IsLink &&
               LinkTextProperty == other.LinkTextProperty &&
               ValueMapEqual(ValueMap, other.ValueMap) &&
               IsUnwrapped == other.IsUnwrapped &&
               SectionEmptyText == other.SectionEmptyText &&
               DeltaMode == other.DeltaMode &&
               Unit == other.Unit &&
               Goal == other.Goal &&
               Noise.Equals(other.Noise) &&
               DeltaNoun == other.DeltaNoun &&
               IsReferenceTypeCell == other.IsReferenceTypeCell &&
               MultiSourceLabelHeader == other.MultiSourceLabelHeader &&
               IsChildFlag == other.IsChildFlag &&
               NumberFormat == other.NumberFormat &&
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
            hash = hash * 397 ^ SkipWhenNull.GetHashCode();
            hash = hash * 397 ^ (DisplayFormat?.GetHashCode() ?? 0);
            hash = hash * 397 ^ (MaxItems?.GetHashCode() ?? 0);
            hash = hash * 397 ^ (MaxItemsEllipsisFormat?.GetHashCode() ?? 0);
            hash = hash * 397 ^ (TableDisplayFormat?.GetHashCode() ?? 0);
            hash = hash * 397 ^ ElementAutoFields.GetHashCode();
            hash = hash * 397 ^ (int)ElementFieldLayout;
            hash = hash * 397 ^ (SectionIgnoreProperty?.GetHashCode() ?? 0);
            hash = hash * 397 ^ (SectionFormatProperty?.GetHashCode() ?? 0);
            hash = hash * 397 ^ (SectionFormatterTypeName?.GetHashCode() ?? 0);
            hash = hash * 397 ^ (SectionColumnName?.GetHashCode() ?? 0);
            hash = hash * 397 ^ (SectionShowWhenProperty?.GetHashCode() ?? 0);
            hash = hash * 397 ^ (SectionGroupByProperty?.GetHashCode() ?? 0);
            hash = hash * 397 ^ SectionHeadless.GetHashCode();
            hash = hash * 397 ^ SectionIncludeInStructuredRows.GetHashCode();
            hash = hash * 397 ^ (int)SectionFieldOrder;
            hash = hash * 397 ^ (SectionIgnoreColumnWhen?.Count ?? 0);
            hash = hash * 397 ^ (ValueFormatterTypeName?.GetHashCode() ?? 0);
            hash = hash * 397 ^ (ShowWhenProperty?.GetHashCode() ?? 0);
            hash = hash * 397 ^ IsLink.GetHashCode();
            hash = hash * 397 ^ (LinkTextProperty?.GetHashCode() ?? 0);
            hash = hash * 397 ^ (ValueMap?.Count ?? 0);
            hash = hash * 397 ^ IsUnwrapped.GetHashCode();
            hash = hash * 397 ^ (SectionEmptyText?.GetHashCode() ?? 0);
            hash = hash * 397 ^ (int)DeltaMode;
            hash = hash * 397 ^ (Unit?.GetHashCode() ?? 0);
            hash = hash * 397 ^ (int)Goal;
            hash = hash * 397 ^ Noise.GetHashCode();
            hash = hash * 397 ^ (DeltaNoun?.GetHashCode() ?? 0);
            hash = hash * 397 ^ IsScalarShape.GetHashCode();
            hash = hash * 397 ^ IsReferenceTypeCell.GetHashCode();
            hash = hash * 397 ^ (MultiSourceLabelHeader?.GetHashCode() ?? 0);
            hash = hash * 397 ^ IsChildFlag.GetHashCode();
            hash = hash * 397 ^ (NumberFormat?.GetHashCode() ?? 0);
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

    private static bool ValueMapEqual(IReadOnlyList<(string Key, string Value)>? a, IReadOnlyList<(string Key, string Value)>? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return a is null && b is null;
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            if (a[i].Key != b[i].Key || a[i].Value != b[i].Value) return false;
        return true;
    }

    private static bool IgnoreColumnWhenEqual(IReadOnlyList<(string MethodName, string ColumnName)>? a, IReadOnlyList<(string MethodName, string ColumnName)>? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return a is null && b is null;
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            if (a[i].MethodName != b[i].MethodName || a[i].ColumnName != b[i].ColumnName) return false;
        return true;
    }
}

/// <summary>
/// How scalar fields are laid out in generated code.
/// Values mirror the runtime Markout.FieldLayout enum.
/// </summary>
internal enum FieldLayoutKind
{
    Table = 0,
    Inline = 1,
    Bulleted = 2,
    Numbered = 3,
    Plain = 4
}

/// <summary>
/// Internal representation of NamingPolicy. Values mirror the runtime Markout.NamingPolicy enum.
/// </summary>
internal enum NamingPolicyKind
{
    PascalCaseWords = 0,
    Verbatim = 1
}

/// <summary>
/// Internal representation of DocumentLayout. Values mirror the runtime Markout.DocumentLayout enum.
/// </summary>
internal enum DocumentLayoutKind
{
    Document = 0,
    Tree = 1
}

/// <summary>
/// Internal representation of MarkoutFieldOrder. Values mirror the runtime Markout.MarkoutFieldOrder enum.
/// </summary>
internal enum MarkoutFieldOrderKind
{
    Input = 0,
    Alphabetical = 1
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
    Graph,
    Metric,
    Description,
    CodeSection,
    Callout,
    Breakdown,
    MetricChange,
    MultiSource,
    CompositeCell,
    Other
}

/// <summary>
/// Internal representation of Markout.Delta. Values mirror the runtime enum.
/// </summary>
internal enum MarkoutDeltaKind
{
    None = 0,
    Percent = 1,
    Absolute = 2,
    Multiple = 3
}

/// <summary>
/// Internal representation of Markout.Goal. Values mirror the runtime enum.
/// </summary>
internal enum MarkoutGoalKind
{
    Context = 0,
    Higher = 1,
    Lower = 2
}
