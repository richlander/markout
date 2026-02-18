namespace MarkdownTable.Formatting;

/// <summary>
/// A parsed field value — either a scalar string or an array of strings.
/// Similar to JsonElement in that it can represent multiple shapes.
/// </summary>
public readonly struct FieldValue
{
    private readonly string? _text;
    private readonly string[]? _items;

    private FieldValue(string? text, string[]? items)
    {
        _text = text;
        _items = items;
    }

    /// <summary>Creates a scalar field value.</summary>
    public static FieldValue FromText(string text) => new(text, null);

    /// <summary>Creates an array field value.</summary>
    public static FieldValue FromItems(string[] items) => new(null, items);

    /// <summary>True if this is a scalar value.</summary>
    public bool IsScalar => _items is null;

    /// <summary>True if this is an array value.</summary>
    public bool IsArray => _items is not null;

    /// <summary>
    /// The scalar text value. For arrays, returns the items joined with ", ".
    /// </summary>
    public string Text => _text ?? (_items is not null ? string.Join(", ", _items) : "");

    /// <summary>
    /// The array items. For scalars, returns a single-element array.
    /// </summary>
    public string[] Items => _items ?? (_text is not null ? [_text] : []);

    /// <summary>The number of items (1 for scalars).</summary>
    public int Count => _items?.Length ?? (_text is not null ? 1 : 0);

    public override string ToString() => Text;

    /// <summary>Implicit conversion to string (returns Text).</summary>
    public static implicit operator string(FieldValue value) => value.Text;
}
