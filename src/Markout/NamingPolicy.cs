namespace Markout;

/// <summary>
/// Specifies how property names are transformed into display names.
/// </summary>
public enum NamingPolicy
{
    /// <summary>
    /// Split PascalCase into separate words.
    /// Example: InformationalVersion → "Informational Version", PublicKeyToken → "Public Key Token".
    /// This is the default.
    /// </summary>
    PascalCaseWords = 0,

    /// <summary>
    /// Use the property name as-is (or [MarkoutPropertyName] if specified).
    /// </summary>
    Verbatim = 1
}
