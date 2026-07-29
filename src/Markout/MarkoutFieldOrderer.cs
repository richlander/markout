namespace Markout;

/// <summary>
/// Applies field ordering policies used by generated serializers.
/// </summary>
public static class MarkoutFieldOrderer
{
    /// <summary>
    /// Returns the fields reordered according to <paramref name="order"/>.
    /// </summary>
    /// <param name="fields">The fields in input order.</param>
    /// <param name="order">The ordering policy to apply.</param>
    /// <returns>
    /// A new array holding the fields in the requested order. Input order is
    /// preserved when <paramref name="order"/> is <see cref="MarkoutFieldOrder.Input"/>.
    /// </returns>
    public static MarkoutField[] Apply(ReadOnlySpan<MarkoutField> fields, MarkoutFieldOrder order)
    {
        var ordered = fields.ToArray();
        if (order == MarkoutFieldOrder.Alphabetical)
            Array.Sort(ordered, static (left, right) => string.Compare(left.Key, right.Key, StringComparison.OrdinalIgnoreCase));
        return ordered;
    }
}
