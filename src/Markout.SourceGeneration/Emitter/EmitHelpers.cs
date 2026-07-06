using System.Linq;
using System.Text;
using Markout.SourceGeneration.Parser;

namespace Markout.SourceGeneration.Emitter;

/// <summary>
/// Shared helper methods for code emission.
/// </summary>
internal static class EmitHelpers
{
    public static string GetScalarValueExpression(PropertyMetadata prop, string propAccess, bool nullable = false)
    {
        var access = nullable ? $"{propAccess}.Value" : propAccess;

        // Composite cell: render its dense, human-readable form
        if (prop.Kind == PropertyKind.CompositeCell)
            return $"global::Markout.MarkoutCell.ToInlineString({access}, {DeltaLiteral(prop)}, {UnitLiteral(prop)})";

        // Value formatter takes highest priority
        if (prop.ValueFormatterTypeName != null)
        {
            var expr = $"new {prop.ValueFormatterTypeName}().Format({access})";
            return WrapWithDisplayFormat(prop, expr);
        }

        // Joined array: render as string.Join(separator, collection) or LINQ Select for complex types
        if (IsJoinedArray(prop))
        {
            if (prop.Kind == PropertyKind.StringArray)
                return $"string.Join(\"{EscapeString(prop.JoinSeparator!)}\", {propAccess})";
            return GetComplexJoinExpression(prop, propAccess);
        }

        if (prop.Kind == PropertyKind.Boolean && prop.BoolTrueValue != null && prop.BoolFalseValue != null)
        {
            return $"({access} ? \"{EscapeString(prop.BoolTrueValue)}\" : \"{EscapeString(prop.BoolFalseValue)}\")";
        }

        // Custom format overrides default formatting for formattable types
        if (prop.CustomFormat != null)
        {
            if (prop.Kind is PropertyKind.Int32 or PropertyKind.Int64 or PropertyKind.Double or PropertyKind.Decimal
                or PropertyKind.DateTime or PropertyKind.DateTimeOffset)
            {
                var expr = $"{access}.ToString(\"{EscapeString(prop.CustomFormat)}\", System.Globalization.CultureInfo.InvariantCulture)";
                return WrapWithDisplayFormat(prop, expr);
            }
        }

        // DisplayFormat wraps the final value
        if (prop.DisplayFormat != null)
        {
            var fmtExpr = $"string.Format(System.Globalization.CultureInfo.InvariantCulture, \"{EscapeString(prop.DisplayFormat)}\", {access})";
            return WrapWithValueMap(prop, fmtExpr);
        }

        var result = prop.Kind switch
        {
            PropertyKind.Boolean => $"({access} ? \"yes\" : \"no\")",
            PropertyKind.String => $"{propAccess} ?? \"\"",
            PropertyKind.Int32 or PropertyKind.Int64 or PropertyKind.Double or PropertyKind.Decimal
                => $"{access}.ToString(System.Globalization.CultureInfo.InvariantCulture)",
            PropertyKind.DateTime or PropertyKind.DateTimeOffset
                => $"{access}.ToString(\"O\", System.Globalization.CultureInfo.InvariantCulture)",
            PropertyKind.Enum => $"{access}.ToString()",
            _ => $"{propAccess}?.ToString() ?? \"\""
        };
        return WrapWithValueMap(prop, result);
    }

    private static string WrapWithDisplayFormat(PropertyMetadata prop, string innerExpr)
    {
        if (prop.DisplayFormat != null)
            return $"string.Format(System.Globalization.CultureInfo.InvariantCulture, \"{EscapeString(prop.DisplayFormat)}\", {innerExpr})";
        return innerExpr;
    }

    private static string WrapWithTableOrDisplayFormat(PropertyMetadata prop, string innerExpr)
    {
        if (prop.TableDisplayFormat != null)
            return $"string.Format(System.Globalization.CultureInfo.InvariantCulture, \"{EscapeString(prop.TableDisplayFormat)}\", {innerExpr})";
        if (prop.DisplayFormat != null)
            return $"string.Format(System.Globalization.CultureInfo.InvariantCulture, \"{EscapeString(prop.DisplayFormat)}\", {innerExpr})";
        return innerExpr;
    }

    private static string WrapWithTableDisplayFormat(PropertyMetadata prop, string innerExpr)
    {
        if (prop.TableDisplayFormat != null)
            return $"string.Format(System.Globalization.CultureInfo.InvariantCulture, \"{EscapeString(prop.TableDisplayFormat)}\", {innerExpr})";
        return innerExpr;
    }

