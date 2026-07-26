using Markout;
using MarkdownTable.Formatting;

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

    /// <summary>
    /// Options for formatting pipe tables found in the template.
    /// When null, tables pass through to the writer with no width optimization.
    /// </summary>
    public TableFormatterOptions? TableOptions { get; set; }

    /// <summary>
    /// When true, unbound block placeholders are silently skipped instead of throwing.
    /// Useful for CLI tools and partial rendering scenarios.
    /// </summary>
    public bool SkipUnboundPlaceholders { get; set; }

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
    /// Binds a sequence of strings rendered as a bullet list at a block placeholder, or joined with
    /// ", " when used inline. An empty or null sequence is falsy for conditional sections.
    /// </summary>
    public MarkoutTemplate Bind(string key, IEnumerable<string>? items)
    {
        _bindings[key] = new ListBinding(items is null ? null : [.. items]);
        return this;
    }

    /// <summary>
    /// Binds a boolean, primarily to drive a <c>{{#if key}}</c> conditional section
    /// (<see langword="false"/> is falsy). Inline, it renders as <c>true</c>/<c>false</c>.
    /// </summary>
    public MarkoutTemplate Bind(string key, bool value)
    {
        _bindings[key] = new BoolBinding(value);
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
    /// Binds a value using its type info from the context.
    /// The binding key is derived from the type name (snake_case).
    /// </summary>
    /// <example>
    /// <code>
    /// var report = JsonSerializer.Deserialize&lt;CveReport&gt;(json);
    /// template.Bind(report, CveContext.Default);  // binds as "cve_report"
    /// </code>
    /// </example>
    public MarkoutTemplate Bind<T>(T value, MarkoutSerializerContext context)
    {
        var typeInfo = context.GetTypeInfo<T>();
        if (typeInfo == null)
        {
            throw new InvalidOperationException(
                $"Type '{typeof(T).FullName}' is not registered in the serializer context. " +
                $"Add [MarkoutContext(typeof({typeof(T).Name}))] to your context class.");
        }
        _bindings[typeInfo.BindingName] = new TypeInfoBinding<T>(value, typeInfo);
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
    /// Binds all fields from a <see cref="FieldDocument"/> as string values.
    /// Each field key becomes a binding; array fields are joined with ", ".
    /// Enables templates driven by markdown field files with no C# model.
    /// </summary>
    /// <example>
    /// <code>
    /// byte[] bytes = File.ReadAllBytes("package.md");
    /// using var doc = FieldDocument.Parse(bytes);
    /// var result = MarkoutTemplate.Parse(template).BindFields(doc).Render();
    /// </code>
    /// </example>
    public MarkoutTemplate BindFields(FieldDocument doc)
    {
        foreach (var key in doc.Keys)
        {
            _bindings[key] = new StringBinding(doc.GetString(key));
        }
        return this;
    }

    /// <summary>
    /// Parses a UTF-8 byte buffer as markdown fields and binds all fields as string values.
    /// Convenience overload that creates and disposes a <see cref="FieldDocument"/> internally.
    /// </summary>
    public MarkoutTemplate BindFields(byte[] utf8)
    {
        using var doc = FieldDocument.Parse(utf8);
        return BindFields(doc);
    }

    /// <summary>
    /// Renders the template using a <see cref="MarkdownFormatter"/> and returns the result as a string.
    /// </summary>
    public string Render()
    {
        var orch = new MarkoutWriter(new MarkdownFormatter());
        Render(orch);
        return orch.ToString();
    }

    /// <summary>
    /// Renders the template using a <see cref="MarkdownFormatter"/> with the specified options
    /// and returns the result as a string.
    /// </summary>
    public string Render(MarkoutWriterOptions options)
    {
        var orch = new MarkoutWriter(new MarkdownFormatter(), options);
        Render(orch);
        return orch.ToString();
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
    /// using a <see cref="MarkdownFormatter"/>.
    /// </summary>
    public void Render(TextWriter output)
    {
        var orch = new MarkoutWriter(output, new MarkdownFormatter());
        Render(orch);
        orch.Flush();
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
                // Inline conditionals may have emptied individual lines; drop those so an omitted
                // optional line leaves no blank gap and the surrounding lines stay tight.
                if (resolvedText.Contains('\n'))
                {
                    var kept = resolvedText
                        .Split('\n')
                        .Where(l => !string.IsNullOrWhiteSpace(l));
                    resolvedText = string.Join('\n', kept);
                }
                if (!string.IsNullOrWhiteSpace(resolvedText))
                    writer.WriteParagraph(resolvedText);
                break;

            case PlaceholderNode placeholder:
                if (_bindings.TryGetValue(placeholder.Key, out var binding))
                {
                    binding.Render(writer);
                }
                else if (!SkipUnboundPlaceholders)
                {
                    throw new InvalidOperationException($"No binding found for placeholder '{placeholder.Key}'.");
                }
                break;

            case BlankLineNode:
                // The writer's spacing state handles blank lines naturally
                break;

            case TableNode table:
                var resolvedHeaders = table.Headers.Select(h => ResolveInline(h)).ToArray();
                var resolvedRows = table.Rows
                    .Select(row => row.Select(cell => ResolveInline(cell)).ToArray())
                    .ToList();

                if (TableOptions is not null)
                {
                    // Use statistical width calculator to plan column widths,
                    // then pass padded cells through WriteTable so any writer benefits
                    var widths = ColumnWidthCalculator.Calculate(resolvedHeaders, resolvedRows, TableOptions);
                    var paddedHeaders = PadCells(resolvedHeaders, widths);
                    var paddedRows = resolvedRows.Select(row => PadCells(row, widths)).ToList();
                    writer.WriteTable(paddedHeaders, paddedRows);
                }
                else
                {
                    writer.WriteTable(resolvedHeaders, resolvedRows);
                }
                break;
        }
    }

    private string ResolveInline(string text)
    {
        var withConditionals = TemplateParser.ResolveInlineConditionals(text, IsBindingTruthy);
        return TemplateParser.ResolveInlinePlaceholders(withConditionals, key =>
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

    private static string[] PadCells(string[] cells, int[] widths)
    {
        var padded = new string[widths.Length];
        for (int i = 0; i < widths.Length; i++)
        {
            var value = i < cells.Length ? cells[i] : "";
            padded[i] = value.PadRight(widths[i]);
        }
        return padded;
    }
}
