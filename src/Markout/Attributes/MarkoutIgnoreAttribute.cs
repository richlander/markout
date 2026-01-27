namespace Markout;

/// <summary>
/// Excludes a property from Markout serialization.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MarkoutIgnoreAttribute : Attribute
{
}