    /// <summary>
    /// Like GetScalarValueExpression but without DisplayFormat wrapping.
    /// Used when we need to apply TableDisplayFormat instead.
    /// </summary>
    private static string GetScalarValueExpressionNoDisplayFormat(PropertyMetadata prop, string propAccess, bool nullable = false)
    {
        var access = nullable ? $"{propAccess}.Value" : propAccess;

        if (prop.ValueFormatterTypeName != null)
            return $"new {prop.ValueFormatterTypeName}().Format({access})";

        if (prop.Kind == PropertyKind.Boolean && prop.BoolTrueValue != null && prop.BoolFalseValue != null)
            return $"({access} ? \"{EscapeString(prop.BoolTrueValue)}\" : \"{EscapeString(prop.BoolFalseValue)}\")";

        if (prop.CustomFormat != null && prop.Kind is PropertyKind.Int32 or PropertyKind.Int64 or PropertyKind.Double or PropertyKind.Decimal or PropertyKind.DateTime or PropertyKind.DateTimeOffset)
            return $"{access}.ToString(\"{EscapeString(prop.CustomFormat)}\", System.Globalization.CultureInfo.InvariantCulture)";

        return prop.Kind switch
        {
            PropertyKind.Boolean => $"({access} ? \"yes\" : \"no\")",
            PropertyKind.String => $"{propAccess} ?? \"\"",
            PropertyKind.Int32 or PropertyKind.Int64 or PropertyKind.Double or PropertyKind.Decimal
                => $"{access}.ToString(System.Globalization.CultureInfo.InvariantCulture)",
            PropertyKind.DateTime or PropertyKind.DateTimeOffset
                => $"{access}.ToString(\"O\", System.Globalization.CultureInfo.InvariantCulture)",
            PropertyKind.Enum => $"{access}.ToString()",
            _ => $"{propAccess}?.ToString() ?? \"\""
        };
    }

    public static string GetTableCellValue(PropertyMetadata prop, string itemExpr)
    {
        var propAccess = $"{itemExpr}.{prop.Name}";
        var result = GetTableCellValueCore(prop, propAccess, itemExpr);
        result = WrapWithValueMap(prop, result);
        return WrapWithLink(prop, result, itemExpr);
    }

