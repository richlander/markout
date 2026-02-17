using System.Diagnostics.CodeAnalysis;

namespace MarkdownTable.Formatting;

/// <summary>
/// Static registry for formatter modes. Captures static metadata at initialization
/// so listing modes is zero-allocation. Instances are created only when selected.
/// </summary>
public static class FormatterRegistry
{
    private readonly record struct Entry(string Name, string Description, Func<IFormatterMode> Create)
    {
        public static Entry For<T>() where T : IFormatterMode, IFormatterModeInfo, new()
            => new(T.Name, T.Description, static () => new T());
    }

    private static readonly Entry[] _entries =
    [
        Entry.For<SmoothMode>(),
        Entry.For<CompactMode>(),
        Entry.For<PrettyMode>(),
        Entry.For<FullWidthMode>(),
    ];

    /// <summary>
    /// Try to create a formatter mode by name.
    /// </summary>
    public static bool TryCreate(string name, [NotNullWhen(true)] out IFormatterMode? mode)
    {
        foreach (ref readonly var entry in _entries.AsSpan())
        {
            if (string.Equals(entry.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                mode = entry.Create();
                return true;
            }
        }

        mode = null;
        return false;
    }

    /// <summary>
    /// All available mode names and descriptions for help text.
    /// </summary>
    public static IEnumerable<(string Name, string Description)> GetModes()
    {
        foreach (var entry in _entries)
            yield return (entry.Name, entry.Description);
    }
}
