namespace MarkdownTable.Formatting;

/// <summary>
/// Delegate for type-safe parsing with validation.
/// </summary>
public delegate bool TryParseDelegate<TValue>(string input, out TValue value);

/// <summary>
/// Generic parameter descriptor that can parse values and set them on a mode configuration.
/// </summary>
public class ParameterDescriptor<TValue>(
    string name,
    string description,
    string type,
    TValue defaultValue,
    Action<TValue> setter,
    TryParseDelegate<TValue> parser) : IParameterDescriptor
{
    public string Name { get; } = name;
    public string Description { get; } = description;
    public string Type { get; } = type;
    public string DefaultValue { get; } = defaultValue?.ToString() ?? "";

    public bool TrySetValue(string value)
    {
        if (parser(value, out var parsedValue))
        {
            setter(parsedValue);
            return true;
        }
        return false;
    }
}
