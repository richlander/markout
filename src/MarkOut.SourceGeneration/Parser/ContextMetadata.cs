using System;
using System.Collections.Generic;

namespace MarkOut.SourceGeneration.Parser;

/// <summary>
/// Metadata about a class marked with [MarkOutContext] attributes.
/// </summary>
internal sealed class ContextMetadata : IEquatable<ContextMetadata>
{
    public string Namespace { get; }
    public string ClassName { get; }
    public IReadOnlyList<TypeMetadata> Types { get; }

    public ContextMetadata(string @namespace, string className, IReadOnlyList<TypeMetadata> types)
    {
        Namespace = @namespace;
        ClassName = className;
        Types = types;
    }

    public bool Equals(ContextMetadata? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Namespace == other.Namespace && ClassName == other.ClassName;
    }

    public override bool Equals(object? obj) => Equals(obj as ContextMetadata);
    public override int GetHashCode()
    {
        unchecked
        {
            return (Namespace?.GetHashCode() ?? 0) * 397 ^ (ClassName?.GetHashCode() ?? 0);
        }
    }
}
