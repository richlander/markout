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

    /// <summary>
    /// Converts the source-generated schema into a <see cref="DocumentSchema"/>
    /// suitable for discovery, projection validation, and diagnostics.
    /// </summary>
    /// <remarks>
    /// This is a lossy reduction: section headings, content types, and item names are
    /// preserved, but dynamic content (field tables, code blocks) produces sections
    /// with no queryable items.
    /// </remarks>
    public DocumentSchema ToDocumentSchema()
    {
        var sectionOrder = new List<string>();
        var sectionData = new Dictionary<string, (string? ItemKind, List<string> Items)>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var prop in AsDocument)
        {
            var sectionName = ExtractSectionName(prop.Rendering);
            if (sectionName == null) continue;

            var contentType = ExtractContentType(prop.Rendering);
            var (itemKind, items) = MapContentToItems(contentType, prop);

            if (!sectionData.TryGetValue(sectionName, out var existing))
            {
                sectionData[sectionName] = (itemKind, items);
                sectionOrder.Add(sectionName);
            }
            else
            {
                // Merge: union of items, upgrade kind if previously section-only
                foreach (var item in items)
                {
                    if (!existing.Items.Contains(item, StringComparer.OrdinalIgnoreCase))
                        existing.Items.Add(item);
                }
                if (existing.ItemKind == null && itemKind != null)
                    sectionData[sectionName] = (itemKind, existing.Items);
            }
        }

        var schema = new DocumentSchema();
        foreach (var name in sectionOrder)
        {
            var (itemKind, items) = sectionData[name];
            if (itemKind != null && items.Count > 0)
                schema.Add(name, itemKind, items.ToArray());
            else
                schema.AddSection(name);
        }
        return schema;
    }

    private static string? ExtractContentType(string rendering)
    {
        // Extract the parenthetical suffix from: H2 Section "Name" (table)
        int lastOpen = rendering.LastIndexOf('(');
        int lastClose = rendering.LastIndexOf(')');
        if (lastOpen < 0 || lastClose <= lastOpen) return null;
        return rendering[(lastOpen + 1)..lastClose];
    }

    private static (string? ItemKind, List<string> Items) MapContentToItems(
        string? contentType, MarkoutPropertySchema prop)
    {
        return contentType switch
        {
            "table" => ("column", CollectChildColumns(prop)),
            "subsections" => ("column", CollectChildColumns(prop)),
            "field" => ("field", [prop.DisplayName]),
            "fields" => ("field", CollectChildFields(prop)),
            "tree" => ("tree", CollectChildColumns(prop)),
            // Content types with no queryable items
            "field table" or "code block" or "bar chart"
                or "labeled list" or "distribution" => (null, []),
            // No parenthetical (e.g. bullet list in section) — no static items
            _ => (null, []),
        };
    }

    private static List<string> CollectChildColumns(MarkoutPropertySchema prop)
    {
        var names = new List<string>();
        foreach (var child in prop.Children)
        {
            if (child.Rendering.StartsWith("Column", StringComparison.Ordinal)
                && !names.Contains(child.DisplayName, StringComparer.OrdinalIgnoreCase))
                names.Add(child.DisplayName);
        }
        return names;
    }

    private static List<string> CollectChildFields(MarkoutPropertySchema prop)
    {
        var names = new List<string>();
        CollectChildFieldsRecursive(prop.Children, names);
        return names;
    }

    private static void CollectChildFieldsRecursive(
        IReadOnlyList<MarkoutPropertySchema> children, List<string> names)
    {
        foreach (var child in children)
        {
            if (child.Rendering.StartsWith("Field", StringComparison.Ordinal))
            {
                if (!names.Contains(child.DisplayName, StringComparer.OrdinalIgnoreCase))
                    names.Add(child.DisplayName);
            }
            else if (child.Rendering == "Fields")
            {
                CollectChildFieldsRecursive(child.Children, names);
            }
        }
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
