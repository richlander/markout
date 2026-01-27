namespace Markout;

/// <summary>
/// Specifies a custom field name to use in Markout output instead of the property name.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MarkoutPropertyNameAttribute : Attribute
{
    /// <summary>
    /// Gets the custom name to use in Markout output.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance with the specified name.
    /// </summary>
    /// <param name="name">The custom name to use in Markout output.</param>
    public MarkoutPropertyNameAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}
