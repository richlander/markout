namespace Markout;

/// <summary>
/// Skips rendering a property when its value is null or empty.
/// For strings, skips when null or empty. For collections, skips when null or empty.
/// For nullable value types, skips when null.
/// Unlike <see cref="MarkoutSkipDefaultAttribute"/>, this does not skip non-null default values
/// like <c>false</c> for <see langword="bool"/> or <c>0</c> for <see langword="int"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MarkoutSkipNullAttribute : Attribute
{
}
