namespace Markout;

/// <summary>
/// Specifies how property names are transformed into display names.
/// </summary>
public enum NamingPolicy
{
    /// <summary>
    /// Use the property name as-is (or [MarkoutPropertyName] if specified).
    /// </summary>
    Default,

    /// <summary>
    /// Split PascalCase into separate words.
    /// Example: InformationalVersion → "Informational Version", PublicKeyToken → "Public Key Token".
    /// </summary>
    PascalCaseWords
}
