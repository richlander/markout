namespace Markout.Templates;

/// <summary>
/// A parsed template that combines human-authored document structure with
/// data-driven content rendered through the Markout writer pipeline.
/// </summary>
/// <remarks>
/// Like the source generator, templates render through the
/// <see cref="MarkoutWriter"/> pipeline:
/// <code>
/// Source Generator:  [MarkoutSerializable] Type → Generated TypeInfo → MarkoutWriter
/// Template Engine:   .md Template + Bindings  → MarkoutTemplate     → MarkoutWriter
/// </code>
/// </remarks>
public class MarkoutTemplate
{
    private readonly List<TemplateNode> _nodes;
    private readonly Dictionary<string, TemplateBinding> _bindings = new(StringComparer.OrdinalIgnoreCase);

    private MarkoutTemplate(List<TemplateNode> nodes)
    {
        _nodes = nodes;
    }

    /// <summary>
    /// Parses template text into a <see cref="MarkoutTemplate"/>.
    /// </summary>
    public static MarkoutTemplate Parse(string text)
    {
        var nodes = TemplateParser.Parse(text);
        return new MarkoutTemplate(nodes);
    }

    /// <summary>
    /// Loads and parses a template from a file path.
    /// </summary>
    public static MarkoutTemplate Load(string path)
    {
        var text = File.ReadAllText(path);
        return Parse(text);
    }

    /// <summary>
    /// Loads and parses a template from a stream.
    /// </summary>
    public static MarkoutTemplate Load(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();
        return Parse(text);
    }

    /// <summary>
    /// Binds a string value for inline substitution.
    /// </summary>
    public MarkoutTemplate Bind(string key, string? value)
    {
        _bindings[key] = new StringBinding(value);
        return this;
    }

    /// <summary>
    /// Binds an <see cref="IMarkoutFormattable"/> value for block-level rendering.
    /// </summary>
    public MarkoutTemplate Bind(string key, IMarkoutFormattable? value)
    {
        _bindings[key] = new FormattableBinding(value);
        return this;
    }

    /// <summary>
    /// Binds a source-generated type for block-level rendering through the shape system.
    /// </summary>
    public MarkoutTemplate Bind<T>(string key, T value, MarkoutTypeInfo<T> typeInfo)
    {
        _bindings[key] = new TypeInfoBinding<T>(value, typeInfo);
        return this;
    }

    /// <summary>
    /// Binds an arbitrary object. Used for conditional truthiness and ToString() fallback.
    /// </summary>
    public MarkoutTemplate BindObject(string key, object? value)
    {
        _bindings[key] = new ObjectBinding(value);
        return this;
    }

    /// <summary>
    /// Renders the template using a <see cref="MarkdownWriter"/> and returns the result as a string.
    /// </summary>
    public string Render()
    {
        var writer = new MarkdownWriter();
        Render(writer);
        return writer.ToString();
    }

    /// <summary>
    /// Renders the template using a <see cref="MarkdownWriter"/> with the specified options
    /// and returns the result as a string.
    /// </summary>
    public string Render(MarkoutWriterOptions options)
    {
        var writer = new MarkdownWriter(options);
        Render(writer);
        return writer.ToString();
    }

    /// <summary>
    /// Renders the template into the specified <see cref="MarkoutWriter"/>.
    /// </summary>
    public void Render(MarkoutWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        int skipDepth = 0;

        foreach (var node in _nodes)
        {
            switch (node)
            {
                case ConditionalStartNode conditional:
                    if (skipDepth > 0)
                    {
                        skipDepth++;
                    }
                    else if (!IsBindingTruthy(conditional.Key))
                    {
                        skipDepth = 1;
                    }
                    break;

                case ConditionalEndNode:
                    if (skipDepth > 0)
                        skipDepth--;
                    break;

                default:
                    if (skipDepth > 0)
                        break;

                    RenderNode(writer, node);
                    break;
            }
        }
    }

    /// <summary>
    /// Renders the template into the specified <see cref="TextWriter"/>
    /// using a <see cref="MarkdownWriter"/>.
    /// </summary>
    public void Render(TextWriter output)
    {
        var writer = new MarkdownWriter(output);
        Render(writer);
        writer.Flush();
    }

    private void RenderNode(MarkoutWriter writer, TemplateNode node)
    {
        switch (node)
        {
            case HeadingNode heading:
                var resolvedHeading = ResolveInline(heading.Text);
                writer.WriteHeading(heading.Level, resolvedHeading, null);
                break;

            case ParagraphNode paragraph:
                var resolvedText = ResolveInline(paragraph.Text);
                writer.WriteParagraph(resolvedText);
                break;

            case PlaceholderNode placeholder:
                if (_bindings.TryGetValue(placeholder.Key, out var binding))
                {
                    binding.Render(writer);
                }
                else
                {
                    throw new InvalidOperationException($"No binding found for placeholder '{placeholder.Key}'.");
                }
                break;

            case BlankLineNode:
                // The writer's spacing state handles blank lines naturally
                break;

            case TableNode table:
                writer.WriteTable(table.Headers, table.Rows);
                break;
        }
    }

    private string ResolveInline(string text)
    {
        return TemplateParser.ResolveInlinePlaceholders(text, key =>
        {
            if (_bindings.TryGetValue(key, out var binding))
                return binding.RenderInline();
            return null;
        });
    }

    private bool IsBindingTruthy(string key)
    {
        return _bindings.TryGetValue(key, out var binding) && binding.IsTruthy;
    }
}
