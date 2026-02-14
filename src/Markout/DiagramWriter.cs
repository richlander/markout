namespace Markout;

/// <summary>
/// A writer specialized for structural and hierarchical visualizations.
/// Supports trees and headings (for tree titles). Future home for
/// flowcharts, dependency graphs, and other diagram shapes.
/// </summary>
public class DiagramWriter : MarkoutWriter
{
    /// <summary>
    /// Creates a diagram writer targeting the specified TextWriter.
    /// </summary>
    public DiagramWriter(TextWriter writer) : base(writer)
    {
    }

    /// <summary>
    /// Creates a diagram writer with the specified options.
    /// </summary>
    public DiagramWriter(TextWriter writer, MarkoutWriterOptions options) : base(writer, options)
    {
    }

    /// <inheritdoc/>
    public override MarkoutShape SupportedShapes => MarkoutShape.Headings | MarkoutShape.Trees;
}
