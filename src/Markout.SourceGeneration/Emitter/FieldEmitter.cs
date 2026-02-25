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
        int sectionLevel = 2)
    {
        switch (fieldLayout)
        {
            case FieldLayoutKind.Line:
                EmitFieldLine(sb, scalarProps, valueExpr, indentLevel, nestingDepth, sectionHeading, sectionLevel);
                break;

            case FieldLayoutKind.List:
                EmitFieldList(sb, scalarProps, valueExpr, indentLevel, nestingDepth, sectionHeading, sectionLevel);
                break;

            case FieldLayoutKind.BareList:
                EmitFieldBareList(sb, scalarProps, valueExpr, indentLevel, nestingDepth, sectionHeading, sectionLevel);
                break;

            default:
                EmitFieldLine(sb, scalarProps, valueExpr, indentLevel, nestingDepth, sectionHeading, sectionLevel);
                break;
        }
    }

    private static void EmitFieldLine(
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
            || (p.Kind == PropertyKind.StringArray && p.JoinSeparator != null)
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
                else if (prop.Kind == PropertyKind.StringArray && prop.JoinSeparator != null)
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
            sb.AppendLine($"{indent}    writer.WriteFieldLine(global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan({fieldsVar}));");
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
            sb.AppendLine($"{indent}writer.WriteFieldLine({string.Join(", ", fields)});");
        }
    }

    private static void EmitFieldBareList(
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
            || (p.Kind == PropertyKind.StringArray && p.JoinSeparator != null)
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
                else if (prop.Kind == PropertyKind.StringArray && prop.JoinSeparator != null)
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
            sb.AppendLine($"{indent}    writer.WriteFieldBareList(global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan({fieldsVar}));");
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
            sb.AppendLine($"{indent}writer.WriteFieldBareList(new global::Markout.MarkoutField[] {{ {string.Join(", ", fields)} }});");
        }
    }

    private static void EmitFieldList(
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
            || (p.Kind == PropertyKind.StringArray && p.JoinSeparator != null)
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
                else if (prop.Kind == PropertyKind.StringArray && prop.JoinSeparator != null)
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
            sb.AppendLine($"{indent}    writer.WriteFieldList(global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan({fieldsVar}));");
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
            sb.AppendLine($"{indent}writer.WriteFieldList(new global::Markout.MarkoutField[] {{ {string.Join(", ", fields)} }});");
        }
    }

}
