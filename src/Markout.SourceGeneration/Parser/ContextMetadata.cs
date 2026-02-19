using System;
using System.Collections.Generic;

namespace Markout.SourceGeneration.Parser;

/// <summary>
/// Metadata about a class marked with [MarkoutContext] attributes.
/// </summary>
internal sealed class ContextMetadata : IEquatable<ContextMetadata>
{
    public string Namespace { get; }
    public string ClassName { get; }
    public bool IsPublic { get; }
    public IReadOnlyList<TypeMetadata> Types { get; }
    public bool? BoldFieldNames { get; }
    public bool? IncludeBadges { get; }
    public bool? IncludeDescription { get; }
    public bool SuppressTableWarnings { get; }

    public ContextMetadata(string @namespace, string className, bool isPublic, IReadOnlyList<TypeMetadata> types,
        bool? boldFieldNames = null, bool? includeBadges = null, bool? includeDescription = null,
        bool suppressTableWarnings = false)
    {
        Namespace = @namespace;
        ClassName = className;
        IsPublic = isPublic;
        Types = types;
        BoldFieldNames = boldFieldNames;
        IncludeBadges = includeBadges;
        IncludeDescription = includeDescription;
        SuppressTableWarnings = suppressTableWarnings;
    }

    public bool Equals(ContextMetadata? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Namespace == other.Namespace &&
               ClassName == other.ClassName &&
               IsPublic == other.IsPublic &&
               BoldFieldNames == other.BoldFieldNames &&
               IncludeBadges == other.IncludeBadges &&
               IncludeDescription == other.IncludeDescription &&
               SuppressTableWarnings == other.SuppressTableWarnings &&
               SequenceEqual(Types, other.Types);
    }

    public override bool Equals(object? obj) => Equals(obj as ContextMetadata);
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = (Namespace?.GetHashCode() ?? 0) * 397 ^ (ClassName?.GetHashCode() ?? 0);
            hash = hash * 397 ^ IsPublic.GetHashCode();
            hash = hash * 397 ^ (BoldFieldNames?.GetHashCode() ?? 0);
            hash = hash * 397 ^ (IncludeBadges?.GetHashCode() ?? 0);
            hash = hash * 397 ^ (IncludeDescription?.GetHashCode() ?? 0);
            hash = hash * 397 ^ SuppressTableWarnings.GetHashCode();
            foreach (var type in Types)
                hash = hash * 397 ^ type.GetHashCode();
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
