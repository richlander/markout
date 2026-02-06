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

        // Joined string array: render as string.Join(separator, collection)
        if (prop.Kind == PropertyKind.StringArray && prop.JoinSeparator != null)
        {
            return $"string.Join(\"{EscapeString(prop.JoinSeparator)}\", {propAccess})";
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
                return $"{access}.ToString(\"{EscapeString(prop.CustomFormat)}\", System.Globalization.CultureInfo.InvariantCulture)";
            }
        }

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

        if (prop.IsNullableValueType)
        {
            var valueExpr = GetScalarValueExpression(prop, propAccess, nullable: true);
            return $"{propAccess}.HasValue ? {valueExpr} : \"\"";
        }

        // Joined string array in table cell
        if (prop.Kind == PropertyKind.StringArray && prop.JoinSeparator != null)
        {
            return $"{propAccess} != null ? string.Join(\"{EscapeString(prop.JoinSeparator)}\", {propAccess}) : \"\"";
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
                return $"{propAccess}.ToString(\"{EscapeString(prop.CustomFormat)}\", System.Globalization.CultureInfo.InvariantCulture)";
            }
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
            _ => $"{propAccess}?.ToString() ?? \"\""
        };
    }

    public static string GetCollectionCountCheck(PropertyMetadata prop, string propAccess)
    {
        var countProp = prop.IsArray ? "Length" : "Count";
        return $"{propAccess} != null && {propAccess}.{countProp} > 0";
    }

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
            PropertyKind.Enum;
    }

    public static string EscapeString(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
