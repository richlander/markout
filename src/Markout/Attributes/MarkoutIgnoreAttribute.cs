namespace Markout;

/// <summary>
/// Excludes a property from MDF serialization.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MarkoutIgnoreAttribute : Attribute
{
}
