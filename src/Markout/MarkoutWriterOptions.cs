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
    private HashSet<string>? _includeSections;
    private HashSet<string>? _excludeSections;

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
    /// If set, only sections whose heading text matches are rendered.
    /// An empty set means no named sections are included (preamble only).
    /// </summary>
    public HashSet<string>? IncludeSections
    {
        get => _includeSections;
        set
        {
            ThrowIfReadOnly();
            _includeSections = value;
        }
    }

    /// <summary>
    /// If set, sections whose heading text matches are excluded from output.
    /// </summary>
    public HashSet<string>? ExcludeSections
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
