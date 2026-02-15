namespace Markout;

/// <summary>
/// Represents a code block with a language identifier and content.
/// Recognized by the source generator as a property type that emits
/// <see cref="MarkoutWriter.WriteCodeBlockStart"/>/<see cref="MarkoutWriter.WriteCodeBlockEnd"/> sequences.
/// </summary>
/// <param name="Language">The language identifier (e.g., "csharp", "json"). Null for plain code blocks.</param>
/// <param name="Content">The code content.</param>
public readonly record struct CodeSection(string? Language, string Content);
