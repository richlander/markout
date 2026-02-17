namespace Markout.Templates;

/// <summary>
/// A parsed element from a Markout template.
/// </summary>
public abstract record TemplateNode;

/// <summary>
/// An ATX heading (# through ######). Text may contain inline {{key}} placeholders.
/// </summary>
public record HeadingNode(int Level, string Text) : TemplateNode;

/// <summary>
/// A paragraph of prose text. May contain inline {{key}} placeholders.
/// </summary>
public record ParagraphNode(string Text) : TemplateNode;

/// <summary>
/// A block-level placeholder ({{key}} on its own line).
/// Resolved by rendering a bound value through the writer.
/// </summary>
public record PlaceholderNode(string Key) : TemplateNode;

/// <summary>
/// Start of a conditional section ({{#if key}}).
/// Content is included only when the key is bound to a truthy value.
/// </summary>
public record ConditionalStartNode(string Key) : TemplateNode;

/// <summary>
/// End of a conditional section ({{/if}}).
/// </summary>
public record ConditionalEndNode() : TemplateNode;

/// <summary>
/// A blank line in the template, preserved for structural spacing.
/// </summary>
public record BlankLineNode() : TemplateNode;
