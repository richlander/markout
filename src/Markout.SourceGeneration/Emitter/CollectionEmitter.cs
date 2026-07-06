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
        int nestingDepth = 0,
        IReadOnlyList<(string ConditionVar, string ColumnName)>? dynamicIgnoreColumns = null)
    {
        if (prop.ElementProperties == null || prop.ElementProperties.Count == 0)
            return;

        // When dynamic ignore columns are present, use List-based dynamic rendering
        if (dynamicIgnoreColumns != null && dynamicIgnoreColumns.Count > 0)
        {
            EmitDynamicTableSerialization(sb, prop, propAccess, indentLevel, nestingDepth, dynamicIgnoreColumns);
            return;
        }

        var indent = new string(' ', indentLevel * 4);
        var ignoreNames = prop.SectionIgnoreProperty != null
            ? new HashSet<string>(prop.SectionIgnoreProperty.Split(',').Select(s => s.Trim()))
            : new HashSet<string>();
        var visibleProps = prop.ElementProperties
            .Where(p => !p.IsIgnored && !ignoreNames.Contains(p.Name))
            .ToList();
        var itemVar = nestingDepth == 0 ? "item" : $"item{nestingDepth}";

        // Build header array (with optional column name override for formatted property)
        var headers = string.Join(", ", visibleProps.Select(p =>
        {
            if (p.Name == prop.SectionFormatProperty && prop.SectionColumnName != null)
                return $"\"{EmitHelpers.EscapeString(prop.SectionColumnName)}\"";
            return $"\"{EmitHelpers.EscapeString(p.DisplayName)}\"";
        }));
        var headerNames = string.Join(", ", visibleProps.Select(p =>
            $"\"{EmitHelpers.EscapeString(p.Name)}\""));

        // Per-column dense value expression (formatted property override, else the cell value).
        string CellExpr(PropertyMetadata p)
        {
            if (p.Name == prop.SectionFormatProperty && prop.SectionFormatterTypeName != null)
            {
                var access = $"{itemVar}.{p.Name}";
                return $"{access} != null ? __fmt.Format({access}) : \"\"";
            }
            return EmitHelpers.GetTableCellValue(p, itemVar);
        }

        void EmitDense(string ind)
        {
            sb.AppendLine($"{ind}writer.WriteTableStart(new string[] {{ {headers} }}, new string[] {{ {headerNames} }});");
            if (prop.SectionFormatterTypeName != null)
                sb.AppendLine($"{ind}var __fmt = new {prop.SectionFormatterTypeName}();");
            sb.AppendLine($"{ind}foreach (var {itemVar} in {propAccess})");
            sb.AppendLine($"{ind}{{");
            var values = visibleProps.Select(CellExpr).ToList();
            sb.AppendLine($"{ind}    writer.WriteTableRow({string.Join(", ", values)});");
            sb.AppendLine($"{ind}}}");
            sb.AppendLine($"{ind}writer.WriteTableEnd();");
        }

        // Scalar-only tables render identically to structured output as before; only tables with
        // composite columns need a decompose branch (typed sub-columns for TSV/JSONL, dense otherwise).
        if (!visibleProps.Any(p => p.Kind == PropertyKind.CompositeCell))
        {
            EmitDense(indent);
            return;
        }

        sb.AppendLine($"{indent}if (writer.DecomposesCompositeCells)");
        sb.AppendLine($"{indent}{{");
        var di = indent + "    ";
        if (prop.SectionFormatterTypeName != null)
            sb.AppendLine($"{di}var __fmt = new {prop.SectionFormatterTypeName}();");
        sb.AppendLine($"{di}var __drows = new global::System.Collections.Generic.List<global::System.Collections.Generic.List<global::System.Collections.Generic.List<global::Markout.MarkoutField>>>();");
        sb.AppendLine($"{di}foreach (var {itemVar} in {propAccess})");
        sb.AppendLine($"{di}{{");
        sb.AppendLine($"{di}    var __cols = new global::System.Collections.Generic.List<global::System.Collections.Generic.List<global::Markout.MarkoutField>>();");
        foreach (var p in visibleProps)
        {
            sb.AppendLine($"{di}    {{");
            sb.AppendLine($"{di}        var __c = new global::System.Collections.Generic.List<global::Markout.MarkoutField>();");
            if (p.Kind == PropertyKind.CompositeCell)
            {
                sb.AppendLine($"{di}        ((global::Markout.IMarkoutCell?){itemVar}.{p.Name})?.Decompose(__c, \"{EmitHelpers.EscapeString(p.Name)}\", {EmitHelpers.CompositeCellFormatLiteral(p)});");
            }
            else
            {
                sb.AppendLine($"{di}        __c.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(p.Name)}\", {CellExpr(p)}));");
            }
            sb.AppendLine($"{di}        __cols.Add(__c);");
            sb.AppendLine($"{di}    }}");
        }
        sb.AppendLine($"{di}    __drows.Add(__cols);");
        sb.AppendLine($"{di}}}");
        sb.AppendLine($"{di}writer.WriteDecomposedRows(__drows);");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine($"{indent}else");
        sb.AppendLine($"{indent}{{");
        EmitDense(indent + "    ");
        sb.AppendLine($"{indent}}}");
    }

    /// <summary>
    /// Emits table rendering with dynamically ignored columns based on runtime conditions.
    /// Uses List-based header/row building with conditional adds.
    /// </summary>
    private static void EmitDynamicTableSerialization(
        StringBuilder sb,
        PropertyMetadata prop,
        string propAccess,
        int indentLevel,
        int nestingDepth,
        IReadOnlyList<(string ConditionVar, string ColumnName)> dynamicIgnoreColumns)
    {
        var indent = new string(' ', indentLevel * 4);
        var ignoreNames = prop.SectionIgnoreProperty != null
            ? new HashSet<string>(prop.SectionIgnoreProperty.Split(',').Select(s => s.Trim()))
            : new HashSet<string>();
        var visibleProps = prop.ElementProperties!
            .Where(p => !p.IsIgnored && !ignoreNames.Contains(p.Name))
            .ToList();
        var itemVar = nestingDepth == 0 ? "item" : $"item{nestingDepth}";
        var dynamicLookup = dynamicIgnoreColumns.ToDictionary(d => d.ColumnName, d => d.ConditionVar);

        // Resolve the dynamic-ignore condition variable (loop-invariant) for a column, if any.
        bool TryDynamicCond(PropertyMetadata p, out string condVar)
        {
            var dynamicKey = dynamicLookup.ContainsKey(p.Name) ? p.Name : p.DisplayName;
            return dynamicLookup.TryGetValue(dynamicKey, out condVar!);
        }

        // Dense cell string for a column (formatted-property override, else the cell value).
        string ScalarValue(PropertyMetadata p)
        {
            if (p.Name == prop.SectionFormatProperty && prop.SectionFormatterTypeName != null)
            {
                var access = $"{itemVar}.{p.Name}";
                return $"{access} != null ? __fmt.Format({access}) : \"\"";
            }
            return EmitHelpers.GetTableCellValue(p, itemVar);
        }

        void EmitDense(string ind)
        {
            sb.AppendLine($"{ind}var __headers = new global::System.Collections.Generic.List<string>();");
            sb.AppendLine($"{ind}var __headerNames = new global::System.Collections.Generic.List<string>();");
            foreach (var p in visibleProps)
            {
                var headerStr = p.Name == prop.SectionFormatProperty && prop.SectionColumnName != null
                    ? $"\"{EmitHelpers.EscapeString(prop.SectionColumnName)}\""
                    : $"\"{EmitHelpers.EscapeString(p.DisplayName)}\"";
                var headerNameStr = $"\"{EmitHelpers.EscapeString(p.Name)}\"";

                if (TryDynamicCond(p, out var condVar))
                {
                    sb.AppendLine($"{ind}if (!{condVar})");
                    sb.AppendLine($"{ind}{{");
                    sb.AppendLine($"{ind}    __headers.Add({headerStr});");
                    sb.AppendLine($"{ind}    __headerNames.Add({headerNameStr});");
                    sb.AppendLine($"{ind}}}");
                }
                else
                {
                    sb.AppendLine($"{ind}__headers.Add({headerStr});");
                    sb.AppendLine($"{ind}__headerNames.Add({headerNameStr});");
                }
            }
            sb.AppendLine($"{ind}writer.WriteTableStart(__headers.ToArray(), __headerNames.ToArray());");
            if (prop.SectionFormatterTypeName != null)
                sb.AppendLine($"{ind}var __fmt = new {prop.SectionFormatterTypeName}();");
            sb.AppendLine($"{ind}foreach (var {itemVar} in {propAccess})");
            sb.AppendLine($"{ind}{{");
            sb.AppendLine($"{ind}    var __row = new global::System.Collections.Generic.List<string>();");
            foreach (var elemProp in visibleProps)
            {
                var value = ScalarValue(elemProp);
                if (TryDynamicCond(elemProp, out var condVar))
                    sb.AppendLine($"{ind}    if (!{condVar}) __row.Add({value});");
                else
                    sb.AppendLine($"{ind}    __row.Add({value});");
            }
            sb.AppendLine($"{ind}    writer.WriteTableRow(__row.ToArray());");
            sb.AppendLine($"{ind}}}");
            sb.AppendLine($"{ind}writer.WriteTableEnd();");
        }

        // Decomposing formatters: same dynamic-hidden-column rules, but composite columns emit typed
        // sub-fields. A hidden column contributes an empty source column (kept so column indices stay stable).
        void EmitDecompose(string ind)
        {
            if (prop.SectionFormatterTypeName != null)
                sb.AppendLine($"{ind}var __fmt = new {prop.SectionFormatterTypeName}();");
            sb.AppendLine($"{ind}var __drows = new global::System.Collections.Generic.List<global::System.Collections.Generic.List<global::System.Collections.Generic.List<global::Markout.MarkoutField>>>();");
            sb.AppendLine($"{ind}foreach (var {itemVar} in {propAccess})");
            sb.AppendLine($"{ind}{{");
            sb.AppendLine($"{ind}    var __cols = new global::System.Collections.Generic.List<global::System.Collections.Generic.List<global::Markout.MarkoutField>>();");
            foreach (var p in visibleProps)
            {
                sb.AppendLine($"{ind}    {{");
                sb.AppendLine($"{ind}        var __c = new global::System.Collections.Generic.List<global::Markout.MarkoutField>();");
                var fill = p.Kind == PropertyKind.CompositeCell
                    ? $"((global::Markout.IMarkoutCell?){itemVar}.{p.Name})?.Decompose(__c, \"{EmitHelpers.EscapeString(p.Name)}\", {EmitHelpers.CompositeCellFormatLiteral(p)});"
                    : $"__c.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(p.Name)}\", {ScalarValue(p)}));";
                if (TryDynamicCond(p, out var condVar))
                    sb.AppendLine($"{ind}        if (!{condVar}) {fill}");
                else
                    sb.AppendLine($"{ind}        {fill}");
                sb.AppendLine($"{ind}        __cols.Add(__c);");
                sb.AppendLine($"{ind}    }}");
            }
            sb.AppendLine($"{ind}    __drows.Add(__cols);");
            sb.AppendLine($"{ind}}}");
            sb.AppendLine($"{ind}writer.WriteDecomposedRows(__drows);");
        }

        if (visibleProps.Any(p => p.Kind == PropertyKind.CompositeCell))
        {
            sb.AppendLine($"{indent}if (writer.DecomposesCompositeCells)");
            sb.AppendLine($"{indent}{{");
            EmitDecompose(indent + "    ");
            sb.AppendLine($"{indent}}}");
            sb.AppendLine($"{indent}else");
            sb.AppendLine($"{indent}{{");
            EmitDense(indent + "    ");
            sb.AppendLine($"{indent}}}");
        }
        else
        {
            EmitDense(indent);
        }
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
        bool skipPerItemHeading = prop.IsUnwrapped && string.IsNullOrEmpty(prop.ElementTitleProperty);

        sb.AppendLine($"{indent}foreach (var {itemVar} in {propAccess})");
        sb.AppendLine($"{indent}{{");

        if (skipPerItemHeading)
        {
            // Unwrapped with no title: emit each item's properties inline at the current level
            SerializerEmitter.EmitPropertySerializations(sb, prop.ElementProperties, itemVar, indentLevel + 1, subsectionLevel, nestingDepth + 1, prop.ElementAutoFields, 0, prop.ElementFieldLayout);
        }
        else
        {
            // Write subsection heading using TitleProperty or first string property
            if (!string.IsNullOrEmpty(prop.ElementTitleProperty))
            {
                if (!string.IsNullOrEmpty(prop.ElementTitleContextProperty))
                {
                    sb.AppendLine($"{indent}    if ({itemVar}.{prop.ElementTitleProperty} != null)");
                    sb.AppendLine($"{indent}        writer.WriteHeading({subsectionLevel}, {itemVar}.{prop.ElementTitleProperty}, {itemVar}.{prop.ElementTitleContextProperty});");
                }
                else
                {
                    sb.AppendLine($"{indent}    if ({itemVar}.{prop.ElementTitleProperty} != null)");
                    sb.AppendLine($"{indent}        writer.WriteHeading({subsectionLevel}, {itemVar}.{prop.ElementTitleProperty});");
                }
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
            SerializerEmitter.EmitPropertySerializations(sb, prop.ElementProperties, itemVar, indentLevel + 1, subsectionLevel + 1, nestingDepth + 1, prop.ElementAutoFields, 0, prop.ElementFieldLayout);
        }

        sb.AppendLine($"{indent}}}");
    }

    /// <summary>
    /// Emits grouped rendering: items partitioned by a property, each group gets a subheading.
    /// Within each group, items render as list items (1 visible prop) or table rows (multiple).
    /// </summary>
    public static void EmitGroupedSerialization(
        StringBuilder sb,
        PropertyMetadata prop,
        string propAccess,
        int indentLevel,
        int parentSectionLevel = 2,
        IReadOnlyList<(string ConditionVar, string ColumnName)>? dynamicIgnoreColumns = null)
    {
        if (prop.ElementProperties == null || prop.ElementProperties.Count == 0)
            return;

        var indent = new string(' ', indentLevel * 4);
        var groupByProp = prop.SectionGroupByProperty!;
        var subsectionLevel = parentSectionLevel + 1;

        // Determine visible properties (exclude group-by property and ignored)
        var ignoreNames = prop.SectionIgnoreProperty != null
            ? new HashSet<string>(prop.SectionIgnoreProperty.Split(',').Select(s => s.Trim()))
            : new HashSet<string>();
        ignoreNames.Add(groupByProp);

        var visibleProps = prop.ElementProperties
            .Where(p => !p.IsIgnored && !ignoreNames.Contains(p.Name))
            .ToList();

        // Group by the specified property
        sb.AppendLine($"{indent}foreach (var __grp in {propAccess}.GroupBy(__i => __i.{groupByProp}))");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    writer.WriteHeading({subsectionLevel}, __grp.Key);");

        if (visibleProps.Count == 1 && visibleProps[0].Kind == PropertyKind.String)
        {
            // Single string property → list items
            var singleProp = visibleProps[0];
            sb.AppendLine($"{indent}    foreach (var __item in __grp)");
            sb.AppendLine($"{indent}        writer.WriteListItem(__item.{singleProp.Name} ?? \"\");");
        }
        else if (visibleProps.Count > 0)
        {
            if (dynamicIgnoreColumns != null && dynamicIgnoreColumns.Count > 0)
            {
                // Dynamic column visibility within grouped tables
                var dynamicLookup = dynamicIgnoreColumns.ToDictionary(d => d.ColumnName, d => d.ConditionVar);

                sb.AppendLine($"{indent}    var __grpHeaders = new global::System.Collections.Generic.List<string>();");
                sb.AppendLine($"{indent}    var __grpHeaderNames = new global::System.Collections.Generic.List<string>();");
                foreach (var p in visibleProps)
                {
                    var headerStr = $"\"{EmitHelpers.EscapeString(p.DisplayName)}\"";
                    var headerNameStr = $"\"{EmitHelpers.EscapeString(p.Name)}\"";
                    var dynamicKey = dynamicLookup.ContainsKey(p.Name) ? p.Name : p.DisplayName;
                    if (dynamicLookup.TryGetValue(dynamicKey, out var condVar))
                    {
                        sb.AppendLine($"{indent}    if (!{condVar})");
                        sb.AppendLine($"{indent}    {{");
                        sb.AppendLine($"{indent}        __grpHeaders.Add({headerStr});");
                        sb.AppendLine($"{indent}        __grpHeaderNames.Add({headerNameStr});");
                        sb.AppendLine($"{indent}    }}");
                    }
                    else
                    {
                        sb.AppendLine($"{indent}    __grpHeaders.Add({headerStr});");
                        sb.AppendLine($"{indent}    __grpHeaderNames.Add({headerNameStr});");
                    }
                }
                sb.AppendLine($"{indent}    writer.WriteTableStart(__grpHeaders.ToArray(), __grpHeaderNames.ToArray());");
                sb.AppendLine($"{indent}    foreach (var __item in __grp)");
                sb.AppendLine($"{indent}    {{");
                sb.AppendLine($"{indent}        var __grpRow = new global::System.Collections.Generic.List<string>();");
                foreach (var p in visibleProps)
                {
                    var value = EmitHelpers.GetTableCellValue(p, "__item");
                    var dynamicKey = dynamicLookup.ContainsKey(p.Name) ? p.Name : p.DisplayName;
                    if (dynamicLookup.TryGetValue(dynamicKey, out var condVar))
                        sb.AppendLine($"{indent}        if (!{condVar}) __grpRow.Add({value});");
                    else
                        sb.AppendLine($"{indent}        __grpRow.Add({value});");
                }
                sb.AppendLine($"{indent}        writer.WriteTableRow(__grpRow.ToArray());");
                sb.AppendLine($"{indent}    }}");
                sb.AppendLine($"{indent}    writer.WriteTableEnd();");
            }
            else
            {
                // Multiple properties → table per group
                var headers = string.Join(", ", visibleProps.Select(p =>
                    $"\"{EmitHelpers.EscapeString(p.DisplayName)}\""));
                var headerNames = string.Join(", ", visibleProps.Select(p =>
                    $"\"{EmitHelpers.EscapeString(p.Name)}\""));
                sb.AppendLine($"{indent}    writer.WriteTableStart(new string[] {{ {headers} }}, new string[] {{ {headerNames} }});");
                sb.AppendLine($"{indent}    foreach (var __item in __grp)");
                sb.AppendLine($"{indent}    {{");

                var values = visibleProps.Select(p => EmitHelpers.GetTableCellValue(p, "__item")).ToList();
                sb.AppendLine($"{indent}        writer.WriteTableRow({string.Join(", ", values)});");

                sb.AppendLine($"{indent}    }}");
                sb.AppendLine($"{indent}    writer.WriteTableEnd();");
            }
        }

        sb.AppendLine($"{indent}}}");
    }
}
