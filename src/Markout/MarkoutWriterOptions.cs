namespace Markout;

/// <summary>
/// Options for configuring Markout output rendering.
/// </summary>
public class MarkoutWriterOptions
{
    private bool _isReadOnly;
    private bool _includeIcons = true;
    private bool _includeDescription = true;
    private bool _boldFieldNames;
    private HashSet<int>? _includeSections;
    private HashSet<int>? _excludeSections;

    /// <summary>
    /// Gets the default options instance. This instance is read-only.
    /// </summary>
    public static MarkoutWriterOptions Default { get; } = CreateDefaultOptions();

    private static MarkoutWriterOptions CreateDefaultOptions()
    {
        var options = new MarkoutWriterOptions();
        options.MakeReadOnly();
        return options;
    }

    /// <summary>
    /// Whether to include icons in tree nodes. Default is true.
    /// </summary>
    public bool IncludeIcons
    {
        get => _includeIcons;
        set
        {
            ThrowIfReadOnly();
            _includeIcons = value;
        }
    }

    /// <summary>
    /// Whether to include the description paragraph (from DescriptionProperty). Default is true.
    /// </summary>
    public bool IncludeDescription
    {
        get => _includeDescription;
        set
        {
            ThrowIfReadOnly();
            _includeDescription = value;
        }
    }

    /// <summary>
    /// Whether to render field names in bold. Default is false.
    /// </summary>
    public bool BoldFieldNames
    {
        get => _boldFieldNames;
        set
        {
            ThrowIfReadOnly();
            _boldFieldNames = value;
        }
    }

    /// <summary>
    /// If set, only sections with these indices (1-based) are rendered.
    /// </summary>
    public HashSet<int>? IncludeSections
    {
        get => _includeSections;
        set
        {
            ThrowIfReadOnly();
            _includeSections = value;
        }
    }

    /// <summary>
    /// If set, sections with these indices (1-based) are excluded from output.
    /// </summary>
    public HashSet<int>? ExcludeSections
    {
        get => _excludeSections;
        set
        {
            ThrowIfReadOnly();
            _excludeSections = value;
        }
    }

    /// <summary>
    /// Gets whether this instance is read-only.
    /// </summary>
    public bool IsReadOnly => _isReadOnly;

    /// <summary>
    /// Marks this instance as read-only. After calling this method, any attempt to set
    /// a property will throw <see cref="InvalidOperationException"/>.
    /// </summary>
    public void MakeReadOnly() => _isReadOnly = true;

    private void ThrowIfReadOnly()
    {
        if (_isReadOnly)
            throw new InvalidOperationException("This MarkoutWriterOptions instance is read-only.");
    }
}
