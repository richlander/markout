namespace Markout;

/// <summary>
/// Skips rendering a property when its value equals the default for its type
/// (e.g., <c>false</c> for <see langword="bool"/>, <c>0</c> for <see langword="int"/>,
/// <see langword="null"/> for reference types).
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MarkoutSkipDefaultAttribute : Attribute
{
}
