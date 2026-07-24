namespace Markout.Formatting;

/// <summary>
/// Opt-in capability marker: a formatter that can render a table cell value <em>emphasized</em>
/// (Markdown <c>**…**</c>). Implemented by rich document sinks that support inline emphasis. Plain text
/// and decomposing sinks (TSV/JSONL) deliberately do not implement it, so their output stays unstyled
/// and the value stays meaningful on its own.
/// </summary>
public interface IEmphasisFormatter
{
    /// <summary>Wraps <paramref name="text"/> in this format's inline emphasis (e.g. Markdown <c>**text**</c>).</summary>
    string Emphasize(string text);
}
