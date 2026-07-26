namespace Markout.Templates;

/// <summary>
/// Wraps a bound value with its rendering strategy.
/// </summary>
public abstract class TemplateBinding
{
    /// <summary>
    /// Renders this binding as block-level content through the writer.
    /// </summary>
    public abstract void Render(MarkoutWriter writer);

    /// <summary>
    /// Returns an inline text representation for use within headings and paragraphs.
    /// </summary>
    public abstract string? RenderInline();

    /// <summary>
    /// Whether this binding represents a truthy value (for conditional sections).
    /// </summary>
    public abstract bool IsTruthy { get; }
}

/// <summary>
/// A binding for a simple string value. Used for inline substitution and conditional checks.
/// An empty string is falsy so <c>{{#if key}}</c> can gate on presence of real content.
/// </summary>
internal sealed class StringBinding(string? value) : TemplateBinding
{
    public override void Render(MarkoutWriter writer)
    {
        if (value is not null)
            writer.WriteParagraph(value);
    }

    public override string? RenderInline() => value;

    public override bool IsTruthy => !string.IsNullOrEmpty(value);
}

/// <summary>
/// A binding for a boolean value, used to drive conditional sections.
/// </summary>
internal sealed class BoolBinding(bool value) : TemplateBinding
{
    public override void Render(MarkoutWriter writer)
    {
        // A bare bool has no block representation; it exists to gate {{#if}} sections.
    }

    public override string? RenderInline() => value ? "true" : "false";

    public override bool IsTruthy => value;
}

/// <summary>
/// A binding for a sequence of strings, rendered as a bullet list at a block placeholder
/// (or joined with ", " inline). Empty and null sequences are falsy.
/// </summary>
internal sealed class ListBinding(string[]? items) : TemplateBinding
{
    public override void Render(MarkoutWriter writer)
    {
        if (items is { Length: > 0 })
            writer.WriteList(items);
    }

    public override string? RenderInline() => items is null ? null : string.Join(", ", items);

    public override bool IsTruthy => items is { Length: > 0 };
}

/// <summary>
/// A binding for a type that controls its own Markout rendering.
/// </summary>
internal sealed class FormattableBinding(IMarkoutFormattable? value) : TemplateBinding
{
    public override void Render(MarkoutWriter writer)
    {
        value?.WriteTo(writer);
    }

    public override string? RenderInline() => value?.ToMarkoutString();

    public override bool IsTruthy => value is not null;
}

/// <summary>
/// A binding for a source-generated Markout type, rendered through its TypeInfo.
/// </summary>
internal sealed class TypeInfoBinding<T>(T value, MarkoutTypeInfo<T> typeInfo) : TemplateBinding
{
    public override void Render(MarkoutWriter writer)
    {
        if (value is not null)
            typeInfo.Serialize(writer, value);
    }

    public override string? RenderInline()
    {
        if (value is IMarkoutFormattable formattable)
            return formattable.ToMarkoutString();

        return value?.ToString();
    }

    public override bool IsTruthy => value is not null;
}

/// <summary>
/// A binding for an arbitrary object. Used for conditional truthiness and ToString() fallback.
/// </summary>
internal sealed class ObjectBinding(object? value) : TemplateBinding
{
    public override void Render(MarkoutWriter writer)
    {
        if (value is IMarkoutFormattable formattable)
        {
            formattable.WriteTo(writer);
        }
        else if (value is not null)
        {
            writer.WriteParagraph(value.ToString());
        }
    }

    public override string? RenderInline() => value switch
    {
        IMarkoutFormattable f => f.ToMarkoutString(),
        not null => value.ToString(),
        _ => null
    };

    public override bool IsTruthy => value switch
    {
        null => false,
        bool b => b,
        string s => s.Length > 0,
        // Non-generic ICollection covers List<T>, arrays, Dictionary, Queue, Stack, Collection<T>,
        // etc. Generic-only collections (e.g. HashSet<T>) and lazy sequences are intentionally NOT
        // probed here: counting them would require reflection (breaks AOT) or enumeration (consumes
        // one-shot sequences). Bind collections through Bind(key, IEnumerable<string>) — which
        // materializes and uses ListBinding — to get emptiness-driven truthiness for any sequence.
        System.Collections.ICollection c => c.Count > 0,
        _ => !IsZeroNumeric(value),
    };

    private static bool IsZeroNumeric(object value) => value switch
    {
        sbyte n => n == 0,
        byte n => n == 0,
        short n => n == 0,
        ushort n => n == 0,
        int n => n == 0,
        uint n => n == 0,
        long n => n == 0,
        ulong n => n == 0,
        float n => n == 0,
        double n => n == 0,
        decimal n => n == 0,
        Half n => n == (Half)0,
        nint n => n == 0,
        nuint n => n == 0,
        Int128 n => n == 0,
        UInt128 n => n == 0,
        System.Numerics.BigInteger n => n.IsZero,
        _ => false,
    };
}
