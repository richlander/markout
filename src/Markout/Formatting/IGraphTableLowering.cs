namespace Markout.Formatting;

/// <summary>
/// Lets the writer route a graph's table lowering through the normal table pipeline.
/// </summary>
internal interface IGraphTableLowering
{
    bool TryLowerGraphToTable(Graph graph, out GraphLowering.GraphEdgeTable table);
}
