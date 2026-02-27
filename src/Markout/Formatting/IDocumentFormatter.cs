namespace Markout.Formatting;

/// <summary>
/// Aggregate interface for full document rendering — composes the core
/// capability interfaces for structured output. A formatter can implement
/// this instead of listing each interface individually.
/// </summary>
/// <remarks>
/// The orchestrator still checks individual interfaces at runtime.
/// This is purely a convenience for declaration and generic constraints.
/// </remarks>
public interface IDocumentFormatter :
    IHeadingFormatter, IFieldFormatter, ITableFormatter,
    IListFormatter, ICodeBlockFormatter, IBlockFormatter,
    ITreeFormatter { }
