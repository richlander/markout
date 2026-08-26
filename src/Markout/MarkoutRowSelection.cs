namespace Markout;

internal sealed class MarkoutRowSelection
{
    private readonly MarkoutRowWindow[] _windows;

    public MarkoutRowSelection(MarkoutRowWindow window)
        : this([window])
    {
    }

    private MarkoutRowSelection(MarkoutRowWindow[] windows)
    {
        _windows = windows;
        IsPositional = true;

        int? retentionBound = null;
        foreach (var window in windows)
        {
            if (window.IsPositional)
                continue;

            IsPositional = false;
            retentionBound = retentionBound is int bound
                ? Math.Min(bound, window.RetentionBound)
                : window.RetentionBound;
        }

        RetentionBound = retentionBound ?? 0;
    }

    public MarkoutRowWindow Primary => _windows[0];

    public bool IsPositional { get; }

    public int RetentionBound { get; }

    public MarkoutRowSelection Intersect(MarkoutRowWindow window)
    {
        var windows = new MarkoutRowWindow[_windows.Length + 1];
        _windows.CopyTo(windows, 0);
        windows[^1] = window;
        return new MarkoutRowSelection(windows);
    }

    public (int KeepStart, int KeepEnd) Resolve(int dataCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(dataCount);

        var keepStart = 0;
        var keepEnd = dataCount;
        foreach (var window in _windows)
        {
            var (windowStart, windowEnd) = window.Resolve(dataCount);
            keepStart = Math.Max(keepStart, windowStart);
            keepEnd = Math.Min(keepEnd, windowEnd);
        }

        if (keepEnd < keepStart)
            keepEnd = keepStart;

        return (keepStart, keepEnd);
    }

    public bool KeepsPosition(int position)
    {
        if (!IsPositional)
        {
            throw new InvalidOperationException(
                "A row selection containing a Tail window cannot be resolved by position.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(position);
        foreach (var window in _windows)
        {
            if (!window.KeepsPosition(position))
                return false;
        }

        return true;
    }
}
