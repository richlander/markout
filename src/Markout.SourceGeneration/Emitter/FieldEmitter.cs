using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Markout.SourceGeneration.Parser;

namespace Markout.SourceGeneration.Emitter;

/// <summary>
/// Emits source code for scalar field rendering across different layouts.
/// </summary>
internal static class FieldEmitter
{
    public static void EmitScalarsWithLayout(
        StringBuilder sb,
        List<PropertyMetadata> scalarProps,
        string valueExpr,
        int indentLevel,
        FieldLayoutKind fieldLayout,
        int nestingDepth = 0,
        string? sectionHeading = null,
        int sectionLevel = 2,
        MarkoutFieldOrderKind fieldOrder = MarkoutFieldOrderKind.Input)
    {
        if (fieldOrder == MarkoutFieldOrderKind.Alphabetical)
            scalarProps = scalarProps.OrderBy(prop => prop.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();

        switch (fieldLayout)
        {
            case FieldLayoutKind.Table:
                EmitFieldsTable(sb, scalarProps, valueExpr, indentLevel, nestingDepth, sectionHeading, sectionLevel);
                break;

            case FieldLayoutKind.Inline:
                EmitFieldsInline(sb, scalarProps, valueExpr, indentLevel, nestingDepth, sectionHeading, sectionLevel);
                break;

            case FieldLayoutKind.Bulleted:
                EmitFieldsBulleted(sb, scalarProps, valueExpr, indentLevel, nestingDepth, sectionHeading, sectionLevel);
                break;

            case FieldLayoutKind.Numbered:
                EmitFieldsNumbered(sb, scalarProps, valueExpr, indentLevel, nestingDepth, sectionHeading, sectionLevel);
                break;

            case FieldLayoutKind.Plain:
                EmitFieldsPlain(sb, scalarProps, valueExpr, indentLevel, nestingDepth, sectionHeading, sectionLevel);
                break;

            default:
                EmitFieldsTable(sb, scalarProps, valueExpr, indentLevel, nestingDepth, sectionHeading, sectionLevel);
                break;
        }
    }

    private static void EmitFieldsInline(
        StringBuilder sb,
        List<PropertyMetadata> scalarProps,
        string valueExpr,
        int indentLevel,
        int nestingDepth = 0,
        string? sectionHeading = null,
        int sectionLevel = 2)
    {
        var indent = new string(' ', indentLevel * 4);
        bool useBuilder = scalarProps.Any(p => p.IsNullableValueType || p.Kind == PropertyKind.String
            || EmitHelpers.IsJoinedArray(p)
            || p.SkipWhenDefault || p.SkipWhenNull || p.ShowWhenProperty != null);
        var fieldsVar = nestingDepth == 0 ? "__fields" : $"__fields{nestingDepth}";

        if (useBuilder)
        {
            // Use List<MarkoutField> builder pattern when any scalar is nullable or string (to skip nulls/empties)
            sb.AppendLine($"{indent}var {fieldsVar} = new global::System.Collections.Generic.List<global::Markout.MarkoutField>();");
            foreach (var prop in scalarProps)
            {
                var propAccess = $"{valueExpr}.{prop.Name}";
                var fieldIndent = indent;

                // ShowWhenProperty guard wraps the entire field emission
                if (prop.ShowWhenProperty != null)
                {
                    sb.AppendLine($"{indent}if ({valueExpr}.{prop.ShowWhenProperty})");
                    sb.AppendLine($"{indent}{{");
                    fieldIndent = indent + "    ";
                }

                if (prop.IsNullableValueType)
                {
                    var valueStr = EmitHelpers.GetScalarValueExpression(prop, propAccess, nullable: true);
                    valueStr = EmitHelpers.WrapWithLink(prop, valueStr, valueExpr);
                    sb.AppendLine($"{fieldIndent}if ({propAccess}.HasValue)");
                    sb.AppendLine($"{fieldIndent}    {fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                }
                else if (prop.Kind == PropertyKind.String)
                {
                    var valueStr = EmitHelpers.WrapWithValueMap(prop, propAccess);
                    valueStr = EmitHelpers.WrapWithLink(prop, valueStr, valueExpr);
                    sb.AppendLine($"{fieldIndent}if (!string.IsNullOrEmpty({propAccess}))");
                    sb.AppendLine($"{fieldIndent}    {fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                }
                else if (EmitHelpers.IsJoinedArray(prop))
                {
                    var countProp = prop.IsArray ? "Length" : "Count";
                    sb.AppendLine($"{fieldIndent}if ({propAccess} != null && {propAccess}.{countProp} > 0)");
                    sb.AppendLine($"{fieldIndent}    {fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {EmitHelpers.GetScalarValueExpression(prop, propAccess)}));");
                }
                else
                {
                    var valueStr = EmitHelpers.GetScalarValueExpression(prop, propAccess);
                    valueStr = EmitHelpers.WrapWithLink(prop, valueStr, valueExpr);
                    if (prop.SkipWhenDefault)
                    {
                        var condition = EmitHelpers.GetNonDefaultCondition(prop, propAccess);
                        sb.AppendLine($"{fieldIndent}if ({condition})");
                        sb.AppendLine($"{fieldIndent}    {fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                    }
                    else if (prop.SkipWhenNull)
                    {
                        var condition = EmitHelpers.GetNonNullCondition(prop, propAccess);
                        if (condition != null)
                        {
                            sb.AppendLine($"{fieldIndent}if ({condition})");
                            sb.AppendLine($"{fieldIndent}    {fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                        }
                        else
                        {
                            sb.AppendLine($"{fieldIndent}{fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"{fieldIndent}{fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                    }
                }

                if (prop.ShowWhenProperty != null)
                {
                    sb.AppendLine($"{indent}}}");
                }
            }
            sb.AppendLine($"{indent}if ({fieldsVar}.Count > 0)");
            sb.AppendLine($"{indent}{{");
            if (sectionHeading != null)
                sb.AppendLine($"{indent}    writer.WriteHeading({sectionLevel}, \"{EmitHelpers.EscapeString(sectionHeading)}\");");
            sb.AppendLine($"{indent}    writer.WriteFieldsInline(global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan({fieldsVar}));");
            sb.AppendLine($"{indent}}}");
        }
        else
        {
            // Build inline MarkoutField array (no nullable/string scalars)
            var fields = new List<string>();
            foreach (var prop in scalarProps)
            {
                var propAccess = $"{valueExpr}.{prop.Name}";
                var valueStr = EmitHelpers.GetScalarValueExpression(prop, propAccess);
                valueStr = EmitHelpers.WrapWithLink(prop, valueStr, valueExpr);
                fields.Add($"new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr})");
            }

            if (sectionHeading != null)
                sb.AppendLine($"{indent}writer.WriteHeading({sectionLevel}, \"{EmitHelpers.EscapeString(sectionHeading)}\");");
            sb.AppendLine($"{indent}writer.WriteFieldsInline({string.Join(", ", fields)});");
        }
    }

    private static void EmitFieldsTable(
        StringBuilder sb,
        List<PropertyMetadata> scalarProps,
        string valueExpr,
        int indentLevel,
        int nestingDepth = 0,
        string? sectionHeading = null,
        int sectionLevel = 2)
    {
        // Composite-cell shapes render as a dense table (Markdown) that decomposes into
        // typed columns for structured formatters. Route the whole field set through the
        // composite-card path so composite and plain rows share one table.
        if (scalarProps.Any(p => p.Kind == PropertyKind.CompositeCell))
        {
            EmitCompositeCard(sb, scalarProps, valueExpr, indentLevel, nestingDepth, sectionHeading, sectionLevel);
            return;
        }

        var indent = new string(' ', indentLevel * 4);
        bool useBuilder = scalarProps.Any(p => p.IsNullableValueType || p.Kind == PropertyKind.String
            || EmitHelpers.IsJoinedArray(p)
            || p.SkipWhenDefault || p.SkipWhenNull || p.ShowWhenProperty != null);
        var fieldsVar = nestingDepth == 0 ? "__fields" : $"__fields{nestingDepth}";

        if (useBuilder)
        {
            // Use List<MarkoutField> builder pattern when any scalar is nullable or string (to skip nulls/empties)
            sb.AppendLine($"{indent}var {fieldsVar} = new global::System.Collections.Generic.List<global::Markout.MarkoutField>();");
            foreach (var prop in scalarProps)
            {
                var propAccess = $"{valueExpr}.{prop.Name}";
                var fieldIndent = indent;

                // ShowWhenProperty guard wraps the entire field emission
                if (prop.ShowWhenProperty != null)
                {
                    sb.AppendLine($"{indent}if ({valueExpr}.{prop.ShowWhenProperty})");
                    sb.AppendLine($"{indent}{{");
                    fieldIndent = indent + "    ";
                }

                if (prop.IsNullableValueType)
                {
                    var valueStr = EmitHelpers.GetScalarValueExpression(prop, propAccess, nullable: true);
                    valueStr = EmitHelpers.WrapWithLink(prop, valueStr, valueExpr);
                    sb.AppendLine($"{fieldIndent}if ({propAccess}.HasValue)");
                    sb.AppendLine($"{fieldIndent}    {fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                }
                else if (prop.Kind == PropertyKind.String)
                {
                    var valueStr = EmitHelpers.WrapWithValueMap(prop, propAccess);
                    valueStr = EmitHelpers.WrapWithLink(prop, valueStr, valueExpr);
                    sb.AppendLine($"{fieldIndent}if (!string.IsNullOrEmpty({propAccess}))");
                    sb.AppendLine($"{fieldIndent}    {fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                }
                else if (EmitHelpers.IsJoinedArray(prop))
                {
                    var countProp = prop.IsArray ? "Length" : "Count";
                    sb.AppendLine($"{fieldIndent}if ({propAccess} != null && {propAccess}.{countProp} > 0)");
                    sb.AppendLine($"{fieldIndent}    {fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {EmitHelpers.GetScalarValueExpression(prop, propAccess)}));");
                }
                else
                {
                    var valueStr = EmitHelpers.GetScalarValueExpression(prop, propAccess);
                    valueStr = EmitHelpers.WrapWithLink(prop, valueStr, valueExpr);
                    if (prop.SkipWhenDefault)
                    {
                        var condition = EmitHelpers.GetNonDefaultCondition(prop, propAccess);
                        sb.AppendLine($"{fieldIndent}if ({condition})");
                        sb.AppendLine($"{fieldIndent}    {fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                    }
                    else if (prop.SkipWhenNull)
                    {
                        var condition = EmitHelpers.GetNonNullCondition(prop, propAccess);
                        if (condition != null)
                        {
                            sb.AppendLine($"{fieldIndent}if ({condition})");
                            sb.AppendLine($"{fieldIndent}    {fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                        }
                        else
                        {
                            sb.AppendLine($"{fieldIndent}{fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"{fieldIndent}{fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                    }
                }

                if (prop.ShowWhenProperty != null)
                {
                    sb.AppendLine($"{indent}}}");
                }
            }
            sb.AppendLine($"{indent}if ({fieldsVar}.Count > 0)");
            sb.AppendLine($"{indent}{{");
            if (sectionHeading != null)
                sb.AppendLine($"{indent}    writer.WriteHeading({sectionLevel}, \"{EmitHelpers.EscapeString(sectionHeading)}\");");
            sb.AppendLine($"{indent}    writer.WriteFieldsTable(global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan({fieldsVar}));");
            sb.AppendLine($"{indent}}}");
        }
        else
        {
            // Build inline MarkoutField array (no nullable/string scalars)
            var fields = new List<string>();
            foreach (var prop in scalarProps)
            {
                var propAccess = $"{valueExpr}.{prop.Name}";
                var valueStr = EmitHelpers.GetScalarValueExpression(prop, propAccess);
                valueStr = EmitHelpers.WrapWithLink(prop, valueStr, valueExpr);
                fields.Add($"new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr})");
            }

            if (sectionHeading != null)
                sb.AppendLine($"{indent}writer.WriteHeading({sectionLevel}, \"{EmitHelpers.EscapeString(sectionHeading)}\");");
            sb.AppendLine($"{indent}writer.WriteFieldsTable(new global::Markout.MarkoutField[] {{ {string.Join(", ", fields)} }});");
        }
    }

    /// <summary>
    /// Emits a composite-cell table: builds a <c>List&lt;MarkoutCompositeRow&gt;</c> from composite
    /// and plain scalar properties, then calls <c>writer.WriteCompositeTable(...)</c>. The writer
    /// renders a dense <c>Field | Value</c> table for document formatters and decomposes each cell
    /// into typed columns for structured formatters.
    /// </summary>
    private static void EmitCompositeCard(
        StringBuilder sb,
        List<PropertyMetadata> scalarProps,
        string valueExpr,
        int indentLevel,
        int nestingDepth,
        string? sectionHeading,
        int sectionLevel)
    {
        var indent = new string(' ', indentLevel * 4);
        var rowsVar = nestingDepth == 0 ? "__cells" : $"__cells{nestingDepth}";

        sb.AppendLine($"{indent}var {rowsVar} = new global::System.Collections.Generic.List<global::Markout.MarkoutCompositeRow>();");

        foreach (var prop in scalarProps)
        {
            var propAccess = $"{valueExpr}.{prop.Name}";
            var fieldIndent = indent;

            if (prop.ShowWhenProperty != null)
            {
                sb.AppendLine($"{indent}if ({valueExpr}.{prop.ShowWhenProperty})");
                sb.AppendLine($"{indent}{{");
                fieldIndent = indent + "    ";
            }

            if (prop.Kind == PropertyKind.CompositeCell)
            {
                EmitCompositeCellRow(sb, prop, propAccess, rowsVar, fieldIndent);
            }
            else
            {
                EmitScalarCompositeRow(sb, prop, propAccess, rowsVar, fieldIndent);
            }

            if (prop.ShowWhenProperty != null)
                sb.AppendLine($"{indent}}}");
        }

        sb.AppendLine($"{indent}if ({rowsVar}.Count > 0)");
        sb.AppendLine($"{indent}{{");
        if (sectionHeading != null)
            sb.AppendLine($"{indent}    writer.WriteHeading({sectionLevel}, \"{EmitHelpers.EscapeString(sectionHeading)}\");");
        sb.AppendLine($"{indent}    writer.WriteCompositeTable(global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan({rowsVar}));");
        sb.AppendLine($"{indent}}}");
    }

    /// <summary>
    /// Emits a composite-cell property as a <c>MarkoutCompositeRow</c>, honoring nullable and
    /// skip guards so a null/default composite is not rendered as a blank row.
    /// </summary>
    private static void EmitCompositeCellRow(
        StringBuilder sb,
        PropertyMetadata prop,
        string propAccess,
        string rowsVar,
        string indent)
    {
        var format = $"new global::Markout.MarkoutCellFormat({EmitHelpers.DeltaLiteral(prop)}, {EmitHelpers.UnitLiteral(prop)})";

        string RowAdd(string cellExpr) =>
            $"{rowsVar}.Add(new global::Markout.MarkoutCompositeRow(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {cellExpr}, {format}));";

        if (prop.IsNullableValueType)
        {
            // Nullable composite: the underlying struct boxes cleanly to IMarkoutCell.
            var valueExpr = $"{propAccess}.Value";
            sb.AppendLine($"{indent}if ({propAccess}.HasValue)");
            sb.AppendLine($"{indent}    {RowAdd(valueExpr)}");
        }
        else if (prop.SkipWhenDefault)
        {
            var condition = EmitHelpers.GetNonDefaultCondition(prop, propAccess);
            sb.AppendLine($"{indent}if ({condition})");
            sb.AppendLine($"{indent}    {RowAdd(propAccess)}");
        }
        else if (prop.SkipWhenNull)
        {
            var condition = EmitHelpers.GetNonNullCondition(prop, propAccess);
            if (condition != null)
            {
                sb.AppendLine($"{indent}if ({condition})");
                sb.AppendLine($"{indent}    {RowAdd(propAccess)}");
            }
            else
            {
                sb.AppendLine($"{indent}{RowAdd(propAccess)}");
            }
        }
        else
        {
            sb.AppendLine($"{indent}{RowAdd(propAccess)}");
        }
    }

    /// <summary>
    /// Emits a plain scalar property as a <c>MarkoutCompositeRow.Scalar(...)</c> so it can share a
    /// table with composite rows. Mirrors the field-table null/empty/skip guards.
    /// </summary>
    private static void EmitScalarCompositeRow(
        StringBuilder sb,
        PropertyMetadata prop,
        string propAccess,
        string rowsVar,
        string indent)
    {
        string RowAdd(string valueStr) =>
            $"{rowsVar}.Add(global::Markout.MarkoutCompositeRow.Scalar(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));";

        if (prop.IsNullableValueType)
        {
            var valueStr = EmitHelpers.GetScalarValueExpression(prop, propAccess, nullable: true);
            sb.AppendLine($"{indent}if ({propAccess}.HasValue)");
            sb.AppendLine($"{indent}    {RowAdd(valueStr)}");
        }
        else if (prop.Kind == PropertyKind.String)
        {
            var valueStr = EmitHelpers.WrapWithValueMap(prop, propAccess);
            sb.AppendLine($"{indent}if (!string.IsNullOrEmpty({propAccess}))");
            sb.AppendLine($"{indent}    {RowAdd(valueStr)}");
        }
        else if (EmitHelpers.IsJoinedArray(prop))
        {
            var countProp = prop.IsArray ? "Length" : "Count";
            sb.AppendLine($"{indent}if ({propAccess} != null && {propAccess}.{countProp} > 0)");
            sb.AppendLine($"{indent}    {RowAdd(EmitHelpers.GetScalarValueExpression(prop, propAccess))}");
        }
        else
        {
            var valueStr = EmitHelpers.GetScalarValueExpression(prop, propAccess);
            if (prop.SkipWhenDefault)
            {
                var condition = EmitHelpers.GetNonDefaultCondition(prop, propAccess);
                sb.AppendLine($"{indent}if ({condition})");
                sb.AppendLine($"{indent}    {RowAdd(valueStr)}");
            }
            else if (prop.SkipWhenNull)
            {
                var condition = EmitHelpers.GetNonNullCondition(prop, propAccess);
                if (condition != null)
                {
                    sb.AppendLine($"{indent}if ({condition})");
                    sb.AppendLine($"{indent}    {RowAdd(valueStr)}");
                }
                else
                {
                    sb.AppendLine($"{indent}{RowAdd(valueStr)}");
                }
            }
            else
            {
                sb.AppendLine($"{indent}{RowAdd(valueStr)}");
            }
        }
    }

    private static void EmitFieldsBulleted(
        StringBuilder sb,
        List<PropertyMetadata> scalarProps,
        string valueExpr,
        int indentLevel,
        int nestingDepth = 0,
        string? sectionHeading = null,
        int sectionLevel = 2)
    {
        var indent = new string(' ', indentLevel * 4);
        bool useBuilder = scalarProps.Any(p => p.IsNullableValueType || p.Kind == PropertyKind.String
            || EmitHelpers.IsJoinedArray(p)
            || p.SkipWhenDefault || p.SkipWhenNull || p.ShowWhenProperty != null);
        var fieldsVar = nestingDepth == 0 ? "__fields" : $"__fields{nestingDepth}";

        if (useBuilder)
        {
            // Use List<MarkoutField> builder pattern when any scalar is nullable or string (to skip nulls/empties)
            sb.AppendLine($"{indent}var {fieldsVar} = new global::System.Collections.Generic.List<global::Markout.MarkoutField>();");
            foreach (var prop in scalarProps)
            {
                var propAccess = $"{valueExpr}.{prop.Name}";
                var fieldIndent = indent;

                // ShowWhenProperty guard wraps the entire field emission
                if (prop.ShowWhenProperty != null)
                {
                    sb.AppendLine($"{indent}if ({valueExpr}.{prop.ShowWhenProperty})");
                    sb.AppendLine($"{indent}{{");
                    fieldIndent = indent + "    ";
                }

                if (prop.IsNullableValueType)
                {
                    var valueStr = EmitHelpers.GetScalarValueExpression(prop, propAccess, nullable: true);
                    valueStr = EmitHelpers.WrapWithLink(prop, valueStr, valueExpr);
                    sb.AppendLine($"{fieldIndent}if ({propAccess}.HasValue)");
                    sb.AppendLine($"{fieldIndent}    {fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                }
                else if (prop.Kind == PropertyKind.String)
                {
                    var valueStr = EmitHelpers.WrapWithValueMap(prop, propAccess);
                    valueStr = EmitHelpers.WrapWithLink(prop, valueStr, valueExpr);
                    sb.AppendLine($"{fieldIndent}if (!string.IsNullOrEmpty({propAccess}))");
                    sb.AppendLine($"{fieldIndent}    {fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                }
                else if (EmitHelpers.IsJoinedArray(prop))
                {
                    var countProp = prop.IsArray ? "Length" : "Count";
                    sb.AppendLine($"{fieldIndent}if ({propAccess} != null && {propAccess}.{countProp} > 0)");
                    sb.AppendLine($"{fieldIndent}    {fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {EmitHelpers.GetScalarValueExpression(prop, propAccess)}));");
                }
                else
                {
                    var valueStr = EmitHelpers.GetScalarValueExpression(prop, propAccess);
                    valueStr = EmitHelpers.WrapWithLink(prop, valueStr, valueExpr);
                    if (prop.SkipWhenDefault)
                    {
                        var condition = EmitHelpers.GetNonDefaultCondition(prop, propAccess);
                        sb.AppendLine($"{fieldIndent}if ({condition})");
                        sb.AppendLine($"{fieldIndent}    {fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                    }
                    else if (prop.SkipWhenNull)
                    {
                        var condition = EmitHelpers.GetNonNullCondition(prop, propAccess);
                        if (condition != null)
                        {
                            sb.AppendLine($"{fieldIndent}if ({condition})");
                            sb.AppendLine($"{fieldIndent}    {fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                        }
                        else
                        {
                            sb.AppendLine($"{fieldIndent}{fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"{fieldIndent}{fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                    }
                }

                if (prop.ShowWhenProperty != null)
                {
                    sb.AppendLine($"{indent}}}");
                }
            }
            sb.AppendLine($"{indent}if ({fieldsVar}.Count > 0)");
            sb.AppendLine($"{indent}{{");
            if (sectionHeading != null)
                sb.AppendLine($"{indent}    writer.WriteHeading({sectionLevel}, \"{EmitHelpers.EscapeString(sectionHeading)}\");");
            sb.AppendLine($"{indent}    writer.WriteFieldsBulleted(global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan({fieldsVar}));");
            sb.AppendLine($"{indent}}}");
        }
        else
        {
            // Build inline MarkoutField array (no nullable/string scalars)
            var fields = new List<string>();
            foreach (var prop in scalarProps)
            {
                var propAccess = $"{valueExpr}.{prop.Name}";
                var valueStr = EmitHelpers.GetScalarValueExpression(prop, propAccess);
                valueStr = EmitHelpers.WrapWithLink(prop, valueStr, valueExpr);
                fields.Add($"new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr})");
            }

            if (sectionHeading != null)
                sb.AppendLine($"{indent}writer.WriteHeading({sectionLevel}, \"{EmitHelpers.EscapeString(sectionHeading)}\");");
            sb.AppendLine($"{indent}writer.WriteFieldsBulleted(new global::Markout.MarkoutField[] {{ {string.Join(", ", fields)} }});");
        }
    }

    private static void EmitFieldsNumbered(
        StringBuilder sb,
        List<PropertyMetadata> scalarProps,
        string valueExpr,
        int indentLevel,
        int nestingDepth = 0,
        string? sectionHeading = null,
        int sectionLevel = 2)
    {
        var indent = new string(' ', indentLevel * 4);
        bool useBuilder = scalarProps.Any(p => p.IsNullableValueType || p.Kind == PropertyKind.String
            || EmitHelpers.IsJoinedArray(p)
            || p.SkipWhenDefault || p.SkipWhenNull || p.ShowWhenProperty != null);
        var fieldsVar = nestingDepth == 0 ? "__fields" : $"__fields{nestingDepth}";

        if (useBuilder)
        {
            sb.AppendLine($"{indent}var {fieldsVar} = new global::System.Collections.Generic.List<global::Markout.MarkoutField>();");
            foreach (var prop in scalarProps)
            {
                var propAccess = $"{valueExpr}.{prop.Name}";
                var fieldIndent = indent;

                if (prop.ShowWhenProperty != null)
                {
                    sb.AppendLine($"{indent}if ({valueExpr}.{prop.ShowWhenProperty})");
                    sb.AppendLine($"{indent}{{");
                    fieldIndent = indent + "    ";
                }

                if (prop.IsNullableValueType)
                {
                    var valueStr = EmitHelpers.GetScalarValueExpression(prop, propAccess, nullable: true);
                    valueStr = EmitHelpers.WrapWithLink(prop, valueStr, valueExpr);
                    sb.AppendLine($"{fieldIndent}if ({propAccess}.HasValue)");
                    sb.AppendLine($"{fieldIndent}    {fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                }
                else if (prop.Kind == PropertyKind.String)
                {
                    var valueStr = EmitHelpers.WrapWithValueMap(prop, propAccess);
                    valueStr = EmitHelpers.WrapWithLink(prop, valueStr, valueExpr);
                    sb.AppendLine($"{fieldIndent}if (!string.IsNullOrEmpty({propAccess}))");
                    sb.AppendLine($"{fieldIndent}    {fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                }
                else if (EmitHelpers.IsJoinedArray(prop))
                {
                    var countProp = prop.IsArray ? "Length" : "Count";
                    sb.AppendLine($"{fieldIndent}if ({propAccess} != null && {propAccess}.{countProp} > 0)");
                    sb.AppendLine($"{fieldIndent}    {fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {EmitHelpers.GetScalarValueExpression(prop, propAccess)}));");
                }
                else
                {
                    var valueStr = EmitHelpers.GetScalarValueExpression(prop, propAccess);
                    valueStr = EmitHelpers.WrapWithLink(prop, valueStr, valueExpr);
                    if (prop.SkipWhenDefault)
                    {
                        var condition = EmitHelpers.GetNonDefaultCondition(prop, propAccess);
                        sb.AppendLine($"{fieldIndent}if ({condition})");
                        sb.AppendLine($"{fieldIndent}    {fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                    }
                    else if (prop.SkipWhenNull)
                    {
                        var condition = EmitHelpers.GetNonNullCondition(prop, propAccess);
                        if (condition != null)
                        {
                            sb.AppendLine($"{fieldIndent}if ({condition})");
                            sb.AppendLine($"{fieldIndent}    {fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                        }
                        else
                        {
                            sb.AppendLine($"{fieldIndent}{fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"{fieldIndent}{fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                    }
                }

                if (prop.ShowWhenProperty != null)
                {
                    sb.AppendLine($"{indent}}}");
                }
            }
            sb.AppendLine($"{indent}if ({fieldsVar}.Count > 0)");
            sb.AppendLine($"{indent}{{");
            if (sectionHeading != null)
                sb.AppendLine($"{indent}    writer.WriteHeading({sectionLevel}, \"{EmitHelpers.EscapeString(sectionHeading)}\");");
            sb.AppendLine($"{indent}    writer.WriteFieldsNumbered(global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan({fieldsVar}));");
            sb.AppendLine($"{indent}}}");
        }
        else
        {
            var fields = new List<string>();
            foreach (var prop in scalarProps)
            {
                var propAccess = $"{valueExpr}.{prop.Name}";
                var valueStr = EmitHelpers.GetScalarValueExpression(prop, propAccess);
                valueStr = EmitHelpers.WrapWithLink(prop, valueStr, valueExpr);
                fields.Add($"new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr})");
            }

            if (sectionHeading != null)
                sb.AppendLine($"{indent}writer.WriteHeading({sectionLevel}, \"{EmitHelpers.EscapeString(sectionHeading)}\");");
            sb.AppendLine($"{indent}writer.WriteFieldsNumbered(new global::Markout.MarkoutField[] {{ {string.Join(", ", fields)} }});");
        }
    }

    private static void EmitFieldsPlain(
        StringBuilder sb,
        List<PropertyMetadata> scalarProps,
        string valueExpr,
        int indentLevel,
        int nestingDepth = 0,
        string? sectionHeading = null,
        int sectionLevel = 2)
    {
        var indent = new string(' ', indentLevel * 4);
        bool useBuilder = scalarProps.Any(p => p.IsNullableValueType || p.Kind == PropertyKind.String
            || EmitHelpers.IsJoinedArray(p)
            || p.SkipWhenDefault || p.SkipWhenNull || p.ShowWhenProperty != null);
        var fieldsVar = nestingDepth == 0 ? "__fields" : $"__fields{nestingDepth}";

        if (useBuilder)
        {
            sb.AppendLine($"{indent}var {fieldsVar} = new global::System.Collections.Generic.List<global::Markout.MarkoutField>();");
            foreach (var prop in scalarProps)
            {
                var propAccess = $"{valueExpr}.{prop.Name}";
                var fieldIndent = indent;

                if (prop.ShowWhenProperty != null)
                {
                    sb.AppendLine($"{indent}if ({valueExpr}.{prop.ShowWhenProperty})");
                    sb.AppendLine($"{indent}{{");
                    fieldIndent = indent + "    ";
                }

                if (prop.IsNullableValueType)
                {
                    var valueStr = EmitHelpers.GetScalarValueExpression(prop, propAccess, nullable: true);
                    valueStr = EmitHelpers.WrapWithLink(prop, valueStr, valueExpr);
                    sb.AppendLine($"{fieldIndent}if ({propAccess}.HasValue)");
                    sb.AppendLine($"{fieldIndent}    {fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                }
                else if (prop.Kind == PropertyKind.String)
                {
                    var valueStr = EmitHelpers.WrapWithValueMap(prop, propAccess);
                    valueStr = EmitHelpers.WrapWithLink(prop, valueStr, valueExpr);
                    sb.AppendLine($"{fieldIndent}if (!string.IsNullOrEmpty({propAccess}))");
                    sb.AppendLine($"{fieldIndent}    {fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                }
                else if (EmitHelpers.IsJoinedArray(prop))
                {
                    var countProp = prop.IsArray ? "Length" : "Count";
                    sb.AppendLine($"{fieldIndent}if ({propAccess} != null && {propAccess}.{countProp} > 0)");
                    sb.AppendLine($"{fieldIndent}    {fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {EmitHelpers.GetScalarValueExpression(prop, propAccess)}));");
                }
                else
                {
                    var valueStr = EmitHelpers.GetScalarValueExpression(prop, propAccess);
                    valueStr = EmitHelpers.WrapWithLink(prop, valueStr, valueExpr);
                    if (prop.SkipWhenDefault)
                    {
                        var condition = EmitHelpers.GetNonDefaultCondition(prop, propAccess);
                        sb.AppendLine($"{fieldIndent}if ({condition})");
                        sb.AppendLine($"{fieldIndent}    {fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                    }
                    else if (prop.SkipWhenNull)
                    {
                        var condition = EmitHelpers.GetNonNullCondition(prop, propAccess);
                        if (condition != null)
                        {
                            sb.AppendLine($"{fieldIndent}if ({condition})");
                            sb.AppendLine($"{fieldIndent}    {fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                        }
                        else
                        {
                            sb.AppendLine($"{fieldIndent}{fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"{fieldIndent}{fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                    }
                }

                if (prop.ShowWhenProperty != null)
                {
                    sb.AppendLine($"{indent}}}");
                }
            }
            sb.AppendLine($"{indent}if ({fieldsVar}.Count > 0)");
            sb.AppendLine($"{indent}{{");
            if (sectionHeading != null)
                sb.AppendLine($"{indent}    writer.WriteHeading({sectionLevel}, \"{EmitHelpers.EscapeString(sectionHeading)}\");");
            sb.AppendLine($"{indent}    writer.WriteFields(global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan({fieldsVar}));");
            sb.AppendLine($"{indent}}}");
        }
        else
        {
            var fields = new List<string>();
            foreach (var prop in scalarProps)
            {
                var propAccess = $"{valueExpr}.{prop.Name}";
                var valueStr = EmitHelpers.GetScalarValueExpression(prop, propAccess);
                valueStr = EmitHelpers.WrapWithLink(prop, valueStr, valueExpr);
                fields.Add($"new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr})");
            }

            if (sectionHeading != null)
                sb.AppendLine($"{indent}writer.WriteHeading({sectionLevel}, \"{EmitHelpers.EscapeString(sectionHeading)}\");");
            sb.AppendLine($"{indent}writer.WriteFields(new global::Markout.MarkoutField[] {{ {string.Join(", ", fields)} }});");
        }
    }

}
