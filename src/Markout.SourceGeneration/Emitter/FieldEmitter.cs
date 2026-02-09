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
        int nestingDepth = 0)
    {
        switch (fieldLayout)
        {
            case FieldLayoutKind.OneLine:
                EmitOneLineScalars(sb, scalarProps, valueExpr, indentLevel, nestingDepth);
                break;

            case FieldLayoutKind.LineBreaks:
                EmitLineBreaksScalars(sb, scalarProps, valueExpr, indentLevel, doubleSpace: false);
                break;

            case FieldLayoutKind.LineBreaksDoubleSpace:
                EmitLineBreaksScalars(sb, scalarProps, valueExpr, indentLevel, doubleSpace: true);
                break;

            case FieldLayoutKind.List:
                EmitListScalars(sb, scalarProps, valueExpr, indentLevel);
                break;

            default:
                EmitOneLineScalars(sb, scalarProps, valueExpr, indentLevel, nestingDepth);
                break;
        }
    }

    private static void EmitOneLineScalars(
        StringBuilder sb,
        List<PropertyMetadata> scalarProps,
        string valueExpr,
        int indentLevel,
        int nestingDepth = 0)
    {
        var indent = new string(' ', indentLevel * 4);
        bool useBuilder = scalarProps.Any(p => p.IsNullableValueType || p.Kind == PropertyKind.String
            || (p.Kind == PropertyKind.StringArray && p.JoinSeparator != null)
            || p.SkipWhenDefault);
        var fieldsVar = nestingDepth == 0 ? "__fields" : $"__fields{nestingDepth}";

        if (useBuilder)
        {
            // Use List<MarkoutField> builder pattern when any scalar is nullable or string (to skip nulls/empties)
            sb.AppendLine($"{indent}var {fieldsVar} = new global::System.Collections.Generic.List<global::Markout.MarkoutField>();");
            foreach (var prop in scalarProps)
            {
                var propAccess = $"{valueExpr}.{prop.Name}";
                if (prop.IsNullableValueType)
                {
                    var valueStr = EmitHelpers.GetScalarValueExpression(prop, propAccess, nullable: true);
                    sb.AppendLine($"{indent}if ({propAccess}.HasValue)");
                    sb.AppendLine($"{indent}    {fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                }
                else if (prop.Kind == PropertyKind.String)
                {
                    sb.AppendLine($"{indent}if (!string.IsNullOrEmpty({propAccess}))");
                    sb.AppendLine($"{indent}    {fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {propAccess}));");
                }
                else if (prop.Kind == PropertyKind.StringArray && prop.JoinSeparator != null)
                {
                    var countProp = prop.IsArray ? "Length" : "Count";
                    sb.AppendLine($"{indent}if ({propAccess} != null && {propAccess}.{countProp} > 0)");
                    sb.AppendLine($"{indent}    {fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {EmitHelpers.GetScalarValueExpression(prop, propAccess)}));");
                }
                else
                {
                    var valueStr = EmitHelpers.GetScalarValueExpression(prop, propAccess);
                    if (prop.SkipWhenDefault)
                    {
                        var condition = EmitHelpers.GetNonDefaultCondition(prop, propAccess);
                        sb.AppendLine($"{indent}if ({condition})");
                        sb.AppendLine($"{indent}    {fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                    }
                    else
                    {
                        sb.AppendLine($"{indent}{fieldsVar}.Add(new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr}));");
                    }
                }
            }
            sb.AppendLine($"{indent}if ({fieldsVar}.Count > 0)");
            sb.AppendLine($"{indent}    writer.WriteCompactFields({fieldsVar});");
        }
        else
        {
            // Build inline MarkoutField array (no nullable/string scalars)
            var fields = new List<string>();
            foreach (var prop in scalarProps)
            {
                var propAccess = $"{valueExpr}.{prop.Name}";
                var valueStr = EmitHelpers.GetScalarValueExpression(prop, propAccess);
                fields.Add($"new global::Markout.MarkoutField(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr})");
            }

            sb.AppendLine($"{indent}writer.WriteCompactFields({string.Join(", ", fields)});");
        }
    }

    private static void EmitLineBreaksScalars(
        StringBuilder sb,
        List<PropertyMetadata> scalarProps,
        string valueExpr,
        int indentLevel,
        bool doubleSpace)
    {
        var indent = new string(' ', indentLevel * 4);
        var methodName = doubleSpace ? "WriteField" : "WriteFieldNoBreak";

        foreach (var prop in scalarProps)
        {
            var propAccess = $"{valueExpr}.{prop.Name}";
            var emitIndent = indent;

            // Wrap with skip-default condition if needed (for types not already conditionally rendered)
            bool needsSkipDefault = prop.SkipWhenDefault && !prop.IsNullableValueType
                && prop.Kind != PropertyKind.String
                && !(prop.Kind == PropertyKind.StringArray && prop.JoinSeparator != null);
            if (needsSkipDefault)
            {
                var condition = EmitHelpers.GetNonDefaultCondition(prop, propAccess);
                sb.AppendLine($"{indent}if ({condition})");
                sb.AppendLine($"{indent}{{");
                emitIndent = indent + "    ";
            }

            if (prop.ValueFormatterTypeName != null)
            {
                if (prop.IsNullableValueType)
                {
                    sb.AppendLine($"{emitIndent}if ({propAccess}.HasValue)");
                    sb.AppendLine($"{emitIndent}    writer.{methodName}(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", new {prop.ValueFormatterTypeName}().Format({propAccess}.Value));");
                }
                else
                {
                    sb.AppendLine($"{emitIndent}writer.{methodName}(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", new {prop.ValueFormatterTypeName}().Format({propAccess}));");
                }
            }
            else if (prop.IsNullableValueType)
            {
                sb.AppendLine($"{emitIndent}if ({propAccess}.HasValue)");
                if (prop.Kind == PropertyKind.Boolean)
                {
                    if (prop.BoolTrueValue != null && prop.BoolFalseValue != null)
                    {
                        sb.AppendLine($"{emitIndent}    writer.{methodName}(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {propAccess}.Value ? \"{EmitHelpers.EscapeString(prop.BoolTrueValue)}\" : \"{EmitHelpers.EscapeString(prop.BoolFalseValue)}\");");
                    }
                    else
                    {
                        sb.AppendLine($"{emitIndent}    writer.{methodName}(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {propAccess}.Value);");
                    }
                }
                else if (prop.CustomFormat != null)
                {
                    sb.AppendLine($"{emitIndent}    writer.{methodName}(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {propAccess}.Value.ToString(\"{EmitHelpers.EscapeString(prop.CustomFormat)}\", System.Globalization.CultureInfo.InvariantCulture));");
                }
                else
                {
                    sb.AppendLine($"{emitIndent}    writer.{methodName}(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {propAccess}.Value);");
                }
            }
            else if (prop.Kind == PropertyKind.String)
            {
                sb.AppendLine($"{emitIndent}if ({propAccess} != null)");
                sb.AppendLine($"{emitIndent}    writer.{methodName}(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {propAccess});");
            }
            else if (prop.Kind == PropertyKind.StringArray && prop.JoinSeparator != null)
            {
                var countProp = prop.IsArray ? "Length" : "Count";
                sb.AppendLine($"{emitIndent}if ({propAccess} != null && {propAccess}.{countProp} > 0)");
                sb.AppendLine($"{emitIndent}    writer.{methodName}(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {EmitHelpers.GetScalarValueExpression(prop, propAccess)});");
            }
            else if (prop.Kind == PropertyKind.Boolean)
            {
                if (prop.BoolTrueValue != null && prop.BoolFalseValue != null)
                {
                    sb.AppendLine($"{emitIndent}writer.{methodName}(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {propAccess} ? \"{EmitHelpers.EscapeString(prop.BoolTrueValue)}\" : \"{EmitHelpers.EscapeString(prop.BoolFalseValue)}\");");
                }
                else
                {
                    sb.AppendLine($"{emitIndent}writer.{methodName}(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {propAccess});");
                }
            }
            else if (prop.CustomFormat != null)
            {
                sb.AppendLine($"{emitIndent}writer.{methodName}(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {propAccess}.ToString(\"{EmitHelpers.EscapeString(prop.CustomFormat)}\", System.Globalization.CultureInfo.InvariantCulture));");
            }
            else
            {
                sb.AppendLine($"{emitIndent}writer.{methodName}(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {propAccess});");
            }

            if (needsSkipDefault)
            {
                sb.AppendLine($"{indent}}}");
            }
        }
    }

    private static void EmitListScalars(
        StringBuilder sb,
        List<PropertyMetadata> scalarProps,
        string valueExpr,
        int indentLevel)
    {
        var indent = new string(' ', indentLevel * 4);

        foreach (var prop in scalarProps)
        {
            var propAccess = $"{valueExpr}.{prop.Name}";
            var emitIndent = indent;

            bool needsSkipDefault = prop.SkipWhenDefault && !prop.IsNullableValueType
                && prop.Kind != PropertyKind.String
                && !(prop.Kind == PropertyKind.StringArray && prop.JoinSeparator != null);
            if (needsSkipDefault)
            {
                var condition = EmitHelpers.GetNonDefaultCondition(prop, propAccess);
                sb.AppendLine($"{indent}if ({condition})");
                sb.AppendLine($"{indent}{{");
                emitIndent = indent + "    ";
            }

            if (prop.IsNullableValueType)
            {
                var valueStr = EmitHelpers.GetScalarValueExpression(prop, propAccess, nullable: true);
                sb.AppendLine($"{emitIndent}if ({propAccess}.HasValue)");
                sb.AppendLine($"{emitIndent}    writer.WriteListItem($\"{EmitHelpers.EscapeString(prop.DisplayName)}: {{{valueStr}}}\");");
            }
            else if (prop.Kind == PropertyKind.String)
            {
                sb.AppendLine($"{emitIndent}if ({propAccess} != null)");
                sb.AppendLine($"{emitIndent}    writer.WriteListItem($\"{EmitHelpers.EscapeString(prop.DisplayName)}: {{{propAccess}}}\");");
            }
            else if (prop.Kind == PropertyKind.StringArray && prop.JoinSeparator != null)
            {
                var countProp = prop.IsArray ? "Length" : "Count";
                var valueStr = EmitHelpers.GetScalarValueExpression(prop, propAccess);
                sb.AppendLine($"{emitIndent}if ({propAccess} != null && {propAccess}.{countProp} > 0)");
                sb.AppendLine($"{emitIndent}    writer.WriteListItem($\"{EmitHelpers.EscapeString(prop.DisplayName)}: {{{valueStr}}}\");");
            }
            else
            {
                var valueStr = EmitHelpers.GetScalarValueExpression(prop, propAccess);
                sb.AppendLine($"{emitIndent}writer.WriteListItem($\"{EmitHelpers.EscapeString(prop.DisplayName)}: {{{valueStr}}}\");");
            }

            if (needsSkipDefault)
            {
                sb.AppendLine($"{indent}}}");
            }
        }
    }
}
