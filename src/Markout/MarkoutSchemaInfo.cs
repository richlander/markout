using System.Runtime.InteropServices;

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
    public IReadOnlyList<MarkoutPropertySchema> AsDocument { get; init; } = [];
    
    /// <summary>
    /// Schema when rendered as a table row (inside List&lt;T&gt;).
    /// </summary>
    public IReadOnlyList<MarkoutPropertySchema> AsTableItem { get; init; } = [];
    
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
        var tree = new TreeWriter(writer);
        
        writer.WriteLine($"{TypeName} (as document)");
        tree.WriteTree(CollectionsMarshal.AsSpan(ToTreeNodes(AsDocument)));

        if (AsTableItem.Count > 0 && HasDifferences())
        {
            writer.WriteLine();
            writer.WriteLine($"{TypeName} (in table)");
            tree.WriteTree(CollectionsMarshal.AsSpan(ToTreeNodes(AsTableItem)));
        }
        
        writer.Flush();
    }
    
    /// <summary>
    /// Returns the display names of all scalar fields in the document schema.
    /// Matches properties with Rendering starting with "Field".
    /// </summary>
    public string[] GetFieldNames()
    {
        var names = new List<string>();
        CollectFieldNames(AsDocument, names);
        return names.ToArray();
    }

    /// <summary>
    /// Returns the display names of all table columns across all tables in the document schema.
    /// Collects column names from Children of table-type properties.
    /// </summary>
    public string[] GetColumnNames()
    {
        var names = new List<string>();
        CollectColumnNames(AsDocument, names);
        return names.ToArray();
    }

    /// <summary>
    /// Returns the names of all sections in the document schema.
    /// Extracts section names from Rendering strings like <c>H2 Section "Dependencies" (table)</c>.
    /// </summary>
    public string[] GetSectionNames()
    {
        var names = new List<string>();
        CollectSectionNames(AsDocument, names);
        return names.ToArray();
    }

    private static void CollectFieldNames(IReadOnlyList<MarkoutPropertySchema> props, List<string> names)
    {
        foreach (var prop in props)
        {
            if (prop.Rendering.StartsWith("Field", StringComparison.Ordinal))
            {
                if (!names.Contains(prop.DisplayName))
                    names.Add(prop.DisplayName);
            }
            else if (prop.Rendering == "Fields")
                CollectFieldNames(prop.Children, names);
        }
    }

    private static void CollectColumnNames(IReadOnlyList<MarkoutPropertySchema> props, List<string> names)
    {
        foreach (var prop in props)
        {
            if (prop.Rendering.Contains("(table)") || prop.Rendering == "Table")
            {
                foreach (var child in prop.Children)
                {
                    if (child.Rendering.StartsWith("Column", StringComparison.Ordinal) &&
                        !names.Contains(child.DisplayName))
                        names.Add(child.DisplayName);
                }
            }
            else if (prop.Children.Count > 0)
            {
                CollectColumnNames(prop.Children, names);
            }
        }
    }

    private static void CollectSectionNames(IReadOnlyList<MarkoutPropertySchema> props, List<string> names)
    {
        foreach (var prop in props)
        {
            var sectionName = ExtractSectionName(prop.Rendering);
            if (sectionName != null)
                names.Add(sectionName);
        }
    }

    /// <summary>
    /// Extracts the section name from a Rendering string like <c>H2 Section "Dependencies" (table)</c>.
    /// Returns null if the string is not a section rendering.
    /// </summary>
    public static string? ExtractSectionName(string rendering)
    {
        // Parse: H2 Section "Dependencies" (table)
        const string marker = "Section \"";
        int start = rendering.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0) return null;
        start += marker.Length;
        int end = rendering.IndexOf('"', start);
        if (end < 0) return null;
        return rendering[start..end];
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
        return props.Select(p => p.Children.Count > 0
            ? new TreeNode(
                $"{p.Name}: {p.TypeName} → {p.Rendering}",
                null,
                [..ToTreeNodes(p.Children)])
            : new TreeNode($"{p.Name}: {p.TypeName} → {p.Rendering}")
        ).ToList();
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
    public IReadOnlyList<MarkoutPropertySchema> Children { get; init; } = [];
}
