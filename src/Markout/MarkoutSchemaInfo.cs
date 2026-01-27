namespace Markout;

/// <summary>
/// Describes how a type will be rendered by Markout.
/// </summary>
public sealed class MarkoutSchemaInfo
{
    /// <summary>
    /// The name of the type.
    /// </summary>
    public string TypeName { get; init; } = "";
    
    /// <summary>
    /// Schema when rendered as a document root.
    /// </summary>
    public IReadOnlyList<MarkoutPropertySchema> AsDocument { get; init; } = Array.Empty<MarkoutPropertySchema>();
    
    /// <summary>
    /// Schema when rendered as a table row (inside List&lt;T&gt;).
    /// </summary>
    public IReadOnlyList<MarkoutPropertySchema> AsTableItem { get; init; } = Array.Empty<MarkoutPropertySchema>();
    
    /// <summary>
    /// Formats the schema as a tree structure for display.
    /// </summary>
    public string ToTreeString()
    {
        using var sw = new System.IO.StringWriter();
        WriteTree(sw);
        return sw.ToString();
    }
    
    /// <summary>
    /// Writes the schema as a tree structure to the specified writer.
    /// </summary>
    public void WriteTree(System.IO.TextWriter writer)
    {
        var markout = new MarkoutWriter(writer);
        
        writer.WriteLine($"{TypeName} (as document)");
        markout.WriteTree(ToTreeNodes(AsDocument));
        
        if (AsTableItem.Count > 0 && HasDifferences())
        {
            writer.WriteLine();
            writer.WriteLine($"{TypeName} (in table)");
            markout.WriteTree(ToTreeNodes(AsTableItem));
        }
        
        markout.Flush();
    }
    
    private bool HasDifferences()
    {
        if (AsDocument.Count != AsTableItem.Count) return true;
        for (int i = 0; i < AsDocument.Count; i++)
        {
            if (AsDocument[i].Rendering != AsTableItem[i].Rendering) return true;
        }
        return false;
    }
    
    private static List<TreeNode> ToTreeNodes(IReadOnlyList<MarkoutPropertySchema> props)
    {
        return props.Select(p => new TreeNode(
            $"{p.Name}: {p.TypeName} → {p.Rendering}",
            p.Children.Count > 0 ? ToTreeNodes(p.Children) : null
        )).ToList();
    }
}

/// <summary>
/// Describes how a property will be rendered.
/// </summary>
public sealed class MarkoutPropertySchema
{
    /// <summary>
    /// The property name.
    /// </summary>
    public string Name { get; init; } = "";
    
    /// <summary>
    /// The display name (after MarkoutPropertyName attribute).
    /// </summary>
    public string DisplayName { get; init; } = "";
    
    /// <summary>
    /// The type name.
    /// </summary>
    public string TypeName { get; init; } = "";
    
    /// <summary>
    /// How this property will be rendered.
    /// </summary>
    public string Rendering { get; init; } = "";
    
    /// <summary>
    /// Child properties (for nested objects or list elements).
    /// </summary>
    public IReadOnlyList<MarkoutPropertySchema> Children { get; init; } = Array.Empty<MarkoutPropertySchema>();
}
