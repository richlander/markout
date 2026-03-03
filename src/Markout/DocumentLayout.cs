namespace Markout;

/// <summary>
/// Specifies the overall document layout for a serializable type.
/// </summary>
public enum DocumentLayout
{
    /// <summary>
    /// Standard document layout with headings, fields, tables, and sections (default).
    /// </summary>
    Document,

    /// <summary>
    /// Tree layout: the title renders as a plain-text root node,
    /// and <see cref="TreeNode"/> list properties render as children with connectors.
    /// No headings, fields, or sections are emitted.
    /// </summary>
    Tree
}
