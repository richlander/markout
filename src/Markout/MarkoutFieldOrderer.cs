namespace Markout;

/// <summary>
/// Applies field ordering policies used by generated serializers.
/// </summary>
public static class MarkoutFieldOrderer
{
    public static MarkoutField[] Apply(ReadOnlySpan<MarkoutField> fields, MarkoutFieldOrder order)
    {
        var ordered = fields.ToArray();
        if (order == MarkoutFieldOrder.Alphabetical)
            Array.Sort(ordered, static (left, right) => string.Compare(left.Key, right.Key, StringComparison.OrdinalIgnoreCase));
        return ordered;
    }
}