    private static string GetTableCellValueCore(PropertyMetadata prop, string propAccess, string itemExpr)
    {
        // Composite cell in a table column renders its dense form (no per-column decomposition).
        if (prop.Kind == PropertyKind.CompositeCell)
            return $"global::Markout.MarkoutCell.ToInlineString({propAccess}, {DeltaLiteral(prop)}, {UnitLiteral(prop)})";

        if (prop.IsNullableValueType)
        {
            if (prop.ValueFormatterTypeName != null)
            {
                var inner = $"new {prop.ValueFormatterTypeName}().Format({propAccess}.Value)";
                return $"{propAccess}.HasValue ? {WrapWithTableOrDisplayFormat(prop, inner)} : \"\"";
            }
            // For nullable with table display, we need to get the inner value then wrap
            if (prop.TableDisplayFormat != null)
            {
                var innerValue = GetScalarValueExpressionNoDisplayFormat(prop, propAccess, nullable: true);
                return $"{propAccess}.HasValue ? {WrapWithTableDisplayFormat(prop, innerValue)} : \"\"";
            }
            var valueExpr = GetScalarValueExpression(prop, propAccess, nullable: true);
            return $"{propAccess}.HasValue ? {valueExpr} : \"\"";
        }

        // Value formatter takes highest priority
        if (prop.ValueFormatterTypeName != null)
        {
            var inner = $"new {prop.ValueFormatterTypeName}().Format({propAccess})";
            return WrapWithTableOrDisplayFormat(prop, inner);
        }

        // Joined array in table cell
        if (IsJoinedArray(prop))
        {
            if (prop.Kind == PropertyKind.StringArray)
                return $"{propAccess} != null ? string.Join(\"{EscapeString(prop.JoinSeparator!)}\", {propAccess}) : \"\"";
            return $"{propAccess} != null ? {GetComplexJoinExpression(prop, propAccess)} : \"\"";
        }

        if (prop.Kind == PropertyKind.Boolean && prop.BoolTrueValue != null && prop.BoolFalseValue != null)
        {
            return $"{propAccess} ? \"{EscapeString(prop.BoolTrueValue)}\" : \"{EscapeString(prop.BoolFalseValue)}\"";
        }

        // Custom format overrides default formatting for formattable types
        if (prop.CustomFormat != null)
        {
            if (prop.Kind is PropertyKind.Int32 or PropertyKind.Int64 or PropertyKind.Double or PropertyKind.Decimal
                or PropertyKind.DateTime or PropertyKind.DateTimeOffset)
            {
                var inner = $"{propAccess}.ToString(\"{EscapeString(prop.CustomFormat)}\", System.Globalization.CultureInfo.InvariantCulture)";
                return WrapWithTableOrDisplayFormat(prop, inner);
            }
        }

        // TableDisplayFormat takes priority over DisplayFormat in table context
        if (prop.TableDisplayFormat != null)
        {
            return $"string.Format(System.Globalization.CultureInfo.InvariantCulture, \"{EscapeString(prop.TableDisplayFormat)}\", {propAccess})";
        }

        // DisplayFormat wraps the final value
        if (prop.DisplayFormat != null)
        {
            return $"string.Format(System.Globalization.CultureInfo.InvariantCulture, \"{EscapeString(prop.DisplayFormat)}\", {propAccess})";
        }

        return prop.Kind switch
        {
            PropertyKind.Boolean => $"{propAccess} ? \"yes\" : \"no\"",
            PropertyKind.String => $"{propAccess} ?? \"\"",
            PropertyKind.Int32 or PropertyKind.Int64 or PropertyKind.Double or PropertyKind.Decimal
                => $"{propAccess}.ToString(System.Globalization.CultureInfo.InvariantCulture)",
            PropertyKind.DateTime or PropertyKind.DateTimeOffset
                => $"{propAccess}.ToString(\"O\", System.Globalization.CultureInfo.InvariantCulture)",
            PropertyKind.Enum => $"{propAccess}.ToString()",
            PropertyKind.Formattable => $"((global::Markout.IMarkoutFormattable){propAccess})?.ToMarkoutString() ?? \"\"",
            _ => $"{propAccess}?.ToString() ?? \"\""
        };
    }

    /// <summary>
    /// Wraps a value expression with markdown link formatting if the property has [MarkoutLink].
    /// </summary>
    /// <param name="prop">The property metadata.</param>
    /// <param name="innerExpr">The formatted value expression (the URL).</param>
    /// <param name="parentExpr">The parent object expression, used to access TextProperty.</param>
    public static string WrapWithLink(PropertyMetadata prop, string innerExpr, string parentExpr)
    {
        if (!prop.IsLink)
            return innerExpr;

        if (prop.LinkTextProperty != null)
        {
            // [text](url) where text comes from another property
            return $"$\"[{{{parentExpr}.{prop.LinkTextProperty}}}]({{{innerExpr}}})\"";
        }

        // [url](url) — bare link
        return $"$\"[{{{innerExpr}}}]({{{innerExpr}}})\"";
    }

    /// <summary>
    /// Wraps a string value expression with a value-map switch expression that prepends a badge.
    /// Unmatched values pass through unchanged.
    /// </summary>
    public static string WrapWithValueMap(PropertyMetadata prop, string innerExpr)
    {
        if (prop.ValueMap == null || prop.ValueMap.Count == 0)
            return innerExpr;

        // Generate: ((value) switch { "key1" => "badge1 " + value, "key2" => "badge2 " + value, _ => value })
        // Parenthesize innerExpr to avoid precedence issues (e.g. ?? vs switch)
        var arms = new StringBuilder();
        foreach (var (key, badge) in prop.ValueMap)
        {
            arms.Append($"\"{EscapeString(key)}\" => \"{EscapeString(badge)} \" + {innerExpr}, ");
        }
        arms.Append($"_ => {innerExpr}");

        return $"(({innerExpr}) switch {{ {arms} }})";
    }

    public static string GetCollectionCountCheck(PropertyMetadata prop, string propAccess)
    {
        var countProp = prop.IsArray ? "Length" : "Count";
        return $"{propAccess} != null && {propAccess}.{countProp} > 0";
    }

