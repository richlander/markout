using System.Collections.Generic;
using System.Linq;
using System.Text;
using Markout.SourceGeneration.Parser;

namespace Markout.SourceGeneration.Emitter;

/// <summary>
/// Emits source code for table and subsection-per-item rendering.
/// </summary>
internal static class CollectionEmitter
{
    public static void EmitTableSerialization(
        StringBuilder sb,
        PropertyMetadata prop,
        string propAccess,
        int indentLevel,
        int nestingDepth = 0)
    {
        if (prop.ElementProperties == null || prop.ElementProperties.Count == 0)
            return;

        var indent = new string(' ', indentLevel * 4);
        var visibleProps = prop.ElementProperties
            .Where(p => !p.IsIgnored && p.Name != prop.SectionIgnoreProperty)
            .ToList();
        var itemVar = nestingDepth == 0 ? "item" : $"item{nestingDepth}";

        // Build header array
        var headers = string.Join(", ", visibleProps.Select(p => $"\"{EmitHelpers.EscapeString(p.DisplayName)}\""));
        sb.AppendLine($"{indent}writer.WriteTableStart({headers});");

        sb.AppendLine($"{indent}foreach (var {itemVar} in {propAccess})");
        sb.AppendLine($"{indent}{{");

        // Build row values
        var values = new List<string>();
        foreach (var elemProp in visibleProps)
        {
            var value = EmitHelpers.GetTableCellValue(elemProp, itemVar);
            values.Add(value);
        }

        sb.AppendLine($"{indent}    writer.WriteTableRow({string.Join(", ", values)});");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine($"{indent}writer.WriteTableEnd();");
    }

    public static void EmitSubsectionPerItemSerialization(
        StringBuilder sb,
        PropertyMetadata prop,
        string propAccess,
        int indentLevel,
        int parentSectionLevel = 2,
        int nestingDepth = 0)
    {
        if (prop.ElementProperties == null || prop.ElementProperties.Count == 0)
            return;

        var indent = new string(' ', indentLevel * 4);
        var subsectionLevel = parentSectionLevel + 1;
        var itemVar = nestingDepth == 0 ? "item" : $"item{nestingDepth}";

        sb.AppendLine($"{indent}foreach (var {itemVar} in {propAccess})");
        sb.AppendLine($"{indent}{{");

        // Write subsection heading using TitleProperty or first string property
        if (!string.IsNullOrEmpty(prop.ElementTitleProperty))
        {
            sb.AppendLine($"{indent}    if ({itemVar}.{prop.ElementTitleProperty} != null)");
            sb.AppendLine($"{indent}        writer.WriteHeading({subsectionLevel}, {itemVar}.{prop.ElementTitleProperty});");
        }
        else
        {
            // Try to find a suitable property for the heading
            var titleProp = prop.ElementProperties.FirstOrDefault(p => 
                !p.IsIgnored && p.Kind == PropertyKind.String && 
                (p.Name == "Name" || p.Name == "Title" || p.Name == "Id"));
            
            if (titleProp != null)
            {
                sb.AppendLine($"{indent}    if ({itemVar}.{titleProp.Name} != null)");
                sb.AppendLine($"{indent}        writer.WriteHeading({subsectionLevel}, {itemVar}.{titleProp.Name});");
            }
            else
            {
                // Fallback: use first string property
                var firstString = prop.ElementProperties.FirstOrDefault(p => !p.IsIgnored && p.Kind == PropertyKind.String);
                if (firstString != null)
                {
                    sb.AppendLine($"{indent}    if ({itemVar}.{firstString.Name} != null)");
                    sb.AppendLine($"{indent}        writer.WriteHeading({subsectionLevel}, {itemVar}.{firstString.Name});");
                }
            }
        }

        // Emit property serializations for each item, at a deeper level
        SerializerEmitter.EmitPropertySerializations(sb, prop.ElementProperties, itemVar, indentLevel + 1, subsectionLevel + 1, nestingDepth + 1);

        sb.AppendLine($"{indent}}}");
    }
}
