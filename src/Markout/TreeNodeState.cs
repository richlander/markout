namespace Markout;

/// <summary>
/// The structural status of a <see cref="TreeNode"/> within a tree lowering — why the node renders
/// the way it does, not what kind of thing the node represents.
/// </summary>
/// <remarks>
/// <para>
/// This is a single axis and its members are mutually exclusive: a node is unexpanded for exactly
/// one reason. Domain classification is a separate concern and does not belong here. A graph node's
/// grouping and emphasis are carried by <see cref="GraphNode.Group"/> and
/// <see cref="GraphNode.Emphasized"/>, which can co-occur with any state.
/// </para>
/// <para>
/// The state is deliberately structural rather than presentational: it is set by the lowering and
/// each sink chooses its own spelling, so rich sinks can render a glyph while plain and
/// machine-readable sinks keep a stable word.
/// </para>
/// </remarks>
public enum TreeNodeState
{
    /// <summary>The node is expanded normally. The default.</summary>
    Normal = 0,

    /// <summary>
    /// The node's subtree is elided because the node already appeared earlier in this lowering.
    /// The node is still named, so a shared or cyclic reference stays visible rather than being
    /// silently dropped or duplicated as though it were a distinct node.
    /// </summary>
    Revisit
}