    /// <summary>
    /// Emits truncation logic for [MarkoutMaxItems]. Call after emitting the collection variable.
    /// Returns the variable name to use for iteration (either the original or a truncated version).
    /// </summary>
    public static (string IterVar, bool NeedsEllipsis) EmitMaxItemsTruncation(
        StringBuilder sb,
        PropertyMetadata prop,
        string propAccess,
        string indent,
        int nestingDepth)
    {
        if (prop.MaxItems == null)
            return (propAccess, false);

        var maxItems = prop.MaxItems.Value;
        var ellipsis = prop.MaxItemsEllipsisFormat ?? "... and {0} more";
        var countProp = prop.IsArray ? "Length" : "Count";
        var truncVar = nestingDepth == 0 ? "__truncated" : $"__truncated{nestingDepth}";
        var remainVar = nestingDepth == 0 ? "__remaining" : $"__remaining{nestingDepth}";

        sb.AppendLine($"{indent}var {remainVar} = {propAccess}.{countProp} - {maxItems};");
        sb.AppendLine($"{indent}var {truncVar} = {remainVar} > 0 ? global::System.Linq.Enumerable.ToArray(global::System.Linq.Enumerable.Take({propAccess}, {maxItems})) : global::System.Linq.Enumerable.ToArray({propAccess});");

        return (truncVar, true);
    }

    public static void EmitMaxItemsEllipsis(
        StringBuilder sb,
        PropertyMetadata prop,
        string propAccess,
        string indent,
        int nestingDepth)
    {
        if (prop.MaxItems == null)
            return;

        var maxItems = prop.MaxItems.Value;
        var ellipsis = prop.MaxItemsEllipsisFormat ?? "... and {0} more";
        var countProp = prop.IsArray ? "Length" : "Count";
        var remainVar = nestingDepth == 0 ? "__remaining" : $"__remaining{nestingDepth}";

        sb.AppendLine($"{indent}if ({remainVar} > 0)");
        sb.AppendLine($"{indent}    writer.WriteParagraph(string.Format(\"{EscapeString(ellipsis)}\", {remainVar}));");
    }

    /// <summary>
    /// Builds a string.Join expression for a complex array with [MarkoutJoin].
    /// Each element is formatted by concatenating its visible scalar properties.
    /// </summary>
    public static string GetComplexJoinExpression(PropertyMetadata prop, string propAccess)
    {
        var separator = EscapeString(prop.JoinSeparator!);
        var elemProps = prop.ElementProperties?
            .Where(p => !p.IsIgnored && IsScalarKind(p.Kind))
            .ToList();

        if (elemProps == null || elemProps.Count == 0)
            return $"string.Join(\"{separator}\", global::System.Linq.Enumerable.Select({propAccess}, __e => __e.ToString() ?? \"\"))";

        if (elemProps.Count == 1)
            return $"string.Join(\"{separator}\", global::System.Linq.Enumerable.Select({propAccess}, __e => {GetElementPropertyValue(elemProps[0])}))";

        var parts = string.Join(" + \" \" + ", elemProps.Select(p => GetElementPropertyValue(p)));
        return $"string.Join(\"{separator}\", global::System.Linq.Enumerable.Select({propAccess}, __e => {parts}))";
    }

    private static string GetElementPropertyValue(PropertyMetadata prop)
    {
        return prop.Kind switch
        {
            PropertyKind.String => $"(__e.{prop.Name} ?? \"\")",
            PropertyKind.Boolean => $"(__e.{prop.Name} ? \"yes\" : \"no\")",
            PropertyKind.Enum => $"__e.{prop.Name}.ToString()",
            _ => $"__e.{prop.Name}.ToString(System.Globalization.CultureInfo.InvariantCulture)"
        };
    }

    /// <summary>
    /// Returns true if the property is an array with [MarkoutJoin], making it render as a scalar field.
    /// </summary>
    public static bool IsJoinedArray(PropertyMetadata prop)
    {
        return prop.JoinSeparator != null &&
               (prop.Kind == PropertyKind.StringArray || prop.Kind == PropertyKind.ComplexArray);
    }

    /// <summary>Emits the runtime Markout.Delta enum literal for a composite cell property.</summary>
    public static string DeltaLiteral(PropertyMetadata prop) => prop.DeltaMode switch
    {
        MarkoutDeltaKind.Percent => "global::Markout.Delta.Percent",
        MarkoutDeltaKind.Absolute => "global::Markout.Delta.Absolute",
        _ => "global::Markout.Delta.None"
    };

    /// <summary>Emits the unit string literal (or <c>null</c>) for a composite cell property.</summary>
    public static string UnitLiteral(PropertyMetadata prop)
        => prop.Unit == null ? "null" : $"\"{EscapeString(prop.Unit)}\"";

