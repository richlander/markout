namespace Markout.Templates;

/// <summary>
/// Wraps a bound value with its rendering strategy.
/// </summary>
public abstract class TemplateBinding
{
    /// <summary>
    /// Renders this binding as block-level content through the writer.
    /// </summary>
    public abstract void Render(MarkoutOrchestrator writer);

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
/// </summary>
internal sealed class StringBinding(string? value) : TemplateBinding
{
    public override void Render(MarkoutOrchestrator writer)
    {
        if (value is not null)
            writer.WriteParagraph(value);
    }

    public override string? RenderInline() => value;

    public override bool IsTruthy => value is not null;
}

/// <summary>
/// A binding for a type that controls its own Markout rendering.
/// </summary>
internal sealed class FormattableBinding(IMarkoutFormattable? value) : TemplateBinding
{
    public override void Render(MarkoutOrchestrator writer)
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
    public override void Render(MarkoutOrchestrator writer)
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
    public override void Render(MarkoutOrchestrator writer)
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

    public override bool IsTruthy => value is not null;
}
