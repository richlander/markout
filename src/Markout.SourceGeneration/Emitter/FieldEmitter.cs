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
            case FieldLayoutKind.OneLine:
                EmitOneLineScalars(sb, scalarProps, valueExpr, indentLevel, nestingDepth, sectionHeading, sectionLevel);
                break;

            case FieldLayoutKind.LineBreaks:
                EmitLineBreaksScalars(sb, scalarProps, valueExpr, indentLevel, doubleSpace: false, sectionHeading, sectionLevel);
                break;

            case FieldLayoutKind.LineBreaksDoubleSpace:
                EmitLineBreaksScalars(sb, scalarProps, valueExpr, indentLevel, doubleSpace: true, sectionHeading, sectionLevel);
                break;

            case FieldLayoutKind.List:
                EmitListScalars(sb, scalarProps, valueExpr, indentLevel, sectionHeading, sectionLevel);
                break;

            default:
                EmitOneLineScalars(sb, scalarProps, valueExpr, indentLevel, nestingDepth, sectionHeading, sectionLevel);
                break;
        }
    }

    private static void EmitOneLineScalars(
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
                    var valueStr = prop.ValueMap is { Count: > 0 }
                        ? EmitHelpers.GetScalarValueExpression(prop, propAccess)
                        : EmitHelpers.WrapWithLink(prop, propAccess, valueExpr);
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
            sb.AppendLine($"{indent}    writer.WriteCompactFields({fieldsVar});");
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
            sb.AppendLine($"{indent}writer.WriteCompactFields({string.Join(", ", fields)});");
        }
    }

    private static void EmitLineBreaksScalars(
        StringBuilder sb,
        List<PropertyMetadata> scalarProps,
        string valueExpr,
        int indentLevel,
        bool doubleSpace,
        string? sectionHeading = null,
        int sectionLevel = 2)
    {
        var indent = new string(' ', indentLevel * 4);
        var methodName = doubleSpace ? "WriteField" : "WriteFieldNoBreak";

        // For LineBreaks layout, all fields are emitted individually so we need a different
        // strategy for section headings: use a builder flag when any field is conditional,
        // or emit heading unconditionally when all fields always render.
        bool anyConditional = scalarProps.Any(p => p.IsNullableValueType || p.Kind == PropertyKind.String
            || (p.Kind == PropertyKind.StringArray && p.JoinSeparator != null)
            || p.SkipWhenDefault || p.SkipWhenNull || p.ShowWhenProperty != null);

        string? renderedVar = null;
        if (sectionHeading != null && anyConditional)
        {
            renderedVar = "__sectionRendered";
            sb.AppendLine($"{indent}var {renderedVar} = false;");
        }
        else if (sectionHeading != null)
        {
            sb.AppendLine($"{indent}writer.WriteHeading({sectionLevel}, \"{EmitHelpers.EscapeString(sectionHeading)}\");");
        }

        foreach (var prop in scalarProps)
        {
            var propAccess = $"{valueExpr}.{prop.Name}";
            var emitIndent = indent;

            // ShowWhenProperty wraps the entire field emission
            bool hasShowWhen = prop.ShowWhenProperty != null;
            if (hasShowWhen)
            {
                sb.AppendLine($"{indent}if ({valueExpr}.{prop.ShowWhenProperty})");
                sb.AppendLine($"{indent}{{");
                emitIndent = indent + "    ";
            }

            // Wrap with skip-default or skip-null condition if needed (for types not already conditionally rendered)
            bool needsSkipDefault = prop.SkipWhenDefault && !prop.IsNullableValueType
                && prop.Kind != PropertyKind.String
                && !(prop.Kind == PropertyKind.StringArray && prop.JoinSeparator != null);
            bool needsSkipNull = !needsSkipDefault && prop.SkipWhenNull && !prop.IsNullableValueType
                && prop.Kind != PropertyKind.String
                && !(prop.Kind == PropertyKind.StringArray && prop.JoinSeparator != null);
            if (needsSkipDefault)
            {
                var condition = EmitHelpers.GetNonDefaultCondition(prop, propAccess);
                sb.AppendLine($"{emitIndent}if ({condition})");
                sb.AppendLine($"{emitIndent}{{");
                emitIndent = emitIndent + "    ";
            }
            else if (needsSkipNull)
            {
                var condition = EmitHelpers.GetNonNullCondition(prop, propAccess);
                if (condition != null)
                {
                    sb.AppendLine($"{emitIndent}if ({condition})");
                    sb.AppendLine($"{emitIndent}{{");
                    emitIndent = emitIndent + "    ";
                }
            }

            // For nullable value types and strings, the condition is emitted inline;
            // we need to wrap the write call in a block to insert the heading guard.
            bool isInlineConditional = prop.IsNullableValueType || prop.Kind == PropertyKind.String
                || (prop.Kind == PropertyKind.StringArray && prop.JoinSeparator != null);

            if (prop.ValueFormatterTypeName != null)
            {
                if (prop.IsNullableValueType)
                {
                    sb.AppendLine($"{emitIndent}if ({propAccess}.HasValue)");
                    if (renderedVar != null)
                    {
                        sb.AppendLine($"{emitIndent}{{");
                        EmitHeadingOnce(sb, renderedVar, sectionHeading!, sectionLevel, emitIndent + "    ");
                        sb.AppendLine($"{emitIndent}    writer.{methodName}(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", new {prop.ValueFormatterTypeName}().Format({propAccess}.Value));");
                        sb.AppendLine($"{emitIndent}}}");
                    }
                    else
                    {
                        sb.AppendLine($"{emitIndent}    writer.{methodName}(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", new {prop.ValueFormatterTypeName}().Format({propAccess}.Value));");
                    }
                }
                else
                {
                    EmitHeadingOnceIfNeeded(sb, renderedVar, sectionHeading, sectionLevel, emitIndent);
                    sb.AppendLine($"{emitIndent}writer.{methodName}(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", new {prop.ValueFormatterTypeName}().Format({propAccess}));");
                }
            }
            else if (prop.IsNullableValueType)
            {
                sb.AppendLine($"{emitIndent}if ({propAccess}.HasValue)");
                if (renderedVar != null)
                {
                    sb.AppendLine($"{emitIndent}{{");
                    EmitHeadingOnce(sb, renderedVar, sectionHeading!, sectionLevel, emitIndent + "    ");
                }
                if (prop.Kind == PropertyKind.Boolean)
                {
                    var writeIndent = renderedVar != null ? emitIndent + "    " : emitIndent + "    ";
                    if (prop.BoolTrueValue != null && prop.BoolFalseValue != null)
                    {
                        sb.AppendLine($"{writeIndent}writer.{methodName}(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {propAccess}.Value ? \"{EmitHelpers.EscapeString(prop.BoolTrueValue)}\" : \"{EmitHelpers.EscapeString(prop.BoolFalseValue)}\");");
                    }
                    else
                    {
                        sb.AppendLine($"{writeIndent}writer.{methodName}(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {propAccess}.Value);");
                    }
                }
                else if (prop.CustomFormat != null)
                {
                    var writeIndent = renderedVar != null ? emitIndent + "    " : emitIndent + "    ";
                    sb.AppendLine($"{writeIndent}writer.{methodName}(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {propAccess}.Value.ToString(\"{EmitHelpers.EscapeString(prop.CustomFormat)}\", System.Globalization.CultureInfo.InvariantCulture));");
                }
                else
                {
                    var writeIndent = renderedVar != null ? emitIndent + "    " : emitIndent + "    ";
                    sb.AppendLine($"{writeIndent}writer.{methodName}(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {propAccess}.Value);");
                }
                if (renderedVar != null)
                {
                    sb.AppendLine($"{emitIndent}}}");
                }
            }
            else if (prop.Kind == PropertyKind.String)
            {
                var valueStr = prop.ValueMap is { Count: > 0 }
                    ? EmitHelpers.GetScalarValueExpression(prop, propAccess)
                    : EmitHelpers.WrapWithLink(prop, propAccess, valueExpr);
                sb.AppendLine($"{emitIndent}if ({propAccess} != null)");
                if (renderedVar != null)
                {
                    sb.AppendLine($"{emitIndent}{{");
                    EmitHeadingOnce(sb, renderedVar, sectionHeading!, sectionLevel, emitIndent + "    ");
                    sb.AppendLine($"{emitIndent}    writer.{methodName}(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr});");
                    sb.AppendLine($"{emitIndent}}}");
                }
                else
                {
                    sb.AppendLine($"{emitIndent}    writer.{methodName}(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {valueStr});");
                }
            }
            else if (prop.Kind == PropertyKind.StringArray && prop.JoinSeparator != null)
            {
                var countProp = prop.IsArray ? "Length" : "Count";
                sb.AppendLine($"{emitIndent}if ({propAccess} != null && {propAccess}.{countProp} > 0)");
                if (renderedVar != null)
                {
                    sb.AppendLine($"{emitIndent}{{");
                    EmitHeadingOnce(sb, renderedVar, sectionHeading!, sectionLevel, emitIndent + "    ");
                    sb.AppendLine($"{emitIndent}    writer.{methodName}(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {EmitHelpers.GetScalarValueExpression(prop, propAccess)});");
                    sb.AppendLine($"{emitIndent}}}");
                }
                else
                {
                    sb.AppendLine($"{emitIndent}    writer.{methodName}(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {EmitHelpers.GetScalarValueExpression(prop, propAccess)});");
                }
            }
            else if (prop.Kind == PropertyKind.Boolean)
            {
                EmitHeadingOnceIfNeeded(sb, renderedVar, sectionHeading, sectionLevel, emitIndent);
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
                EmitHeadingOnceIfNeeded(sb, renderedVar, sectionHeading, sectionLevel, emitIndent);
                sb.AppendLine($"{emitIndent}writer.{methodName}(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {propAccess}.ToString(\"{EmitHelpers.EscapeString(prop.CustomFormat)}\", System.Globalization.CultureInfo.InvariantCulture));");
            }
            else
            {
                EmitHeadingOnceIfNeeded(sb, renderedVar, sectionHeading, sectionLevel, emitIndent);
                sb.AppendLine($"{emitIndent}writer.{methodName}(\"{EmitHelpers.EscapeString(prop.DisplayName)}\", {propAccess});");
            }

            if (needsSkipDefault || needsSkipNull)
            {
                sb.AppendLine($"{(hasShowWhen ? indent + "    " : indent)}}}");
            }
            if (hasShowWhen)
            {
                sb.AppendLine($"{indent}}}");
            }
        }
    }

    private static void EmitListScalars(
        StringBuilder sb,
        List<PropertyMetadata> scalarProps,
        string valueExpr,
        int indentLevel,
        string? sectionHeading = null,
        int sectionLevel = 2)
    {
        var indent = new string(' ', indentLevel * 4);

        bool anyConditional = scalarProps.Any(p => p.IsNullableValueType || p.Kind == PropertyKind.String
            || (p.Kind == PropertyKind.StringArray && p.JoinSeparator != null)
            || p.SkipWhenDefault || p.SkipWhenNull || p.ShowWhenProperty != null);

        string? renderedVar = null;
        if (sectionHeading != null && anyConditional)
        {
            renderedVar = "__sectionRendered";
            sb.AppendLine($"{indent}var {renderedVar} = false;");
        }
        else if (sectionHeading != null)
        {
            sb.AppendLine($"{indent}writer.WriteHeading({sectionLevel}, \"{EmitHelpers.EscapeString(sectionHeading)}\");");
        }

        foreach (var prop in scalarProps)
        {
            var propAccess = $"{valueExpr}.{prop.Name}";
            var emitIndent = indent;

            // ShowWhenProperty wraps the entire field emission
            bool hasShowWhen = prop.ShowWhenProperty != null;
            if (hasShowWhen)
            {
                sb.AppendLine($"{indent}if ({valueExpr}.{prop.ShowWhenProperty})");
                sb.AppendLine($"{indent}{{");
                emitIndent = indent + "    ";
            }

            bool needsSkipDefault = prop.SkipWhenDefault && !prop.IsNullableValueType
                && prop.Kind != PropertyKind.String
                && !(prop.Kind == PropertyKind.StringArray && prop.JoinSeparator != null);
            bool needsSkipNull = !needsSkipDefault && prop.SkipWhenNull && !prop.IsNullableValueType
                && prop.Kind != PropertyKind.String
                && !(prop.Kind == PropertyKind.StringArray && prop.JoinSeparator != null);
            if (needsSkipDefault)
            {
                var condition = EmitHelpers.GetNonDefaultCondition(prop, propAccess);
                sb.AppendLine($"{emitIndent}if ({condition})");
                sb.AppendLine($"{emitIndent}{{");
                emitIndent = emitIndent + "    ";
            }
            else if (needsSkipNull)
            {
                var condition = EmitHelpers.GetNonNullCondition(prop, propAccess);
                if (condition != null)
                {
                    sb.AppendLine($"{emitIndent}if ({condition})");
                    sb.AppendLine($"{emitIndent}{{");
                    emitIndent = emitIndent + "    ";
                }
            }

            if (prop.IsNullableValueType)
            {
                var valueStr = EmitHelpers.GetScalarValueExpression(prop, propAccess, nullable: true);
                valueStr = EmitHelpers.WrapWithLink(prop, valueStr, valueExpr);
                sb.AppendLine($"{emitIndent}if ({propAccess}.HasValue)");
                if (renderedVar != null)
                {
                    sb.AppendLine($"{emitIndent}{{");
                    EmitHeadingOnce(sb, renderedVar, sectionHeading!, sectionLevel, emitIndent + "    ");
                    sb.AppendLine($"{emitIndent}    writer.WriteListItem($\"{EmitHelpers.EscapeString(prop.DisplayName)}: {{{valueStr}}}\");");
                    sb.AppendLine($"{emitIndent}}}");
                }
                else
                {
                    sb.AppendLine($"{emitIndent}    writer.WriteListItem($\"{EmitHelpers.EscapeString(prop.DisplayName)}: {{{valueStr}}}\");");
                }
            }
            else if (prop.Kind == PropertyKind.String)
            {
                var valueStr = prop.ValueMap is { Count: > 0 }
                    ? EmitHelpers.GetScalarValueExpression(prop, propAccess)
                    : EmitHelpers.WrapWithLink(prop, propAccess, valueExpr);
                sb.AppendLine($"{emitIndent}if ({propAccess} != null)");
                if (renderedVar != null)
                {
                    sb.AppendLine($"{emitIndent}{{");
                    EmitHeadingOnce(sb, renderedVar, sectionHeading!, sectionLevel, emitIndent + "    ");
                    sb.AppendLine($"{emitIndent}    writer.WriteListItem($\"{EmitHelpers.EscapeString(prop.DisplayName)}: {{{valueStr}}}\");");
                    sb.AppendLine($"{emitIndent}}}");
                }
                else
                {
                    sb.AppendLine($"{emitIndent}    writer.WriteListItem($\"{EmitHelpers.EscapeString(prop.DisplayName)}: {{{valueStr}}}\");");
                }
            }
            else if (prop.Kind == PropertyKind.StringArray && prop.JoinSeparator != null)
            {
                var countProp = prop.IsArray ? "Length" : "Count";
                var valueStr = EmitHelpers.GetScalarValueExpression(prop, propAccess);
                sb.AppendLine($"{emitIndent}if ({propAccess} != null && {propAccess}.{countProp} > 0)");
                if (renderedVar != null)
                {
                    sb.AppendLine($"{emitIndent}{{");
                    EmitHeadingOnce(sb, renderedVar, sectionHeading!, sectionLevel, emitIndent + "    ");
                    sb.AppendLine($"{emitIndent}    writer.WriteListItem($\"{EmitHelpers.EscapeString(prop.DisplayName)}: {{{valueStr}}}\");");
                    sb.AppendLine($"{emitIndent}}}");
                }
                else
                {
                    sb.AppendLine($"{emitIndent}    writer.WriteListItem($\"{EmitHelpers.EscapeString(prop.DisplayName)}: {{{valueStr}}}\");");
                }
            }
            else
            {
                var valueStr = EmitHelpers.GetScalarValueExpression(prop, propAccess);
                valueStr = EmitHelpers.WrapWithLink(prop, valueStr, valueExpr);
                EmitHeadingOnceIfNeeded(sb, renderedVar, sectionHeading, sectionLevel, emitIndent);
                sb.AppendLine($"{emitIndent}writer.WriteListItem($\"{EmitHelpers.EscapeString(prop.DisplayName)}: {{{valueStr}}}\");");
            }

            if (needsSkipDefault || needsSkipNull)
            {
                sb.AppendLine($"{(hasShowWhen ? indent + "    " : indent)}}}");
            }
            if (hasShowWhen)
            {
                sb.AppendLine($"{indent}}}");
            }
        }
    }

    /// <summary>
    /// Emits a heading-once guard: if the flag is false, write the heading and set it to true.
    /// </summary>
    private static void EmitHeadingOnce(StringBuilder sb, string renderedVar, string heading, int level, string indent)
    {
        sb.AppendLine($"{indent}if (!{renderedVar}) {{ writer.WriteHeading({level}, \"{EmitHelpers.EscapeString(heading)}\"); {renderedVar} = true; }}");
    }

    /// <summary>
    /// Convenience: emits heading-once guard only when renderedVar is set (conditional section heading).
    /// </summary>
    private static void EmitHeadingOnceIfNeeded(StringBuilder sb, string? renderedVar, string? heading, int level, string indent)
    {
        if (renderedVar != null)
            EmitHeadingOnce(sb, renderedVar, heading!, level, indent);
    }
}