    /// <summary>Emits the runtime Markout.Goal enum literal for a composite cell property.</summary>
    public static string GoalLiteral(PropertyMetadata prop) => prop.Goal switch
    {
        MarkoutGoalKind.Higher => "global::Markout.Goal.Higher",
        MarkoutGoalKind.Lower => "global::Markout.Goal.Lower",
        _ => "global::Markout.Goal.Context"
    };

    /// <summary>Emits the noise-tolerance double literal for a composite cell property.</summary>
    public static string NoiseLiteral(PropertyMetadata prop)
        => prop.Noise.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

    public static bool IsScalarKind(PropertyKind kind)
    {
        return kind is
            PropertyKind.String or
            PropertyKind.Boolean or
            PropertyKind.Int32 or
            PropertyKind.Int64 or
            PropertyKind.Double or
            PropertyKind.Decimal or
            PropertyKind.DateTime or
            PropertyKind.DateTimeOffset or
            PropertyKind.Enum or
            PropertyKind.CompositeCell;
    }

    public static string EscapeString(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    /// <summary>
    /// Converts a PascalCase type name to snake_case for template binding.
    /// Example: CveReport → cve_report, VulnerabilityTable → vulnerability_table
    /// </summary>
    public static string ToSnakeCase(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return typeName;

        var result = new StringBuilder();
        for (int i = 0; i < typeName.Length; i++)
        {
            var c = typeName[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                    result.Append('_');
                result.Append(char.ToLowerInvariant(c));
            }
            else
            {
                result.Append(c);
            }
        }
        return result.ToString();
    }

    /// <summary>
    /// Returns a condition expression that is true when the property value is NOT null/empty.
    /// Used by [MarkoutSkipNull] to conditionally skip rendering.
    /// Unlike GetNonDefaultCondition, this does not skip non-null defaults like false or 0.
    /// </summary>
    public static string? GetNonNullCondition(PropertyMetadata prop, string propAccess)
    {
        if (prop.IsNullableValueType)
            return $"{propAccess}.HasValue";

        return prop.Kind switch
        {
            PropertyKind.String => $"!string.IsNullOrEmpty({propAccess})",
            PropertyKind.StringArray => $"{propAccess} != null && {propAccess}.{(prop.IsArray ? "Length" : "Count")} > 0",
            PropertyKind.Formattable => $"{propAccess} != null",
            // Non-nullable value types (bool, int, etc.) are never null — no condition needed
            // Reference-type composite cells can be null; value-type ones never are.
            PropertyKind.CompositeCell => prop.IsReferenceTypeCell ? $"{propAccess} != null" : null,
            PropertyKind.Boolean or PropertyKind.Int32 or PropertyKind.Int64
                or PropertyKind.Double or PropertyKind.Decimal
                or PropertyKind.DateTime or PropertyKind.DateTimeOffset
                or PropertyKind.Enum => null,
            _ => $"{propAccess} != null"
        };
    }

    /// <summary>
    /// Returns a condition expression that is true when the property value is NOT the default.
    /// Used by [MarkoutSkipDefault] to conditionally skip rendering.
    /// </summary>
    public static string? GetNonDefaultCondition(PropertyMetadata prop, string propAccess)
    {
        if (prop.IsNullableValueType)
            return $"{propAccess}.HasValue";

        return prop.Kind switch
        {
            PropertyKind.String => $"!string.IsNullOrEmpty({propAccess})",
            PropertyKind.Boolean => propAccess,
            PropertyKind.Int32 or PropertyKind.Int64 => $"{propAccess} != 0",
            PropertyKind.Double or PropertyKind.Decimal => $"{propAccess} != 0",
            PropertyKind.DateTime or PropertyKind.DateTimeOffset => $"{propAccess} != default",
            PropertyKind.Enum => $"{propAccess} != default({prop.TypeName})",
            // Null-safe for both value-type and reference-type IMarkoutCell implementations.
            PropertyKind.CompositeCell => $"!global::System.Collections.Generic.EqualityComparer<{prop.TypeName}>.Default.Equals({propAccess}, default({prop.TypeName}))",
            PropertyKind.StringArray => $"{propAccess} != null && {propAccess}.{(prop.IsArray ? "Length" : "Count")} > 0",
            PropertyKind.Formattable => $"{propAccess} != null",
            _ => $"{propAccess} != null"
        };
    }
}
