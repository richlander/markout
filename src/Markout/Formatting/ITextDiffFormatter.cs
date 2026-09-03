namespace Markout.Formatting;

/// <summary>Capability interface for rendering a mapped text diff.</summary>
public interface ITextDiffFormatter
{
    /// <summary>Renders a validated mapped text diff.</summary>
    void FormatTextDiff(TextWriter writer, MappedTextDiff diff, MarkoutWriterOptions options);
}
