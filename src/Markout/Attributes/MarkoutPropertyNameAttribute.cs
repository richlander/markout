namespace Markout;

/// <summary>
/// Specifies a custom field name to use in MDF output instead of the property name.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MarkoutPropertyNameAttribute : Attribute
{
    /// <summary>
    /// Gets the custom name to use in MDF output.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance with the specified name.
    /// </summary>
    /// <param name="name">The custom name to use in MDF output.</param>
    public MarkoutPropertyNameAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}
